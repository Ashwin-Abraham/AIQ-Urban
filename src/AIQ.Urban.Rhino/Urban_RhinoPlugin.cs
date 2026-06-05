using System;
using System.Diagnostics;
using System.IO;
using AIQ.Urban.RhinoInterface.EventHandler;
using Rhino;
using Rhino.PlugIns;

namespace AIQ.Urban.RhinoPlugin;

public class Urban_RhinoPlugin : PlugIn
{
    private RhinoUrbanEventHandler _eventHandler;
    private Process? _mcpProcess;

    public Urban_RhinoPlugin()
    {
        Instance = this;
    }

    public static Urban_RhinoPlugin Instance { get; private set; }

    public RhinoUrbanEventHandler EventHandler => _eventHandler;

    protected override LoadReturnCode OnLoad(ref string errorMessage)
    {
        var doc = RhinoDoc.ActiveDoc;
        if (doc != null && _eventHandler == null)
        {
            _eventHandler = new RhinoUrbanEventHandler(doc);
        }

        return LoadReturnCode.Success;
    }

    public void StartMcpProcess()
    {
        if (_mcpProcess is not null && !_mcpProcess.HasExited)
            return;

        try
        {
            var pluginDir = Path.GetDirectoryName(GetType().Assembly.Location);
            var exePath = Path.Combine(pluginDir ?? ".", "mcp", "AIQ.MCP.exe");

            if (!File.Exists(exePath))
            {
                RhinoApp.WriteLine($"AIQ.MCP.exe not found at {exePath}");
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = "--bridge ws://127.0.0.1:6400/mcp-bridge",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _mcpProcess = new Process { StartInfo = startInfo };
            _mcpProcess.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    RhinoApp.WriteLine($"[MCP] {e.Data}");
            };
            _mcpProcess.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    RhinoApp.WriteLine($"[MCP Error] {e.Data}");
            };

            _mcpProcess.Start();
            _mcpProcess.BeginOutputReadLine();
            _mcpProcess.BeginErrorReadLine();

            RhinoApp.WriteLine($"AIQ.MCP process started (PID: {_mcpProcess.Id})");
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"Failed to start AIQ.MCP: {ex.Message}");
        }
    }

    protected override void OnShutdown()
    {
        if (_mcpProcess is not null && !_mcpProcess.HasExited)
        {
            RhinoApp.WriteLine("Stopping AIQ.MCP process...");

            try
            {
                _mcpProcess.Kill();
                _mcpProcess.WaitForExit(5000);
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error stopping AIQ.MCP: {ex.Message}");
            }
            finally
            {
                _mcpProcess.Dispose();
                _mcpProcess = null;
            }
        }

        var portFile = Path.Combine(Path.GetTempPath(), "AIQ.MCP.port");
        try { if (File.Exists(portFile)) File.Delete(portFile); } catch { }

        base.OnShutdown();
    }
}
