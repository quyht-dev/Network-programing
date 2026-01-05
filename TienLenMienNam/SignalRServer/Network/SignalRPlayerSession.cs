// CardGameServer/Network/SignalRPlayerSession.cs
using System;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;

namespace CardGameServer.Network
{
    public interface IPlayerSession
    {
        string PlayerId { get; }
        string PlayerName { get; set; }
        string RoomId { get; set; }
        DateTime LastSeenUtc { get; set; }

        void SendWelcome();
        void SendState(object payload);
        void SendEvent(string name, object payload);
        void SendError(string code, string message);
        void SendPong(string requestId, JObject pingPayload);
        void Send(string type, string requestId, object payloadObj);
    }

    public sealed class SignalRPlayerSession : IPlayerSession
    {
        public string ConnectionId { get; }
        public string PlayerId { get; }
        public string PlayerName { get; set; }
        public string RoomId { get; set; }
        public DateTime LastSeenUtc { get; set; }

        private readonly IClientProxy _clientProxy;

        public SignalRPlayerSession(string connectionId, string playerId, IClientProxy clientProxy)
        {
            ConnectionId = connectionId;
            PlayerId = playerId;
            _clientProxy = clientProxy;
            LastSeenUtc = DateTime.UtcNow;
            PlayerName = "Player";
        }

        public void Touch()
        {
            LastSeenUtc = DateTime.UtcNow;
        }

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
            Send("pong", requestId, pingPayload ?? new JObject());
        }

        public async void Send(string type, string requestId, object payloadObj)
        {
            try
            {
                var msg = new NetMessage
                {
                    Type = type,
                    RequestId = requestId,
                    Payload = payloadObj == null ? new JObject() : JObject.FromObject(payloadObj)
                };
                await _clientProxy.SendAsync("ReceiveMessage", msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send error: {ex.Message}");
            }
        }
    }
}