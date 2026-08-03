namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public record CurveCoordinates(List<Point2d> Points)
{
    internal List<(double X, double Y)> ToCoreCoordinates()
    {
        return Points.Select(x => (x.X, x.Y)).ToList();
    }
}
