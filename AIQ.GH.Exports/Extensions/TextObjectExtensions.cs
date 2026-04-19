using Rhino.DocObjects;

namespace AIQ.GH.Exports.Extensions;
public static class TextObjectExtensions
{
    public static bool SetText(this TextObject textObject, string newText)
    {
        try
        {
            var textEntity = textObject.TextGeometry;
            if (textEntity is null)
                return false;

            textEntity.PlainText = newText;
            textObject.CommitChanges();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
