using ArchSitePlan.Core.Extensions;
using BH.Engine.Geometry;
using BH.Engine.Spatial;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Models
{
    public class Building
    {
        public int FloorCount { get; set; }
        public double FloorToFloorHeight { get; set;}
        public Guid FootprintId { get; init;}
        public Guid EnvelopeId { get; set;} = Guid.Empty;
        public Guid HostParcelId { get; set;} = Guid.Empty;
        public Point Origin { get; private set; }
        public Mesh Envelope { get; private set; }
        public Landuse Use { get; set; }

        public List<(PlanarSurface Profile, Guid Id)> FloorPlates { get; set; }

        public Building(
            Mesh envelope,
            List<PlanarSurface> floorPlates,
            double floorToFloorHeight,
            Guid envelopeId,
            Landuse use,
            Guid footprintId = default)
        {
            Use = use;
            Envelope = envelope;
            FloorToFloorHeight = floorToFloorHeight;
            
            FloorPlates = floorPlates
                .Where(p => p!=null && Math.Abs(p.Area()) > 1E-06)
                .OrderBy(p => p.ControlPointCentroid().Z)
                .Select(f => (f, Guid.Empty))
                .ToList();

            FootprintId = footprintId;
            EnvelopeId = envelopeId;
            Origin = CalculateOrigin(envelope);
            FloorCount = floorPlates.Count;
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

        public Landuse FindHostParcelUse()
        {
            var host = CoreModel.Parcels.Find(p => p.Boundary.ExternalBoundary.IIsContaining(Origin.ObjectToList()));
            return host?.LandUse?? AppLanduses.Undefined;
        }

        internal List<PlanarSurface> GetFloorPlates()
        {
            List<PlanarSurface> floors = FloorPlates.Select(x => x.Profile).ToList();
            return floors;
        }

        //private List<(PlanarSurface, Guid)> CreateFloorPlates(PlanarSurface baseOutline)
        //{
        //    var floorPlates = new List<(PlanarSurface,Guid)>();

        //    for(int i = 0; i < FloorCount; i++)
        //    {
        //        var mover = Create.Vector(0,0, i * FloorToFloorHeight);
        //        var floor = baseOutline.Translate(mover);
        //        floorPlates.Add((floor, Guid.Empty));
        //    }
        //    return floorPlates;
        //}
        //public Mesh GetEnvelope()
        //{
        //    if (BaseOutline != null)
        //    {
        //        var extr = Create.Extrusion(
        //            BaseOutline.ExternalBoundary,
        //            Create.Vector(0, 0, FloorToFloorHeight * FloorCount),
        //            true);

        //        return extr.ToMesh();
        //    }
        //    else
        //    {
        //        return Envelope;
        //    }
        //}
        //public void UpdateProperties(PlanarSurface footPrint, int floorCount, double floorToFloorHeight)
        //{
        //    BaseOutline = footPrint;
        //    FloorCount = floorCount;
        //    FloorToFloorHeight = floorToFloorHeight;
        //    FloorPlates = CreateFloorPlates(footPrint);
        //}
    }
}
