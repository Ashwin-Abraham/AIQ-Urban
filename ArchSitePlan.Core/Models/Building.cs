using ArchSitePlan.Core.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.Core.Models
{
    public class Building
    {
        public int FloorCount { get; set; }
        public double FloorToFloorHeight { get; set;}
        public Guid FootprintId { get; init;}
        public Guid EnvelopeId { get; set;} = Guid.Empty;
        public Point Origin { get; private set; }
        public Mesh Envelope { get; private set; }

        public List<(PlanarSurface Profile, Guid Id)> FloorPlates { get; set; }

        public Building(
            Mesh envelope,
            List<PlanarSurface> floorPlates,
            double floorToFloorHeight,
            Guid envelopeId,
            Guid footprintId = default)
        {
            Envelope = envelope;
            FloorToFloorHeight = floorToFloorHeight;
            FloorPlates = floorPlates.Select(f => (f, Guid.Empty)).ToList();
            FootprintId = footprintId;
            EnvelopeId = envelopeId;
            Origin = Envelope.Bounds().Centre();
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
