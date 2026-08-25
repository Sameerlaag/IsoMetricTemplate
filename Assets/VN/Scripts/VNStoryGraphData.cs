using System.Collections.Generic;
using UnityEngine;

namespace VN
{
    public enum VNCharacterPosition
    {
        Left,
        Middle,
        Right
    }

    public enum VNStoryNodeType
    {
        // Flow / Triggers
        Start,
        PhaseTrigger,
        FlagTrigger,
        EventTrigger,

        // Conditions
        CheckFlag,

        // Actions
        SetCharacterSprite,
        SetCharacterPosition,
        ClearCharacter,
        SetBackground,
        SetFlag,
        FireEvent,

        // Dialogue & Choices
        PlayDialogue,
        PlayChoices
    }

    [System.Serializable]
    public class VNChoiceOption
    {
        public string choiceText;
        public string targetNodeId;
    }

    [System.Serializable]
    public class VNStoryNodeData
    {
        public string id;
        public VNStoryNodeType type;
        public float posX;
        public float posY;

        // Triggers / Flags / Keys
        public string triggerKey;
        public string flagKey;
        public bool invertFlag;

        // Actions
        public string characterId;
        public VNCharacterPosition position;
        public Sprite characterSprite;
        public string animationFlag;
        public string spriteFlag;

        public Sprite backgroundSprite;

        public string setFlagKey;
        public bool removeFlag;
        public string eventKey;

        // Dialogue
        public string speakerName;
        [TextArea(3, 5)]
        public string dialogueText;

        // Choices
        public List<VNChoiceOption> choices = new List<VNChoiceOption>();
    }

    [System.Serializable]
    public class VNStoryLinkData
    {
        public string sourceNodeId;
        public int portIndex;
        public string targetNodeId;
    }

    [CreateAssetMenu(fileName = "VNStoryGraph", menuName = "UI-VN/VN Story Graph Data")]
    public class VNStoryGraphData : ScriptableObject
    {
        public string graphName;
        public List<VNStoryNodeData> nodes = new List<VNStoryNodeData>();
        public List<VNStoryLinkData> links = new List<VNStoryLinkData>();

        public VNStoryNodeData GetNode(string id)
        {
            return nodes.Find(n => n.id == id);
        }

        public List<VNStoryLinkData> GetLinksFrom(string nodeId)
        {
            List<VNStoryLinkData> result = new List<VNStoryLinkData>();
            foreach (var link in links)
            {
                if (link.sourceNodeId == nodeId)
                    result.Add(link);
            }
            result.Sort((a, b) => a.portIndex.CompareTo(b.portIndex));
            return result;
        }
    }
}
