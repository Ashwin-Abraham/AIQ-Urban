using ArchSitePlan.Core.Models;
using ArchSitePlan.RhinoInterface.Constants;
using ArchSitePlan.RhinoInterface.Helpers;
using ArchSitePlan.RhinoInterface.InputValidation.Results;
using Rhino;
using Rhino.DocObjects;

namespace ArchSitePlan.RhinoInterface.InputValidation.Validators;
public class UseChangeValidator : IRhinoInputValidator
{
    public GeneralValidationResult ValidateInput(List<RhinoObject> rhinoObjs)
    {
        string message = string.Empty;
        int originalCount = rhinoObjs.Count;
        int removedBuildings = 0;
        int removedCount = 0;
        var doc = RhinoDoc.ActiveDoc;

        if (rhinoObjs.Count == 0)
            return new(new(), ValidationResult.EmptyInput, "Nothing Selected");

        var rhinoIds = rhinoObjs.Select(x => x.Id).ToList();

        // If both a building envelope and its floorplates are selected, remove the floorplates from the selection
        var selectedFloorPlates =
            CoreModelSingelton.Instance.Buildings.SelectMany(x => x.FloorPlates)
            .Where(x => rhinoIds.Contains(x.Id))
            .ToList();

        var selectedBuildings
            = CoreModelSingelton.Instance.Buildings
            .Where(bldg => rhinoIds.Contains(bldg.EnvelopeId))
            .ToList();

        var forbiddenBuildingIds = selectedBuildings
            .Where(x => x.FloorPlates.Any(fp => selectedFloorPlates.Contains(fp)))
            .Select(x => x.EnvelopeId)
            .ToList();

        removedBuildings += rhinoObjs.RemoveAll(obj => forbiddenBuildingIds.Contains(obj.Id));
        if (removedBuildings > 0)
            message += $"\nSkipped editing {removedBuildings} building(s) whose floorplates' use was also being changed";

        // Early exit if layers dont exist in the document
        var buildingFloorPlateLayerIndices = LayerHelpers.GetBuildingOutlineRhinoLayerIndices(doc);
        int buildingEnvelopeLayerIndex =doc.Layers.FindByFullPath(LayerConstants.BuildingMassLayer, LayerConstants.DefaultLayerIndex);
        var parcelLayerIndices = LayerHelpers.GetParcelRhinoLayerIndices(doc);

        if(buildingEnvelopeLayerIndex == LayerConstants.DefaultLayerIndex)
            removedCount += rhinoObjs.RemoveAll(x => x.ObjectType == ObjectType.Mesh);

        if(buildingFloorPlateLayerIndices.Count == 0)
            removedCount += rhinoObjs.RemoveAll(x => x.ObjectType == ObjectType.Curve);

        if(parcelLayerIndices.Count == 0)
            removedCount += rhinoObjs.RemoveAll(x => x.ObjectType == ObjectType.Brep || x.ObjectType == ObjectType.Surface);

        // Validate each object
        var objectsToRemove = new List<RhinoObject>();
        foreach (var obj in rhinoObjs)
        {
            var layerIndex = obj.Attributes.LayerIndex;

            switch (obj.ObjectType)
            {
                // Building envelope validation
                case ObjectType.Mesh:
                    if(layerIndex != buildingEnvelopeLayerIndex)
                    {
                        removedCount++;
                        objectsToRemove.Add(obj);
                    }
                    break;

                // Floorplate validation
                case ObjectType.Curve:
                    if (!buildingFloorPlateLayerIndices.Contains(layerIndex))
                    {
                        removedCount++;
                        objectsToRemove.Add(obj);
                    }
                    break;

                // Parcel validation
                case ObjectType.Brep:
                case ObjectType.Surface:
                    if(!parcelLayerIndices.Contains(layerIndex))
                    {
                        removedCount++;
                        objectsToRemove.Add(obj);
                    }
                    break;

                default:
                    removedCount++;
                    objectsToRemove.Add(obj);
                    continue;
            }
        }

        // Finalise results and return
        rhinoObjs.RemoveAll(objectsToRemove.Contains);

        if (removedCount > 0)
            message += $"\nSkipped {removedCount} objects as they are not parcels, floorplates or buildings";

        var validationResult = IRhinoInputValidator.GetValidationResult(originalCount, removedCount);

        return new(rhinoObjs, validationResult, message.Trim());
    }
}
