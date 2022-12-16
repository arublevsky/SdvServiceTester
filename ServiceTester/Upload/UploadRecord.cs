using Newtonsoft.Json;

namespace ServiceTester.Upload
{
    public class UploadRecord
    {
        [JsonProperty("rId")]
        public long RecommendedId { get; set; }

        [JsonProperty("s")]
        public decimal Score { get; set; }
    }
}