using System.Reflection;
using System.Text.Json;

namespace ArchSitePlan.Licensing;

public class License(string customerID, string subscriptionID, long lastActivated)
{
    public string CustomerID { get; set; } = customerID;
    public string SubscriptionID { get; set; } = subscriptionID;
    public long LastActivation { get; set; } = lastActivated;

    public void WriteToFile(string path)
    {
        string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public static License? FromFile(string path)
    {
        if (!File.Exists(path))
            return null;

        string json = File.ReadAllText(path);
        License? license = JsonSerializer.Deserialize<License>(json);

        return license;
    }

}

