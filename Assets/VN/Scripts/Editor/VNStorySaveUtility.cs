#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace VN.Editor
{
    public static class VNStorySaveUtility
    {
        public static VNStoryNodeView CreateNodeView(VNStoryNodeType type)
        {
            switch (type)
            {
                case VNStoryNodeType.Start: return new VNStartNodeView();
                case VNStoryNodeType.PhaseTrigger: return new VNTriggerNodeView("Phase Trigger", type);
                case VNStoryNodeType.FlagTrigger: return new VNTriggerNodeView("Flag Trigger", type);
                case VNStoryNodeType.EventTrigger: return new VNTriggerNodeView("Event Trigger", type);

                case VNStoryNodeType.CheckFlag: return new VNCheckFlagNodeView();

                case VNStoryNodeType.SetCharacterSprite: return new VNSetCharacterSpriteNodeView();
                case VNStoryNodeType.SetCharacterPosition: return new VNSetCharacterSpriteNodeView();
                case VNStoryNodeType.ClearCharacter: return new VNClearCharacterNodeView();
                case VNStoryNodeType.SetBackground: return new VNSetBackgroundNodeView();

                case VNStoryNodeType.PlayBGM: return new VNPlayBGMNodeView();
                case VNStoryNodeType.PlaySFX: return new VNPlaySFXNodeView();

                case VNStoryNodeType.SetFlag: return new VNSetFlagNodeView();
                case VNStoryNodeType.FireEvent: return new VNFireEventNodeView();

                case VNStoryNodeType.PlayDialogue: return new VNPlayDialogueNodeView();
                case VNStoryNodeType.PlayChoices: return new VNPlayChoicesNodeView();

                default:
                    Debug.LogWarning($"[VNStorySaveUtility] Unknown type: {type}");
                    return null;
            }
        }

        public static VNStoryGraphData BuildGraphData(VNStoryGraphView graphView)
        {
            var data = ScriptableObject.CreateInstance<VNStoryGraphData>();
            data.nodes = new List<VNStoryNodeData>();
            data.links = new List<VNStoryLinkData>();

            foreach (var element in graphView.nodes)
            {
                if (element is VNStoryNodeView view)
                {
                    data.nodes.Add(view.ToData());
                }
            }

            foreach (var element in graphView.edges)
            {
                if (element is Edge edge && edge.output?.node is VNStoryNodeView outputNode && edge.input?.node is VNStoryNodeView inputNode)
                {
                    int portIdx = outputNode.outputContainer.IndexOf(edge.output);
                    data.links.Add(new VNStoryLinkData
                    {
                        sourceNodeId = outputNode.NodeID,
                        portIndex = portIdx,
                        targetNodeId = inputNode.NodeID
                    });
                }
            }

            return data;
        }

        public static void ExportGraphSO(VNStoryGraphView graphView, string exportFolder)
        {
            var data = BuildGraphData(graphView);
            string defaultDir = string.IsNullOrEmpty(exportFolder) ? "Assets" : exportFolder;
            string path = EditorUtility.SaveFilePanelInProject("Export VN Story Graph Asset", "VNStoryGraph", "asset", "Save VN Story Graph", defaultDir);

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(data, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[VNStorySaveUtility] Exported to '{path}'");
            }
        }

        public static void LoadFromSO(VNStoryGraphView graphView, VNStoryGraphData asset)
        {
            if (asset == null) return;
            graphView.DeleteElements(graphView.graphElements.ToList());

            Dictionary<string, VNStoryNodeView> nodeMap = new Dictionary<string, VNStoryNodeView>();

            foreach (var nodeData in asset.nodes)
            {
                var view = CreateNodeView(nodeData.type);
                if (view != null)
                {
                    view.LoadFromData(nodeData);
                    view.SetPosition(new Rect(nodeData.posX, nodeData.posY, 0, 0));
                    graphView.AddElement(view);
                    nodeMap[nodeData.id] = view;
                }
            }

            foreach (var link in asset.links)
            {
                if (nodeMap.TryGetValue(link.sourceNodeId, out var sourceNode) &&
                    nodeMap.TryGetValue(link.targetNodeId, out var targetNode))
                {
                    if (link.portIndex >= 0 && link.portIndex < sourceNode.outputContainer.childCount)
                    {
                        Port outPort = sourceNode.outputContainer[link.portIndex] as Port;
                        Port inPort = targetNode.inputContainer[0] as Port;

                        if (outPort != null && inPort != null)
                        {
                            Edge edge = outPort.ConnectTo(inPort);
                            graphView.AddElement(edge);
                        }
                    }
                }
            }
        }
    }
}
#endif
