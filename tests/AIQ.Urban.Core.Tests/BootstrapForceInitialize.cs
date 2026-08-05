using System.Runtime.CompilerServices;
using AIQ.Urban.DebugViz;

namespace AIQ.Urban.Core.Tests;
public class BootstrapForceInitialize
{
    [ModuleInitializer]
    public static void ForcePreviewBootstrapStart()
    {
#if DEBUG
        PreviewAssemblyBootstrap.Initialize();
#endif
    }

}
