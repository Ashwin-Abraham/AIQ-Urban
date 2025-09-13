using AIQ.Primitives.Extensions;
using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Metrics;
using ClosedXML.Excel;
using System.Drawing;

namespace ArchSitePlan.Core.Models
{
    public class ProjectReport
    {
        // Row formats
        private record LandUseSummaryDataRow(
            string Landuse,
            double Area,
            Color Colour,
            double AreaPctg,
            double BuiltArea,
            double BuiltPctg,
            double FAR);

        private record TransitSummaryDataRow(
            string TransitType,
            Color Colour,
            double RunningLength,
            double RunningLengthPctg,
            double TransitArea,
            double TransitAreaPctg);

        private record StudySummaryDataRow(
            string StudyName,
            Color Colour,
            double BoundedArea,
            double ParcelArea,
            double ParcelAreaPctg,
            double TransitArea,
            double TransitAreaPctg,
            double BuiltArea,
            double BuiltAreaPctg,
            double FAR);

        // Variables
        public string ExcelPath { get; set; }
        public StudyAreaMetrics ProjectWideStudy { get; set; }
        private List<LandUseSummaryDataRow> _landUseSummary { get; set; }
        private List<TransitSummaryDataRow> _transitSummary { get; set; }
        private List<StudySummaryDataRow> _studyAreasSummary { get; set; }
        
        
        public ProjectReport(
            string excelFilePath, 
            StudyAreaMetrics projectWideStudy,
            List<StudyArea> studies)
        {
            ProjectWideStudy = projectWideStudy;
            ExcelPath = excelFilePath;

            BuildLandUseData(projectWideStudy);
            BuildTransitData(studies.First(s => s.Name == CoreGeneralConstants.FullProjectStudyName));
            BuildStudyAreasData(studies);
        }

        private void BuildStudyAreasData(List<StudyArea> studies)
        {
            double totalBuiltArea = studies
                .First(s => s.Name.Contains(CoreGeneralConstants.FullProjectStudyName))
                .Metrics.BuiltArea;

            double totalDevArea = studies
                .First(s => s.Name.Contains(CoreGeneralConstants.FullProjectStudyName))
                .Metrics.DevelopableArea;

            _studyAreasSummary = new();
            foreach (var study in studies)
            {
                double builtPctg = totalBuiltArea.AlmostZero()?
                    0: study.Metrics.BuiltArea / totalBuiltArea;

                double parcelPctg = totalDevArea.AlmostZero() ?
                    0 : study.Metrics.DevelopableArea / totalDevArea;

                double transitArea = study.Metrics.TransitMetrics.Sum(tm => tm.Area);

                var row = new StudySummaryDataRow(
                    StudyName: study.Name,
                    Colour: Color.Aqua,
                    BoundedArea: study.Metrics.BoundedArea,
                    ParcelArea: study.Metrics.DevelopableArea,
                    ParcelAreaPctg: parcelPctg,
                    TransitArea: transitArea,
                    TransitAreaPctg: transitArea/ study.Metrics.BoundedArea,
                    BuiltArea: study.Metrics.BuiltArea,
                    BuiltAreaPctg: builtPctg,
                    FAR: study.Metrics.FAR);

                _studyAreasSummary.Add(row);
            }
        }

        private void BuildTransitData(StudyArea fullProjectStudy)
        {
            var transitMetrics = fullProjectStudy.Metrics.TransitMetrics;
            _transitSummary = new();

            double totalTransitArea = transitMetrics.Sum(t => t.Area);
            double totalTransitLength = transitMetrics.Sum(t => t.RunningLength);

            foreach(var transit in transitMetrics)
            {
                var row = new TransitSummaryDataRow(
                    TransitType:  $"{transit.Type.ToString()} - {transit.RoadWidth}m",
                    Colour: Color.FromArgb(_transitSummary.Count * 10,0, 0,0),
                    RunningLength: transit.RunningLength,
                    RunningLengthPctg: transit.RunningLength/ totalTransitLength,
                    TransitArea: transit.Area,
                    TransitAreaPctg: transit.Area/ totalTransitArea);

                _transitSummary.Add(row);
            }
        }

        private void BuildLandUseData(StudyAreaMetrics projectWideStudy)
        {
            // Landuse summary is grouped by landuse
            var landUseGroups = projectWideStudy.ParcelMetrics.GroupBy(p => p.Use);
            _landUseSummary = new();
            
            double totalLandArea = landUseGroups.Sum(grp => grp.Sum(parcel => parcel.Area));
            double totalBuiltArea = landUseGroups.Sum(grp => grp.Sum(parcel => parcel.BUA));

            foreach (var useGroup in landUseGroups)
            {
                double landArea = useGroup.Sum(grp => grp.Area);
                double builtArea= useGroup.Sum(grp => grp.BUA);
                double areaPctg = Math.Clamp(landArea / totalLandArea, 0, 1);
                areaPctg = double.IsNaN(areaPctg) ? 0 : areaPctg;

                double builtPctg = Math.Clamp(builtArea / totalBuiltArea, 0, 1);
                builtPctg = double.IsNaN(builtPctg)? 0: builtPctg;

                var row = new LandUseSummaryDataRow(
                    Landuse: useGroup.Key.Name,
                    Area: landArea,
                    AreaPctg: areaPctg,
                    BuiltArea: builtArea,
                    BuiltPctg: builtPctg,
                    FAR: totalBuiltArea/totalLandArea,
                    Colour: useGroup.Key.Color
                    );

                _landUseSummary.Add( row );
            }
        }

        // Landuse Styles
        private IXLStyleData TableSubHeadingStyle_LandUse = new IXLStyleData(
            FillColour: Color.PeachPuff,
            formatId: (int)XLPredefinedFormat.Number.Precision2,
            BoldFont: true,
            HorizontalAlign: XLAlignmentHorizontalValues.Center);

        private IXLStyleData TableSubHeadingStyle_Pctg_LandUse = new IXLStyleData(
            FillColour: Color.PeachPuff,
            formatId: (int)XLPredefinedFormat.Number.PercentPrecision2,
            BoldFont: true,
            HorizontalAlign: XLAlignmentHorizontalValues.Center);

        private IXLStyleData TableHeadingStyle_LandUse = new IXLStyleData(
            FillColour: Color.PeachPuff,
            formatId: (int)XLPredefinedFormat.General,
            FontSize: 16,
            BoldFont: true);

        // Study Area Styles
        private IXLStyleData TableSubHeadingStyle_StudyArea = new IXLStyleData(
            FillColour: Color.LightPink,
            formatId: (int)XLPredefinedFormat.Number.Precision2,
            BoldFont: true,
            HorizontalAlign: XLAlignmentHorizontalValues.Center);

        private IXLStyleData TableSubHeadingStyle_Pctg_StudyArea = new IXLStyleData(
            FillColour: Color.LightPink,
            formatId: (int)XLPredefinedFormat.Number.PercentInteger,
            BoldFont: true,
            HorizontalAlign: XLAlignmentHorizontalValues.Center);

        private IXLStyleData TableHeadingStyle_StudyArea = new IXLStyleData(
            FillColour: Color.LightPink,
            formatId: (int)XLPredefinedFormat.General,
            FontSize: 16,
            BoldFont: true);

        // Transit Styles
        private IXLStyleData TableSubHeadingStyle_Transit = new IXLStyleData(
            FillColour: Color.Silver,
            formatId: (int)XLPredefinedFormat.Number.Precision2,
            BoldFont: true,
            HorizontalAlign: XLAlignmentHorizontalValues.Center);

        private IXLStyleData TableSubHeadingStyle_Pctg_Transit = new IXLStyleData(
           FillColour: Color.Silver,
           formatId: (int)XLPredefinedFormat.Number.PercentInteger,
           BoldFont: true,
           HorizontalAlign: XLAlignmentHorizontalValues.Center);

        private IXLStyleData TableHeadingStyle_Transit = new IXLStyleData(
            FillColour: Color.Silver,
            formatId: (int)XLPredefinedFormat.General,
            FontSize: 16,
            BoldFont: true);

        // General Data Styles
        private IXLStyleData AreaLengthDataStyle = new IXLStyleData(
           formatId: (int)XLPredefinedFormat.Number.Precision2WithSeparator);

        private IXLStyleData PercentageDatasStyle = new IXLStyleData(
            formatId: (int)XLPredefinedFormat.Number.PercentPrecision2);

        public void WriteExcelFile()
        {

            var wb = new XLWorkbook();
            // Add a new worksheet to the workbook
            var worksheet = wb.AddWorksheet("Sheet1");
            int rowOffset = 5;
            int tableRowGap = 2;

            //////////////// LANDUSE SUMMARY //////////////////
            // LANDUSE HEADER
            worksheet.Cell(0 + rowOffset, 1).SetContents("LANDUSE SUMMARY", TableHeadingStyle_LandUse);
            worksheet.Cells(0 + rowOffset, 2, 7).SetContents("", TableHeadingStyle_LandUse);
            rowOffset += 1;

            worksheet.Cell(0 + rowOffset, 1).SetContents("Landuse", TableSubHeadingStyle_LandUse);
            worksheet.Cell(0 + rowOffset, 2).SetContents("Colour", TableSubHeadingStyle_LandUse);
            worksheet.Cell(0 + rowOffset, 3).SetContents("Land Area", TableSubHeadingStyle_LandUse);
            worksheet.Cell(0 + rowOffset, 4).SetContents("Percentage", TableSubHeadingStyle_LandUse);
            worksheet.Cell(0 + rowOffset, 5).SetContents("Built Area", TableSubHeadingStyle_LandUse);
            worksheet.Cell(0 + rowOffset, 6).SetContents("Built Area Percentage", TableSubHeadingStyle_LandUse);
            worksheet.Cell(0 + rowOffset, 7).SetContents("FAR", TableSubHeadingStyle_LandUse);
            rowOffset += 1;

            // LANDUSE TABLE BODY
            for (int i = 0; i < _landUseSummary.Count;i++ )
            {
                int r = i + rowOffset;
                int rTotals = r + _landUseSummary.Count - i -1;

                worksheet.Cell(r, 1).Value = _landUseSummary[i].Landuse; 
                worksheet.Cell(r, 2).Style.Fill
                    .SetBackgroundColor(XLColor.FromColor(_landUseSummary[i].Colour)); 
                worksheet.Cell(r, 3).SetContents(_landUseSummary[i].Area, AreaLengthDataStyle);
                var s = worksheet.Cell(r, 3).GetFormattedString();

                worksheet.Cell(r, 4).SetContents("",
                    PercentageDatasStyle,
                    BuildPercentageFormula(r,3,rowOffset, rTotals, 3));

                worksheet.Cell(r, 5).SetContents(_landUseSummary[i].BuiltArea, AreaLengthDataStyle);

                worksheet.Cell(r, 6).SetContents("",
                    PercentageDatasStyle,
                    BuildPercentageFormula(r, 5, rowOffset, rTotals, 5));

                worksheet.Cell(r, 7).SetContents("",
                    AreaLengthDataStyle,
                    BuildRatioFormula(r, 5, r, 3));
            }

            rowOffset += _landUseSummary.Count;

            int totalStartRow = rowOffset - _landUseSummary.Count;
            int totalEndRow = rowOffset - 1;

            // LANDUSE TOTALS
            worksheet.Cell(rowOffset, 1).SetContents("Total", TableSubHeadingStyle_LandUse);
            worksheet.Cell(rowOffset, 2).SetContents("", TableSubHeadingStyle_LandUse);
            
            // Land Area Total
            worksheet.Cell(rowOffset, 3).SetContents("",
                TableSubHeadingStyle_LandUse,
                BuildTotalFormula(totalStartRow, totalEndRow, 3));
            //  Land Area Percentage Total
            worksheet.Cell(rowOffset, 4).SetContents("",
                TableSubHeadingStyle_Pctg_LandUse,
                BuildTotalFormula(totalStartRow, totalEndRow, 4));
            // Built Area total
            worksheet.Cell(rowOffset, 5).SetContents("",
                TableSubHeadingStyle_LandUse,
                BuildTotalFormula(totalStartRow, totalEndRow, 5));
            // Built Area Percentage Total
            worksheet.Cell(rowOffset, 6).SetContents("",
                TableSubHeadingStyle_Pctg_LandUse,
                BuildTotalFormula(totalStartRow, totalEndRow, 6));
            // FAR Total
            worksheet.Cell(rowOffset, 7).SetContents("",
                TableSubHeadingStyle_LandUse,
                BuildTotalFormula(totalStartRow, totalEndRow, 7));

            rowOffset += tableRowGap;

            //////////////// TRANSIT SUMMARY //////////////////
            // Heading
            worksheet.Cell(0 + rowOffset, 1).SetContents("TRANSIT AREA SUMMARY", TableHeadingStyle_Transit);
            worksheet.Cells(0 + rowOffset, 2, 7).SetContents("", TableHeadingStyle_Transit);
            rowOffset += 1;

            // Sub heading
            worksheet.Cell(0 + rowOffset, 1).SetContents("Transit Type", TableSubHeadingStyle_Transit);
            worksheet.Cell(0 + rowOffset, 2).SetContents("Colour", TableSubHeadingStyle_Transit);
            worksheet.Cell(0 + rowOffset, 3).SetContents("Running Length", TableSubHeadingStyle_Transit);
            worksheet.Cell(0 + rowOffset, 4).SetContents("Running Length Percentage", TableSubHeadingStyle_Transit);
            worksheet.Cell(0 + rowOffset, 5).SetContents("Area", TableSubHeadingStyle_Transit);
            worksheet.Cell(0 + rowOffset, 6).SetContents("Area Percentage", TableSubHeadingStyle_Transit);
            worksheet.Cell(0 + rowOffset, 7).SetContents("", TableSubHeadingStyle_Transit);
            rowOffset += 1;

            // Table Body
            for (int i = 0; i < _transitSummary.Count; i++)
            {
                int r = i + rowOffset;
                int totalsRow = rowOffset + _transitSummary.Count;
                worksheet.Cell(r, 1).Value = _transitSummary[i].TransitType;
                worksheet.Cell(r, 2).Style.Fill
                    .SetBackgroundColor(XLColor.FromColor(_transitSummary[i].Colour));
                worksheet.Cell(r, 3).SetContents(_transitSummary[i].RunningLength, AreaLengthDataStyle);
                worksheet.Cell(r, 4).SetContents("",
                    PercentageDatasStyle,
                    BuildPercentageFormula(r, 3, totalsRow, totalsRow, 3));

                worksheet.Cell(r, 5).SetContents(_transitSummary[i].TransitArea, AreaLengthDataStyle);
                worksheet.Cell(r, 6).SetContents("",
                    PercentageDatasStyle,
                    BuildPercentageFormula(r, 5, totalsRow, totalsRow, 5));
            }
            rowOffset += _transitSummary.Count;

            // Totals
            totalStartRow = rowOffset - _transitSummary.Count;
            totalEndRow = rowOffset - 1;

            worksheet.Cell(rowOffset, 1).SetContents("Total", TableSubHeadingStyle_Transit);
            worksheet.Cell(rowOffset, 2).SetContents("", TableSubHeadingStyle_Transit);

            // Running Length Total

            worksheet.Cell(rowOffset, 3).SetContents("",
                TableSubHeadingStyle_Transit,
                BuildTotalFormula(totalStartRow, totalEndRow, 3));
            //  Running Length Percentage Total
            worksheet.Cell(rowOffset, 4).SetContents("",
                TableSubHeadingStyle_Pctg_Transit,
                BuildTotalFormula(totalStartRow, totalEndRow, 4));
            // Transit Area total
            worksheet.Cell(rowOffset, 5).SetContents("",
                TableSubHeadingStyle_Transit,
                BuildTotalFormula(totalStartRow, totalEndRow, 5));
            // Transit Area Percentage Total
            worksheet.Cell(rowOffset, 6).SetContents("",
                TableSubHeadingStyle_Pctg_Transit,
                BuildTotalFormula(totalStartRow, totalEndRow, 6));

            worksheet.Cell(rowOffset, 7).SetContents("", TableSubHeadingStyle_Transit);

            rowOffset += tableRowGap;

            //////////////// STUDY AREA SUMMARY //////////////////
            // Heading
            worksheet.Cell(0 + rowOffset, 1).SetContents("STUDY AREA SUMMARY", TableHeadingStyle_StudyArea);
            worksheet.Cells(0 + rowOffset, 2, 10).SetContents("", TableHeadingStyle_StudyArea);
            rowOffset += 1;

            // Sub heading
            worksheet.Cell(0 + rowOffset, 1).SetContents("Study Name", TableSubHeadingStyle_StudyArea);
            worksheet.Cell(0 + rowOffset, 2).SetContents("Colour", TableSubHeadingStyle_StudyArea);
            worksheet.Cell(0 + rowOffset, 3).SetContents("Bounded Area", TableSubHeadingStyle_StudyArea);
            worksheet.Cell(0 + rowOffset, 4).SetContents("Parcel Area", TableSubHeadingStyle_StudyArea);
            worksheet.Cell(0 + rowOffset, 5).SetContents("Parcel Area Percentage (of entire proect)", TableSubHeadingStyle_StudyArea);
            worksheet.Cell(0 + rowOffset, 6).SetContents("Transit Area", TableSubHeadingStyle_StudyArea);
            worksheet.Cell(0 + rowOffset, 7).SetContents("Transit Area Percentage (of entire proect)", TableSubHeadingStyle_StudyArea);
            worksheet.Cell(0 + rowOffset, 8).SetContents("Built Area", TableSubHeadingStyle_StudyArea);
            worksheet.Cell(0 + rowOffset, 9).SetContents("Built Area Percentage (of entire proect)", TableSubHeadingStyle_StudyArea);
            worksheet.Cell(0 + rowOffset, 10).SetContents("FAR (of this study area)", TableSubHeadingStyle_StudyArea);
            rowOffset += 1;

            // Table body
            for (int i = 0; i < _studyAreasSummary.Count; i++)
            {
                int r = i + rowOffset;
                int totalsRow = rowOffset;

                worksheet.Cell(r, 1).Value = _studyAreasSummary[i].StudyName;
                worksheet.Cell(r, 2).Style.Fill
                    .SetBackgroundColor(XLColor.FromColor(_studyAreasSummary[i].Colour));
                worksheet.Cell(r, 3).SetContents(_studyAreasSummary[i].BoundedArea, AreaLengthDataStyle);
                worksheet.Cell(r, 4).SetContents(_studyAreasSummary[i].ParcelArea, AreaLengthDataStyle);
                worksheet.Cell(r, 5).SetContents("",
                    PercentageDatasStyle,
                    BuildPercentageFormula(r, 4, totalsRow, totalsRow, 4));

                worksheet.Cell(r, 6).SetContents(_studyAreasSummary[i].TransitArea, AreaLengthDataStyle);
                worksheet.Cell(r, 7).SetContents("",
                    PercentageDatasStyle,
                    BuildPercentageFormula(r, 6, totalsRow, totalsRow, 6));

                worksheet.Cell(r, 8).SetContents(_studyAreasSummary[i].BuiltArea, AreaLengthDataStyle);
                worksheet.Cell(r, 9).SetContents("",
                    PercentageDatasStyle,
                    BuildPercentageFormula(r, 8, totalsRow, totalsRow, 8));

                worksheet.Cell(r, 10).SetContents(_studyAreasSummary[i].FAR, AreaLengthDataStyle);
                worksheet.Cell(r, 10).SetContents("",
                    AreaLengthDataStyle,
                    BuildRatioFormula(r, 8, r, 4));
            }

            //////////////// SHEET POST PROCESSING/ STYLING //////////////////
            // Set Cell Widths
            worksheet.ColumnWidth = 12;
            worksheet.Column(1).Width = 30;
            worksheet.Column(2).Width = 7;

            // Save the workbook to a file
            wb.SaveAs(ExcelPath);
        }

        private string BuildRatioFormula(int numeratorRow, 
            int numeratorCol, 
            int denominatorRow, 
            int denominatorCol)
        {
            return $"R{numeratorRow}C{numeratorCol}/R{denominatorRow}C{denominatorCol}";
        }

        private string BuildPercentageFormula(int itemRow,
            int itemColumn,
            int totalStartRow, 
            int totalEndRow,
            int totalColumn)
        {
            string formula =  $"R{itemRow}C{itemColumn}/{BuildTotalFormula(totalStartRow, totalEndRow, totalColumn)}";
            formula = AddIfError(formula, 0);
            return formula;
        }

        private string AddIfError(string formula, double returnValue)
        {
            formula = $"IFERROR({formula},{returnValue})";
            return formula;
        }

        private string BuildTotalFormula(int startRow, int endRow, int column)
        {
            string formula = $"SUM(R{startRow}C{column}:R{endRow}C{column})";
            return formula;
        }
    }
}
