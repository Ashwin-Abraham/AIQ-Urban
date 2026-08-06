using ShapeCrawler;

namespace AIQ.Exports.PowerpointExport.ShapeActions;
public class EditShapeFillColorAction(
    Func<IShape, string?> _colorHexMapper) : IShapeEditAction
{
    public IShape EditShape(IShape shape)
    {
        var color = _colorHexMapper.Invoke(shape);

        if (color == null)
            return shape;

        shape.Fill!.SetColor(color);
        return shape;
    }
}
