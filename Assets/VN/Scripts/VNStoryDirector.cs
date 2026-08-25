using System.Collections.Generic;
using UnityEngine;

namespace VN
{
    public class VNStoryDirector : MonoBehaviour
    {
        public static VNStoryDirector Instance { get; private set; }

        [Header("Graph Asset")]
        [SerializeField] private VNStoryGraphData _currentGraph;

        [Header("Component Controllers")]
        [SerializeField] private SceneController _activeSceneController;
        [SerializeField] private DialogueBoxController _dialogueBoxController;
        [SerializeField] private ChoiceController _choiceController;

        private HashSet<string> _activeFlags = new HashSet<string>();
        private VNStoryNodeData _currentNode;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
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
                    if (_activeSceneController != null)
                    {
                        _activeSceneController.SetCharacterSprite(node.position, node.characterSprite);
                    }
                    if (!string.IsNullOrEmpty(node.animationFlag)) SetFlag(node.animationFlag);
                    if (!string.IsNullOrEmpty(node.spriteFlag)) SetFlag(node.spriteFlag);
                    ContinueNextNode(0);
                    break;

                case VNStoryNodeType.ClearCharacter:
                    if (_activeSceneController != null)
                    {
                        _activeSceneController.ClearCharacterSlot(node.position);
                    }
                    ContinueNextNode(0);
                    break;

                case VNStoryNodeType.SetBackground:
                    if (_activeSceneController != null)
                    {
                        _activeSceneController.SetBackground(node.backgroundSprite);
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
                    if (_choiceController != null) _choiceController.Hide();
                    if (_dialogueBoxController != null)
                    {
                        _dialogueBoxController.ShowDialogue(node.speakerName, node.dialogueText, () =>
                        {
                            ContinueNextNode(0);
                        });
                    }
                    break;

                case VNStoryNodeType.PlayChoices:
                    if (_dialogueBoxController != null) _dialogueBoxController.Hide();
                    if (_choiceController != null)
                    {
                        _choiceController.ShowChoices(node.choices, (choiceIndex, targetNodeId) =>
                        {
                            if (!string.IsNullOrEmpty(targetNodeId))
                            {
                                ExecuteNode(_currentGraph.GetNode(targetNodeId));
                            }
                            else
                            {
                                ContinueNextNode(choiceIndex);
                            }
                        });
                    }
                    break;

                default:
                    ContinueNextNode(0);
                    break;
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
