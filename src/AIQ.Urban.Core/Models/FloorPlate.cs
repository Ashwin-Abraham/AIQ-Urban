using AIQ.Urban.Core.Models.GeometryElements;

namespace AIQ.Urban.Core.Models;

public class FloorPlate
{
    public Profile2D Outline { get; }
    public Use Use { get; }

    public FloorPlate(Profile2D outline, Use use)
    {
        Outline = outline;
        Use = use;
    }
}
