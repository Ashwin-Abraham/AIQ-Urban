namespace AIQ.Urban.Core.Models.UrbanElements;

/// <summary>Base type for all urban model elements.</summary>
public abstract class UrbanElement
{
    /// <summary>Unique identifier generated at construction.</summary>
    public Guid Id { get; } = Guid.NewGuid();
}
