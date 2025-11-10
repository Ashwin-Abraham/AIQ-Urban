using ShapeCrawler;

namespace AIQ.Exports.PowerpointExport.ShapeActions;
public class ConditionalSetShapeTextAction(Func<IShape, string?> _textMapper, decimal _fontSize)
    : IShapeEditAction
{
    public IShape EditShape(IShape shape)
    {
        var textToSet = _textMapper.Invoke(shape);

        if (textToSet == null)
            return shape;

        shape.TextBox!.SetText(textToSet);
        shape.SetFontSize(_fontSize);

        return shape;
    }
}