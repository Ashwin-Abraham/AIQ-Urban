using ArchSitePlan.BHoM.Geometry.Extensions;
using ArchSitePlan.Core.Comparer;
using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Pipeline.CreationData;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using Newtonsoft.Json;
using System.Drawing;

namespace ArchSitePlan.Core.Models
{
    public class CoreModel
    {
        private StudyArea _allStudy;
        public List<Road> Roads;
        public List<PlanarSurface> ROWs { get; set; }
        public List<Parcel> Parcels { get; set; } 
        public List<Landuse> Landuses { get; set; }
        public List<Building> Buildings { get; set; } = new List<Building>();

        [JsonIgnore]
        public List<StudyArea> Studies { get; set; }

        public List<ParcelDivider> Dividers { get; set; } = new ();
        public CoreModel() 
        {
            Roads = [];
            ROWs = [];
            Parcels = [];
            Buildings = [];
            Dividers = [];

            _allStudy = new(null, Guid.NewGuid(), CoreGeneralConstants.FullProjectStudyName, new(), new(), new());
            Studies = new List<StudyArea>() { _allStudy };

            Landuses = AppLanduses.InitialUses.ToList();
        }

        public void AddStudyArea(StudyArea studyArea) => Studies.Add(studyArea);
        public void RemoveStudyArea(StudyArea studyArea) => Studies.Remove(studyArea);

        public void RecalculateBuildingHostParcelIDs()
        {
            foreach(var building in Buildings)
            {
                var parcel = Parcels.FirstOrDefault(p => p.Boundary.ExternalBoundary.IIsContaining([building.Origin]));
                building.HostParcelCoreModelId = parcel?.CoreModelId?? Guid.Empty;
            }
        }
        internal void RefreshStudyAreaMetrics()
        {
            for (int i = 0; i < Studies.Count; i++) 
            {
                if (Studies[i].Boundary == null)
                {
                    Studies[i] = new StudyArea(Studies[i], Parcels, Buildings, Roads.ToList());
                }
                else
                {
                    var parcelsInArea = Parcels
                        .Where(p => Studies[i].Boundary.IIsContaining([p.Boundary.ControlPointCentroid()]))
                        .ToList();

                    var buildingsInArea = Buildings
                        .Where(bldg =>
                        {
                            var boundaryPt = Studies[i].Boundary.IStartPoint();
                            var bldgPt = bldg.Origin.Project(Create.Plane(boundaryPt, Vector.ZAxis));
                            return Studies[i].Boundary.IIsContaining([bldgPt]);
                        })
                        .ToList();

                    var roadsInArea = Roads.Where(road =>
                    {
                        var roadPoint = road.Centerline.PointAtParameter(0.5);
                        return Studies[i].Boundary.IIsContaining([roadPoint]);
                    }).ToList();

                    Studies[i] = new StudyArea(Studies[i], parcelsInArea, buildingsInArea, roadsInArea);
                }
            }
        }

        public bool TryAddBuilding(Mesh envelope,
            List<PlanarSurface> floorPlates,
            Guid envelopeID,
            Guid footPrintId,
            double floorToFloorHt)
        {

            if (!TryGetBuilding(footPrintId, envelopeID, out Building building))
            {
                var origin = Building.CalculateOrigin(envelope);

                var nearest = Parcels.FirstOrDefault(p => p.Boundary.ExternalBoundary.IIsContaining([origin]));
                var use = nearest != null ? nearest.LandUse : AppLanduses.Undefined;

                var newBldg = new Building(
                    envelope,
                    floorPlates,
                    floorToFloorHt,
                    envelopeID,
                    use,
                    footPrintId);
                
                Buildings.Add(newBldg);

                return true;
            }
            return false;
        }

        public bool TryGetStudyArea(Guid id, out StudyArea study)
        {
            study = Studies.Find(s => s.Id == id);
            return study != null;
        }

        public bool TryGetStudyArea(string name, out StudyArea? study)
        {
            study = Studies.Find(s => s.Name == name);
            return study != null;
        }

        public bool TryAddOrUpdateStudyArea(Polyline boundary, Guid id, string name)
        {
            if (Studies.Any(s => s.Id == id))
            {
                int index = Studies.FindIndex(s => s.Id == id);
                Studies[index] = new StudyArea(boundary, id, name, Parcels, Buildings, Roads.ToList());
            }
            else
                Studies.Add(new StudyArea(boundary, id, name, Parcels, Buildings, Roads.ToList()));
            return true;
        }

        public bool TryFindRoad(Guid id, out Road road)
        {
            road = Roads.FirstOrDefault(r => r.CenterLineId == id);
            return road != null;
        }

        public bool TryRemoveBuilding(Guid id)
        {
            int count = Buildings.RemoveAll(b => b.FootprintId == id || b.EnvelopeId == id);
            
            return count > 0;
        }

        public bool TryGetBuilding(Guid footPrintId, Guid massId, out Building? building)
        {
            building = Buildings.FirstOrDefault(b =>
            (footPrintId != Guid.Empty && b.FootprintId == footPrintId)
            || (massId != Guid.Empty && b.EnvelopeId == massId));
            return building != null;
        }

        public List<Building> FindBuildingsInParcels(List<Parcel> parcels)
        {
            var parcelGuids = parcels.Select(p => p.CoreModelId).ToList();
            var buildings = Buildings.Where(b => parcelGuids.Contains(b.HostParcelCoreModelId)).ToList();

            return buildings;
        }

        public void ClearModel()
        {
            Roads = new();
            ROWs = new();
            Buildings = new();
            Parcels= new();
            Dividers= new();
            Landuses = AppLanduses.InitialUses.ToList();
            ResetStudyAreas();
        }

        public void ResetStudyAreas()
        {
            Studies = new();
            AddStudyArea(new StudyArea(
                null,
                Guid.NewGuid(),
                CoreGeneralConstants.FullProjectStudyName,
                new List<Parcel>(),
                new List<Building>(),
                new List<Road>()));
        }

        

        public StudyArea ProjectWideStudy()
            => Studies.FirstOrDefault(s => s.Name == CoreGeneralConstants.FullProjectStudyName);

        public bool TryGetParcelDivider(Guid objectId, out ParcelDivider divider)
        {
            divider = Dividers.FirstOrDefault(div => div.Id == objectId);
            return divider != null;
        }

        public Dictionary<double, Color> SetRoadColorsByWidth()
        {
            var widths = Roads
                .GroupBy(r => r.Width, new DoubleAsDecimalComparer())
                .Select(x => x.Key)
                .OrderBy(x => x)
                .ToList();

            Dictionary<double, Color> widthColors = new();
            for (int i = 0; i < widths.Count; i++)
            {
                int cIndex = Math.Min(i, AppColours.StandardColors.Count - 1);
                var color = AppColours.StandardColors[cIndex];
                widthColors.Add(widths[i], AppColours.StandardColors[cIndex]);
            }

            return widthColors;
        }

        public Landuse GetUndefinedUse()
        {
            var undefinedTag = AppLanduses.Undefined;
            return Landuses.First(
                lu => lu.Name == undefinedTag.Name
                || lu.Id == undefinedTag.Id);
        }

        internal Landuse GetUse(LanduseData useData)
        {
            var use = Landuses.First(
                lu => lu.Name == useData.Name
                || lu.Id == useData.Id);

            return use == null? GetUndefinedUse() : use;
        }
    }
}
