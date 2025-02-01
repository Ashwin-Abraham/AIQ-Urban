namespace ArchSitePlan.Core.Models;

public static class AppLanduses
{

    public static List<Landuse> Uses { get; } = new()
    {
        new Landuse("Undefined", "#34553"),
        new Landuse("Resi_Low_Density", "#FF5733"),
        new Landuse("Resi_High_Density", "#FFBD33"),
        new Landuse("Parking", "#33FF57"),
        new Landuse("Transit", "#3357FF"),
        new Landuse("Retail", "#FF33A1"),
        new Landuse("Office", "#5733FF"),
        new Landuse("Food & Beverage", "#F2A9C7"),
        new Landuse("Religious", "#9C33FF"),
        new Landuse("Medical", "#33FF9C"),
        new Landuse("Mixed_Use", "#FF3333"),
        new Landuse("Civic", "#FF8333"),
        new Landuse("Sport", "#33FFCC"),
        new Landuse("Parks & Open Space", "#73FF33"),
        new Landuse("Forest", "#2D6A4F"),
        new Landuse("Water", "#337AB7")
    };

    public static Landuse Undefined = Uses.Find(x => x.Name == "Undefined");

    public static bool TryGetUse(string name, out Landuse? use)
    {
        use = Uses.Find(x => x.Name == name);
        return use != null;
    }

    public static Landuse GetUse(string name)
    {
        var use = Uses.Find(x => x.Name == name);
        return use ?? Undefined;
    }
}
