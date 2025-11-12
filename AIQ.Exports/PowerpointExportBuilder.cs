using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AIQ.Exports.Helpers;
using ShapeCrawler;

namespace AIQ.Exports;
public class PowerpointExportBuilder
{
    //public bool ExportToPowerpoint()
    //{
    //    // Setup
    //    var templatePath = PathHelpers.GetPowerpointTemplatePath();
    //    bool success = true;

    //    // Copy template presentation
    //    var presentation = new Presentation(templatePath);
    //    var outputPath = Path.Combine(_config.SaveLocation);
    //    presentation.Save(outputPath);
    //    presentation = new Presentation(outputPath);

    //    // Modify cover slide
    //    var coverSlide = FindCoverSlide(presentation);
    //    if (coverSlide != null)
    //    {
    //        var coverSlideEditActions = BuildCoverSlideShapeEditActions();
    //        ModifySlideContents(coverSlide, coverSlideEditActions, [], []);
    //    }
    //    else
    //    {
    //        success = false;
    //    }

    //    // Modify zone slides
    //    var slideShapeReplacementSteps = BuildZoneShapeReplacementActions(_config.FullProjectExportConfig);
    //    var shapeEditActions = BuildZoneShapeEditActions(_config.FullProjectExportConfig);
    //    var shapeRemovalActions = RemoveShapeActions(_config.FullProjectExportConfig);

    //    ModifySlideContents(presentation.Slide(2), shapeEditActions, slideShapeReplacementSteps, shapeRemovalActions);
    //    ModifySlideContents(presentation.Slide(3), shapeEditActions, slideShapeReplacementSteps, shapeRemovalActions);
    //    ModifySlideContents(presentation.Slide(4), shapeEditActions, slideShapeReplacementSteps, shapeRemovalActions);

    //    // Save and exit
    //    presentation.Save(_config.SaveLocation);
    //    return success;
    //}
}
