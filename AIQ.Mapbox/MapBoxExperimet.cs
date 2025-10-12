using Mapbox.Vector.Tile;
using System.Net.Http;

namespace AIQ.Mapbox;
public class MapBoxExperimet
{
    private static readonly HttpClient _httpClient = new HttpClient();

    public static async Task GetMapboxData()
    {
        string ACCESS_TOKEN = "YOUR_MAPBOX_TOKEN";

        int zoom = 16;
        int tileX = 33123;
        int tileY = 21545;

        string url = $"https://api.mapbox.com/v4/mapbox.mapbox-streets-v8/{zoom}/{tileX}/{tileY}.mvt?access_token={ACCESS_TOKEN}";
        byte[] tileData = await _httpClient.GetByteArrayAsync(url);

        // Parse the vector tile
        using var stream = new MemoryStream(tileData);
        var layers = VectorTileParser.Parse(stream);

        foreach (var layer in layers)
        {
            Console.WriteLine($"Layer: {layer.Name}, Features: {layer.VectorTileFeatures.Count}");

            for (int i = 0; i < layer.VectorTileFeatures.Count; i++)
            {
                var feature = layer.VectorTileFeatures[i];
                var geomType = feature.GeometryType;
                var geometry = feature.Geometry;
                var tags = feature.Attributes;

                // Mapbox features documentation for further development:
                // https://docs.mapbox.com/data/tilesets/reference/mapbox-streets-v8/#airport_label
                //Console.WriteLine($"  Feature {i}: {geomType}, {tags.Count} tags");

                //// Optionally decode geometry
                //List<List<Point2d<long>>> geometry = feature.Geometry<int>();
                //Console.WriteLine($"    Geometry parts: {geometry.Count}");
            }
        }
    }

}
