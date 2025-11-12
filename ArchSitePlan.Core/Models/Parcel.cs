using BH.Engine.Geometry;
using BH.oM.Geometry;
using Newtonsoft.Json;

namespace ArchSitePlan.Core.Models;
public class Parcel
{
    public PlanarSurface Boundary {  get; set; }
    public Landuse LandUse { get; set; }
    // TODO: Try to get rid of these duplicate ids
    public Guid CoreModelId { get; set; }
    public Guid ClientModelId { get; set; }

    [JsonConstructor]
    public Parcel(PlanarSurface boundary, Landuse landUse, Guid? coreModelId = null, Guid? clientModelId = null)
    {
        Boundary = boundary;
        LandUse = landUse;
        CoreModelId = coreModelId ?? Guid.NewGuid();
        ClientModelId = clientModelId ?? Guid.NewGuid();
    }

    public double Area()
    {
        return Math.Abs(Boundary.Area());
    }

    public double Coverage()
    {
        var buildings = GetParcelsBuildings();
        double footPrint = buildings.Sum(b => b.FootprintArea());
        double coveragePctg = footPrint / Area() * 100;

        return coveragePctg;
    }

    private List<Building> GetParcelsBuildings()
    {
        var buildings = CoreModelSingelton.Instance.Buildings
            .Where(b => Boundary.ExternalBoundary.IIsContaining([b.Origin]))
            .ToList();

        return buildings;
    }

    public double Perimeter()
    {
        double perimeter = Boundary.ExternalBoundary.ILength() 
            + Boundary.InternalBoundaries.Sum(crv => crv.ILength());
        return perimeter;
    }

    public double BUA()
    {
        var bldgs = GetParcelsBuildings();
        double bua = bldgs.Sum(b => b.BUA());

        return bua;
    }
}
