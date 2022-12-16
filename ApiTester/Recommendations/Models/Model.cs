using System.Text.Json.Serialization;

namespace ApiTester.Recommendations.Models;

public class Model
{
    public long SetId { get; set; }

    public long SetsTotal { get; set; }

    public DateTime Updated { get; set; }

    [JsonPropertyName("recommendations")]
    public Recommendations Recommendations { get; set; }
}