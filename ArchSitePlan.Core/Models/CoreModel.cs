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

        public List<Road> Roads { get; set; }
        public List<PlanarSurface> ROWs { get; set; }
        public List<Parcel> Parcels { get; set; } 
        public List<Landuse> Landuses { get; set; }
        public List<Building> Buildings { get; set; }

        [JsonIgnore]
        public List<StudyArea> Studies { get; set; }

        public List<ParcelDivider> Dividers { get; set; }
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
