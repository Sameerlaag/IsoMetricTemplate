[System.Serializable]
public class Requirement
{
    public StatType stat;
    public Comparison comparison;
    public int value;
    //
    // public bool IsMet(GameState state)
    // {
    //     int current = state.GetStat(stat);
    //
    //     switch (comparison)
    //     {
    //         case Comparison.GreaterOrEqual:
    //             return current >= value;
    //
    //         case Comparison.LessOrEqual:
    //             return current <= value;
    //
    //         case Comparison.Equal:
    //             return current == value;
    //     }
    //
    //     return false;
    // }
}