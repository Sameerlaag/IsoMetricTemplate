using System.Collections.Generic;
using UnityEngine;


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

        // Audio
        PlayBGM,
        PlaySFX,

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
        public string animationFlag; // e.g. "slow_fade_in", "slow_fade_out", "fast_fade_in"
        public string spriteFlag;

        public Sprite backgroundSprite;
        public string backgroundAnimFlag; // e.g. "slow_fade_in", "slow_fade_out"

        public string setFlagKey;
        public bool removeFlag;
        public string eventKey;

        // Audio
        public AudioClip audioClip;

        // Dialogue
        public string speakerName;
        [TextArea(3, 5)]
        public string dialogueText;
        public AudioClip dialogueAudio;

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

