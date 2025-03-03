using ArchSitePlan.Core.Pipeline.CreationData;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Models;

public class RectangleBuilding //: Building
{
    public static RectangleBuilding Generate( AutoRectangleBuildingData config)
    {
        var site = config.SiteBoundary;
        Polyline primaryAxis = Building.GeneratePrimarySiteAxis(config.SiteBoundary);

        return new();
    }

}
