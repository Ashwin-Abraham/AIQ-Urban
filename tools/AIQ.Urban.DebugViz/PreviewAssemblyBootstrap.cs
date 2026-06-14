#if DEBUG

using System.Runtime.CompilerServices;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.DebugViz.Converters;

namespace AIQ.Urban.DebugViz;


/// <summary>
/// Registers the preview sink and its converters into <see cref="PreviewHub"/>
/// when the assembly is loaded, making preview available across all projects in DEBUG builds.
/// </summary>
public static class PreviewAssemblyBootstrap
{
    /// <summary>
    /// Attaches preview converters to the <see cref="PreviewHub"/>.
    /// The converters are then accessible within any other project,
    /// enabling previews during debug sessions without circular dependencies.
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