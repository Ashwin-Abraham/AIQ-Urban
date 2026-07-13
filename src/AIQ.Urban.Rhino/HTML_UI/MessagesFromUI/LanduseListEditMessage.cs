using System;
using System.Drawing;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.RhinoInterface;
using AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;
using Rhino;

namespace AIQ.Urban.RhinoPlugin.HTML_UI.MessagesFromUI;

public class LanduseListEditMessage : IMessageFromUI<ModelStateMessage>
{
    public TypeOfMessage MessageType => TypeOfMessage.LanduseListEdit;

    public string useEditType { get; init; } = string.Empty;
    public string name { get; init; } = string.Empty;
    public string color { get; init; } = string.Empty;
    public string id { get; init; } = string.Empty;

    public ModelStateMessage HandleMessage()
    {
        var doc = RhinoDoc.ActiveDoc ?? throw new Exception("No active Rhino document.");
        var editType = useEditType switch
        {
            "add" => UseEditType.Add,
            "edit" => UseEditType.Edit,
            "remove" => UseEditType.Remove,
            _ => throw new ArgumentException($"Unknown useEditType: {useEditType}")
        };
        var parsedColor = ParseColor(color);
        var guid = string.IsNullOrEmpty(id) ? Guid.NewGuid() : Guid.Parse(id);

        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithLanduseListEditSteps(editType, name, parsedColor, guid)
            .WithFullModelOutput()
            .WithMetricsCalculation(tolerance: doc.ModelAbsoluteTolerance);

        if (Urban_RhinoPlugin.Instance.EventHandler is { } handler)
            pipeline.WithEventHandler(handler);

        pipeline.Run();
        doc.Views.Redraw();

        return new ModelStateMessage(UrbanModel.Instance);
    }

    internal static System.Drawing.Color ParseColor(string colorString)
    {
        if (string.IsNullOrEmpty(colorString))
            return System.Drawing.Color.Gray;

        var trimmed = colorString.TrimStart('#');
        if (trimmed.Length is 6 or 8
            && int.TryParse(trimmed[..2], System.Globalization.NumberStyles.HexNumber, null, out var r)
            && int.TryParse(trimmed[2..4], System.Globalization.NumberStyles.HexNumber, null, out var g)
            && int.TryParse(trimmed[4..6], System.Globalization.NumberStyles.HexNumber, null, out var b))
        {
            return trimmed.Length == 8
                ? System.Drawing.Color.FromArgb(int.Parse(trimmed[6..8], System.Globalization.NumberStyles.HexNumber), r, g, b)
                : System.Drawing.Color.FromArgb(r, g, b);
        }

        var parts = colorString.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 3
            && int.TryParse(parts[0], out r)
            && int.TryParse(parts[1], out g)
            && int.TryParse(parts[2], out b))
        {
            return System.Drawing.Color.FromArgb(r, g, b);
        }

        return System.Drawing.Color.FromName(colorString);
    }
}
