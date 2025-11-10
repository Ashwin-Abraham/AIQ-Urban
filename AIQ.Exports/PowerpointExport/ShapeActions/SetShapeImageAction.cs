using ShapeCrawler;

namespace AIQ.Exports.PowerpointExport.ShapeActions;
public class SetShapeImageAction(Predicate<IShape> _filter, string _newImagePath) : IShapeEditAction
{
    public IShape EditShape(IShape shape)
    {
        if (!_filter.Invoke(shape))
            return shape;

        using var newImage = File.OpenRead(_newImagePath);
        shape.Picture?.Image!.Update(newImage);
        return shape;
    }
}
