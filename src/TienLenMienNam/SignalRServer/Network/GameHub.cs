using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using CardGameServer.Game;
using CardGameServer.Models; // Quan trọng: Để nhận diện class Card
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
            catch (Exception ex)
            {
                Console.WriteLine($"OnConnected error: {ex.Message}");
            }
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
            catch (Exception ex)
            {
                Console.WriteLine($"OnDisconnected error: {ex.Message}");
            }
        }

        // ================================================================
        // CLIENT METHODS (CÁC HÀM CLIENT GỌI LÊN)
        // ================================================================

        public async Task Join(string name, string roomId)
        {
            var session = _connectionManager.GetSession(Context.ConnectionId);
            if (session == null) return;

            var msg = new NetMessage
            {
                Type = "join",
                Payload = JObject.FromObject(new { name, roomId })
            };
            _engine.HandleMessage(session, msg);
        }

        public async Task Ready(bool ready)
        {
            var session = _connectionManager.GetSession(Context.ConnectionId);
            if (session == null) return;

            var msg = new NetMessage
            {
                Type = "ready",
                Payload = JObject.FromObject(new { ready })
            };
            _engine.HandleMessage(session, msg);
        }

        // --- HÀM PLAY: ĐÃ FIX TÌM PHÒNG ---
        public async Task Play(string[] cards)
        {
            var session = _connectionManager.GetSession(Context.ConnectionId);
            if (session == null) return;

            // 1. Tìm phòng chơi (Thêm logic dự phòng FindRoom)
            var room = _engine.GetRoom(session.PlayerId);
            if (room == null && !string.IsNullOrEmpty(session.RoomId)) 
                room = _engine.FindRoom(session.RoomId); 
            
            // 2. Kiểm tra luật (Validation)
            if (room != null)
            {
                // Convert string[] -> List<Card>
                var playCards = cards.Select(code => Card.FromCode(code)).ToList();
                
                // Lấy nước đi hiện tại trên bàn (là object Move)
                var currentMove = room.CurrentTrick; 

                bool isValid = false;

                // A. Trường hợp bàn trống (hoặc đánh mở màn, hoặc cả làng bỏ lượt quay về mình)
                if (currentMove == null)
                {
                    // Chỉ cần bộ bài hợp lệ (Đôi, Sảnh, Rác...) là được
                    if (CardHelper.GetHandType(playCards) != HandType.None)
                    {
                        isValid = true;
                    }
                }
                // B. Trường hợp chặn bài (Đè)
                else
                {
                    // Lấy danh sách bài từ thuộc tính .Cards của Move
                    var cardsOnTable = currentMove.Cards;
                    if (cardsOnTable != null)
                    {
                        isValid = CardHelper.CanBeat(cardsOnTable, playCards);
                    }
                }

                // Nếu không hợp lệ -> Báo lỗi và không xử lý tiếp
                if (!isValid)
                {
                    await Clients.Caller.SendAsync("Error", "Bài không hợp lệ hoặc không chặt được!");
                    return; 
                }
            }

            // 3. Nếu hợp lệ -> Chuyển cho Engine
            var msg = new NetMessage
            {
                Type = "play",
                Payload = JObject.FromObject(new { cards })
            };
            _engine.HandleMessage(session, msg);
        }

        public async Task Pass()
        {
            var session = _connectionManager.GetSession(Context.ConnectionId);
            if (session == null) return;

            var msg = new NetMessage
            {
                Type = "pass"
            };
            _engine.HandleMessage(session, msg);
        }

        public async Task Chat(string text)
        {
            var session = _connectionManager.GetSession(Context.ConnectionId);
            if (session == null) return;

            var msg = new NetMessage
            {
                Type = "chat",
                Payload = JObject.FromObject(new { text })
            };
            _engine.HandleMessage(session, msg);
        }

        public async Task Ping(JObject payload)
        {
            var session = _connectionManager.GetSession(Context.ConnectionId);
            if (session == null) return;

            var msg = new NetMessage
            {
                Type = "ping",
                Payload = payload
            };
            _engine.HandleMessage(session, msg);
        }

        // ================================================================
        // TÍNH NĂNG RESET GAME (XIN CHƠI LẠI) - ĐÃ SỬA
        // ================================================================

        public async Task RequestReset()
        {
            var session = _connectionManager.GetSession(Context.ConnectionId);
            if (session == null) return;
            
            // Tìm phòng an toàn hơn
            var room = _engine.GetRoom(session.PlayerId);
            if (room == null && !string.IsNullOrEmpty(session.RoomId))
                room = _engine.FindRoom(session.RoomId);

            if (room != null)
            {
                // SỬA LỖI Ở ĐÂY: Gửi cho TẤT CẢ mọi người (kể cả người bấm)
                // Để popup hiện lên ngay lập tức cho bạn thấy
                foreach (var p in room.SnapshotPlayers())
                {
                    p.SendEvent("ask_reset", new { requester = session.PlayerName });
                }
            }
        }

        public async Task AcceptReset()
        {
            var session = _connectionManager.GetSession(Context.ConnectionId);
            if (session == null) return;

            // Tìm phòng an toàn hơn
            var room = _engine.GetRoom(session.PlayerId);
            if (room == null && !string.IsNullOrEmpty(session.RoomId))
                room = _engine.FindRoom(session.RoomId);

            if (room != null)
            {
                // Gọi hàm Reset trong GameRoom
                room.ResetGame();

                // Thông báo cho tất cả client để xóa bàn
                foreach (var p in room.SnapshotPlayers())
                {
                    p.SendEvent("game_reset", null);
                    
                    // Cập nhật lại trạng thái bàn (về Lobby, xóa bài trên tay)
                    var pub = room.BuildPublicState();
                    var per = room.BuildPersonalState(p.PlayerId);
                    
                    p.SendState(new 
                    { 
                        publicState = pub, 
                        personalState = per 
                    });
                }
            }
        }
    }
}