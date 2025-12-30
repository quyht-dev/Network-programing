// CardGameServer/Network/PlayerSession.cs
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

    public sealed class PlayerSession
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        public string PlayerId { get; private set; }
        public string PlayerName { get; set; }
        public string RoomId { get; set; }

        public DateTime LastSeenUtc { get; private set; }

        public PlayerSession(TcpClient client, string playerId)
        {
            _client = client;
            _client.NoDelay = true;
            _stream = _client.GetStream();

            PlayerId = playerId;
            PlayerName = "Player";
            LastSeenUtc = DateTime.UtcNow;
        }

        public NetworkStream Stream { get { return _stream; } }
        public bool IsConnected
        {
            get
            {
                try { return _client != null && _client.Connected; }
                catch { return false; }
            }
        }

        public void Touch()
        {
            LastSeenUtc = DateTime.UtcNow;
        }

        // ======== Send helpers ========

        public void SendWelcome()
        {
            Send("welcome", null, new { playerId = PlayerId });
        }

        public void SendState(object payload)
        {
            Send("state", null, payload);
        }

        public void SendEvent(string name, object payload)
        {
            Send("event", null, new { name = name, data = payload });
        }

        public void SendError(string code, string message)
        {
            Send("error", null, new { code = code, message = message });
        }

        public void SendPong(string requestId, JObject pingPayload)
        {
            // echo timestamp nếu có
            Send("pong", requestId, pingPayload ?? new JObject());
        }

        public void Send(string type, string requestId, object payloadObj)
        {
            var msg = new NetMessage
            {
                Type = type,
                RequestId = requestId,
                Payload = payloadObj == null ? new JObject() : JObject.FromObject(payloadObj)
            };

            byte[] jsonBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msg));
            // length-prefix 4 bytes
            byte[] header = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(jsonBytes.Length));

            // serialize writes (tránh 2 thread write chồng lên nhau)
            _sendLock.Wait();
            try
            {
                _stream.Write(header, 0, 4);
                _stream.Write(jsonBytes, 0, jsonBytes.Length);
                _stream.Flush();
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public void Close()
        {
            try { _stream.Close(); } catch { }
            try { _client.Close(); } catch { }
        }
    }
}
