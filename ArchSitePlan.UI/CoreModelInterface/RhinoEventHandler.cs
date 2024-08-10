using ArchSitePlan.Core.Models;
using Rhino;
using Rhino.DocObjects;
using System.Windows;
using static ArchSitePlan.UI.Constants.Enums;

namespace ArchSitePlan.UI.Actions
{
    public static class RhinoEventHandler
    {
        private static Guid _lastDeletedId = Guid.Empty;
        private static RhinoSelectionMode _selectionMode = RhinoSelectionMode.None;
        public static double ActiveRoadWidth = 3;
        private static List<Guid> _selectedParcels = new List<Guid>();

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
                if (_selectionMode == RhinoSelectionMode.Road)
                {
                    var crvObjs = args.RhinoObjects
                        .Where(obj => obj.ObjectType == ObjectType.Curve)
                        .ToList();

                    crvObjs.ForEach(obj => CoreModelInput.AddRoad(obj, ActiveRoadWidth));
                    OutputFromCoreModel.OutputCoreModelToRhino(RhinoDoc.ActiveDoc);
                }

                if(_selectionMode == RhinoSelectionMode.Parcel)
                {
                    var srfObjs = args.RhinoObjects
                        .Where(obj => obj.ObjectType == ObjectType.Surface)
                        .ToList();

                    srfObjs.ForEach(obj => CoreModelInput.AddRoad(obj, ActiveRoadWidth));
                    OutputFromCoreModel.OutputCoreModelToRhino(RhinoDoc.ActiveDoc);
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
         
            CoreModelInput.AddRoad(args.TheObject, ActiveRoadWidth);
            OutputFromCoreModel.OutputCoreModelToRhino(RhinoDoc.ActiveDoc);
        }

        public static void OnRhinoDocObjectRemove(RhinoObjectEventArgs args)
        {
             if (args.TheObject.IsDeleted)
            {
                if(CoreModel.TryRemoveRoad(args.TheObject.Id))
                    _lastDeletedId = args.TheObject.Id;
            }
        }

        public static void OnIdle(object sender, EventArgs e)
        {
            //For example you want to get the list of all selected curves
            var selectedCurveObjects = RhinoDoc.ActiveDoc.Objects.GetObjectsByType<RhinoObject>(new ObjectEnumeratorSettings() { ObjectTypeFilter = ObjectType.Curve, SelectedObjectsFilter = true }).ToList();
            //Update your lists 
        }

        internal static void ChangeSelectionMode(RhinoSelectionMode mode)
        {
            _selectionMode = mode;
        }
    }
}
