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

        // --- SỬA ĐỔI CHÍNH Ở CÁC HÀM DƯỚI ĐÂY ---

        public void SendWelcome()
        {
            // Thay vì bọc trong "ReceiveMessage", ta gọi thẳng hàm "Welcome"
            _clientProxy.SendAsync("Welcome", PlayerId);
        }

        public void SendState(object payload)
        {
            // Gọi thẳng hàm "GameState" để Client cập nhật bàn chơi
            _clientProxy.SendAsync("GameState", payload);
        }

        public void SendEvent(string name, object payload)
        {
            // Xử lý các sự kiện game đặc biệt
            if (name == "player_joined")
            {
                _clientProxy.SendAsync("PlayerJoined", payload);
            }
            else
            {
                // Các event khác gửi tạm qua ReceiveMessage hoặc tạo hàm riêng
                 _clientProxy.SendAsync("ReceiveMessage", $"Event: {name}");
            }
        }

        public void SendError(string code, string message)
        {
            // Gọi thẳng hàm "Error"
            _clientProxy.SendAsync("Error", $"{code}: {message}");
        }

        public void SendPong(string requestId, JObject pingPayload)
        {
            // Pong giữ nguyên hoặc gửi qua channel riêng nếu cần
             _clientProxy.SendAsync("Pong", pingPayload);
        }

        // Hàm generic này ít dùng hơn trong SignalR hiện đại, nhưng giữ lại để thỏa mãn Interface
        public async void Send(string type, string requestId, object payloadObj)
        {
            try
            {
                // Fallback: Nếu code cũ gọi hàm này, ta cố gắng map sang hàm chuẩn
                switch (type)
                {
                    case "welcome": 
                        await _clientProxy.SendAsync("Welcome", payloadObj); 
                        break;
                    case "state": 
                        await _clientProxy.SendAsync("GameState", payloadObj); 
                        break;
                    default:
                        // Nếu không biết type là gì thì gửi dạng tin nhắn chung
                        await _clientProxy.SendAsync("ReceiveMessage", $"[{type}] Data update...");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send error: {ex.Message}");
            }
        }
    }
}