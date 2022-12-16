using System.Collections.Generic;
using Newtonsoft.Json;

namespace ServiceTester.Upload
{
    public class UploadData
    {
        [JsonProperty("uId")]
        public long UserId { get; set; }

        public ICollection<UploadRecord> Records { get; set; }
    }
}