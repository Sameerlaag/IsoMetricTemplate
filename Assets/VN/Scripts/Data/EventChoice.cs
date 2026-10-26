using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EventChoice
{
    public string text;

    [Header("Availability")]
    public List<Requirement> requirements;

    [Header("Result")]
    public List<GameEffect> effects;

    [Header("Next")]
    public string nextNodeId;

    // public bool IsAvailable(GameState state)
    // {
    //     foreach (var requirement in requirements)
    //     {
    //         if (!requirement.IsMet(state))
    //             return false;
    //     }
    //
    //     return true;
    // }
}