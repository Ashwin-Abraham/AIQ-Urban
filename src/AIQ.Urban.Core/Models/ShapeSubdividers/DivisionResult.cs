using AIQ.Urban.Core.Models.GeometryElements;

namespace AIQ.Urban.Core.Models.ShapeSubdividers;
public class DivisionResult(List<Profile2D> fragments, List<Curve2D> dividers)
{
    public List<Profile2D> Fragmenets { get; } = fragments;
    public List<Curve2D> Dividers { get; } = dividers;
}
