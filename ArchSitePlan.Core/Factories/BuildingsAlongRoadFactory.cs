using ArchSitePlan.BHoM.Geometry.Extensions;
using ArchSitePlan.Core.Factories.Models;
using ArchSitePlan.Core.Pipeline.CreationData;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Factories;

public class BuildingsAlongRoadFactory 
    :BuildingLayoutFactoryBase
{
    BuildingAlongRoadConfig _config;
    public BuildingsAlongRoadFactory(BuildingAlongRoadConfig config)
    {
        _config = config;
    }

    public BuildingLayout Generate(PolyCurve site, PolyCurve generatorEdge)
    {
        var targetGFA = GetRequiredGFA(
            _config.TargetMode,
            _config.TargetGFA,
            _config.TargetFAR,
            site.Area());

        var smallestBuildingGfa = _config.Definitions.Min(def => def.Building.BUA());

        double posOnCurve = _config.Definitions[0].FrontageWidth/2;
        double achievedGFA = 0;
        int definitionIndex = 0;
        double generatorCrvLength = generatorEdge.Length();
        Vector zDirection = Vector.ZAxis;
        List<BuildingInstance> instances = new();

        while (achievedGFA < targetGFA)
        {
            definitionIndex = definitionIndex < _config.Definitions.Count ?
                definitionIndex : 0;

            var definitionToPlace = _config.Definitions[definitionIndex];
            var nextNextDefinition = GetNextNextDefinition(definitionIndex);

            var frontageDistanceToMove = (definitionToPlace.FrontageWidth + nextNextDefinition.FrontageWidth)/ 2;

            if (posOnCurve > generatorCrvLength) break;

            var curvePoint = generatorEdge.PointAtLength(posOnCurve);
            var tangent = generatorEdge.TangentAtLength(posOnCurve);
            var inwardNormal = tangent.Rotate(Math.PI / 2, zDirection);

            var orientationSystem = Create.CartesianCoordinateSystem(curvePoint,
                x: tangent,
                y: inwardNormal);

            var instanceTransform = Create.OrientationMatrixGlobalToLocal(orientationSystem);

            var placedInstance = definitionToPlace.CreateInstance(instanceTransform);
            var checkPoints = placedInstance.GetConvexHull().Vertices.ToList();

            // Increments
            definitionIndex++;
            
            // Check that the instance is within the site
            if(!site.IsContaining(checkPoints))
            {
                posOnCurve += frontageDistanceToMove / 2;
                continue;
            }

            // If none are placed no checks for clashes with other instances
            if(instances.Count == 0)
            {
                instances.Add(placedInstance);
                posOnCurve += frontageDistanceToMove;
                continue;
            }

            // Verify no clash with previous
            var previousFootrint = instances[^1]
                .GetConvexHull()
                .IOffsetSafe(_config.Setback);
                
            // If clash exists continue
            if(previousFootrint.IIsContainingAny(checkPoints))
            {
                posOnCurve += frontageDistanceToMove / 2;
                continue;
            }

            // Dont let the last building overspill the GFA limit
            double placedBuildingGFA = placedInstance.Definition.Building.BUA();
            double newGFA = achievedGFA + placedBuildingGFA;
            if (newGFA > targetGFA)
            {
                // Lets continue till we place that small building
                if (achievedGFA + smallestBuildingGfa < targetGFA)
                    continue;
                // Or just end the loop
                else
                    break;
            }

            // Otherwise add it to accepted list
            instances.Add(placedInstance);
            posOnCurve += frontageDistanceToMove;
            achievedGFA += placedBuildingGFA;
        }

        return new BuildingLayout(site, site, instances.Select(x => x.GetBuilding()).ToList());
    }

    private BuildingDefinition GetNextNextDefinition(int currentIndex)
    {
        var nextNextDefinitionIndex = currentIndex < _config.Definitions.Count - 1 ?
               currentIndex + 1 : 0;
        return _config.Definitions[nextNextDefinitionIndex];
    }
}
