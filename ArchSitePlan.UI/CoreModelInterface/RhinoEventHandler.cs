using ArchSitePlan.Core.Models;
using Rhino;
using Rhino.DocObjects;
using System.Windows;
using static ArchSitePlan.Core.Constants.Enums;
using static ArchSitePlan.UI.Constants.Enums;

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
        }
        public static void ActivateRoadSelectionFilter(bool activate)
        {
            Rhino.ApplicationSettings.SelectionFilterSettings.OneShotGeometryFilter = ObjectType.Curve;
        }
        private static void OnRhinoDocObjectSelection(RhinoObjectSelectionEventArgs args)
        {
            try
            {
                if (ViewModel.SelectionMode == RhinoSelectionMode.Road)
                {
                    var crvObjs = args.RhinoObjects
                        .Where(obj => obj.ObjectType == ObjectType.Curve)
                        .ToList();

                    crvObjs.ForEach(obj => CoreModelInput.AddRoad(obj, ViewModel.RoadWidth));
                    OutputFromCoreModel.OutputCoreModelToRhino(RhinoDoc.ActiveDoc);
                }

                if(ViewModel.SelectionMode == RhinoSelectionMode.Parcel)
                {
                    var srfObjs = args.RhinoObjects
                        .Where(obj => obj.ObjectType == ObjectType.Brep)
                        .ToList();

                    foreach( var obj in srfObjs)
                    {
                        CoreModelInput.TryUpdateParcelUse(obj, ViewModel.SelectedLandUse);
                        OutputFromCoreModel.UpdateRhinoParcel(obj);
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }

        public static void OnRhinoDocObjectAddition(RhinoObjectEventArgs args)
        {
            if (_lastDeletedId != args.ObjectId) return;
         
            if(ViewModel.SelectionMode == RhinoSelectionMode.Road)
            {
                CoreModelInput.AddRoad(args.TheObject, ViewModel.RoadWidth);
                OutputFromCoreModel.OutputCoreModelToRhino(RhinoDoc.ActiveDoc);
            }
        }

        public static void OnRhinoDocObjectRemove(RhinoObjectEventArgs args)
        {
             if (args.TheObject.IsDeleted && ViewModel.SelectionMode == RhinoSelectionMode.Road)
            {
                if(CoreModel.TryRemoveRoad(args.TheObject.Id))
                    _lastDeletedId = args.TheObject.Id;
            }
        }
    }
}
