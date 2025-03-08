namespace AIQ.Primitives.Extensions;
public static class IEnumerableExtensions
{
    public static List<T> ObjectToList<T>(this T obj)
    {
        return new List<T>() { obj };
    }

    public static List<T> Reversed<T>(this List<T> list)
    {
        List<T> reversedList = new List<T>(list);
        reversedList.Reverse();
        return reversedList;
    }
}
