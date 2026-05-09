using AIQ.Urban.Core.Models;

namespace AIQ.Urban.Core;

/// <summary>Singleton holding all <see cref="ModelElement"/>s and <see cref="ModelGroup"/>s. Single source of truth for the in-memory model.</summary>
public class UrbanModel
{
    private static readonly Lazy<UrbanModel> _instance = new(() => new UrbanModel());

    /// <summary>Access the singleton instance (lazy-initialized).</summary>
    public static UrbanModel Instance => _instance.Value;

    /// <summary>All transit corridors in the model.</summary>
    public List<Transit> Transits { get; } = [];

    private UrbanModel() { }

    /// <summary>Add a transit corridor to the model and trigger dependent recalculations.</summary>
    public void AddTransit(Transit transit)
    {
        Transits.Add(transit);
    }
}
