using ArchSitePlan.BHoM.Geometry;
using ArchSitePlan.Core.Models;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Test.Core.Models;

public class BuildingTests
{
    [Fact]
    public void CanGenerateSiteAxis()
    {
        var siteBoundary = new Rectangle(
            Point.Origin,
            Create.Point(5, 2));

        var axis = Building.GeneratePrimarySiteAxis(
            Create.PolyCurve([siteBoundary]));

#if DEBUG
        axis.Visualize();
#endif
    }
}
