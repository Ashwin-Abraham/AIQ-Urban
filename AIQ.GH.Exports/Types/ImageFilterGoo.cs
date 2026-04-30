using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using AIQ.Exports.GoogleSlides.Filters;

namespace AIQ.GH.Exports.Types;
public class ImageFilterGoo : GH_Goo<ImageFilter>
{
    public override string TypeName => "Image Filter";
    public override string TypeDescription => "Filter for image elements in Google Slides";

    public ImageFilterGoo() { }

    public ImageFilterGoo(ImageFilter value) => Value = value;

    public override bool IsValid => Value != null;
    public override string IsValidWhyNot => Value == null ? "ImageFilter is null" : "";

    public override IGH_Goo Duplicate() => new ImageFilterGoo(Value);

    public override string ToString() => Value is null ? "null" : $"ImageFilter ({Value.ImageElementName})";
}