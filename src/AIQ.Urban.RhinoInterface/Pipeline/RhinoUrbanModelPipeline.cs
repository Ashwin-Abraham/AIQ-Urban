using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.RhinoInterface.Pipeline.Outputters;
using Rhino;

namespace AIQ.Urban.RhinoInterface;

public class RhinoUrbanModelPipeline(RhinoDoc _rhinoDoc, UrbanModel _urbanModel)
{
    private readonly List<IUrbanModelStep> _steps = [];
    private readonly List<Guid> _directDeleteIds = [];
    private readonly List<Action<UrbanModel>> _postActions = [];

    public RhinoUrbanModelPipeline WithRhinoObjectDeletion(List<Guid> ids)
    {
        _directDeleteIds.AddRange(ids);
        return this;
    }

    public RhinoUrbanModelPipeline WithUrbanElementStep(IUrbanModelStep step)
    {
        _steps.Add(step);
        return this;
    }

    /// <summary>Register an action to execute after all steps, deletions, and baking complete.</summary>
    public RhinoUrbanModelPipeline WithUrbanModelPostAction(Action<UrbanModel> action)
    {
        _postActions.Add(action);
        return this;
    }

    public void Run()
    {
        var deletedIds = new List<Guid>();
        var editedIds = new List<Guid>();
        var addedIds = new List<Guid>();

        foreach (var step in _steps)
        {
            var result = step.Run(_urbanModel);

            if (step is IDeleteUrbanElementsStep)
                deletedIds.AddRange(result);
            if (step is IEditUrbanElementsStep)
                editedIds.AddRange(result);
            if (step is IAddUrbanElementsStep)
                addedIds.AddRange(result);
        }

        var idsToDelete = editedIds
            .Concat(deletedIds)
            .Concat(_directDeleteIds)
            .ToList();

        var idsToAdd = editedIds.Concat(addedIds).ToList();

        DeleteRhinoObjects(idsToDelete);
        AddRhinoObjects(idsToAdd);

        foreach (var action in _postActions)
            action(_urbanModel);
    }

    private void AddRhinoObjects(List<Guid> addedIds)
    {
        var elementsToAdd = FilterElementsToAdd(addedIds);

        var outputters = new List<IUrbanModelRhinoOutputter>
        {
            new TransitOutputter(elementsToAdd.OfType<Transit>().ToList()),
            new BuildingOutputter(elementsToAdd.OfType<Building>().ToList()),
            new ParcelOutputter(elementsToAdd.OfType<Parcel>().ToList()),
            new ZoneOutputter(elementsToAdd.OfType<Zone>().ToList()),
            new TransitSurfaceOutputter()
        };

        foreach (var outputter in outputters)
            outputter.Run(_urbanModel, _rhinoDoc);
    }

    private List<IUrbanElement> FilterElementsToAdd(List<Guid> addedIds)
    {
        var elementsToAdd = new List<IUrbanElement>();
        var allElements = _urbanModel.GetUrbanElements();

        foreach (var element in allElements)
        {
            var ids = element.GetGeometryIds();

            if (!ids.Any(addedIds.Contains))
                continue;

            elementsToAdd.Add(element);
        }

        return elementsToAdd;
    }

    private void DeleteRhinoObjects(List<Guid> idsToDelete)
    {
        _rhinoDoc.Objects.Delete(idsToDelete, true);
    }
}
