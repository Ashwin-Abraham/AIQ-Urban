using System.Linq;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.Core.Tests.ModelActions.Tools;

public static class UrbanModelVisualizers
{
    public static void VisualizeAll(this UrbanModel model)
    {
        new Visualiser()
            .AddUrbanElements(model.GetUrbanElements())
            .AddTransitSurfaces(model.TransitSurfaces)
            .View();
    }

    public static void VisualizeBuildings(this UrbanModel model)
    {
        new Visualiser()
            .AddUrbanElements(model.Buildings.Cast<IUrbanElement>().ToList())
            .View();
    }

    public static void VisualizeTransits(this UrbanModel model)
    {
        new Visualiser()
            .AddUrbanElements(model.Transits.Cast<IUrbanElement>().ToList())
            .View();
    }

    public static void VisualizeTransitSurfaces(this UrbanModel model)
    {
        new Visualiser()
            .AddTransitSurfaces(model.TransitSurfaces)
            .View();
    }

    public static void VisualizeParcels(this UrbanModel model)
    {
        new Visualiser()
            .AddUrbanElements(model.Parcels.Cast<IUrbanElement>().ToList())
            .View();
    }

    public static void VisualizeZones(this UrbanModel model)
    {
        new Visualiser()
            .AddUrbanElements(model.Zones.Cast<IUrbanElement>().ToList())
            .View();
    }
}
