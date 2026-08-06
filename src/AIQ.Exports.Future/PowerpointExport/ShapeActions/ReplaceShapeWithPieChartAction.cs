using ShapeCrawler;

namespace AIQ.Exports.PowerpointExport.ShapeActions;
public class ReplaceShapeWithPieChartAction(
    Predicate<IShape> _requiresReplacement,
    string _chartName, 
    List<GraphData> _data) 
    : IShapeReplaceAction
{
    public bool ReplaceShape(ISlide parentSlide, IShape shape)
    {
        if (!_requiresReplacement(shape) || _data.Count == 0)
            return false;

        var dictionary = _data.ToDictionary(x => x.Label, x => x.Data);

        parentSlide.Shapes.AddPieChart(
            x: (int)shape.X,
            y: (int)shape.Y,
            width: (int)shape.Width,
            height: (int)shape.Height,
            categoryValues: dictionary,
            seriesName: _chartName);

        shape.Remove();
        return true;
    }
}
