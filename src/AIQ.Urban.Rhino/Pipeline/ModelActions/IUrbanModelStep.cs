using System;
using System.Collections.Generic;
using AIQ.Urban.Core;

namespace AIQ.Urban.RhinoPlugin.Pipeline.ModelActions;
internal interface IUrbanModelStep
{
    internal List<Guid> Run(UrbanModel model);
}

internal interface IAddUrbanElementsStep:IUrbanModelStep;
internal interface IDeleteUrbanElementsStep:IUrbanModelStep;
internal interface IEditUrbanElementsStep:IUrbanModelStep;
