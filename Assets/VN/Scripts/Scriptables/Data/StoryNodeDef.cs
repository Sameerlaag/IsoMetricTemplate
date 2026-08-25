//
// using UnityEngine;
//
// public class StoryNodeDef : ScriptableObject
// {
//     public string NodeName;
//    
//     public string id;
//     public VStoryNodeType type;
//     public float posX;
//     public float posY;
//
//     // ── Trigger fields ───────────────────────────────────────
//     /// <summary>Day to match. -1 = any day.</summary>
//     public int day = -1;
//     /// <summary>Phase to match.</summary>
//     public GamePhase phase;
//     /// <summary>If true, matches any phase (PhaseTrigger only).</summary>
//     public bool anyPhase;
//     /// <summary>Trigger zone ID or event key (SpatialTrigger, EventTrigger, FlagTrigger).</summary>
//     public string triggerKey;
//
//     // ── Condition fields ─────────────────────────────────────
//     /// <summary>Flag key to check (CheckFlag).</summary>
//     public string flagKey;
//     /// <summary>If true, condition is inverted (NOT has flag).</summary>
//     public bool invert;
//     /// <summary>Day to check (CheckDay).</summary>
//     public int checkDay;
//     /// <summary>Phase to check (CheckPhase).</summary>
//     public GamePhase checkPhase;
//
//
//
//     // ── Action fields ────────────────────────────────────────
//     /// <summary>NPC actor key (SetNPCBehavior, AssignDialogue, ForceDialogue).</summary>
//     public string npcKey;
//     /// <summary>Whether the NPC is active/visible (SetNPCBehavior).</summary>
//     public bool isActive = true;
//     /// <summary>Waypoint key for NPC positioning (SetNPCBehavior) L - M - R.</summary>
//     public string positionKey;
//     /// <summary>Animator state name (SetNPCBehavior) sprite.</summary>
//     public string npcState;
//
//     /// <summary>GUID of the DialogueTree asset to assign (AssignDialogue).</summary>
//     public string dialogueTreeGuid;
//     /// <summary>Direct reference to the DialogueTree asset (resolved at editor export/save and used at runtime).</summary>
//     public DialogueTree dialogueTree;
//     /// <summary>Activate Curtain.</summary>
//     public bool requiresCurtain;
//     /// <summary>Flag key to set or remove (SetFlag).</summary>
//     public string setFlagKey;
//     /// <summary>If true, removes the flag instead of setting it (SetFlag).</summary>
//     public bool removeFlag;
//
//     /// <summary>Event key to fire (FireEvent).</summary>
//     public string eventKey;
//     
// }
