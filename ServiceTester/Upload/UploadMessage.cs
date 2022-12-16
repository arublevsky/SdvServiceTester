using Gems.MessageBus.Emitting;

namespace ServiceTester.Upload
{
    public class UploadMessage : BaseMessage
    {
        public UploadModel Recommendations { get; set; }
    }
}