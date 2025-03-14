using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchSitePlan.BHoM.Geometry.Models;
using BH.Engine.Geometry;

namespace ArchSitePlan.Core.Factories.Models;

public static class SampleBuildingDefinition
{
    public static BuildingDefinition DetachedHouse = new(
        siteBoundary: new Rectangle(
            Create.Point(-8, 0),
            Create.Point(8, 25)),
        footprint: Create.PolyCurve([
            new Rectangle(
                Create.Point(-6,2),
                Create.Point(12,20))
            ]),
        floorCount: 3);

}
