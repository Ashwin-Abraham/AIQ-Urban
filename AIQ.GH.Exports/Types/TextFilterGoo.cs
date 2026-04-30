using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using AIQ.Exports.GoogleSlides.Filters;

namespace AIQ.GH.Exports.Types;
public class TextFilterGoo : GH_Goo<TextFilter>
{
    public override string TypeName => "Text Filter";
    public override string TypeDescription => "Filter for text elements in Google Slides";

    public TextFilterGoo() { }

    public TextFilterGoo(TextFilter value) => Value = value;

    public override bool IsValid => Value != null;
    public override string IsValidWhyNot => Value == null ? "TextFilter is null" : "";

    public override IGH_Goo Duplicate() => new TextFilterGoo(Value);

    public override string ToString() => Value is null ? "null" : $"TextFilter ({Value.TextElementName})";
}