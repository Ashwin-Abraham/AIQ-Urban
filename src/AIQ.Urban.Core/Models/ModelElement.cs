namespace AIQ.Urban.Core.Models;

/// <summary>Base type for all urban model elements.</summary>
public abstract class ModelElement
{
    /// <summary>Unique identifier generated at construction.</summary>
    public Guid Id { get; } = Guid.NewGuid();
}
