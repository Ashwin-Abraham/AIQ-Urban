using System;
using AIQ.MCP.Services;
using Rhino;
using Rhino.PlugIns;

namespace AIQ.Urban.RhinoInterface;

///<summary>
/// <para>Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
/// class. DO NOT create instances of this class yourself. It is the
/// responsibility of Rhino to create an instance of this class.</para>
/// <para>To complete plug-in information, please also see all PlugInDescription
/// attributes in AssemblyInfo.cs (you might need to click "Project" ->
/// "Show All Files" to see it in the "Solution Explorer" panel).</para>
///</summary>
public class Urban_RhinoPlugin : PlugIn
{
    private RhinoUrbanEventHandler? _eventHandler;
    private McpServer? _mcpServer;

    public Urban_RhinoPlugin()
    {
        Instance = this;
    }

    ///<summary>Gets the only instance of the Urban_RhinoPlugin plug-in.</summary>
    public static Urban_RhinoPlugin Instance { get; private set; }

    /// <summary>
    /// The active document event handler. Created on plugin load.
    /// Disposal is handled by Rhino on plugin unload.
    /// </summary>
    public RhinoUrbanEventHandler? EventHandler => _eventHandler;

    protected override LoadReturnCode OnLoad(ref string errorMessage)
    {
        var doc = RhinoDoc.ActiveDoc;
        if (doc != null && _eventHandler == null)
        {
            _eventHandler = new RhinoUrbanEventHandler(doc);
        }

        var code = StartMcp(doc, ref errorMessage);

        return code;
    }

    private LoadReturnCode StartMcp(RhinoDoc doc, ref string errorMessage)
    {
        try
        {
            _mcpServer = new McpServer(doc, Core.UrbanModel.Instance);
            _ = _mcpServer.Start(0).ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully && _mcpServer?.Port is int port)
                    RhinoApp.WriteLine($"MCP server started on http://localhost:{port}/mcp");
            });
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to start MCP server: {ex.Message}";
            return LoadReturnCode.ErrorShowDialog;
        }

        return LoadReturnCode.Success;
    }

    protected override void OnShutdown()
    {
        if (_mcpServer is not null)
        {
            RhinoApp.WriteLine("MCP server stopping...");
            _mcpServer.Stop().GetAwaiter().GetResult();
            _mcpServer = null;
            RhinoApp.WriteLine("MCP server stopped.");
        }

        base.OnShutdown();
    }
}
