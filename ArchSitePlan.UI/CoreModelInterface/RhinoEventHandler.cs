using ArchSitePlan.Core.Metrics;
using ArchSitePlan.Core.Models;
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
            EventHandler<RhinoObjectEventArgs> addRhinoObjectEvent = (sender, args) => OnRhinoDocObjectAddition(args);
            RhinoDoc.AddRhinoObject += addRhinoObjectEvent;

            EventHandler<RhinoObjectEventArgs> removedRhinoObjectEvent = (sender, args) => OnRhinoDocObjectRemove(args);
            RhinoDoc.DeleteRhinoObject += removedRhinoObjectEvent;

            EventHandler<RhinoObjectSelectionEventArgs> selectedRhinoObjectsEvent = (sender, args) => OnRhinoDocObjectSelection(args);
            RhinoDoc.SelectObjects += selectedRhinoObjectsEvent;

            //ViewModel.LandUseGraphData = ViewModel.ComputeLandUseGraphData(CoreModel.ProjectMetrics);
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

                    foreach( var obj in srfObjs)
                    {
                        RhinoToCoreModel.TryUpdateParcelUse(obj, ViewModel.SelectedLandUse);
                        CoreModelToRhino.OutputParcelsToRhino(RhinoDoc.ActiveDoc);
                    }
                
                    ViewModel.UpdateDisplayedStudyAreaData();
                }

                else if(ViewModel.AppMode == AppMode.Building)
                {
                    // Update view model with current building data
                    var crvRhinoObjs = args.RhinoObjects
                                            .Where(obj => obj.ObjectType == ObjectType.Curve)
                                            .Where(obj => (obj.Geometry as Curve).IsClosed)
                                            .ToList();

                    var brepRhinoObjs = args.RhinoObjects
                        .Where(obj => obj.ObjectType == ObjectType.Brep)
                        .ToList();

                    var rhinoObjs = ViewModel.BuildingSelectorMode == BuildingSelectorMode.Footprint? crvRhinoObjs: brepRhinoObjs;
                    var rhinoObjsGuids = rhinoObjs.Select(b => b.Id).ToList();

                    var building = CoreModel.Buildings.FirstOrDefault(b => rhinoObjsGuids.Contains(b.Id));

                    if (building == null) return;

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

        public static void OnRhinoDocObjectAddition(RhinoObjectEventArgs args)
        {
            // Check if object was modified
            if (_lastDeletedId != args.ObjectId) return;
         
            if(ViewModel.AppMode == AppMode.Road)
            {
                RhinoToCoreModel.AddRoad(args.TheObject, ViewModel.RoadWidth);
                CoreModelToRhino.OutputCoreModelToRhino(RhinoDoc.ActiveDoc);

                var activeStudyMetric = CoreModel.Studies.FirstOrDefault(s => s.Name == ViewModel.ActiveStudyAreaName);
                ViewModel.UpdateDisplayedStudyAreaData();
            }
        }

        public static void OnRhinoDocObjectRemove(RhinoObjectEventArgs args)
        {
             if (args.TheObject.IsDeleted && ViewModel.AppMode == AppMode.Road)
            {
                if(CoreModel.TryRemoveRoad(args.TheObject.Id))
                {
                    _lastDeletedId = args.TheObject.Id;

                    var activeStudyMetric = CoreModel.Studies.FirstOrDefault(s => s.Name == ViewModel.ActiveStudyAreaName);
                    ViewModel.UpdateDisplayedStudyAreaData();
                    CoreModelToRhino.OutputCoreModelToRhino(RhinoDoc.ActiveDoc);
                }
            }
        }

        internal static void UpdateSelectedBuildings(List<RhinoObject> rhinoObjs, 
            ObjectType requiredType, 
            int floorCount, 
            double floorToFloorHt)
        {
            foreach (var obj in rhinoObjs)
            {
                bool buildingExists = CoreModel.Buildings.Any(b => b.Id == obj.Id);
                var footPrint = GetFootPrint(obj);
                Building building = null;

                if (!buildingExists)
                {
                    CoreModel.TryAddBuilding(obj.Id, footPrint, floorCount, floorToFloorHt);
                    building = CoreModel.Buildings.Find(b => b.Id == obj.Id);
                }
                else
                {
                    building = CoreModel.Buildings.Find(b => b.Id == obj.Id);
                    CoreModelToRhino.DeleteRhinoBuilding(building);
                    CoreModel.UpdateBuilding(obj.Id, footPrint, floorCount, floorToFloorHt);
                }

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
