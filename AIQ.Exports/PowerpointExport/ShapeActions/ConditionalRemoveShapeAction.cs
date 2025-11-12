using ShapeCrawler;

namespace AIQ.Exports.PowerpointExport.ShapeActions;
public class ConditionalRemoveShapeAction (Predicate<IShape> _removeShape): IShapeRemoveAction
{
    public bool TryRemove(IShape shape)
    {
        if (!_removeShape.Invoke(shape))
            return false;
    
        shape.Remove();
        return true;
    }
}
