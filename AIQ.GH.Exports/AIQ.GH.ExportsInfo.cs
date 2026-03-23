using System;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;

namespace AIQ.GH.Exports;
public class AIQ_GH_ExportsInfo : GH_AssemblyInfo
{
    public override string Name => "AIQ.GH.Exports";

    //Return a 24x24 pixel bitmap to represent this GHA library.
    public override Bitmap Icon => null;

    //Return a short string describing the purpose of this GHA library.
    public override string Description =>
        "Layout management and PDF export components for Grasshopper. " +
        "Create layouts from templates, configure detail views, manage layer visibility, " +
        "and export to PDF.";

    public override Guid Id => new Guid("5f2fd04a-4177-4998-9730-780a6c572ee0");

    //Return a string identifying you or your company.
    public override string AuthorName => "AIQ";

    //Return a string representing your preferred contact details.
    public override string AuthorContact => "";

    //Return a string representing the version.  This returns the same version as the assembly.
    public override string AssemblyVersion => GetType().Assembly.GetName().Version.ToString();
}