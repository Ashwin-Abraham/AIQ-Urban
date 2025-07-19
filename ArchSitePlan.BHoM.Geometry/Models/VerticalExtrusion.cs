using BH.oM.Geometry;

namespace ArchSitePlan.BHoM.Geometry.Models;
public class VerticalExtrusion:Extrusion
{
    public override Vector Direction { get => Vector.ZAxis; set { } }
    public double Height { get; set; }

    public VerticalExtrusion(ICurve curve, double height)
    {
        Curve = curve;
        Height = height;
    }
}
