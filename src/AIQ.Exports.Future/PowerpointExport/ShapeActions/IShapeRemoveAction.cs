using ShapeCrawler;

namespace AIQ.Exports.PowerpointExport.ShapeActions;
public interface IShapeRemoveAction
{
    public bool TryRemove(IShape shape);
}
