using System.Text.Json.Serialization;

namespace ApiTester.Recommendations.Models;

public class Record
{
    [JsonPropertyName("rId")]
    public long RId { get; set; }

    [JsonPropertyName("s")]
    public long S { get; set; }
}