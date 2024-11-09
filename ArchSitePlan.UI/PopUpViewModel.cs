using ArchSitePlan.Core.Models;
using BH.Engine.Geometry;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.UI
{
    public class PopUpViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private static readonly int _roundingDigits = 2;

        public static void ConfigureRoadsInPopUp(TestPopUp popUp, 
            List<Road> roads)
        {
            bool singleWidth = roads.GroupBy(r => r.RoW.Area(3)).Count() == 1;

            // Title
            popUp.Title.Text = "ROAD DETAILS";
            popUp.SelectionCount.Text = roads.Count.ToString();

            // Data 1
            popUp.DataTitle1.Text = "Width";
            if (singleWidth)
                popUp.DataContent1.Text = Math.Round(roads[0].Width, _roundingDigits).ToString();
            else
                popUp.DataContent1.Text = "varies";

            // Data 2
            popUp.DataTitle2.Text = "Length";
            double totalLength = roads.Sum(r => r.Centerline.Length());
            popUp.DataContent2.Text = Math.Round(totalLength, _roundingDigits).ToString();

            // Data 3
            popUp.DataTitle3.Text = string.Empty;
            popUp.DataContent3.Text = string.Empty;

            // Data 4
            popUp.DataTitle4.Text = "Area";
            double area = roads.Sum(r => r.RoW.Area());
            popUp.DataContent4.Text = Math.Round(area, _roundingDigits).ToString();



            // Data 5
            popUp.DataTitle5.Text = string.Empty;
            popUp.DataContent5.Text = string.Empty;

            // Data 6
            popUp.DataTitle6.Text = string.Empty;
            popUp.DataContent6.Text = string.Empty;
        }

        internal static void ConfigureBuildingsInPopUp(TestPopUp popUp, List<Building> buildings)
        {
            // Title
            popUp.Title.Text = "Building Details";
            popUp.SelectionCount.Text = buildings.Count.ToString();

            // Data 1
            var uses = buildings.Select(b => b.FindHostParcelUse())
                .ToList();
            bool singleUse = uses.Count == 1;
         
            popUp.DataTitle1.Text = "Use";
            if (singleUse)
                popUp.DataContent1.Text = buildings[0].FindHostParcelUse().ToString();
            else
                popUp.DataContent1.Text = "varies";

            // Data 2
            popUp.DataTitle2.Text = "Built Area";
            double bua = buildings.Sum(bldg => bldg.BUA());
            popUp.DataContent2.Text = Math.Round(bua, _roundingDigits).ToString();

            // Data 3
            popUp.DataTitle3.Text = string.Empty;
            popUp.DataContent3.Text = string.Empty;

            // Data 4
            popUp.DataTitle4.Text = "Footprint Area";
            double footprint = buildings.Sum(bldg => bldg.FootprintArea());
            popUp.DataContent4.Text = Math.Round(footprint, _roundingDigits).ToString();

            // Data 5
            popUp.DataTitle5.Text = string.Empty;
            popUp.DataContent5.Text = string.Empty;

            // Data 6
            popUp.DataTitle6.Text = string.Empty;
            popUp.DataContent6.Text = string.Empty;
        }

        internal static void ConfigureLandUseInPopUp(TestPopUp popUp, List<Parcel> parcels)
        {
            bool singleUse = parcels.GroupBy(p => p.LandUse).Count() == 1;
            int roundingDigits = 2;

            // Title
            popUp.Title.Text = "Parcel Details";
            popUp.SelectionCount.Text = parcels.Count.ToString();

            // Data 1
            popUp.DataTitle1.Text = "Landuse";
            if (singleUse)
                popUp.DataContent1.Text = parcels[0].LandUse.ToString();
            else
                popUp.DataContent1.Text = "varies";

            // Data 2
            popUp.DataTitle2.Text = "BUA";
            double bua = parcels.Sum(p => p.BUA());
            popUp.DataContent2.Text = Math.Round(bua, roundingDigits).ToString();

            // Data 3
            popUp.DataTitle3.Text = "Area";
            double area = parcels.Sum(p => p.Area());
            popUp.DataContent3.Text = Math.Round(area, roundingDigits).ToString();

            // Data 4
            popUp.DataTitle4.Text = "Coverage";
            double coverage = parcels.Sum(p => p.Coverage());
            popUp.DataContent4.Text = Math.Round(coverage, roundingDigits).ToString() + " %";

            // Data 5
            popUp.DataTitle5.Text = "FAR";
            double far = bua / area;
            popUp.DataContent5.Text = Math.Round(far, roundingDigits).ToString();

            // Data 6
            double perimeter = parcels.Sum(p => p.Perimeter());
            popUp.DataTitle6.Text = "Perimeter";
            popUp.DataContent6.Text = Math.Round(perimeter, roundingDigits).ToString();
        }
    }
}
