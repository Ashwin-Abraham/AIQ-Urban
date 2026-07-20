using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.RhinoInterface.EventHandler;
using AIQ.Urban.RhinoInterface.Extensions;
using AIQ.Urban.RhinoInterface.Pipeline.Outputters;
using AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;
using AIQ.Urban.RhinoInterface.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rhino;

namespace AIQ.Urban.RhinoInterface;

public class RhinoUrbanModelPipeline(
    RhinoDoc _rhinoDoc,
    UrbanModel _urbanModel,
    ILoggerFactory? _loggerFactory = null)
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance)
        .CreateLogger<RhinoUrbanModelPipeline>();

    private IRhinoDataGatherStep? _objectSelectionStep;
    private List<ICreationData> _creationData = [];
    private readonly List<IUrbanModelStep> _steps = [];
    private readonly List<Guid> _directDeleteIds = [];
    private readonly List<Action<UrbanModel>> _postActions = [];
    private readonly List<IUrbanModelRhinoOutputter> _outputters = [];

    private RhinoUrbanEventHandler? _eventHandler = null;

    internal UrbanModel UrbanModel => _urbanModel;
    public ILoggerFactory LoggerFactory => _loggerFactory ?? NullLoggerFactory.Instance;

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
        _creationData.AddRange(data);
        return this;
    }

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

    public RhinoUrbanModelPipeline WithFullModelOutput()
    {
        _outputters.Add(new TransitOutputter());
        _outputters.Add(new BuildingOutputter());
        _outputters.Add(new ParcelOutputter());
        _outputters.Add(new ZoneOutputter());
        _outputters.Add(new TransitSurfaceOutputter());
        return this;
    }

    public RhinoUrbanModelPipeline WithRhinoOutputter<T>() where T : IUrbanModelRhinoOutputter, new()
    {
        _outputters.Add(new T());
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

        _logger.LogInformation("Pipeline started ({StepCount} steps)", _steps.Count);

        try
        {
            var creationData = _objectSelectionStep?.Run(_rhinoDoc) ?? _creationData;

            foreach (var step in _steps)
            {
                try
                {
                    var result = step.Run(_urbanModel, creationData);
                    addedIds.AddRange(result.AddedIds);
                    deletedIds.AddRange(result.DeletedIds);
                    _logger.LogInformation("{StepType}: +{Added} -{Deleted}",
                        step.GetType().Name, result.AddedIds.Count, result.DeletedIds.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{StepType} failed", step.GetType().Name);
                }
            }

            deletedIds.AddRange(_directDeleteIds);

            if (_outputters.Count > 0)
            {
                DeleteRhinoObjects(deletedIds);
                AddRhinoObjects(addedIds);
                _rhinoDoc.Layers.PurgeEmpty(_rhinoDoc, LayerConstants.Root);
            }

            foreach (var action in _postActions)
            {
                try
                {
                    action(_urbanModel);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Post-action failed");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipeline failed during data gather");
        }
        finally
        {
            if (_eventHandler is not null)
            {
                _eventHandler.RebuildLookup();
                _eventHandler.EventLock = false;
            }
        }

        _logger.LogInformation("Pipeline completed — {Added} added, {Deleted} deleted",
            addedIds.Count, deletedIds.Count);
    }

    private void AddRhinoObjects(List<Guid> addedIds)
    {
        var elementsToAdd = FilterElementsToAdd(addedIds);

        foreach (var outputter in _outputters)
        {
            try
            {
                outputter.Run(_urbanModel, _rhinoDoc, elementsToAdd);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{OutputterType} failed", outputter.GetType().Name);
            }
        }
    }

    private List<IUrbanElement> FilterElementsToAdd(List<Guid> addedIds)
    {
        return _urbanModel.GetUrbanElements()
            .Where(elem => elem.GetGeometryIds().Any(elemtnId => addedIds.Contains(elemtnId)))
            .ToList();
    }

    private void DeleteRhinoObjects(List<Guid> idsToDelete)
    {
        var deletableObjects = _rhinoDoc.Objects.Where(o => idsToDelete.Contains(o.Id)).ToList();
        deletableObjects.ForEach(obj => _rhinoDoc.Objects.Delete(obj, true, true));
    }
}
