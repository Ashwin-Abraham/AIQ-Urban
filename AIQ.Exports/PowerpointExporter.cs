using AIQ.Exports.Helpers;
using AIQ.Exports.PowerpointExport;
using AIQ.Exports.PowerpointExport.ShapeActions;
using AIQ.Primitives.Extensions;
using ArchSitePlan.Core.Utils;
using BH.Engine.Data;
using ShapeCrawler;
using static AIQ.Exports.PowerpointExport.ShapeFilters;

namespace AIQ.Exports;

public class PowerpointExporter
{
    private const string CoverSlideIdentifier = "AIQ_SlideTemplateName_CoverPage";
    private const string ZonePlanImageIdentifier = "AIQ_Image_ZonePlan";
    private const string ZoneNameIdentifier = "AIQ_ZoneName ";

    private const string ZoneDonutChartForLandAreasIdentifier = "AIQ_DonutChart_LandAreas";
    private const string ZoneDonutChartForBuiltAreasIdentifier = "AIQ_DonutChart_BuiltAreas";
    private const string ZoneLandAreaTotal = "AIQ_ZoneLandArea";
    private const string ZoneBuiltAreaTotal = "AIQ_ZoneBuiltArea";
    private const string ZoneFar = "AIQ_ZoneFAR";
    private const string ZoneCoveragePctg = "AIQ_PlotCoveragePctg";
    private const string ProjectNameIdentifier = "AIQ_ProjectName";
    private const string LegendUseNameIdentifier = "AIQ_Legend_UseName";
    private const string LegendUseColorIdentifier = "AIQ_Legend_UseColour";
    private const string ZoneUsesTableIdentifier = "AIQ_Table_Uses";
    private const int MaxUseCountInTemplatePPT = 18;

    // Author identifiers
    private const string ZoneAuthorIdentifier = "AIQ_Author";
    private const string ZoneAuthorLogoIdentifier = "AIQ_AuthorLogo";

    // Table text identifiers
    // For data rows
    private const string TableUseNameIdentifier = "AIQ_UseName";
    private const string TableUseLandAreaIdentifier = "AIQ_UseLandArea";
    private const string TableUseBuiltAreaIdentifier = "AIQ_UseBuiltArea";
    private const string TableUseFARIdentifier = "AIQ_UseFAR";
    private const string TableUseLandShareIdentifier = "AIQ_UseLandShare";
    private const string TableUseBuiltShareIdentifier = "AIQ_UseBuiltShare";
    // For total row
    private const string TableUseLandAreaTotalIdentifier = "AIQ_TableTotal_LandArea";
    private const string TableUseBuiltAreaTotalIdentifier = "AIQ_TableTotal_BuiltArea";
    private const string TableUseFARTotalIdentifier = "AIQ_TableTotal_FAR";
    private const string TableUseLandShareTotalIdentifier = "AIQ_TableTotal_LandShare";
    private const string TableUseBuiltShareTotalIdentifier = "AIQ_TableTotal_BuiltShare";

    private readonly PowerpointExportConfig _config;
    private readonly List<GraphData> _listOfAllUses;

    public PowerpointExporter(PowerpointExportConfig config)
    {
        _config = config;
        _listOfAllUses = config.ExtractUniqueLandUseBasedGraphData();
    }

   
    public bool ExportToPowerpoint()
    {
        // Setup
        var templatePath = PathHelpers.GetPowerpointTemplatePath();
        bool success = true;

        // Copy template presentation
        var presentation = new Presentation(templatePath);
        var outputPath = Path.Combine(_config.SaveLocation);
        presentation.Save(outputPath);
        presentation = new Presentation(outputPath);

        // Modify cover slide
        var coverSlide = FindCoverSlide(presentation);
        if (coverSlide != null)
        {
            var coverSlideEditActions = BuildCoverSlideShapeEditActions();
            ModifySlideContents(coverSlide, coverSlideEditActions, [], []);
        }
        else
        {
            success = false;
        }

        // Create 1 copy of repeating slides per zone
        int[] repeatingSlides = [2, 3, 4];
        List<(ZoneExportConfig Config, List<int> SlideNos)> configSlideMap = [];
        int newIndex = presentation.Slides.Count;

        for(int i = 0; i < _config.OtherZoneExportConfigs.Count; i++)
        {
            List<int> slideNos = [];

            foreach(var slideIndex in repeatingSlides)
            {
                var newSlide = presentation.Slide(slideIndex);
                presentation.Slides.Add(newSlide);
                slideNos.Add(newIndex);
                newIndex++;
            }

            configSlideMap.Add((_config.OtherZoneExportConfigs[i], slideNos));
        }

        configSlideMap.Add((_config.FullProjectExportConfig, repeatingSlides.ToList()));

        // Modify zone slides
        foreach((ZoneExportConfig Config, List<int> SlideNos) in configSlideMap)
        {
            var zoneConfig = Config;
            var slideShapeReplacementSteps = BuildZoneShapeReplacementActions(zoneConfig);
            var shapeEditActions = BuildZoneShapeEditActions(zoneConfig);
            var shapeRemovalActions = RemoveShapeActions(zoneConfig);

            foreach (var slideIndex in SlideNos)
            {
                var slideToEdit = presentation.Slide(slideIndex);
                ModifySlideContents(slideToEdit, shapeEditActions, slideShapeReplacementSteps, shapeRemovalActions);
                ModifySlideContents(slideToEdit, shapeEditActions, slideShapeReplacementSteps, shapeRemovalActions);
                ModifySlideContents(slideToEdit, shapeEditActions, slideShapeReplacementSteps, shapeRemovalActions);
            }
        }
        
        // Save and exit
        presentation.Save(_config.SaveLocation);
        return success;
    }

    private List<IShapeRemoveAction> RemoveShapeActions(ZoneExportConfig config)
    {
        return [
            new ConditionalRemoveShapeAction(_removeShape: shape => TextboxContains(shape, LegendUseNameIdentifier)),
            new ConditionalRemoveShapeAction(_removeShape: shape => NameContains(shape, ConstructUnusedLegendColorIdentifiers(config)))
            ];
    }

    /// <summary>
    /// The template ppt contains shapes that are placed as dummy shapes. These are not being used,
    /// depending on how many <see cref="ZoneExportConfig"/> uses exist.
    /// 
    /// This constructs the names of all those unused shapes
    /// </summary>
    private List<string> ConstructUnusedLegendColorIdentifiers(ZoneExportConfig config)
    {
        List<string> identifiers = [];

        for (int i = config.Metrics.ExtractLandAreaGraphData().Count + 1; i < MaxUseCountInTemplatePPT; i++)
        {
            string identifier = $"{LegendUseColorIdentifier}{i}_";
            identifiers.Add(identifier);
        }

        return identifiers;
    }

    private List<IShapeEditAction> BuildZoneShapeEditActions(ZoneExportConfig zoneConfig)
    {
        return new()
        {
            // For Plan Legends
            new ConditionalSetShapeTextAction(
                _textMapper: shape => LanduseLegendNameCasesAlphabetically(shape, zoneConfig)),

            new EditShapeFillColorAction(_colorHexMapper: shape => LanduseColorLegendCasesAlphabetically(shape, zoneConfig)),

            // Slide Template Actions like author, project name company logo
            new SetShapeTextAction(
                _filter: shape => TextboxContains(shape, ProjectNameIdentifier),
                _text: _config.ProjectName),

            new SetShapeImageAction(
                _filter: shape => NameContains(shape, ZoneAuthorLogoIdentifier) && IsPicture(shape),
                _newImagePath: _config.AuthorLogoPath,
                _alignRight: true),

            // Zone HUD actions
            new SetShapeTextAction(
                _filter: shape => TextboxContains(shape, ZoneLandAreaTotal),
                _text: zoneConfig.Metrics.DevelopableArea.Round(0).ToString()),

            new SetShapeTextAction(
                _filter: shape => TextboxContains(shape, ZoneBuiltAreaTotal),
                _text: zoneConfig.Metrics.BuiltArea.Round(0).ToString()),

            new SetShapeTextAction(
                _filter: shape => TextboxContains(shape, ZoneFar),
                _text: zoneConfig.Metrics.FAR.Round(2).ToString()),

            new SetShapeTextAction(
                _filter: shape => TextboxContains(shape, ZoneCoveragePctg),
                _text: (zoneConfig.Metrics.CoverageRatio * 100).Round(2).ToString()+"%"),

            new SetShapeTextAction(
                _filter: shape => TextboxContains(shape, ZoneNameIdentifier ),
                _text: zoneConfig.Metrics.ZoneName),

            // Plan Image Actions
            new SetShapeTextAction(
                _filter: shape => TextboxContains(shape, ZoneLandAreaTotal),
                _text: zoneConfig.Metrics.DevelopableArea.Round(0).ToString()),

            new SetShapeImageAction(
                _filter: shape => NameContains(shape, ZonePlanImageIdentifier) && IsPicture(shape),
                _newImagePath: zoneConfig.PlanImagePath),

            // Data table actions
            new ReplaceTableTextAction(
                _filter: shape => NameContains(shape, ZoneUsesTableIdentifier),
                _cellTextReplacementMap: BuildUseTableReplacementMap(zoneConfig)),

            new DeleteTableRowAction(
                _shapeFilter: shape => IsTable(shape),
                _removeRowFilter: row => AnyCellContains(
                    row,
                    [TableUseNameIdentifier, TableUseLandAreaIdentifier, TableUseBuiltAreaIdentifier, TableUseFARIdentifier, TableUseBuiltShareIdentifier, TableUseLandShareIdentifier]))
        };
    }

    private static bool AnyCellContains(ITableRow row, List<string> identifiers)
    {
        foreach(var cell in row.Cells)
        {
            string? text = cell.TextBox?.Text;
            
            if(text == null)
                continue;

            if(identifiers.Any(x => text.Contains(x)))
                return true;
        }

        return false;
    }

    private static List<(string, string)> BuildUseTableReplacementMap(ZoneExportConfig zoneConfig)
    {
        var landUseMetrics = zoneConfig.Metrics.ExtractLandAreaGraphData();
        var builtUseMetrics = zoneConfig.Metrics.ExtractBuiltUseGraphData();

        landUseMetrics = landUseMetrics.EnsureItIncludes(builtUseMetrics).InAplhabeticalOrder();
        builtUseMetrics = builtUseMetrics.EnsureItIncludes(landUseMetrics).InAplhabeticalOrder();

        double totalLandArea = landUseMetrics.Sum(x => x.Data);
        double totalBuiltArea = builtUseMetrics.Sum(x => x.Data);

        List<(string, string)> map = [];
        for (int i = 0; i < landUseMetrics.Count; i++)
        {
            int index = i + 1;
            var landUse = landUseMetrics[i];
            var builtUse = builtUseMetrics[i];

            // Create some mappings for each element of this use's data
            map.Add(($"{TableUseNameIdentifier}{index}_", landUse.Label));
            map.Add(($"{TableUseLandAreaIdentifier}{index}_", landUse.Data.ToFormattedText(decimals: 2)));
            map.Add(($"{TableUseBuiltAreaIdentifier}{index}_", builtUse.Data.ToFormattedText(decimals: 2)));

            double far = builtUse.Data / landUse.Data;
            map.Add(($"{TableUseFARIdentifier}{index}_", far.ToFormattedText(decimals: 2)));

            double landShare = landUse.Data / totalLandArea;
            map.Add(($"{TableUseLandShareIdentifier}{index}_", landShare.ToFormattedText(decimals: 2)));

            double builtShare = builtUse.Data / totalBuiltArea;
            map.Add(($"{TableUseBuiltShareIdentifier}{index}_", builtShare.ToFormattedText(decimals: 2)));
        }

        map.Add(($"{TableUseLandAreaTotalIdentifier}", zoneConfig.Metrics.DevelopableArea.ToFormattedText(decimals: 2)));
        map.Add(($"{TableUseBuiltAreaTotalIdentifier}", zoneConfig.Metrics.BuiltArea.ToFormattedText(decimals: 2)));
        map.Add(($"{TableUseFARTotalIdentifier}", zoneConfig.Metrics.FAR.ToFormattedText(decimals: 2)));
        map.Add(($"{TableUseBuiltShareTotalIdentifier}", "100%"));
        map.Add(($"{TableUseLandShareTotalIdentifier}", "100%"));

        return map;
    }

    private static string? LanduseColorLegendCasesAlphabetically(IShape shape, ZoneExportConfig zoneConfig)
    {
        var uses = zoneConfig.Metrics.ExtractLandAreaGraphData().InAplhabeticalOrder();

        List<(string Identifier, string HexColor)> map = [];

        for (int i = 0; i < uses.Count; i++)
        {
            string key = LegendUseColorIdentifier + (i + 1).ToString() + "_";
            string hexColor = uses[i].GetColorHex();

            map.Add((key, hexColor));
        }

        var result = map.Find(x => shape.Name.Contains(x.Identifier)).HexColor;

        if (result == string.Empty || result == null)
            return null;

        return result;
    }

    private static string? LanduseLegendNameCasesAlphabetically(IShape shape, ZoneExportConfig zoneConfig)
    {
        if (shape.TextBox == null)
            return null;

        var uses = zoneConfig.Metrics.ExtractLandAreaGraphData().InAplhabeticalOrder();

        List<(string Identifier, string Use)> map = [];

        for(int i = 0; i < uses.Count; i++)
        {
            string key = LegendUseNameIdentifier + (i+1).ToString() + "_";
            string use = uses[i].Label;

            map.Add((key, use));
        }

        var result = map.Find(x => shape.TextBox.Text.Contains(x.Identifier)).Use;

        return result == string.Empty ? null : result;
    }

    private List<IShapeEditAction> BuildCoverSlideShapeEditActions()
    {
        return new()
        {
            new SetShapeTextAction(
                _filter: shape => TextboxContains(shape, ZoneAuthorIdentifier), 
                _text: _config.AuthorName),

            new SetShapeImageAction(
                _filter: shape => NameContains(shape, ZonePlanImageIdentifier) && IsPicture(shape),
                _newImagePath: _config.FullProjectExportConfig.PlanImagePath),
        };
    }

    private List<IShapeReplaceAction> BuildZoneShapeReplacementActions(ZoneExportConfig zoneConfig)
    {
        return [
            new ReplaceShapeWithPieChartAction(
                _requiresReplacement: shape => TextboxContains(shape, ZoneDonutChartForLandAreasIdentifier),
                _chartName: "Land Areas",
                _data: zoneConfig.Metrics.ExtractLandAreaGraphData().EnsureItIncludes(_listOfAllUses).InAplhabeticalOrder()),

            new ReplaceShapeWithPieChartAction(
                _requiresReplacement: shape => TextboxContains(shape, ZoneDonutChartForBuiltAreasIdentifier),
                _chartName: "Built Areas",
                _data: zoneConfig.Metrics.ExtractBuiltUseGraphData().EnsureItIncludes(_listOfAllUses).InAplhabeticalOrder())
            ];
    }

    private static ISlide? FindCoverSlide(Presentation presentation)
    {
        return presentation.Slides.FirstOrDefault(x => x.Shapes.Any(x => x.TextBox?.Text.Equals(CoverSlideIdentifier)?? false));
    }

    private void ModifySlideContents(
        ISlide slide,
        List<IShapeEditAction> editActions,
        List<IShapeReplaceAction> replaceActions,
        List<IShapeRemoveAction> removeActions)
    {
        var shapeList = slide.Shapes.ToList();

        // Edit Shapes
        for (int i = 0; i < shapeList.Count; i++)
        {
            IShape shape = shapeList[i];

            foreach (var editAction in editActions)
                editAction.EditShape(shape);
        }

        // Replace Shapes
        shapeList = slide.Shapes.ToList();
        for (int i = 0; i < shapeList.Count; i++)
        {
            IShape shape = shapeList[i];
            foreach (var replaceAction in replaceActions)
            {
                if (replaceAction.ReplaceShape(slide, shape))
                    break;
            }
        }

        // Remove Shapes
        shapeList = slide.Shapes.ToList();
        for (int i = 0; i < shapeList.Count; i++)
        {
            IShape shape = shapeList[i];

            foreach (var removeAction in removeActions)
            {
                if (removeAction.TryRemove(shape))
                    break;
            }
        }
    }
}
