using System;

namespace AIQ.Urban.RhinoPlugin.MCP_Bridge;

[AttributeUsage(AttributeTargets.Method)]
internal class McpToolAttribute(string name, string description) : Attribute
{
    public string Name { get; } = name;
    public string Description { get; } = description;
}
