namespace ArchSitePlan.OSM.Models;

public class OsmModel(List<IOsmObject> osmObjects)
{
    public List<IOsmObject> OsmObjects { get; set; } = osmObjects;
}
