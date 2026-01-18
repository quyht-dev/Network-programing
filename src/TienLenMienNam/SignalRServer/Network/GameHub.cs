using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using CardGameServer.Game;
using CardGameServer.Models;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;

namespace CardGameServer.Network
{
    public class GameHub : Hub
    {
        private readonly GameEngine _engine;
        private readonly ConnectionManager _connectionManager;

        public GameHub(GameEngine engine, ConnectionManager connectionManager)
        {
            _engine = engine;
            _connectionManager = connectionManager;
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var session = new SignalRPlayerSession(
                    Context.ConnectionId,
                    Guid.NewGuid().ToString("N"),
                    Clients.Caller
                );
                _connectionManager.AddSession(session);
                session.SendWelcome();
                await base.OnConnectedAsync();
            }
            catch (Exception ex) { Console.WriteLine($"OnConnected error: {ex.Message}"); }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var session = _connectionManager.GetSession(Context.ConnectionId);
                if (session != null)
                {
                    _engine.OnDisconnected(session);
                    _connectionManager.RemoveSession(Context.ConnectionId);
                }
                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex) { Console.WriteLine($"OnDisconnected error: {ex.Message}"); }
        }

        public async Task Join(string name, string roomId)
        {
            var session = _connectionManager.GetSession(Context.ConnectionId);
            if (session == null) return;
            var msg = new NetMessage { Type = "join", Payload = JObject.FromObject(new { name, roomId }) };
            _engine.HandleMessage(session, msg);
        }

        public async Task Ready(bool ready)
        {
            var session = _connectionManager.GetSession(Context.ConnectionId);
            if (session == null) return;
            var msg = new NetMessage { Type = "ready", Payload = JObject.FromObject(new { ready }) };
            _engine.HandleMessage(session, msg);
        }

        // --- HÀM PLAY ĐÃ SỬA LỖI ---
        public async Task Play(string[] cards)
        {
            var session = _connectionManager.GetSession(Context.ConnectionId);
            if (session == null) return;

            // 1. Lấy thông tin phòng
            // (Lúc này GameEngine.GetRoom đã được sửa ở trên nên gọi an toàn)
            var room = _engine.GetRoom(session.PlayerId); 
            
            if (room != null)
            {
                var playCards = cards.Select(code => Card.FromCode(code)).ToList();
                var currentTrickMove = room.CurrentTrick; // Đây là đối tượng Move

                bool isValid = false;

                // TRƯỜNG HỢP 1: Bàn trống
                if (currentTrickMove == null)
                {
                    // SỬA LỖI: Thêm (playCards) vào
                    if (CardHelper.GetHandType(playCards) != HandType.None)
                    {
                        isValid = true;
                    }
                }
                // TRƯỜNG HỢP 2: Đè bài
                else
                {
                    // SỬA LỖI: Truy cập vào thuộc tính Cards của Move (giả sử tên là Cards)
                    // Nếu trong Move bạn đặt tên là PlayCards thì sửa thành .PlayCards
                    var cardsOnTable = currentTrickMove.Cards; 
                    
                    if (cardsOnTable != null)
                    {
                        isValid = CardHelper.CanBeat(cardsOnTable, playCards);
                    }
                }

                if (!isValid)
                {
                    await Clients.Caller.SendAsync("Error", "Bài không hợp lệ hoặc không chặt được!");
                    return; 
                }
            }

            var msg = new NetMessage { Type = "play", Payload = JObject.FromObject(new { cards }) };
            _engine.HandleMessage(session, msg);
        }

        public async Task Pass()
        {
            var session = _connectionManager.GetSession(Context.ConnectionId);
            if (session == null) return;
            var msg = new NetMessage { Type = "pass" };
            _engine.HandleMessage(session, msg);
        }

        public async Task Chat(string text)
        {
            var session = _connectionManager.GetSession(Context.ConnectionId);
            if (session == null) return;
            var msg = new NetMessage { Type = "chat", Payload = JObject.FromObject(new { text }) };
            _engine.HandleMessage(session, msg);
        }

        public async Task Ping(JObject payload)
        {
            var session = _connectionManager.GetSession(Context.ConnectionId);
            if (session == null) return;
            var msg = new NetMessage { Type = "ping", Payload = payload };
            _engine.HandleMessage(session, msg);
        }
    }
}