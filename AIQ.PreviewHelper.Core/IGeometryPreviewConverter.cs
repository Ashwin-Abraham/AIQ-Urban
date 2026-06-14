using Rhino;

namespace AIQ.PreviewHelper.Core;

/// <summary>
/// Holds the logic for converting an object into a list of elements suitable for previewing in the <see cref="PreviewHub"/>
/// and the <see cref="PreviewSink"/>."/>
/// </summary>
/// <param name="PreviewStyle">The preview style index of the preview helper. 
/// This allows for the presetting of complex preview logic</param>
/// <param name="Convert">The logic used to convert an object into previewable geometry</param>
public interface IGeometryPreviewConverter
{
    /// <summary>
    /// Checks if an object can be converted by this converter.
    /// </summary>
    public bool IsPreviewable(object @object);

    /// <summary>
    /// Converts the object using the specified preview style preset.
    /// </summary>
    public void AddToPreviewDoc(object @object, RhinoDoc doc);
}