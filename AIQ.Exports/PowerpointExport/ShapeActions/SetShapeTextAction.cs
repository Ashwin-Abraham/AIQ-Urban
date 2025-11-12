using ShapeCrawler;

namespace AIQ.Exports.PowerpointExport.ShapeActions;
public class SetShapeTextAction(Predicate<IShape> _filter, string _text) : IShapeEditAction
{
    public IShape EditShape(IShape shape)
    {
        if (!_filter.Invoke(shape) || shape.TextBox == null)
            return shape;

        var priorFont = shape.TextBox
                    .Paragraphs.FirstOrDefault()?
                    .Portions.FirstOrDefault()?
                    .Font;
        bool isBold = priorFont?.IsBold?? false;
        decimal size = priorFont?.Size ?? 14;
        var priorColor = priorFont?.Color;

        shape.TextBox.SetText(_text);
        var fontToChange = shape.TextBox.Paragraphs[0].Portions[0].Font;

        if (priorFont != null && fontToChange != null)
        {
            fontToChange.IsBold = isBold;
            fontToChange.Size = size;
        }

        return shape;
    }
}