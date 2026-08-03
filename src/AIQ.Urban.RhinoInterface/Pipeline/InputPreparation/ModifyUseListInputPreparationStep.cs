using System.Drawing;
using System.Globalization;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public class ModifyUseListInputPreparationStep(
    string editType,
    string name,
    string color,
    string id) : IInputPreparationStep
{
    public List<ICreationData> Run(InputPreparationContext context)
    {
        var parsedEditType = editType.ToLowerInvariant() switch
        {
            "add" => UseEditType.Add,
            "edit" => UseEditType.Edit,
            "remove" => UseEditType.Remove,
            _ => throw new ArgumentException($"Unknown useEditType: {editType}", nameof(editType)),
        };

        var parsedId = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid() : Guid.Parse(id);
        return [new UseModificationData(parsedEditType, name, ParseColor(color), parsedId)];
    }

    public static Color ParseColor(string colorString)
    {
        if (string.IsNullOrWhiteSpace(colorString))
            return Color.Gray;

        var trimmed = colorString.TrimStart('#');
        if (trimmed.Length == 8
            && int.TryParse(trimmed[..2], NumberStyles.HexNumber, null, out var a)
            && int.TryParse(trimmed[2..4], NumberStyles.HexNumber, null, out var r)
            && int.TryParse(trimmed[4..6], NumberStyles.HexNumber, null, out var g)
            && int.TryParse(trimmed[6..8], NumberStyles.HexNumber, null, out var b))
        {
            return Color.FromArgb(a, r, g, b);
        }

        if (trimmed.Length == 6
            && int.TryParse(trimmed[..2], NumberStyles.HexNumber, null, out r)
            && int.TryParse(trimmed[2..4], NumberStyles.HexNumber, null, out g)
            && int.TryParse(trimmed[4..6], NumberStyles.HexNumber, null, out b))
        {
            return Color.FromArgb(r, g, b);
        }

        var parts = colorString.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 3
            && int.TryParse(parts[0], out r)
            && int.TryParse(parts[1], out g)
            && int.TryParse(parts[2], out b))
        {
            return Color.FromArgb(r, g, b);
        }

        return Color.FromName(colorString);
    }
}
