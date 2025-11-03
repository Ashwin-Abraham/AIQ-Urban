using ArchSitePlan.BHoM.Geometry.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using System.Text.Json.Serialization;

namespace ArchSitePlan.Core.Models
{
    public class Building
    {
        public int FloorCount { get; set; }
        public double FloorToFloorHeight { get; set;}
        public Guid FootprintId { get; init;}
        public Guid EnvelopeId { get; set;}
        public Guid HostParcelCoreModelId { get; set;} = Guid.Empty;
        public Point Origin { get; private set; }
        public Mesh Envelope { get; private set; }
        public List<FloorPlate> FloorPlates { get; set; }

        public Building(
            Building building,
            Mesh envelope,
            List<(PlanarSurface, Guid)> floorPlates)
            : this(envelope,
                floorPlates.Select(x => new FloorPlate(x.Item1, x.Item2, null)).ToList(),
                building.FloorToFloorHeight,
                building.EnvelopeId,
                building.FootprintId) { }

        public Building(
            Mesh envelope,
            List<PlanarSurface> floorPlates,
            double floorToFloorHeight,
            Guid envelopeId = default,
            Guid footprintId = default)
            : this(
                envelope,
                floorPlates.Select(x => new FloorPlate(x, Guid.Empty, null)).ToList(),
                floorToFloorHeight,
                envelopeId,
                footprintId)
        { }

        [JsonConstructor]
        public Building(
            Mesh envelope,
            List<FloorPlate> floorPlates,
            double floorToFloorHeight,
            Guid envelopeId = default,
            Guid footprintId = default)
        {
            Envelope = envelope;
            FloorToFloorHeight = floorToFloorHeight;
            
            FloorPlates = floorPlates
                .Where(p => Math.Abs(p.Profile.Area()) > 1E-06)
                .OrderBy(p => p.Profile.ControlPointCentroid().Z)
                .ToList();

            FootprintId = footprintId;
            EnvelopeId = envelopeId;
            Origin = CalculateOrigin(envelope);
            FloorCount = floorPlates.Count;
        }

        public Building Transformed(TransformMatrix transform)
        {
            var transformedEnvelope = Envelope.Transform(transform);
            var transformedFloors = FloorPlates
                .Select(x => (x.Profile.Transform(transform), x.Id))
                .ToList();

            return new(this, transformedEnvelope, transformedFloors);
        }

        public static Point CalculateOrigin(Mesh envelope)
        {
            return envelope.Bounds().Centre().ProjectToXY();
        }

        public double BUA()
        {
            return FloorPlates.Sum(p => Math.Abs(p.Profile.IArea()));
        }

        public double FootprintArea()
        {
            double area = 0;
            if(FloorPlates.Any())
                area = FloorPlates.First().Profile.Area();

            return Math.Abs(area);
        }

        internal List<PlanarSurface> GetFloorPlates()
        {
            List<PlanarSurface> floors = FloorPlates.Select(x => x.Profile).ToList();
            return floors;
        }

        public Landuse? GetLargestUse()
        {
            var usesAndAreas = new Dictionary<Landuse, double>();

            foreach (var floorplate in FloorPlates)
            {
                if (floorplate.Use is null)
                    continue;

                if (!usesAndAreas.ContainsKey(floorplate.Use))
                    usesAndAreas.Add(floorplate.Use, floorplate.AreaAbs());

                usesAndAreas[floorplate.Use] += floorplate.AreaAbs();
            }

            if(usesAndAreas.Count == 0)
                return null;

            return usesAndAreas.MaxBy(x => x.Value).Key;
        }
    }
}
