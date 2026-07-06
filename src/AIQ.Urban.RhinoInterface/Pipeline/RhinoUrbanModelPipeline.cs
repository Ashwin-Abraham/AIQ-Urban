using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.RhinoInterface.EventHandler;
using AIQ.Urban.RhinoInterface.Pipeline.Outputters;
using AIQ.Urban.RhinoInterface.Pipeline.RhinoDocActions;
using Rhino;

namespace AIQ.Urban.RhinoInterface;

public class RhinoUrbanModelPipeline(
    RhinoDoc _rhinoDoc,
    UrbanModel _urbanModel)
{
    private IRhinoDataGatherStep? _objectSelectionStep;
    private List<ICreationData> _creationData = [];
    private readonly List<IUrbanModelStep> _addDeleteSteps = [];
    private readonly List<IEditUrbanElementsStep> _editSteps = [];
    private readonly List<Guid> _directDeleteIds = [];
    private readonly List<Action<UrbanModel>> _postActions = [];

    private RhinoUrbanEventHandler? _eventHandler = null;

    public RhinoUrbanModelPipeline WithEventHandler(RhinoUrbanEventHandler eventHandler)
    {
        _eventHandler = eventHandler;
        return this;
    }

    public RhinoUrbanModelPipeline WithDataGatherStep(IRhinoDataGatherStep step)
    {
        _objectSelectionStep = step;
        return this;
    }

    public RhinoUrbanModelPipeline WithCreationData(List<ICreationData> data)
    {
        _creationData = data;
        return this;
    }

    public RhinoUrbanModelPipeline WithRhinoObjectDeletion(List<Guid> ids)
    {
        _directDeleteIds.AddRange(ids);
        return this;
    }

    public RhinoUrbanModelPipeline WithUrbanElementStep(IUrbanModelStep step)
    {
        _addDeleteSteps.Add(step);
        return this;
    }

    public RhinoUrbanModelPipeline WithUrbanElementStep(IEditUrbanElementsStep step)
    {
        _editSteps.Add(step);
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
        if (_eventHandler is not null)
        {
            _eventHandler.EventLock = true;
        }

        var deletedIds = new List<Guid>();
        var addedIds = new List<Guid>();

        var creationData = _objectSelectionStep?.Run(_rhinoDoc) ?? _creationData;

        if (creationData.Count == 0 && _addDeleteSteps.OfType<IAddUrbanElementsStep>().Any())
            throw new InvalidOperationException("No creation data provided for add steps.");

        foreach (var step in _addDeleteSteps)
        {
            var result = step.Run(_urbanModel, creationData);

            if (step is IDeleteUrbanElementsStep)
                deletedIds.AddRange(result);
            if (step is IAddUrbanElementsStep)
                addedIds.AddRange(result);
        }

        foreach (var step in _editSteps)
        {
            var result = step.Run(_urbanModel);
            addedIds.AddRange(result.AddedIds);
            deletedIds.AddRange(result.DeletedIds);
        }

        deletedIds.AddRange(_directDeleteIds);
        DeleteRhinoObjects(deletedIds);
        AddRhinoObjects(addedIds);

        foreach (var action in _postActions)
            action(_urbanModel);

        if (_eventHandler is not null)
        {
            _eventHandler.RebuildLookup();
            _eventHandler.EventLock = false;
        }
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
        var deletableObjects = _rhinoDoc.Objects.Where(o => idsToDelete.Contains(o.Id)).ToList();
        deletableObjects.ForEach(obj => _rhinoDoc.Objects.Delete(obj, true, true));
    }
}
