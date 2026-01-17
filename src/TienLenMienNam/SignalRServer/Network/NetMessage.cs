using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CardGameServer.Network
{
    public sealed class NetMessage
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("payload")]
        public JObject Payload { get; set; }
    }
}