using System.Drawing;
using ClosedXML.Excel;

namespace ArchSitePlan.Core.Extensions;

internal record IXLStyleData(
    int formatId,
    Color FillColour = default,
    double? FontSize = null,
    bool BoldFont = false,
    bool Wrap = true,
    XLAlignmentHorizontalValues HorizontalAlign = XLAlignmentHorizontalValues.Left);
internal static class XMLExtensions
{
    internal static void SetContents(this IXLCell cell,
        XLCellValue value,
        IXLStyleData styleData,
        string formulaR1C1 = null
        )
    {
        cell.Value = value;
        cell.Style.NumberFormat.NumberFormatId = styleData.formatId;
        cell.Style.Font.Bold = styleData.BoldFont;
        cell.Style.Fill.SetBackgroundColor(XLColor.FromColor(styleData.FillColour));
        cell.Style.Alignment.WrapText = styleData.Wrap;
        cell.Style.Alignment.Horizontal = styleData.HorizontalAlign;

        if (styleData.FontSize != null)
            cell.Style.Font.FontSize = (double)styleData.FontSize;

        if(formulaR1C1 != null)
            cell.FormulaR1C1 = formulaR1C1;
    }

    internal static List<IXLCell> Cells(this IXLWorksheet sheet, 
        int row, 
        int startColumn, 
        int endColumn)
    {
        var cells = new List<IXLCell>();
        for(int c = startColumn; c <= endColumn; c++)
        {
            cells.Add(sheet.Cell(row,c));
        }

        return cells;
    }
    internal static void SetContents(this List<IXLCell> cells,
        XLCellValue value, 
        IXLStyleData styleData)
    {
        foreach(IXLCell cell in cells)
        {
            cell.SetContents(value, styleData);
        }
    }
}
