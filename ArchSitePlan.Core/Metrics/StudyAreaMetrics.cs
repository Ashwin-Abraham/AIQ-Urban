using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Models;
using BH.Engine.Data;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Metrics
{
    public class StudyAreaMetrics
    {
        public double BoundedArea { get; private set; } = 25;
        public double BoundedArea_Display { get; private set; } = 25;
        public double FAR { get; private set; } = 12;
        public double FAR_Display { get; private set; } = 12;
        public double DevelopableArea { get; private set; } = 5;
        public double DevelopableArea_Display { get; private set; } = 5;
        public double BuiltArea { get; private set; } = 2;
        public double BuiltArea_Display { get; private set; } = 2;
        public List<ParcelMetrics> ParcelMetrics = new();
        public List<TransitMetrics> TransitMetrics = new();
        private void UpdateParcelMetrics(ICurve boundary, 
            List<Parcel> parcels, 
            List<Building> buildings,
            List<Road> transitRoutes)
        {
            CalculateParcelMetrics(parcels, buildings);
            CalculateTransitMetrics(transitRoutes);

            BoundedArea = Math.Abs(boundary?.IArea()?? 0);
            DevelopableArea = ParcelMetrics.Sum(p => p.Area);
            BuiltArea = buildings.SelectMany(b => b.FloorPlates)
                .Sum(flr => Math.Abs(flr.Profile.Area()));
            FAR = BuiltArea / DevelopableArea;

            PopulateDisplayAreas();
        }

        private void CalculateParcelMetrics(List<Parcel> parcels, List<Building> buildings)
        {
            ParcelMetrics = new();

            foreach (Parcel p in parcels)
            {
                var buildingsOnParcel = buildings
                    .Where(b => p.Boundary.ExternalBoundary.IIsContaining([b.Origin]))
                    .ToList();

                ParcelMetrics.Add(new ParcelMetrics(p, buildingsOnParcel));
            }
        }

        private void CalculateTransitMetrics(List<Road> transitRoutes)
        {
            var groupedByWidth = transitRoutes.GroupBy(b =>  Math.Round(b.Width, CoreGeneralConstants.TransitRouteWidthPrecision));
            TransitMetrics.Clear();

            foreach(var roadType in groupedByWidth)
            {
                TransitMetrics.Add(new TransitMetrics(roadType));
            }    
        }

        private void PopulateDisplayAreas()
        {
            BoundedArea_Display = BoundedArea.Round();
            FAR_Display = FAR.Round(CoreGeneralConstants.DecimalPts);
            DevelopableArea_Display = DevelopableArea.Round();
            BuiltArea_Display = BuiltArea.Round();
        }

        public StudyAreaMetrics(
            ICurve boundary, 
            List<Parcel> parcels, 
            List<Building> buildings,
            List<Road> transitRoutes)
            //List<Building> buildings, 
            //List<Road> roads,
            //List<PlanarSurface> roadRoWs) 
        {
            UpdateParcelMetrics(boundary, parcels, buildings, transitRoutes);
        }
    }
}
