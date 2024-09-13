using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Metrics;
using ArchSitePlan.Core.Models;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.Models;
using ArchSitePlan.UI.RhinoBHoMInterop;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Windows;
using static ArchSitePlan.Core.Constants.Enums;
using static ArchSitePlan.UI.Constants.Enums;
using Mesh = BH.oM.Geometry.Mesh;
using Vector = BH.oM.Geometry.Vector;

namespace ArchSitePlan.UI.Actions
{
    public static class RhinoEventHandler
    {
        private static Guid _lastDeletedId = Guid.Empty;
        private static List<Guid> _selectedParcels = new List<Guid>();
        public static MainViewModel ViewModel;
        static RhinoEventHandler()
        {
            EventHandler<RhinoReplaceObjectEventArgs> replaceRhinoObjectEvent = (sender, args) => OnRhinoDocObjectReplace(args);
            RhinoDoc.ReplaceRhinoObject += replaceRhinoObjectEvent;

            EventHandler<RhinoObjectEventArgs> addRhinoObjectEvent = (sender, args) => OnRhinoDocObjectAddition(args);
            RhinoDoc.AddRhinoObject += addRhinoObjectEvent;

            EventHandler<RhinoObjectEventArgs> removedRhinoObjectEvent = (sender, args) => OnRhinoDocObjectRemove(args);
            RhinoDoc.DeleteRhinoObject += removedRhinoObjectEvent;

            EventHandler<RhinoObjectSelectionEventArgs> selectedRhinoObjectsEvent = (sender, args) => OnRhinoDocObjectSelection(args);
            RhinoDoc.SelectObjects += selectedRhinoObjectsEvent;

            //ViewModel.LandUseGraphData = ViewModel.ComputeLandUseGraphData(CoreModel.ProjectMetrics);
        }

        private static void OnRhinoDocObjectReplace(RhinoReplaceObjectEventArgs args)
        {
            RhinoDoc doc = RhinoDoc.ActiveDoc;
            RhinoObject obj = args.NewRhinoObject;

            if (ViewModel.AppMode == AppMode.Building)
            {

                if (!(obj.ObjectType == ObjectType.Curve 
                    || obj.ObjectType == ObjectType.Brep
                    || obj.ObjectType == ObjectType.Extrusion
                    || obj.ObjectType == ObjectType.Mesh)) return;

                CoreModelToRhino.ToggleLockModelLayers(doc, false);

                if (!CoreModel.TryGetBuilding(obj.Id, obj.Id, out Building building)) return;

                UpdateSelectedBuildings(obj.ObjectToList(),
                        building.FloorCount,
                        building.FloorToFloorHeight);

                CoreModel.RefreshStudyAreaMetrics();
                ViewModel.RefreshActiveStudy();
                //CoreModelToRhino.OutputCoreModelToRhino(doc);
                CoreModelToRhino.ToggleLayerStateBasedOnAppMode(doc, AppMode.Building);
            }
            else if(ViewModel.AppMode == AppMode.Road)
            {
                CoreModelToRhino.ToggleLockModelLayers(doc, false);
                RhinoToCoreModel.AddRoad(obj, ViewModel.RoadWidth);
                CoreModelToRhino.OutputCoreModelToRhino(RhinoDoc.ActiveDoc);
                CoreModel.RefreshStudyAreaMetrics();
                ViewModel.RefreshActiveStudy();
                var activeStudyMetric = CoreModel.Studies.FirstOrDefault(s => s.Name == ViewModel.ActiveStudyAreaName);
                ViewModel.RefreshActiveStudy();
                CoreModelToRhino.ToggleLayerStateBasedOnAppMode(doc, AppMode.Road);
            }
        }

        public static void ActivateRoadSelectionFilter(bool activate)
        {
            Rhino.ApplicationSettings.SelectionFilterSettings.OneShotGeometryFilter = ObjectType.Curve;
        }
        private static void OnRhinoDocObjectSelection(RhinoObjectSelectionEventArgs args)
        {
            try
            {
                if (ViewModel.AppMode == AppMode.Road)
                {
                    var crvObjs = args.RhinoObjects
                        .Where(obj => obj.ObjectType == ObjectType.Curve)
                        .ToList();

                    // Set active width as first roads width
                    foreach (var crvObj in crvObjs)
                    {
                        bool roadExists = RhinoToCoreModel.TryFindRoad(crvObj.Id, out Road road);
                        if(roadExists)
                        {
                            ViewModel.RoadWidth = road.Width;
                            break;
                        }
                    }

                    // crvObjs.ForEach(obj => RhinoToCoreModel.AddRoad(obj, ViewModel.RoadWidth));
                    // CoreModelToRhino.OutputCoreModelToRhino(RhinoDoc.ActiveDoc);
                }

                else if(ViewModel.AppMode == AppMode.LandUse)
                {
                    var srfObjs = args.RhinoObjects
                        .Where(obj => obj.ObjectType == ObjectType.Brep)
                        .ToList();

                    if(srfObjs.Any())
                        UpdateSelectedParcelsMetrics(srfObjs);
                }

                else if(ViewModel.AppMode == AppMode.Building)
                {
                    // Update view model with current building data
                    var crvRhinoObjs = args.RhinoObjects
                                            .Where(obj => obj.ObjectType == ObjectType.Curve)
                                            .Where(obj => (obj.Geometry as Curve).IsClosed)
                                            .ToList();

                    var envelopeRhinoObjs = args.RhinoObjects
                        .Where(obj => obj.ObjectType == ObjectType.Brep 
                            || obj.ObjectType == ObjectType.Extrusion
                            || obj.ObjectType == ObjectType.Mesh)
                        .ToList();

                    var rhinoObjs = ViewModel.BuildingSelectorMode == BuildingSelectorMode.Footprint? crvRhinoObjs: envelopeRhinoObjs;
                    var firstObj = rhinoObjs.FirstOrDefault(b => b.Id != Guid.Empty);

                    Building building = null;
                    if (firstObj == null || !CoreModel.TryGetBuilding(firstObj.Id, firstObj.Id, out building))
                        return;

                    ViewModel.ActiveBuildingFloorCount = building.FloorCount;
                    ViewModel.ActiveBuildingFloorHeight = building.FloorToFloorHeight;
                }

                else if(ViewModel.AppMode != AppMode.None)
                {
                    //var activeStudyMetric = CoreModel.StudiesAndMetrics.FirstOrDefault(s => s.Study.Name == ViewModel.ActiveStudyAreaName);
                    //ViewModel.UpdateDisplayedStudyAreaData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }

        private static void UpdateSelectedParcelsMetrics(List<RhinoObject> selectedBreps)
        {
            if(selectedBreps == null || selectedBreps.Count == 0) return;

            RhinoToCoreModel.RecalculateBuildingHostParcelIDs();

            // Check that they are parcels
            var guids = selectedBreps.Select(obj => obj.Id).ToList();
            var parcels = RhinoToCoreModel.GetParcel(guids).ToList();
            var containedBuildings = RhinoToCoreModel.FindBuildingsInParcels(parcels);

            // Aggregate metrics
            double parcelsArea = parcels.Sum(p => p.Area());
            double parcelsBuiltArea = containedBuildings.Sum(b => b.BUA());
            double parcelsCoverage = containedBuildings.Sum(b => b.FootprintArea()) / parcelsArea * 100;
            double parcelsFAR = parcelsBuiltArea / parcelsArea;

            string use;
            if (parcels.GroupBy(p => p.LandUse).Count() > 1)
                use = "Mixed";
            else
                use = parcels.First().LandUse.ToString();

            // Change viewmodel variables
            ViewModel.ActiveParcelMetrics = new VMActiveParcelsDetails(
                Use: use,
                TotalArea: Math.Round(parcelsArea).ToString(),
                BUA: Math.Round(parcelsBuiltArea).ToString(),
                CoveragePctg: Math.Round(parcelsCoverage, 1).ToString(),
                FAR: Math.Round(parcelsFAR, 2).ToString());
        }

        public static void OnRhinoDocObjectAddition(RhinoObjectEventArgs args)
        {
            //RhinoDoc doc = RhinoDoc.ActiveDoc;

            //// Check if object was modified
            //if (_lastDeletedId != args.ObjectId) return;
         
            //if(ViewModel.AppMode == AppMode.Road)
            //{
            //    RhinoToCoreModel.AddRoad(args.TheObject, ViewModel.RoadWidth);
            //    CoreModelToRhino.OutputCoreModelToRhino(RhinoDoc.ActiveDoc);

            //    var activeStudyMetric = CoreModel.Studies.FirstOrDefault(s => s.Name == ViewModel.ActiveStudyAreaName);
            //    ViewModel.UpdateDisplayedStudyAreaData();
            //}
        }

        public static void OnRhinoDocObjectRemove(RhinoObjectEventArgs args)
        {
            RhinoDoc doc = RhinoDoc.ActiveDoc;

            if(!args.TheObject.IsDeleted) return;

            if (ViewModel.AppMode == AppMode.Road)
            {
                if(CoreModel.TryRemoveRoad(args.TheObject.Id))
                {
                    _lastDeletedId = args.TheObject.Id;

                    var activeStudyMetric = CoreModel.Studies.FirstOrDefault(s => s.Name == ViewModel.ActiveStudyAreaName);
                    ViewModel.RefreshActiveStudy();
                    CoreModelToRhino.OutputCoreModelToRhino(doc);
                }
            }
            else if(ViewModel.AppMode== AppMode.Building)
            {
                if (!CoreModel.TryGetBuilding(args.ObjectId, args.ObjectId, out Building building))
                    return;

                CoreModelToRhino.ToggleLockModelLayers(doc, false);
                CoreModelToRhino.TryDeleteBakedBuilding(args.ObjectId);
                RhinoToCoreModel.TryRemoveBuilding(args.ObjectId);
                //ViewModel.UpdateDisplayedStudyAreaData();
                //CoreModelToRhino.OutputCoreModelToRhino(doc);
                CoreModelToRhino.ToggleLayerStateBasedOnAppMode(doc, AppMode.Building);
            }
        }

        internal static void UpdateSelectedBuildings(List<RhinoObject> rhinoObjs, 
            int floorCount, 
            double floorToFloorHt)
        {
            RhinoDoc doc = RhinoDoc.ActiveDoc;
            foreach (var obj in rhinoObjs)
            {
                bool buildingExists = CoreModel.TryGetBuilding(obj.Id, obj.Id, out Building building);
                Guid footPrintID = Guid.Empty;
                Guid envelopeID = Guid.Empty;
                
                if (buildingExists)
                    CoreModelToRhino.TryDeleteBakedBuilding(obj.Id);

                CoreModel.TryRemoveBuilding(obj.Id);
                Rhino.Geometry.Mesh rhinoMesh = null;

                if(obj.ObjectType == ObjectType.Mesh)
                {
                    envelopeID = obj.Id;
                    rhinoMesh = obj.Geometry as Rhino.Geometry.Mesh;
                }
                else if (obj.ObjectType == ObjectType.Brep)
                {
                    envelopeID = obj.Id;
                    rhinoMesh = GeometryHelpers.ConvertToMesh(obj.Geometry as Rhino.Geometry.Brep);
                }
                else if(obj.ObjectType == ObjectType.Extrusion)
                {
                    envelopeID = obj.Id;
                    rhinoMesh = GeometryHelpers.ConvertToMesh(obj.Geometry as Rhino.Geometry.Extrusion);
                }
                else if (obj.ObjectType == ObjectType.Curve)
                {
                    footPrintID = obj.Id;
                    var extrusion = Rhino.Geometry.Extrusion.Create(
                        obj.Geometry as Curve,
                        Rhino.Geometry.Plane.WorldXY,
                        floorCount * floorToFloorHt,
                        true);

                    rhinoMesh = GeometryHelpers.ConvertToMesh(extrusion);
                }
                else
                {
                    continue;
                }

                var floorPlates = GeometryHelpers
                    .GetContours(rhinoMesh, floorToFloorHt)
                    .Select(c => c.ToBHoM())
                    .Select(c => new PlanarSurface(c, new List<ICurve>()))
                    .ToList();

                var bhomMesh  = rhinoMesh.ToBHoM();
                CoreModel.TryAddBuilding(bhomMesh,
                    floorPlates,
                    envelopeID,
                    footPrintID,
                    floorToFloorHt);

                CoreModel.TryGetBuilding(footPrintID, envelopeID,out building);
                //CoreModelToRhino.TryDeleteBakedBuilding(building.FootprintId, building.EnvelopeId);
                CoreModelToRhino.BakeRhinoBuilding(building);
                
            }
        }

        private static PlanarSurface GetFootPrint(RhinoObject crv)
        {
            var curve = crv.Geometry as Curve;
            BH.oM.Geometry.Polyline bhom = null;

            if (curve != null && curve.TryGetPolyline(out Rhino.Geometry.Polyline polyline)
                && polyline.IsClosed)
                bhom = polyline.ToBHoM();
            else
                return null;

            bhom = bhom.IsClockwise(Vector.ZAxis) ? bhom : bhom.Flip();

            bool clockWise = bhom.IsClockwise(Vector.ZAxis);

            PlanarSurface footPrint = Create.PlanarSurface(bhom);
            return footPrint;
        }
    }
}
