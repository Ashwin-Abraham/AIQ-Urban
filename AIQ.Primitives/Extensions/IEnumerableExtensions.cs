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

    public static IEnumerable<T> PickRandom<T>(this IEnumerable<T> items, int count, Random random)
    {
        var randomSelection = items
            .OrderBy(_ => random.Next())
            .Take(count);

        return randomSelection;
    }

    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> items, Random? random = null)
    {
        var itemList = items.ToList();
        var indexList = Enumerable.Range(0, itemList.Count).ToList();
        int count = itemList.Count;

        random ??= new Random();

        for(int i = 0; i < count; i++)
        {
            var nextIndexListPosition = random.Next(0, indexList.Count - 1);
            
            var nextIndex = indexList[nextIndexListPosition];
            indexList.RemoveAt(nextIndexListPosition);

            var nextItem = itemList[nextIndex];

            yield return nextItem;
        }
    }
}
