using System;
using System.Collections.Generic;

namespace ServiceTester.Upload
{
    public class UploadModel
    {
        public string ModelType { get; set; }

        public DateTime Expiration { get; set; }

        public IReadOnlyCollection<UploadData> Data { get; set; }
    }
}