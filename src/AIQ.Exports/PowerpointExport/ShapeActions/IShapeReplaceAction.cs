using ShapeCrawler;

namespace AIQ.Exports.PowerpointExport.ShapeActions;
public interface IShapeReplaceAction
{
    public bool ReplaceShape(ISlide parentSlide, IShape shape);
}