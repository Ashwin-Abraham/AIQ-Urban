namespace AIQ.Urban.Core.Models;

/// <summary>Graph node for network topology. Future: replace with topologyNet.</summary>
public class Node
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; } = Guid.NewGuid();
}
