using AIQ.Urban.Core;
using Microsoft.Extensions.Logging;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public record InputPreparationContext(
    RhinoDoc RhinoDoc,
    UrbanModel UrbanModel,
    ILoggerFactory LoggerFactory);
