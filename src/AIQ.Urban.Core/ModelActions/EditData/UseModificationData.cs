using System.Drawing;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models;

namespace AIQ.Urban.Core.ModelActions.EditData;

public record UseModificationData(UseEditType EditType, string Name, Color Color, Guid Id) : IModelEditData;
