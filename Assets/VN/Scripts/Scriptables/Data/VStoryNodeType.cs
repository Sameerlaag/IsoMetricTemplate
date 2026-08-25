
public enum VStoryNodeType
{
    // ── Triggers (entry points) ──────────────────────────────
    PhaseTrigger,
    FlagTrigger,
    EventTrigger,

    // ── Conditions (branching) ───────────────────────────────
    CheckFlag,
    CheckDay,
    CheckPhase,

    // ── Actions (execution) ──────────────────────────────────
    SetNPCBehavior,
    AssignDialogue,
    SetFlag,
    FireEvent,
    ForceDialogue,

}
