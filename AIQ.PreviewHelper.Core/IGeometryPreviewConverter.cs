using Rhino;

namespace AIQ.PreviewHelper.Core;

/// <summary>
/// Converts a domain object into Rhino geometry and adds it to a headless <see cref="RhinoDoc"/>
/// for previewing via <see cref="PreviewHub"/> or <see cref="PreviewSink"/>.
/// </summary>
public interface IGeometryPreviewConverter
{
    /// <summary>
    /// Checks if an object can be converted by this converter.
    /// </summary>
    bool IsPreviewable(object @object);

    /// <summary>
    /// Converts the object into Rhino geometry and adds it to the given document.
    /// </summary>
    void AddToPreviewDoc(object @object, RhinoDoc doc);
}