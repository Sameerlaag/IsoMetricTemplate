using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VN
{
    public class VNStoryDirector : MonoBehaviour
    {
        public static VNStoryDirector Instance { get; private set; }

        [Header("Graph & Scene Assets")]
        [SerializeField] private VNStoryGraphData _currentGraph;

        [Header("Character Position Slots (Left, Middle, Right)")]
        [SerializeField] private Image _leftCharacterSlot;
        [SerializeField] private Image _middleCharacterSlot;
        [SerializeField] private Image _rightCharacterSlot;

        [Header("Background")]
        [SerializeField] private Image _backgroundImage;

        [Header("Dialogue Box UI")]
        [SerializeField] private GameObject _dialogueBoxRoot;
        [SerializeField] private TextMeshProUGUI _speakerText;
        [SerializeField] private TextMeshProUGUI _dialogueText;
        [SerializeField] private Button _nextButton;

        [Header("Choices UI")]
        [SerializeField] private GameObject _choicesRoot;
        [SerializeField] private Transform _choicesContainer;
        [SerializeField] private GameObject _choiceButtonPrefab;

        private Dictionary<VNCharacterPosition, Image> _characterSlots;
        private HashSet<string> _activeFlags = new HashSet<string>();
        private VNStoryNodeData _currentNode;
        private bool _isWaitingForInput = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _characterSlots = new Dictionary<VNCharacterPosition, Image>
            {
                { VNCharacterPosition.Left, _leftCharacterSlot },
                { VNCharacterPosition.Middle, _middleCharacterSlot },
                { VNCharacterPosition.Right, _rightCharacterSlot }
            };

            if (_nextButton != null)
            {
                _nextButton.onClick.AddListener(OnNextButtonClicked);
            }
        }

        public void PlayGraph(VNStoryGraphData graphData)
        {
            _currentGraph = graphData;
            if (_currentGraph == null || _currentGraph.nodes.Count == 0) return;

            VNStoryNodeData startNode = _currentGraph.nodes.Find(n => n.type == VNStoryNodeType.Start);
            if (startNode == null) startNode = _currentGraph.nodes[0];

            ExecuteNode(startNode);
        }

        public void SetFlag(string flagKey)
        {
            if (string.IsNullOrEmpty(flagKey)) return;
            _activeFlags.Add(flagKey);
            EvaluateFlagTriggers(flagKey);
        }

        public void RemoveFlag(string flagKey)
        {
            if (string.IsNullOrEmpty(flagKey)) return;
            _activeFlags.Remove(flagKey);
        }

        public bool HasFlag(string flagKey)
        {
            return !string.IsNullOrEmpty(flagKey) && _activeFlags.Contains(flagKey);
        }

        private void EvaluateFlagTriggers(string flagKey)
        {
            if (_currentGraph == null) return;
            foreach (var node in _currentGraph.nodes)
            {
                if (node.type == VNStoryNodeType.FlagTrigger && node.triggerKey == flagKey)
                {
                    ExecuteNode(node);
                    break;
                }
            }
        }

        public void FireEvent(string eventKey)
        {
            if (_currentGraph == null || string.IsNullOrEmpty(eventKey)) return;
            foreach (var node in _currentGraph.nodes)
            {
                if (node.type == VNStoryNodeType.EventTrigger && node.triggerKey == eventKey)
                {
                    ExecuteNode(node);
                    break;
                }
            }
        }

        private void ExecuteNode(VNStoryNodeData node)
        {
            if (node == null) return;
            _currentNode = node;

            switch (node.type)
            {
                case VNStoryNodeType.Start:
                case VNStoryNodeType.PhaseTrigger:
                case VNStoryNodeType.FlagTrigger:
                case VNStoryNodeType.EventTrigger:
                    ContinueNextNode(0);
                    break;

                case VNStoryNodeType.CheckFlag:
                    bool has = HasFlag(node.flagKey);
                    bool condition = node.invertFlag ? !has : has;
                    ContinueNextNode(condition ? 0 : 1);
                    break;

                case VNStoryNodeType.SetCharacterSprite:
                case VNStoryNodeType.SetCharacterPosition:
                    ApplyCharacterSlot(node.position, node.characterSprite, true, node.animationFlag, node.spriteFlag);
                    ContinueNextNode(0);
                    break;

                case VNStoryNodeType.ClearCharacter:
                    ApplyCharacterSlot(node.position, null, false, null, null);
                    ContinueNextNode(0);
                    break;

                case VNStoryNodeType.SetBackground:
                    if (_backgroundImage != null && node.backgroundSprite != null)
                    {
                        _backgroundImage.sprite = node.backgroundSprite;
                        _backgroundImage.enabled = true;
                    }
                    ContinueNextNode(0);
                    break;

                case VNStoryNodeType.SetFlag:
                    if (node.removeFlag) RemoveFlag(node.setFlagKey);
                    else SetFlag(node.setFlagKey);
                    ContinueNextNode(0);
                    break;

                case VNStoryNodeType.FireEvent:
                    FireEvent(node.eventKey);
                    ContinueNextNode(0);
                    break;

                case VNStoryNodeType.PlayDialogue:
                    ShowDialogue(node.speakerName, node.dialogueText);
                    break;

                case VNStoryNodeType.PlayChoices:
                    ShowChoices(node);
                    break;

                default:
                    ContinueNextNode(0);
                    break;
            }
        }

        private void ApplyCharacterSlot(VNCharacterPosition pos, Sprite sprite, bool active, string animFlag, string spriteFlag)
        {
            if (_characterSlots.TryGetValue(pos, out Image slotImage) && slotImage != null)
            {
                slotImage.sprite = sprite;
                slotImage.enabled = active && (sprite != null);

                if (!string.IsNullOrEmpty(animFlag)) SetFlag(animFlag);
                if (!string.IsNullOrEmpty(spriteFlag)) SetFlag(spriteFlag);
            }
        }

        private void ShowDialogue(string speaker, string text)
        {
            if (_dialogueBoxRoot != null) _dialogueBoxRoot.SetActive(true);
            if (_choicesRoot != null) _choicesRoot.SetActive(false);

            if (_speakerText != null) _speakerText.text = speaker;
            if (_dialogueText != null) _dialogueText.text = text;

            _isWaitingForInput = true;
        }

        private void OnNextButtonClicked()
        {
            if (!_isWaitingForInput) return;
            _isWaitingForInput = false;
            ContinueNextNode(0);
        }

        private void ShowChoices(VNStoryNodeData node)
        {
            if (_dialogueBoxRoot != null) _dialogueBoxRoot.SetActive(false);
            if (_choicesRoot != null) _choicesRoot.SetActive(true);

            foreach (Transform child in _choicesContainer)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < node.choices.Count; i++)
            {
                var choice = node.choices[i];
                int choiceIndex = i;

                GameObject btnObj = Instantiate(_choiceButtonPrefab, _choicesContainer);
                var btn = btnObj.GetComponent<Button>();
                var txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = choice.choiceText;

                btn.onClick.AddListener(() =>
                {
                    if (_choicesRoot != null) _choicesRoot.SetActive(false);
                    if (!string.IsNullOrEmpty(choice.targetNodeId))
                    {
                        ExecuteNode(_currentGraph.GetNode(choice.targetNodeId));
                    }
                    else
                    {
                        ContinueNextNode(choiceIndex);
                    }
                });
            }
        }

        private void ContinueNextNode(int portIndex)
        {
            if (_currentGraph == null || _currentNode == null) return;
            var links = _currentGraph.GetLinksFrom(_currentNode.id);
            var link = links.Find(l => l.portIndex == portIndex);

            if (link != null)
            {
                var target = _currentGraph.GetNode(link.targetNodeId);
                if (target != null)
                {
                    ExecuteNode(target);
                }
            }
        }
    }
}
