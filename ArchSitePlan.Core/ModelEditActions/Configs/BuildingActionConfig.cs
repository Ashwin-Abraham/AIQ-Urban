using ArchSitePlan.Core.Models;
using BH.oM.Geometry;
namespace ArchSitePlan.Core.ModelEditActions.Configs
{
    public record BuildingActionConfig
    {
        public Mesh? EnvelopeMesh;
        public Guid EnvelopeId;
        public Guid FootprintId;
        public double FlootToFloorHt;
        public int FloorCount;
        public List<PlanarSurface> FloorPlates;

        public BuildingActionConfig(Mesh? envelopeMesh,
            Guid envelopeId,
            Guid footprintId,
            double flootToFloorHt,
            int floorCount,
        List<PlanarSurface> floorPlates) 
        {
            EnvelopeMesh = envelopeMesh;
            EnvelopeId = envelopeId;
            FootprintId = footprintId;
            FlootToFloorHt = flootToFloorHt;
            FloorPlates = floorPlates;
            FloorCount = floorCount;
        }

        public BuildingActionConfig(Building building) :
            this(building.Envelope,
                building.EnvelopeId,
                building.FootprintId,
                building.FloorToFloorHeight,
                building.FloorCount,
                building.GetFloorPlates())
        { }
    }
}
