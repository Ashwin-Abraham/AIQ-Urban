using System;
using System.Drawing;
using AIQ.Exports.GoogleSlides;
using AIQ.GH.Exports.Types;
using AIQ.GH.Exports.Utils;
using Grasshopper.Kernel;

namespace AIQ.GH.Exports.Components.GoogleSlides;
public class GoogleSlidesSignInComponent : GH_Component
{
    public GoogleSlidesSignInComponent()
        : base("Google Slides Sign In", "GSignIn",
               "Sign in to Google Slides with a personal account",
               "AIQ", "Google Slides")
    {
    }

    public override Guid ComponentGuid => ComponentGuids.GoogleSlidesSignIn;

    protected override Bitmap Icon => IconLoader.LoadIcon("Google Slides Sign In.png");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Credential", "Cred", "Google Slides credential", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        try
        {
            var credential = AsyncHelper.RunSync(ct => GoogleSlidesAuth.SignInAsync(ct), Constants.CommonToken);
            DA.SetData(0, new GoogleSlidesCredentialGoo(credential));
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Sign in failed: {ex.Message}");
        }
    }
}
