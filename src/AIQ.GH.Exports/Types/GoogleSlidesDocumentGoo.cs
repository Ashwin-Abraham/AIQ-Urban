using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using AIQ.Exports.GoogleSlides;

namespace AIQ.GH.Exports.Types;
public class GoogleSlidesDocumentGoo : GH_Goo<GoogleSlidesDocument>
{
    public override string TypeName => "Google Slides Document";
    public override string TypeDescription => "Google Slides document with pending actions";

    public GoogleSlidesDocumentGoo() { }

    public GoogleSlidesDocumentGoo(GoogleSlidesDocument value) => Value = value;

    public override bool IsValid => Value != null;
    public override string IsValidWhyNot => Value == null ? "GoogleSlidesDocument is null" : "";

    public override IGH_Goo Duplicate() =>
        Value is null ? 
        new GoogleSlidesDocumentGoo()
        : new GoogleSlidesDocumentGoo(Value.Clone());

    public override string ToString() => Value is null ? "null" : $"GoogleSlidesDocument ({Value.PendingActions.Count} actions)";
}