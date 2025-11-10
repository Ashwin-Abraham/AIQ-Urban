using AIQ.Exports.Helpers;
using AIQ.Exports.PowerpointExport;
using AIQ.Exports.PowerpointExport.ShapeActions;
using ArchSitePlan.Core.Utils;
using BH.Engine.Data;
using ShapeCrawler;
using static AIQ.Exports.PowerpointExport.ShapeFilters;

namespace AIQ.Exports;

public class PowerpointExporter
{
    private const string CoverSlideIdentifier = "AIQ_SlideTemplateName_CoverPage";
    private const string ZonePlanImageIdentifier = "AIQ_Image_ZonePlan";
    private const string ZoneAuthorIdentifier = "AIQ_Author";
    private const string ZoneDonutChartForLandAreasIdentifier = "AIQ_DonutChart_LandAreas";
    private const string ZoneDonutChartForBuiltAreasIdentifier = "AIQ_DonutChart_BuiltAreas";
    private const string ZoneLandAreaTotal = "AIQ_ZoneLandArea";
    private const string ZoneBuiltAreaTotal = "AIQ_ZoneBuiltArea";
    private const string ZoneFar = "AIQ_ZoneFAR";
    private const string ZoneCoveragePctg = "AIQ_PlotCoveragePctg";
    private const string ProjectNameIdentifier = "AIQ_ProjectName";
    private const string LegendUseNameIdentifier = "AIQ_Legend_UseName";
    private const string LegendUseColorIdentifier = "AIQ_Legend_UseColour";
    private const int MaxUseCountInTemplatePPT = 20;

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

        // Modify zone slides
        var slideShapeReplacementSteps = BuildZoneShapeReplacementActions(_config.FullProjectExportConfig);
        var shapeEditActions = BuildZoneShapeEditActions(_config.FullProjectExportConfig);
        var shapeRemovalActions = RemoveShapeActions(_config.FullProjectExportConfig);

        ModifySlideContents(presentation.Slide(2), shapeEditActions, slideShapeReplacementSteps, shapeRemovalActions);
        ModifySlideContents(presentation.Slide(3), shapeEditActions, slideShapeReplacementSteps, shapeRemovalActions);
        ModifySlideContents(presentation.Slide(4), shapeEditActions, slideShapeReplacementSteps, shapeRemovalActions);

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
            new ConditionalSetShapeTextAction(
                _textMapper: shape => LanduseLegendNameCasesAlphabetically(shape, zoneConfig),
                _fontSize: (decimal)10.5
                ),

            new EditShapeFillColorAction(_colorHexMapper: shape => LanduseColorLegendCasesAlphabetically(shape, zoneConfig)),

            new SetShapeTextAction(
                _filter: shape => TextboxContains(shape, ProjectNameIdentifier),
                _text: _config.ProjectName,
                _fontSize: 36),

            new SetShapeTextAction(
                _filter: shape => TextboxContains(shape, ZoneLandAreaTotal),
                _text: zoneConfig.Metrics.DevelopableArea.Round(0).ToString(),
                _fontSize: 18),

            new SetShapeTextAction(
                _filter: shape => TextboxContains(shape, ZoneBuiltAreaTotal),
                _text: zoneConfig.Metrics.BuiltArea.Round(0).ToString(),
                _fontSize: 18),

            new SetShapeTextAction(
                _filter: shape => TextboxContains(shape, ZoneFar),
                _text: zoneConfig.Metrics.FAR.Round(2).ToString(),
                _fontSize: 18),

            new SetShapeTextAction(
                _filter: shape => TextboxContains(shape, ZoneFar),
                _text: zoneConfig.Metrics.FAR.Round(2).ToString(),
                _fontSize: 18),

            new SetShapeTextAction(
                _filter: shape => TextboxContains(shape, ZoneCoveragePctg),
                _text: zoneConfig.Metrics.CoverageRatio.Round(1).ToString() + "%",
                _fontSize: 18),

            new SetShapeImageAction(
                _filter: shape => NameContains(shape, ZonePlanImageIdentifier) && IsPicture(shape),
                _newImagePath: zoneConfig.PlanImagePath),
        };
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
                _text: _config.AuthorName,
                _fontSize : 14),

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
