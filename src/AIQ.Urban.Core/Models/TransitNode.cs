namespace AIQ.Urban.Core.Models;

/// <summary>Junction or intersection in a transit network.</summary>
public class TransitNode : Node
{
    /// <summary>Connected neighbour nodes in the transit graph.</summary>
    public List<Node> Neighbours { get; } = [];
}
