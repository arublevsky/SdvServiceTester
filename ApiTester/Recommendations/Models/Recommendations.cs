using System.Text.Json.Serialization;

namespace ApiTester.Recommendations.Models;

public class Recommendations
{
    [JsonPropertyName("expiration")]
    public DateTime Expiration { get; set; }

    [JsonPropertyName("data")]
    public List<Data> Data { get; } = new();
}