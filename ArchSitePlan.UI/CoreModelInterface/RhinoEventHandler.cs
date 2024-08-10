using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Singletons;
using Rhino;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ArchSitePlan.UI.Actions
{
    public static class RhinoEventHandler
    {
        private static Guid _lastDeletedId = Guid.Empty;
        public static bool RoadCLSelectionModeActive = false;
        public static double ActiveRoadWidth =3;

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
                if (RoadCLSelectionModeActive)
                {
                    var crvObjs = args.RhinoObjects
                        .Where(obj => obj.ObjectType == ObjectType.Curve)
                        .ToList();

                    crvObjs.ForEach(obj => CoreModelInput.AddRoad(obj, ActiveRoadWidth));
                    OutputFromCoreModel.OutputRoadsToRhino(RhinoDoc.ActiveDoc, Singletons.RoadNetwork);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }

        public static void OnRhinoDocObjectAddition(RhinoObjectEventArgs args)
        {
            if (_lastDeletedId == args.ObjectId)
            {
                CoreModelInput.AddRoad(args.TheObject, ActiveRoadWidth);
                OutputFromCoreModel.OutputRoadsToRhino(RhinoDoc.ActiveDoc, Singletons.RoadNetwork);
            }
        }

        public static void OnRhinoDocObjectRemove(RhinoObjectEventArgs args)
        {
             if (args.TheObject.IsDeleted)
            {
                if(Singletons.RoadNetwork.TryRemoveRoad(args.TheObject.Id))
                    _lastDeletedId = args.TheObject.Id;
            }
        }

        public static void OnIdle(object sender, EventArgs e)
        {
            //For example you want to get the list of all selected curves
            var selectedCurveObjects = RhinoDoc.ActiveDoc.Objects.GetObjectsByType<RhinoObject>(new ObjectEnumeratorSettings() { ObjectTypeFilter = ObjectType.Curve, SelectedObjectsFilter = true }).ToList();
            //Update your lists 
        }
    }
}
