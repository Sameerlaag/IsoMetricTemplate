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
            Debug.Log("[VNStoryDirector] Awake — Initialized Instance.");
        }

        private void Start()
        {
            if (_currentGraph != null)
            {
                Debug.Log($"[VNStoryDirector] Start — Auto-playing assigned graph: '{_currentGraph.graphName ?? _currentGraph.name}'");
                PlayGraph(_currentGraph);
            }
            else
            {
                Debug.LogWarning("[VNStoryDirector] Start — No VNStoryGraphData assigned to _currentGraph in Inspector!");
            }
        }

        public void PlayGraph(VNStoryGraphData graphData)
        {
            _currentGraph = graphData;
            if (_currentGraph == null)
            {
                Debug.LogError("[VNStoryDirector] PlayGraph failed: graphData is NULL.");
                return;
            }

            if (_currentGraph.nodes == null || _currentGraph.nodes.Count == 0)
            {
                Debug.LogError($"[VNStoryDirector] PlayGraph failed: Graph '{_currentGraph.name}' has 0 nodes.");
                return;
            }

            Debug.Log($"[VNStoryDirector] Playing Graph '{_currentGraph.graphName ?? _currentGraph.name}' with {_currentGraph.nodes.Count} nodes.");

            VNStoryNodeData startNode = _currentGraph.nodes.Find(n => n.type == VNStoryNodeType.Start);
            if (startNode == null)
            {
                Debug.LogWarning("[VNStoryDirector] No Start node found in graph. Starting at node index 0.");
                startNode = _currentGraph.nodes[0];
            }

            ExecuteNode(startNode);
        }

        public void SetFlag(string flagKey)
        {
            if (string.IsNullOrEmpty(flagKey)) return;
            Debug.Log($"[VNStoryDirector] SetFlag: '{flagKey}'");
            _activeFlags.Add(flagKey);
            EvaluateFlagTriggers(flagKey);
        }

        public void RemoveFlag(string flagKey)
        {
            if (string.IsNullOrEmpty(flagKey)) return;
            Debug.Log($"[VNStoryDirector] RemoveFlag: '{flagKey}'");
            _activeFlags.Remove(flagKey);
        }

        public bool HasFlag(string flagKey)
        {
            bool has = !string.IsNullOrEmpty(flagKey) && _activeFlags.Contains(flagKey);
            Debug.Log($"[VNStoryDirector] HasFlag check '{flagKey}': {has}");
            return has;
        }

        private void EvaluateFlagTriggers(string flagKey)
        {
            if (_currentGraph == null) return;
            foreach (var node in _currentGraph.nodes)
            {
                if (node.type == VNStoryNodeType.FlagTrigger && node.triggerKey == flagKey)
                {
                    Debug.Log($"[VNStoryDirector] FlagTrigger matched flag '{flagKey}', executing node '{node.id}'");
                    ExecuteNode(node);
                    break;
                }
            }
        }

        public void FireEvent(string eventKey)
        {
            if (_currentGraph == null || string.IsNullOrEmpty(eventKey)) return;
            Debug.Log($"[VNStoryDirector] FireEvent: '{eventKey}'");
            foreach (var node in _currentGraph.nodes)
            {
                if (node.type == VNStoryNodeType.EventTrigger && node.triggerKey == eventKey)
                {
                    Debug.Log($"[VNStoryDirector] EventTrigger matched event '{eventKey}', executing node '{node.id}'");
                    ExecuteNode(node);
                    break;
                }
            }
        }

        private void ExecuteNode(VNStoryNodeData node)
        {
            if (node == null)
            {
                Debug.LogWarning("[VNStoryDirector] ExecuteNode called with NULL node.");
                return;
            }

            _currentNode = node;
            Debug.Log($"[VNStoryDirector] ExecuteNode -> Type: {node.type}, ID: {node.id}");

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
                    Debug.Log($"[VNStoryDirector] CheckFlag '{node.flagKey}' -> result: {condition}");
                    ContinueNextNode(condition ? 0 : 1);
                    break;

                case VNStoryNodeType.SetCharacterSprite:
                case VNStoryNodeType.SetCharacterPosition:
                    if (_activeSceneController != null)
                    {
                        Debug.Log($"[VNStoryDirector] SetCharacterSprite pos: {node.position}, sprite: {node.characterSprite?.name ?? "null"}, animFlag: {node.animationFlag}");
                        _activeSceneController.SetCharacterSprite(node.position, node.characterSprite, node.animationFlag);
                    }
                    else
                    {
                        Debug.LogWarning("[VNStoryDirector] _activeSceneController is unassigned in Inspector!");
                    }
                    if (!string.IsNullOrEmpty(node.spriteFlag)) SetFlag(node.spriteFlag);
                    ContinueNextNode(0);
                    break;

                case VNStoryNodeType.ClearCharacter:
                    if (_activeSceneController != null)
                    {
                        Debug.Log($"[VNStoryDirector] ClearCharacter pos: {node.position}, animFlag: {node.animationFlag}");
                        _activeSceneController.ClearCharacterSlot(node.position, node.animationFlag);
                    }
                    else
                    {
                        Debug.LogWarning("[VNStoryDirector] _activeSceneController is unassigned in Inspector!");
                    }
                    ContinueNextNode(0);
                    break;

                case VNStoryNodeType.SetBackground:
                    if (_activeSceneController != null)
                    {
                        Debug.Log($"[VNStoryDirector] SetBackground sprite: {node.backgroundSprite?.name ?? "null"}, animFlag: {node.backgroundAnimFlag}");
                        _activeSceneController.SetBackground(node.backgroundSprite, node.backgroundAnimFlag);
                    }
                    else
                    {
                        Debug.LogWarning("[VNStoryDirector] _activeSceneController is unassigned in Inspector!");
                    }
                    ContinueNextNode(0);
                    break;

                case VNStoryNodeType.PlayBGM:
                    if (VNAudioManager.Instance != null)
                    {
                        VNAudioManager.Instance.PlayBGM(node.audioClip);
                    }
                    else
                    {
                        Debug.LogWarning("[VNStoryDirector] VNAudioManager.Instance is null!");
                    }
                    ContinueNextNode(0);
                    break;

                case VNStoryNodeType.PlaySFX:
                    if (VNAudioManager.Instance != null)
                    {
                        VNAudioManager.Instance.PlaySFX(node.audioClip);
                    }
                    else
                    {
                        Debug.LogWarning("[VNStoryDirector] VNAudioManager.Instance is null!");
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
                        Debug.Log($"[VNStoryDirector] PlayDialogue speaker: '{node.speakerName}', text: '{node.dialogueText}'");
                        _dialogueBoxController.ShowDialogue(node.speakerName, node.dialogueText, node.dialogueAudio, () =>
                        {
                            Debug.Log("[VNStoryDirector] PlayDialogue finished/advancing.");
                            ContinueNextNode(0);
                        });
                    }
                    else
                    {
                        Debug.LogWarning("[VNStoryDirector] _dialogueBoxController is unassigned in Inspector!");
                    }
                    break;

                case VNStoryNodeType.PlayChoices:
                    if (_dialogueBoxController != null) _dialogueBoxController.Hide();
                    if (_choiceController != null)
                    {
                        Debug.Log($"[VNStoryDirector] PlayChoices with {node.choices.Count} choices.");
                        _choiceController.ShowChoices(node.choices, (choiceIndex, targetNodeId) =>
                        {
                            Debug.Log($"[VNStoryDirector] Choice selected index {choiceIndex}, targetNodeId: '{targetNodeId}'");
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
                    else
                    {
                        Debug.LogWarning("[VNStoryDirector] _choiceController is unassigned in Inspector!");
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
                else
                {
                    Debug.LogWarning($"[VNStoryDirector] Target node '{link.targetNodeId}' not found in graph!");
                }
            }
            else
            {
                Debug.Log($"[VNStoryDirector] End of branch / No link found out of node '{_currentNode.id}' on portIndex {portIndex}.");
            }
        }
    }
}
