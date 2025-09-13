using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline.CreationData;
using ArchSitePlan.OSM;
using ArchSitePlan.RhinoInterface.Constants;
using ArchSitePlan.RhinoInterface.HTML_UI.HtmlCommsClasses;
using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
using ArchSitePlan.RhinoInterface.HTML_UI.Websockets;
using ArchSitePlan.RhinoInterface.InputValidation.Results;
using ArchSitePlan.RhinoInterface.InputValidation.Validators;
using ArchSitePlan.RhinoInterface.Models;
using Rhino;
using Rhino.DocObjects;
using static ArchSitePlan.RhinoInterface.Constants.Enums;
using MessageBox = Eto.Forms.MessageBox;

namespace ArchSitePlan.RhinoInterface.CoreModelInterface;


public static class RhinoEventHandler
{
    public static bool EventLock = false;
    public static bool ModellingToolModeActive = false;
    public static bool ValidLicense = true;

    public static UIViewModel HTMLViewModel = new();

    private static bool _isReplacement = false;
    private static List<RhinoObject> _objectsForReplace = new();
    private static List<RhinoObject> _objectsForAddition = new();
    private static List<RhinoObject> _objectsForRemoval = new();
    private static List<RhinoObject> _objectsForUndelete = new();

    private static EventHandler<RhinoObjectEventArgs> _undeleteQueueBuilder = (sender, args) => QueueObjectForUndelete(args);
    private static EventHandler _processPipeline = (sender, args) => ProcessEventPipepline(sender, args);
    private static EventHandler<RhinoReplaceObjectEventArgs> _replacementQueueBuilder = (sender, args) => QueueObjectForReplacement(args);
    private static EventHandler<RhinoObjectEventArgs> _addObjectQueueBuilder = (sender, args) => QueueObjectForAddition(args);
    private static EventHandler<RhinoObjectEventArgs> _removeObjectQueueBuilder = (sender, args) => QueueObjectForRemoval(args);
    private static EventHandler<RhinoObjectSelectionEventArgs> _selectObjectHandler = (sender, args) => OnRhinoDocObjectSelection(args);
    private static EventHandler<RhinoObjectSelectionEventArgs> _deselectObjectHandler = (sender, args) => OnRhinoDocObjectDeSelection(args);
    private static EventHandler<RhinoDeselectAllObjectsEventArgs> _deselectAllObjectsHandler = (sender, args) => OnAllRhinoDocObjectDeSelection(args);

    public static UsageStatsMessage UseStatsMessage = new();

    private static bool _eventsSubscribed = false;
    // TODO: Implement osm objects data pop up
    // RhinoDoc.SelectObjects += (sender, args) => ShowObjectInfoEvent(args);

    public static RhinoWebSocketBehavior WS = new();

    public static void UnsubscribeModelEvents()
    {
        if (!_eventsSubscribed) return;

        RhinoApp.Idle -= _processPipeline;
        RhinoDoc.ReplaceRhinoObject -= _replacementQueueBuilder;
        RhinoDoc.AddRhinoObject -= _addObjectQueueBuilder;
        RhinoDoc.DeleteRhinoObject -= _removeObjectQueueBuilder;
        RhinoDoc.UndeleteRhinoObject -= _undeleteQueueBuilder;
        RhinoDoc.SelectObjects -= _selectObjectHandler;
        RhinoDoc.DeselectObjects -= _deselectObjectHandler;
        RhinoDoc.DeselectAllObjects -= _deselectAllObjectsHandler;

    }

    public static void SubscribeModelEvents()
    {
        if (_eventsSubscribed) return;

        RhinoApp.Idle += _processPipeline;
        RhinoDoc.ReplaceRhinoObject += _replacementQueueBuilder;
        RhinoDoc.AddRhinoObject += _addObjectQueueBuilder;
        RhinoDoc.DeleteRhinoObject += _removeObjectQueueBuilder;
        RhinoDoc.UndeleteRhinoObject += _undeleteQueueBuilder;
        RhinoDoc.SelectObjects += _selectObjectHandler;
        RhinoDoc.DeselectObjects += _deselectObjectHandler;
        RhinoDoc.DeselectAllObjects += _deselectAllObjectsHandler;
    }

    private static void ShowObjectInfoEvent(RhinoObjectSelectionEventArgs args)
    {
        var doc = RhinoDoc.ActiveDoc;
        var osmObject = args.RhinoObjects
            .FirstOrDefault(obj => doc.Layers[obj.Attributes.LayerIndex].FullPath
                .Contains(LayerConstants.MainOSMLayer));

        // Trigger a websocket message containing the osmObject details
        if(osmObject != null)
        {
            string? osmIdText = osmObject
                .Attributes.GetUserStrings()[OSMConstants.OsmIdAttributeKey];
            
            if(osmIdText != null && osmIdText != ""
                && long.TryParse(osmIdText, out long osmId)
                && OSMConstants.OsmIdsByFile.TryGetValue(osmId, out string? filePath))
            {
                var tags = OSMFileReader.FindObjectTags(filePath, osmId);
                WebSocketComms.BroadcastMessage(new OsmTagsMessage(tags));
            }
        }

        var aiqModellingObjects = args.RhinoObjects
            .Where(obj => doc.Layers[obj.Attributes.LayerIndex].FullPath.Contains(LayerConstants.MainModellingLayer))
            .ToList();

        // Trigger a eto form popup containing the aiq model objects' details

    }

    private static void QueueObjectForUndelete(RhinoObjectEventArgs args)
    {
        if (EventLock) return;
        _objectsForUndelete.Add(args.TheObject);
    }

    private static void QueueObjectForRemoval(RhinoObjectEventArgs args)
    {
        if (EventLock) return;
        _objectsForRemoval.Add(args.TheObject);
    }

    private static void QueueObjectForAddition(RhinoObjectEventArgs args)
    {
        if (EventLock) return;
        _objectsForAddition.Add(args.TheObject);
    }

    private static void QueueObjectForReplacement(RhinoReplaceObjectEventArgs args)
    {
        if (EventLock) return;
        _objectsForReplace.Add(args.NewRhinoObject);
    }

    private static void ProcessEventPipepline(object? sender, EventArgs e)
    {
        if (EventLock) return;

        bool runEvents = _objectsForReplace.Count > 0 
            || _objectsForUndelete.Count > 0
            || _objectsForAddition.Count > 0
            || _objectsForRemoval.Count > 0
            ;

        // Pick the actions to execute.
        // Because Rhino sometimes sends both remove + add as well as replace objects events, we need to pick the one of these pathways only
        _objectsForAddition = _objectsForAddition.Where(id => !_objectsForReplace.Contains(id)).ToList();
        _objectsForRemoval = _objectsForRemoval.Where(id => !_objectsForReplace.Contains(id)).ToList();

        if (!runEvents) return;

        // Rhino events are a little peculiar in that a replacement event also
        // triggers an add+remove event. 
        // However SOME replacement events like Curve Join are merely remove + add events
         if (_objectsForReplace.Count > 0) 
            RunReplacementOperations(_objectsForReplace);

        else
        {
            if (_objectsForAddition.Count > 0)
                RunObjectAddOperations(_objectsForAddition);

            if (_objectsForRemoval.Count > 0)
                RunObjectRemovalOperations(_objectsForRemoval);
        }

        if (_objectsForUndelete.Count > 0)
            RunCoreModelUndoOperations(_objectsForUndelete);

        // Clean queue
        ResetEventQueueAndObjects();

        // Output metrics
        WS.SendMessage(new StudyAreaMetricsMessage());
    }

    public static void ResetEventQueueAndObjects()
    {
        _objectsForReplace.Clear();
        _objectsForAddition.Clear();
        _objectsForRemoval.Clear();
        _objectsForUndelete.Clear();
    }

    private static void OnAllRhinoDocObjectDeSelection(RhinoDeselectAllObjectsEventArgs args)
    {
        if (HTMLViewModel.AppMode == ModellingTool.None) return;

        RhinoApp.InvokeOnUiThread(() => SelectedObjectMetricsMessage.FromEmpty().BroadcastMessage());
    }

    private static void OnRhinoDocObjectDeSelection(RhinoObjectSelectionEventArgs args)
    {
        RhinoApp.InvokeOnUiThread(() => SelectedObjectMetricsMessage.FromEmpty().BroadcastMessage());
    }

    private static void RunCoreModelUndoOperations(List<RhinoObject> objectsForUndelete)
    {
        try
        {
            if (EventLock) return;

            EventLock = true;
            RhinoDoc doc = RhinoDoc.ActiveDoc;

            // TODO if the last road is deleted and ctrl-z-ed 
            // an error is thrown because the app layer for roads doesnt exist
            var appObjects = objectsForUndelete
                .Where(o => doc.Layers.FindIndex(o.Attributes.LayerIndex) != null
                && !doc.Layers.FindIndex(o.Attributes.LayerIndex).IsDeleted
                && doc.Layers[o.Attributes.LayerIndex].FullPath.Contains(LayerConstants.MainModellingLayer))
                .ToList();

            var ids = appObjects.Select(o => o.Id).ToList();

            if (objectsForUndelete.Count == 0)
            {
                EventLock = false;
                return;
            }

            doc.Objects.Delete(ids, true);
            doc.Views.Redraw();
            EventLock = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
            EventLock = false;
        }
    }

    private static void RunReplacementOperations(List<RhinoObject> replacedObjects) 
    {
        try
        {
            if (EventLock) return;

            RhinoDoc doc = RhinoDoc.ActiveDoc;

            if (replacedObjects.Count == 0 || HTMLViewModel.AppMode == ModellingTool.None)
                return;

            if (HTMLViewModel.AppMode == ModellingTool.Building || HTMLViewModel.AppMode == ModellingTool.AutoBuilding)
            {
                List<RhinoBuildingData> buildingData = RhinoBuildingData.FromExisting(replacedObjects).ToList();

                if (buildingData.Count == 0)
                    return;

                UpdateSelectedBuildingsPosition(buildingData);
            }
            else if (HTMLViewModel.AppMode == ModellingTool.Transit)
            {
                var roadData = RhinoRoadData.FromExisting(replacedObjects).ToList();
                var coreRoadData = RhinoRoadData.ToCoreModelCreationData(roadData).ToList();

                if (coreRoadData.Count == 0)
                    return;

                ModelBuilderPipelines.AddOrUpdateRoadPipeline(coreRoadData);
            }

            // This is actually for dividers
            else if (HTMLViewModel.AppMode == ModellingTool.LandUse 
                || HTMLViewModel.AppMode == ModellingTool.Dividers)
            {
                var validation = new DividerInputValidator(true).ValidateInput(replacedObjects);
                var rhinoDividerData = RhinoDividerData.Create(validation.ValidObjects);
                var coreDividerData = RhinoDividerData.ToCoreModelData(rhinoDividerData).ToList();

                if (coreDividerData.Count == 0)
                    return;

                ModelBuilderPipelines.AddOrUpdateParcelDividerPipeline(coreDividerData);

            }
            else if (HTMLViewModel.AppMode == ModellingTool.Zones)
            {
                var rhinoStudyData = RhinoStudyData.FromExisting(replacedObjects);
                var coreStudyData = RhinoStudyData.ToCoreModelData(rhinoStudyData).ToList();

                if (coreStudyData.Count == 0)
                    return;

                ModelBuilderPipelines.UpdateStudyAreas(RhinoDoc.ActiveDoc, coreStudyData, HTMLViewModel.AppMode);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    public static void UpdateSelectedBuildingsPosition(List<RhinoBuildingData> rhinoCreationData)
    {
        var buildingData = rhinoCreationData.Select(x => x.ToCoreModelData()).OfType<BuildingData>().ToList();
        List<Guid> ids = rhinoCreationData.Select(x => x.ID).ToList();

        RhinoDoc doc = RhinoDoc.ActiveDoc;
        ModelBuilderPipelines.DeleteBuildingPipeline(doc, buildingData, HTMLViewModel.AppMode);
        ModelBuilderPipelines.AddOrUpdateBuldingPipeline(buildingData, HTMLViewModel.AppMode);
    }

    private static void RunObjectAddOperations(List<RhinoObject> newObjects)
    {
        try
         {
             if (EventLock || newObjects.Count == 0)
                return;

            // Road needs the add event because the join command
            // on curves in rhino will not trigger a replace event
            else if (HTMLViewModel.AppMode == ModellingTool.Transit)
            {
                var result = new TransitInputValidator().ValidateInput(newObjects);
                if (result.ValidObjects.Count == 0)
                    return;

                var roadData = RhinoRoadData.FromExisting(result.ValidObjects).ToList();
                var coreRoadData = RhinoRoadData.ToCoreModelCreationData(roadData).ToList();

                if (coreRoadData.Count == 0)
                    return;

                ModelBuilderPipelines.AddOrUpdateRoadPipeline(coreRoadData);
            }
            // Landuse (ie Dividers) needs the add event because the join command
            // on curves in rhino will not trigger a replace event
            else if (HTMLViewModel.AppMode == ModellingTool.LandUse)
            {
                var result = new DividerInputValidator(true).ValidateInput(newObjects);
                if(result.ValidObjects.Count == 0)
                    return;

                var rhinoDividerData = RhinoDividerData.Create(result.ValidObjects);
                var coreDividerData = RhinoDividerData.ToCoreModelData(rhinoDividerData).ToList();

                if (coreDividerData.Count == 0)
                    return;

                ModelBuilderPipelines.AddOrUpdateParcelDividerPipeline(coreDividerData);
            }
        }
        catch(Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }
    public static void RunObjectRemovalOperations(List<RhinoObject> removedObjects)
    {
        try
        {
            List<Guid> removedIDs = removedObjects.Select(obj => obj.Id).ToList();

            if (EventLock || removedObjects.Count == 0) return;
            RhinoDoc doc = RhinoDoc.ActiveDoc;

            switch (HTMLViewModel.AppMode)
            {
                case ModellingTool.Transit:
                    var transitResult = new TransitInputValidator().ValidateInput(removedObjects);
                    var transitData = RhinoRoadData.FromExisting(transitResult.ValidObjects)
                        .Select(x => x.ToCoreModelCreationData())
                        .ToList();

                    if (transitResult.Result == ValidationResult.All_Invalid
                        || transitResult.Result == ValidationResult.EmptyInput)
                        break;

                    ModelBuilderPipelines.RemoveRoadPipeline(transitData,doc, HTMLViewModel.AppMode);
                    break;

                case ModellingTool.AutoBuilding:
                case ModellingTool.Building:
                    var buildingResult = new BuildingInputValidator(BuildingGeneratorStartPoint.Mass)
                        .ValidateInput(removedObjects);

                    if (buildingResult.Result == ValidationResult.All_Invalid
                        || buildingResult.Result == ValidationResult.EmptyInput)
                        break;

                    var buildingData = RhinoBuildingData
                        .FromExisting(buildingResult.ValidObjects)
                        .Select(x => x.ToCoreModelData())
                        .Where(x => x != null)
                        .Cast<BuildingData>()
                        .ToList();

                    ModelBuilderPipelines.DeleteBuildingPipeline(doc, buildingData, HTMLViewModel.AppMode, true);
                    break;

                case ModellingTool.Zones:
                    var result = new StudyInputValidator().ValidateInput(removedObjects);
                    if (result.ValidObjects.Count == 0)
                        return;
                    
                    ModelBuilderPipelines.DeleteStudyAreaPipeline(doc, removedIDs, HTMLViewModel.AppMode);
                    break;

                case ModellingTool.LandUse:

                    var validation = new DividerInputValidator(true).ValidateInput(removedObjects);

                    if (validation.ValidObjects.Count == 0) break;

                    var divData = RhinoDividerData.FromExisting(validation.ValidObjects)
                        .Select(x => x.ToCoreModelData())
                        .ToList();

                    ModelBuilderPipelines.DeleteParcelDividers(doc, divData, HTMLViewModel.AppMode);
                    break;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private static void OnRhinoDocObjectSelection(RhinoObjectSelectionEventArgs args)
    {
        try
        {
            RhinoDoc doc = RhinoDoc.ActiveDoc;
            var selectedIDs = doc.Objects
                .GetSelectedObjects(false, false)
                .Select(obj => obj.Id).ToList();

            if (selectedIDs.Count == 0) return;

            RhinoApp.InvokeOnUiThread(() =>
            {
                var message = SelectedObjectMetricsMessage.FromEmpty();

                switch (HTMLViewModel.AppMode)
                {
                    case ModellingTool.Transit:
                        var roads = CoreModelSingelton.Instance.Roads.Where(rd => selectedIDs.Contains(rd.CenterLineId)).ToList();
                        
                        if (roads.Count > 0)
                            message = SelectedObjectMetricsMessage.FromRoads(roads);
                        break;

                    case ModellingTool.LandUse:
                        var parcels = CoreModelSingelton.Instance.Parcels.Where(p => selectedIDs.Contains(p.ClientModelId)).ToList();

                        if (parcels.Count > 0)
                            message = SelectedObjectMetricsMessage.FromParcels(parcels);
                        break;

                    case ModellingTool.AutoBuilding:
                    case ModellingTool.Building:
                        var buildings = CoreModelSingelton.Instance.Buildings.Where(bldg => selectedIDs.Contains(bldg.EnvelopeId)).ToList();

                        if (buildings.Count > 0) 
                            message = SelectedObjectMetricsMessage.FromBuildings(buildings);
                        break;

                    case ModellingTool.Dividers:
                        var dividers = CoreModelSingelton.Instance.Dividers.Where(bldg => selectedIDs.Contains(bldg.Id)).ToList();

                        if (dividers.Count > 0)
                            message = SelectedObjectMetricsMessage.FromDividers(dividers);
                        break;

                }
                message.BroadcastMessage();
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }
}
