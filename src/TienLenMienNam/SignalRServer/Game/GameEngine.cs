using System;
using System.Collections.Generic;
using System.Linq;
using CardGameServer.Models;
using CardGameServer.Network;
using Newtonsoft.Json.Linq;

namespace CardGameServer.Game
{
    public sealed class GameEngine
    {
        private readonly object _gate = new object();
        private readonly Dictionary<string, GameRoom> _rooms = new Dictionary<string, GameRoom>(StringComparer.OrdinalIgnoreCase);

        // --- SỬA LẠI HÀM NÀY ĐỂ KHÔNG BỊ LỖI ---
        public GameRoom GetRoom(string playerId)
        {
            lock (_gate)
            {
                foreach (var room in _rooms.Values)
                {
                    // Dùng SnapshotPlayers() để kiểm tra danh sách người chơi
                    if (room.SnapshotPlayers().Any(p => p.PlayerId == playerId))
                    {
                        return room;
                    }
                }
                return null;
            }
        }
        // ----------------------------------------

        public GameRoom GetOrCreateRoom(string roomId)
        {
            lock (_gate)
            {
                if (!_rooms.TryGetValue(roomId, out var room))
                {
                    room = new GameRoom(roomId);
                    _rooms[roomId] = room;
                }
                return room;
            }
        }

        public GameRoom FindRoom(string roomId)
        {
            lock (_gate)
            {
                _rooms.TryGetValue(roomId, out var room);
                return room;
            }
        }

        public void OnDisconnected(IPlayerSession session)
        {
            if (session == null) return;
            // Tìm phòng mà user này đang ở
            var room = !string.IsNullOrWhiteSpace(session.RoomId) ? FindRoom(session.RoomId) : GetRoom(session.PlayerId);
            
            if (room != null)
            {
                room.Leave(session);
            }
        }

        public void HandleMessage(IPlayerSession session, NetMessage msg)
        {
            if (session == null || msg == null) return;

            switch (msg.Type)
            {
                case "join": HandleJoin(session, msg); break;
                case "ready": HandleReady(session, msg); break;
                case "play": HandlePlay(session, msg); break;
                case "pass": HandlePass(session); break;
                case "chat": HandleChat(session, msg); break;
                case "ping": session.SendPong(msg.RequestId, msg.Payload); break;
                default: session.SendError("UNKNOWN_TYPE", "Unknown type: " + msg.Type); break;
            }
        }

        private void HandleJoin(IPlayerSession session, NetMessage msg)
        {
            var payload = msg.Payload ?? new JObject();
            string name = payload.Value<string>("name") ?? "Player";
            string roomId = payload.Value<string>("roomId") ?? "ROOM-1";

            var room = GetOrCreateRoom(roomId);
            if (!room.Join(session, name, out string error))
            {
                session.SendError("JOIN_FAILED", error);
                return;
            }
            BroadcastRoomState(room, "player_joined", new { playerId = session.PlayerId, name = session.PlayerName });
        }

        private void HandleReady(IPlayerSession session, NetMessage msg)
        {
            var room = GetRoom(session.PlayerId);
            if (room == null) { session.SendError("NOT_IN_ROOM", "Join a room first."); return; }

            bool ready = (msg.Payload != null) && (msg.Payload.Value<bool?>("ready") ?? false);
            if (!room.SetReady(session, ready, out string error))
            {
                session.SendError("READY_FAILED", error);
                return;
            }

            if (room.CanStart(out _))
            {
                room.StartGame();
                BroadcastRoomState(room, "game_started", null);
            }
            else
            {
                BroadcastRoomState(room, "ready_updated", new { playerId = session.PlayerId, ready });
            }
        }

        private void HandlePlay(IPlayerSession session, NetMessage msg)
        {
            var room = GetRoom(session.PlayerId);
            if (room == null) { session.SendError("NOT_IN_ROOM", "Join a room first."); return; }

            var payload = msg.Payload ?? new JObject();
            var arr = payload["cards"] as JArray;
            if (arr == null) { session.SendError("BAD_PAYLOAD", "Missing cards array."); return; }

            var cards = new List<Card>();
            foreach (var token in arr) cards.Add(Card.FromCode(token.ToString()));

            // Gọi logic Play trong Room (Logic kiểm tra luật chặt nằm ở GameHub, 
            // nhưng Room.Play cũng nên có check cơ bản)
            if (!room.Play(session, cards, out string error, out string info))
            {
                session.SendError("INVALID_MOVE", error);
                SendRoomStateToPlayer(room, session);
                return;
            }

            BroadcastRoomState(room, info == "WINNER" ? "game_finished" : "played", new
            {
                by = session.PlayerId,
                cards = cards.Select(c => c.ToCode()).ToList()
            });
        }

        private void HandlePass(IPlayerSession session)
        {
            var room = GetRoom(session.PlayerId);
            if (room == null) { session.SendError("NOT_IN_ROOM", "Join a room first."); return; }

            if (!room.Pass(session, out string error))
            {
                session.SendError("PASS_FAILED", error);
                return;
            }
            BroadcastRoomState(room, "passed", new { by = session.PlayerId });
        }

        private void HandleChat(IPlayerSession session, NetMessage msg)
        {
            var room = GetRoom(session.PlayerId);
            if (room == null) return;
            string text = (msg.Payload != null) ? (msg.Payload.Value<string>("text") ?? "") : "";
            BroadcastEvent(room, "chat", new { from = session.PlayerName, text });
        }

        private void BroadcastRoomState(GameRoom room, string eventName, object evtPayload)
        {
            BroadcastEvent(room, eventName, evtPayload);
            foreach (var p in room.SnapshotPlayers()) SendRoomStateToPlayer(room, p);
        }

        private void BroadcastEvent(GameRoom room, string name, object payload)
        {
            foreach (var p in room.SnapshotPlayers()) p.SendEvent(name, payload);
        }

        private void SendRoomStateToPlayer(GameRoom room, IPlayerSession player)
        {
            player.SendState(new { publicState = room.BuildPublicState(), personalState = room.BuildPersonalState(player.PlayerId) });
        }
    }
}