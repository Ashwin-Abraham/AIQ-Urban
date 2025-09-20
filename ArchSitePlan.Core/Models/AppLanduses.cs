
using System.Drawing;

namespace ArchSitePlan.Core.Models;

public static class AppLanduses
{
    public static IReadOnlyList<Landuse> InitialUses { get; } =
    [      
        new Landuse("Undefined", Guid.NewGuid(), "#565D64"),
        new Landuse("Resi_Low_Density", Guid.NewGuid(), "#FF5733"),
        new Landuse("Resi_High_Density",Guid.NewGuid(), "#FFBD33"),
        new Landuse("Parking", Guid.NewGuid(),"#33FF57"),
        new Landuse("Transit", Guid.NewGuid(), "#3357FF"),
        new Landuse("Retail", Guid.NewGuid(), "#FF33A1"),
        new Landuse("Office", Guid.NewGuid(), "#5733FF"),
        new Landuse("Food & Beverage", Guid.NewGuid(), "#F2A9C7"),
        new Landuse("Religious", Guid.NewGuid(), "#9C33FF"),
        new Landuse("Medical", Guid.NewGuid(), "#33FF9C"),
        new Landuse("Mixed_Use", Guid.NewGuid(), "#FF3333"),
        new Landuse("Civic", Guid.NewGuid(), "#FF8333"),
        new Landuse("Sport", Guid.NewGuid(), "#33FFCC"),
        new Landuse("Parks & Open Space", Guid.NewGuid(), "#73FF33"),
        new Landuse("Forest", Guid.NewGuid(), "#2D6A4F"),
        new Landuse("Water", Guid.NewGuid(), "#337AB7")
    ];

    public static Landuse Undefined = InitialUses[0];
}
