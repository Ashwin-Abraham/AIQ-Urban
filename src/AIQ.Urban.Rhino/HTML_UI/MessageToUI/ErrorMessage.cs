using System;

namespace AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;

public class ErrorMessage(string message) : IMessageToUI
{
    public string Type => "Error";
    public string Message { get; } = message;
}
