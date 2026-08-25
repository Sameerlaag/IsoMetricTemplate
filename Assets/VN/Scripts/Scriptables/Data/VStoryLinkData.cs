using System;

/// <summary>
/// Serializable edge connecting two Story Graph nodes.
/// portIndex maps to the output port on the source node:
///   - For condition nodes: 0 = TRUE, 1 = FALSE
///   - For all others: 0 = default output
/// </summary>
[Serializable]
public class VStoryLinkData
{
    public string sourceNodeId;
    public int portIndex;
    public string targetNodeId;
}
