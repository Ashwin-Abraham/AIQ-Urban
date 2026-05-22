using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.DebugViz;

public static class UrbanModelVisualizers
{
    public static void VisualizeAll(this UrbanModel model)
    {
        var doc = new UrbanSceneBuilder()
            .AddUrbanElements(model.GetUrbanElements())
            .AddTransitSurfaces(model.TransitSurfaces)
            .Build();
        Visualizer.View(doc);
    }

    public static void VisualizeBuildings(this UrbanModel model, bool viewLocal = false)
    {
        var doc = new UrbanSceneBuilder()
            .AddUrbanElements(model.Buildings.Cast<IUrbanElement>().ToList())
            .Build();
        Visualizer.View(doc, viewLocal: viewLocal);
    }

    public static void VisualizeTransits(this UrbanModel model)
    {
        var doc = new UrbanSceneBuilder()
            .AddUrbanElements(model.Transits.Cast<IUrbanElement>().ToList())
            .Build();
        Visualizer.View(doc);
    }

    public static void VisualizeTransitSurfaces(this UrbanModel model)
    {
        var doc = new UrbanSceneBuilder()
            .AddTransitSurfaces(model.TransitSurfaces)
            .Build();
        Visualizer.View(doc);
    }

    public static void VisualizeParcels(this UrbanModel model)
    {
        var doc = new UrbanSceneBuilder()
            .AddUrbanElements(model.Parcels.Cast<IUrbanElement>().ToList())
            .Build();
        Visualizer.View(doc);
    }

    public static void VisualizeZones(this UrbanModel model)
    {
        var doc = new UrbanSceneBuilder()
            .AddUrbanElements(model.Zones.Cast<IUrbanElement>().ToList())
            .Build();
        Visualizer.View(doc);
    }
}
