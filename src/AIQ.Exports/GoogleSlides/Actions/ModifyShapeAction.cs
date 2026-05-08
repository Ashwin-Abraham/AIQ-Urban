using AIQ.Exports.GoogleSlides.Utils;
using Google.Apis.Slides.v1.Data;
using System.Drawing;

namespace AIQ.Exports.GoogleSlides.Actions;
public class ModifyShapeAction(
    string filterText,
    bool deleteText = true,
    System.Drawing.Color? fillColor = null,
    System.Drawing.Color? outlineColor = null,
    int? outlineWeight = null,
    string? outlineStyle = null,
    int? slideNumber = null,
    TextMatchOption matchOption = TextMatchOption.DontMatchCase) : IGoogleSlidesAction
{
    public string FilterText { get; } = filterText;
    public bool DeleteText { get; } = deleteText;
    public System.Drawing.Color? FillColor { get; } = fillColor;
    public System.Drawing.Color? OutlineColor { get; } = outlineColor;
    public int? OutlineWeight { get; } = outlineWeight;
    public string? OutlineStyle { get; } = outlineStyle;
    public int? SlideNumber { get; } = slideNumber;
    public TextMatchOption MatchOption { get; } = matchOption;

    public IEnumerable<Request> ToRequest(Presentation presentation)
    {
        if (string.IsNullOrWhiteSpace(FilterText))
            yield break;

        var hasVisualChanges = FillColor.HasValue ||
                              OutlineWeight.HasValue ||
                              OutlineColor.HasValue ||
                              !string.IsNullOrEmpty(OutlineStyle);

        if (!hasVisualChanges && !DeleteText)
            yield break;

        var targetSlides = GetTargetSlides(presentation);

        foreach (var slide in targetSlides)
        {
            var matchingShapeIds = slide.PageElements
                .Where(pe => pe.Shape != null &&
                             TextMatches(pe.Shape, FilterText, MatchOption))
                .Select(pe => pe.ObjectId)
                .ToList();

            foreach (var shapeId in matchingShapeIds)
            {
                if (hasVisualChanges)
                {
                    yield return CreateUpdateShapeRequest(shapeId);
                }

                if (DeleteText)
                {
                    yield return new Request
                    {
                        DeleteText = new DeleteTextRequest
                        {
                            ObjectId = shapeId,
                            TextRange = new Google.Apis.Slides.v1.Data.Range { Type = "ALL" }
                        }
                    };
                }
            }
        }
    }

    private static bool TextMatches(Shape shape, string filterText, TextMatchOption matchOption)
    {
        if (shape.Text?.TextElements == null)
            return false;

        var text = string.Concat(
            shape.Text.TextElements
                .Where(te => te.TextRun != null && te.TextRun.Content != null)
                .Select(te => te.TextRun!.Content));

        if (string.IsNullOrEmpty(text))
            return false;

        return matchOption == TextMatchOption.MatchCase
            ? text.Contains(filterText)
            : text.Contains(filterText, StringComparison.OrdinalIgnoreCase);
    }

    private IEnumerable<Page> GetTargetSlides(Presentation presentation)
    {
        if (SlideNumber.HasValue)
        {
            var targetSlide = presentation.Slides?
                .Skip(SlideNumber.Value - 1)
                .FirstOrDefault();

            if (targetSlide != null)
                yield return targetSlide;
        }
        else
        {
            foreach (var slide in presentation.Slides ?? [])
                yield return slide;
        }
    }

    private Request CreateUpdateShapeRequest(string shapeId)
    {
        var fields = new List<string>();
        var shapeProperties = new ShapeProperties();

        if (FillColor.HasValue)
        {
            shapeProperties.ShapeBackgroundFill = new ShapeBackgroundFill
            {
                PropertyState = "RENDERED",
                SolidFill = new SolidFill
                {
                    Color = new OpaqueColor
                    {
                        RgbColor = ToRgbColor(FillColor.Value)
                    }
                }
            };
            fields.Add("shapeBackgroundFill.solidFill.color");
        }

        if (OutlineWeight.HasValue && OutlineWeight.Value == 0)
        {
            shapeProperties.Outline = new Outline
            {
                PropertyState = "INVISIBLE"
            };
            fields.Add("outline.propertyState");
        }
        else
        {
            var hasOutlineChanges = false;
            var outline = new Outline();

            if (OutlineColor.HasValue)
            {
                outline.OutlineFill = new OutlineFill
                {
                    SolidFill = new SolidFill
                    {
                        Color = new OpaqueColor
                        {
                            RgbColor = ToRgbColor(OutlineColor.Value)
                        }
                    }
                };
                fields.Add("outline.outlineFill.solidFill.color");
                hasOutlineChanges = true;
            }

            if (OutlineWeight.HasValue)
            {
                outline.Weight = new Dimension
                {
                    Magnitude = OutlineWeight.Value,
                    Unit = "PT"
                };
                fields.Add("outline.weight");
                hasOutlineChanges = true;
            }

            if (!string.IsNullOrEmpty(OutlineStyle))
            {
                outline.DashStyle = OutlineStyle;
                fields.Add("outline.dashStyle");
                hasOutlineChanges = true;
            }

            if (hasOutlineChanges)
            {
                shapeProperties.Outline = outline;
            }
        }

        return new Request
        {
            UpdateShapeProperties = new UpdateShapePropertiesRequest
            {
                ObjectId = shapeId,
                ShapeProperties = shapeProperties,
                Fields = string.Join(",", fields)
            }
        };
    }

    private static RgbColor ToRgbColor(Color color)
    {
        return new RgbColor
        {
            Red = color.R / 255f,
            Green = color.G / 255f,
            Blue = color.B / 255f
        };
    }
}
