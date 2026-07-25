using AIQ.Exports.Excel.Models;
using ClosedXML.Excel;

namespace AIQ.Exports.Excel.Writing;

internal interface IExcelBlockWriter
{
    bool CanWrite(IExcelBlock block);

    void Write(
        IXLWorksheet worksheet,
        IExcelBlock block);
}
