using ShapeCrawler;

namespace AIQ.Exports.PowerpointExport.ShapeActions;
public class ReplaceTableTextAction(
    Predicate<IShape> _filter,
    List<(string Text, string Replacement)> _cellTextReplacementMap) : IShapeEditAction
{
    public IShape EditShape(IShape shape)
    {
        if (!_filter.Invoke(shape))
            return shape;

        foreach(var row in shape.Table.Rows)
            foreach(var cell in row.Cells)
            {
                if (cell.TextBox == null)
                    continue;

                var cellText = cell.TextBox.Text;
                var mapEntry = _cellTextReplacementMap.Find(x => x.Text.Equals(cellText));

                if (mapEntry == default)
                    continue;

                var priorFont = cell.TextBox
                    .Paragraphs.FirstOrDefault()?
                    .Portions.FirstOrDefault()?
                    .Font;

                bool isBold = priorFont?.IsBold ?? false;
                decimal priorSize = priorFont?.Size?? 10;
                var priorColor = priorFont?.Color;

                cell.TextBox.SetText(mapEntry.Replacement);

                if (priorFont != null)
                {
                    cell.TextBox.Paragraphs[0]!.Portions[0]!.Font!.Size = priorSize;
                    cell.TextBox.Paragraphs[0]!.Portions[0]!.Font!.IsBold = isBold;
                }
            }

        return shape;
    }
}
