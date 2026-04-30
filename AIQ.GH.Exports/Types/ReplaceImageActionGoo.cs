using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using AIQ.Exports.GoogleSlides.Actions;

namespace AIQ.GH.Exports.Types;
public class ReplaceImageActionGoo : GH_Goo<ReplaceImageAction>
{
    public override string TypeName => "Replace Image Action";
    public override string TypeDescription => "Action to replace image in Google Slides";

    public ReplaceImageActionGoo() { }

    public ReplaceImageActionGoo(ReplaceImageAction value) => Value = value;

    public override bool IsValid => Value != null;
    public override string IsValidWhyNot => Value == null ? "ReplaceImageAction is null" : "";

    public override IGH_Goo Duplicate() => new ReplaceImageActionGoo(Value);

    public override string ToString() => Value is null ? "null" : $"ReplaceImageAction ({Value.ImageSource})";
}