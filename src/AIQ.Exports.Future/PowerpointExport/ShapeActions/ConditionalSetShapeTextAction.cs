using ShapeCrawler;

namespace AIQ.Exports.PowerpointExport.ShapeActions;
public class ConditionalSetShapeTextAction(Func<IShape, string?> _textMapper)
    : IShapeEditAction
{
    public IShape EditShape(IShape shape)
    {
        var textToSet = _textMapper.Invoke(shape);

        if (textToSet == null)
            return shape;

        var priorSize = shape.TextBox!.Paragraphs.FirstOrDefault()?.Portions.FirstOrDefault()?.Font?.Size?? 14;
        shape.TextBox!.SetText(textToSet);
        shape.SetFontSize(priorSize);

        return shape;
    }
}