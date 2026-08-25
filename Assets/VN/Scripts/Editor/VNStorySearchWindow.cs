#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace VN.Editor
{
    public class VNStorySearchWindow : ScriptableObject, ISearchWindowProvider
    {
        public VNStoryGraphView graphView;
        public EditorWindow window; // Store reference to parent EditorWindow

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create VN Node"), 0),

                new SearchTreeGroupEntry(new GUIContent("Triggers / Flow"), 1),
                new SearchTreeEntry(new GUIContent("Start")) { level = 2, userData = VNStoryNodeType.Start },
                new SearchTreeEntry(new GUIContent("Wait")) { level = 2, userData = VNStoryNodeType.Wait },
                new SearchTreeEntry(new GUIContent("Phase Trigger")) { level = 2, userData = VNStoryNodeType.PhaseTrigger },
                new SearchTreeEntry(new GUIContent("Flag Trigger")) { level = 2, userData = VNStoryNodeType.FlagTrigger },
                new SearchTreeEntry(new GUIContent("Event Trigger")) { level = 2, userData = VNStoryNodeType.EventTrigger },

                new SearchTreeGroupEntry(new GUIContent("Conditions"), 1),
                new SearchTreeEntry(new GUIContent("Check Flag")) { level = 2, userData = VNStoryNodeType.CheckFlag },

                new SearchTreeGroupEntry(new GUIContent("Actions"), 1),
                new SearchTreeEntry(new GUIContent("Set Character Sprite")) { level = 2, userData = VNStoryNodeType.SetCharacterSprite },
                new SearchTreeEntry(new GUIContent("Clear Character")) { level = 2, userData = VNStoryNodeType.ClearCharacter },
                new SearchTreeEntry(new GUIContent("Set Background")) { level = 2, userData = VNStoryNodeType.SetBackground },
                new SearchTreeEntry(new GUIContent("Set / Remove Flag")) { level = 2, userData = VNStoryNodeType.SetFlag },
                new SearchTreeEntry(new GUIContent("Fire Event")) { level = 2, userData = VNStoryNodeType.FireEvent },

                new SearchTreeGroupEntry(new GUIContent("Audio"), 1),
                new SearchTreeEntry(new GUIContent("Play BGM")) { level = 2, userData = VNStoryNodeType.PlayBGM },
                new SearchTreeEntry(new GUIContent("Play SFX")) { level = 2, userData = VNStoryNodeType.PlaySFX },

                new SearchTreeGroupEntry(new GUIContent("Dialogue & Choices"), 1),
                new SearchTreeEntry(new GUIContent("Play Dialogue")) { level = 2, userData = VNStoryNodeType.PlayDialogue },
                new SearchTreeEntry(new GUIContent("Play Choices")) { level = 2, userData = VNStoryNodeType.PlayChoices }
            };

            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            if (searchTreeEntry.userData is VNStoryNodeType type)
            {
                var nodeView = VNStorySaveUtility.CreateNodeView(type);
                if (nodeView != null)
                {
                    // Calculate mouse position using the parent window or local screen offset
                    Vector2 windowMousePosition = window != null 
                        ? context.screenMousePosition - window.position.position 
                        : context.screenMousePosition;

                    var worldPosition = graphView.ChangeCoordinatesTo(
                        graphView.contentViewContainer,
                        windowMousePosition
                    );
                    nodeView.SetPosition(new Rect(worldPosition, Vector2.zero));
                    graphView.AddElement(nodeView);
                    return true;
                }
            }
            return false;
        }
    }
}
#endif