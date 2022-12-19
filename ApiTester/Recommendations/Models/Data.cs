using System.Text.Json.Serialization;

namespace ApiTester.Recommendations.Models;

public class Data
{
    [JsonPropertyName("uId")]
    public long UserId { get; set; }

    [JsonPropertyName("records")]
    public List<Record> Records { get; set; }
}