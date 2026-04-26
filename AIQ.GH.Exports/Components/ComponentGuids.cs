using System;

namespace AIQ.GH.Exports.Components;

public static class ComponentGuids
{
    public static Guid CopyLayoutView => new("a1b2c3d4-1234-5678-abcd-ef0123456789");
    public static Guid SetDetailTarget => new("e5f6a7b8-6668-9aba-dcf3-123336789abd");
    public static Guid PrintLayoutToPdf => new("d4e5f6a7-4567-89ab-cdef-0123456789ac");
    public static Guid MergePdfs => new("e5f6a7b8-5678-9abc-def0-123456789abd");
    public static Guid CreateDocLayerStateFromList => new("f6a7b8c9-6789-abcd-ef01-23456789abcd");
    public static Guid CreateDocLayerStateFromDocument => new("a7b8c9d0-789a-bcde-f012-3456789abcde");
    public static Guid ApplyLayerState => new("b7c8d9e0-8901-bcde-f012-3456789abcde");
    public static Guid EditLayoutText => new("c8d9e0f1-9012-cdef-0123-456789abcdef");
    public static Guid FindDetails => new("f1a2b3c4-2345-6789-abcd-ef0123456789");
    public static Guid ApplyLayerStateToDetail => new("a2b3c4d5-3456-789a-bcde-0123456789ac");
    public static Guid CreateDocLayerStateFromRhinoLayerState => new("a3b5c4d5-3455-789a-bcde-5155456589ab");
}
