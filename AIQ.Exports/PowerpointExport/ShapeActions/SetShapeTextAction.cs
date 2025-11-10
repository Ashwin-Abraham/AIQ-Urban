using ShapeCrawler;

namespace AIQ.Exports.PowerpointExport.ShapeActions;
public class SetShapeTextAction(Predicate<IShape> _filter, string _text, decimal _fontSize) : IShapeEditAction
{
    public IShape EditShape(IShape shape)
    {
        if (!_filter.Invoke(shape) || shape.TextBox == null)
            return shape;


        shape.TextBox.SetText(_text);
        shape.SetFontSize(_fontSize);
        
        return shape;
    }
}