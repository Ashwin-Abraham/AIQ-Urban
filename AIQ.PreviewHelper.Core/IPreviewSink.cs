namespace AIQ.PreviewHelper.Core;

/// <summary>
/// The preview methods live inside kope.PreviewHelper.Converters, which depends on the kope.X project where
/// a class is defined. The kope.X project cannot make use of the preview methods living in kope.PreviewHelper.Converters 
/// without resulting in a circular dependancy.
/// 
/// This interface acts as an adapter to hold logic for converting different objects into previewable data types
/// without resulting in a circular dependancy.
/// 
/// This adapter is to be consumed by <see cref="PreviewHub"/>
/// </summary>
public interface IPreviewSink
{
    /// <summary>
    /// Previews the provided elements.
    /// </summary>
    /// <param name="elements"></param>
    void PreviewDoc(bool useWebViewer = false);

    /// <summary>
    /// Converts the provided object into a list of elements suitable for previewing.
    /// </summary>
    /// <param name="object">The object to convert to previewable elements</param>
    /// <param name="previewStyle">The preset style of the converter to use for the preview. 
    /// Intended to point more complex preview logic for complex objects</param>
    /// <returns></returns>
    public void AddToPreviewDoc(object @object);
}
