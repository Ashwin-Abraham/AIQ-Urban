using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.Core;

/// <summary>Singleton holding all <see cref="UrbanElement"/>s and <see cref="ModelGroup"/>s. Single source of truth for the in-memory model.</summary>
public class UrbanModel
{
    private static readonly Lazy<UrbanModel> _instance = new(() => new UrbanModel());

    /// <summary>Access the singleton instance (lazy-initialized).</summary>
    public static UrbanModel Instance => _instance.Value;

    /// <summary>All transit corridors in the model.</summary>
    public List<Transit> Transits { get; } = [];

    /// <summary>All buildings in the model.</summary>
    public List<Building> Buildings { get; } = [];

    public List<(SurfaceElement, TransitType)> TransitSurfaces { get; } = [];

    private UrbanModel() { }

    public List<IUrbanElement> GetUrbanElements()
    {
        var list = new List<IUrbanElement>();

        list.AddRange(Transits);
        list.AddRange(Buildings);

        return list;
    }


    /// <summary>Add a transit corridor to the model and trigger dependent recalculations.</summary>
    public void AddTransit(IEnumerable<Transit> transits)
    {
        Transits.AddRange(transits);
    }

    /// <summary>Add a building to the model.</summary>
    public void AddBuilding(Building building)
    {
        Buildings.Add(building);
    }

}
