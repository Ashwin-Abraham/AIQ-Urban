using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using AIQ.Exports.GoogleSlides.Actions;

namespace AIQ.GH.Exports.Types;
public class ReplaceTextActionGoo : GH_Goo<ReplaceTextAction>
{
    public override string TypeName => "Replace Text Action";
    public override string TypeDescription => "Action to replace text in Google Slides";

    public ReplaceTextActionGoo() { }

    public ReplaceTextActionGoo(ReplaceTextAction value) => Value = value;

    public override bool IsValid => Value != null;
    public override string IsValidWhyNot => Value == null ? "ReplaceTextAction is null" : "";

    public override IGH_Goo Duplicate() => new ReplaceTextActionGoo(Value);

    public override string ToString() => Value is null ? "null" : $"ReplaceTextAction ({Value.FindText} -> {Value.ReplacementText})";
}