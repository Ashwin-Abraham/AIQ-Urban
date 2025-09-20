using ArchSitePlan.Core.Models;
using Newtonsoft.Json;

namespace ArchSitePlan.Core.ModelEditActions;
public class HistoryAction:IModelEditAction
{
    private static List<string> _historyJson = new();
    private static List<string> _futureJson = new();

    private const int _maxHistory = 10;
    private static JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
    {
        // This is to prevent duplication of an object that appears twice in the model. Like 2 parcels bearing the same landuse object
        PreserveReferencesHandling = PreserveReferencesHandling.Objects,

        // Explicit typing for interfaces like ICurve
        TypeNameHandling = TypeNameHandling.All,
        Formatting = Formatting.Indented
    };

    public static void StoreState()
    {
        _historyJson.Add(JsonConvert.SerializeObject(CoreModelSingelton.Instance, _jsonSettings));

        if (_historyJson.Count > _maxHistory)
            _historyJson.RemoveAt(0);

        _futureJson.Clear();
    }

    public static bool Undo()
    {
        if (_historyJson.Count == 0) 
            return false;

        var model = JsonConvert.DeserializeObject<CoreModel>(_historyJson[^1], _jsonSettings);
        if (model == null)
            return false;

        var currentState = JsonConvert.SerializeObject(CoreModelSingelton.Instance, _jsonSettings);
        CoreModelSingelton.Instance = model;

        _historyJson.RemoveAt(_historyJson.Count - 1);
        _futureJson = _futureJson.Prepend(currentState).ToList();

        return true;
    }

    public static bool Redo()
    {
        if (_futureJson.Count == 0)
            return false;

        if(_historyJson.Count >= _maxHistory)
            return false;

        var futureModel = JsonConvert.DeserializeObject<CoreModel>(_futureJson[0], _jsonSettings);
        if (futureModel == null)
            return false;

        var currentState = JsonConvert.SerializeObject(CoreModelSingelton.Instance, _jsonSettings);
        CoreModelSingelton.Instance = futureModel;

        _futureJson.RemoveAt(0);
        _historyJson.Add(currentState);
        return true;
    }
}
