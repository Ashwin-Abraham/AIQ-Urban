using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Google.Apis.Http;

namespace AIQ.GH.Exports.Types;
public class GoogleSlidesCredentialGoo : GH_Goo<IConfigurableHttpClientInitializer>
{
    public override string TypeName => "Google Slides Credential";
    public override string TypeDescription => "Google Slides OAuth credential";

    public GoogleSlidesCredentialGoo() { }

    public GoogleSlidesCredentialGoo(IConfigurableHttpClientInitializer value) => Value = value;

    public override bool IsValid => Value != null;
    public override string IsValidWhyNot => Value == null ? "Credential is null" : "";

    public override IGH_Goo Duplicate() => new GoogleSlidesCredentialGoo(Value);

    public override string ToString() => Value is null ? "null" : "Google Slides Credential";
}
