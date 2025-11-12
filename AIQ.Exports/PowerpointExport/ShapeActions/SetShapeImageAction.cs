using System.Drawing;
using ShapeCrawler;

namespace AIQ.Exports.PowerpointExport.ShapeActions;
public class SetShapeImageAction(
    Predicate<IShape> _filter, string _newImagePath,
    bool _alignRight = false) : IShapeEditAction
{
    public IShape EditShape(IShape shape)
    {
        if (!_filter.Invoke(shape))
            return shape;

        var previousWidth = shape.Width;
        var previousHeight = shape.Height;

        using var newImageStream = File.OpenRead(_newImagePath);
        using var image = Image.FromStream(newImageStream);

        (decimal newWidth, decimal newHeight) = FitNewShapeIntoOld(previousWidth, previousHeight, image.Width, image.Height);

        shape.Picture?.Image!.Update(newImageStream);
        shape.Width = newWidth;
        shape.Height = newHeight;

        if(_alignRight)
        {
            var xMovement = previousWidth - newWidth;
            if(xMovement > 0)
                shape.X = shape.X + xMovement;
        }

        return shape;
    }

    private (decimal Width, decimal Height) FitNewShapeIntoOld(
        decimal oldWidth,
        decimal oldHeight,
        decimal newWidth,
        decimal newHeight)
    {
        var widthMapper = oldWidth / newWidth;
        var heightMapper = oldHeight / newHeight;

        var mapper = Math.Min(widthMapper, heightMapper);

        return(mapper * newWidth, mapper * newHeight);
    }
}
