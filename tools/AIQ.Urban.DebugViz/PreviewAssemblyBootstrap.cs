#if DEBUG

using System.Runtime.CompilerServices;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.DebugViz.Converters;

namespace AIQ.Urban.DebugViz;


/// <summary>
/// Registers the preview sink for the kope.PreviewHelper.Converters in the test assembly.
/// </summary>
public static class PreviewAssemblyBootstrap
{
    /// <summary>
    /// Attaches preview converters to the <see cref="PreviewHub"/>.
    /// The converters are then accessible within any other project
    /// enabling previews during debug sessions and avoids 
    /// </summary>
    [ModuleInitializer]
    public static void Initialize()
    {
        var converters = new List<IGeometryPreviewConverter>()
        {
            new DefaultCurveConverter(),
        };

        // TODO: Add additional converters for all classes that need to be visualised
        PreviewHub.Sink = new PreviewSink(converters.ToHashSet());
    }
}
#endif