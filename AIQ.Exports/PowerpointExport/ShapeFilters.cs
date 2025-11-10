using ShapeCrawler;

namespace AIQ.Exports.PowerpointExport;
public static class ShapeFilters
{
    public static bool TextboxContains(IShape shape, string text)
    {
        if(shape.TextBox == null)
            return false;

        return shape.TextBox.Text.Contains(text);
    }

    public static bool NameContains(IShape shape, string name)
    {
        return shape.Name.Contains(name);
    }

    public static bool NameContains(IShape shape, List<string> names)
    {
        return names.Any(name => shape.Name.Contains(name));
    }

    public static bool IsPicture(IShape shape)
    {
        return shape.Picture != null;
    }
}
