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
    public static BuildingDefinition DetachedHouse = BuildingDefinition.FromFootprint(
        buildingFootprint: Create.PolyCurve([
            new Rectangle(
                Create.Point(-6,2),
                Create.Point(12,20))
            ]),
        floorHeight: 3.6,
        floorCount: 3,
        frontageWidth: 20);

}
