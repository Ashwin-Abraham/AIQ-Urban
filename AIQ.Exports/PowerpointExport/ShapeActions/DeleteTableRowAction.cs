using ShapeCrawler;

namespace AIQ.Exports.PowerpointExport.ShapeActions;
public class DeleteTableRowAction(Predicate<IShape>_shapeFilter, Predicate<ITableRow> _removeRowFilter) : IShapeEditAction
{
    public IShape EditShape(IShape shape)
    {
        if (!_shapeFilter.Invoke(shape))
            return shape;

        List<ITableRow> rowsToRemove = [];

        for(int i = 0; i < shape.Table.Rows.Count;i ++)
        {
            var row = shape.Table.Rows[i];

            if (_removeRowFilter.Invoke(row))
                rowsToRemove.Add(row);
        }

        rowsToRemove.ForEach(x => shape.Table.Rows.Remove(x));
        return shape;
    }
}
