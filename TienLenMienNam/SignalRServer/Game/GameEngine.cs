// CardGameServer/Game/GameEngine.cs
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

        public GameRoom GetOrCreateRoom(string roomId)
        {
            lock (_gate)
            {
                GameRoom room;
                if (!_rooms.TryGetValue(roomId, out room))
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
                GameRoom room;
                _rooms.TryGetValue(roomId, out room);
                return room;
            }
        }

        public void OnDisconnected(IPlayerSession session)
        {
            if (session == null) return;
            if (string.IsNullOrWhiteSpace(session.RoomId)) return;

            var room = FindRoom(session.RoomId);
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
                case "join":
                    HandleJoin(session, msg);
                    break;

                case "ready":
                    HandleReady(session, msg);
                    break;

                case "play":
                    HandlePlay(session, msg);
                    break;

                case "pass":
                    HandlePass(session);
                    break;

                case "chat":
                    HandleChat(session, msg);
                    break;

                case "ping":
                    session.SendPong(msg.RequestId, msg.Payload);
                    break;

                default:
                    session.SendError("UNKNOWN_TYPE", "Unknown type: " + msg.Type);
                    break;
            }
        }

        private void HandleJoin(IPlayerSession session, NetMessage msg)
        {
            var payload = msg.Payload ?? new JObject();
            string name = payload.Value<string>("name") ?? "Player";
            string roomId = payload.Value<string>("roomId") ?? "ROOM-1";

            var room = GetOrCreateRoom(roomId);

            string error;
            if (!room.Join(session, name, out error))
            {
                session.SendError("JOIN_FAILED", error);
                return;
            }

            // broadcast event + state
            BroadcastRoomState(room, "player_joined", new { playerId = session.PlayerId, name = session.PlayerName });
        }

        private void HandleReady(IPlayerSession session, NetMessage msg)
        {
            if (string.IsNullOrWhiteSpace(session.RoomId))
            {
                session.SendError("NOT_IN_ROOM", "Join a room first.");
                return;
            }

            var room = FindRoom(session.RoomId);
            if (room == null)
            {
                session.SendError("ROOM_NOT_FOUND", "Room not found.");
                return;
            }

            bool ready = (msg.Payload != null) && (msg.Payload.Value<bool?>("ready") ?? false);

            string error;
            if (!room.SetReady(session, ready, out error))
            {
                session.SendError("READY_FAILED", error);
                return;
            }

            // nếu đủ điều kiện thì start
            string startErr;
            if (room.CanStart(out startErr))
            {
                room.StartGame();
                BroadcastRoomState(room, "game_started", null);
            }
            else
            {
                // chỉ broadcast state
                BroadcastRoomState(room, "ready_updated", new { playerId = session.PlayerId, ready = ready });
            }
        }

        private void HandlePlay(IPlayerSession session, NetMessage msg)
        {
            if (string.IsNullOrWhiteSpace(session.RoomId))
            {
                session.SendError("NOT_IN_ROOM", "Join a room first.");
                return;
            }
            var room = FindRoom(session.RoomId);
            if (room == null)
            {
                session.SendError("ROOM_NOT_FOUND", "Room not found.");
                return;
            }

            var payload = msg.Payload ?? new JObject();
            var arr = payload["cards"] as JArray;
            if (arr == null)
            {
                session.SendError("BAD_PAYLOAD", "Missing cards array.");
                return;
            }

            var cards = new List<Card>();
            foreach (var token in arr)
            {
                string code = token.ToString();
                cards.Add(Card.FromCode(code));
            }

            string error, info;
            if (!room.Play(session, cards, out error, out info))
            {
                session.SendError("INVALID_MOVE", error);
                // gửi state cá nhân lại để client đồng bộ (phòng trường hợp lệch)
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
            if (string.IsNullOrWhiteSpace(session.RoomId))
            {
                session.SendError("NOT_IN_ROOM", "Join a room first.");
                return;
            }
            var room = FindRoom(session.RoomId);
            if (room == null)
            {
                session.SendError("ROOM_NOT_FOUND", "Room not found.");
                return;
            }

            string error;
            if (!room.Pass(session, out error))
            {
                session.SendError("PASS_FAILED", error);
                return;
            }

            BroadcastRoomState(room, "passed", new { by = session.PlayerId });
        }

        private void HandleChat(IPlayerSession session, NetMessage msg)
        {
            if (string.IsNullOrWhiteSpace(session.RoomId))
            {
                session.SendError("NOT_IN_ROOM", "Join a room first.");
                return;
            }
            var room = FindRoom(session.RoomId);
            if (room == null)
            {
                session.SendError("ROOM_NOT_FOUND", "Room not found.");
                return;
            }

            string text = (msg.Payload != null) ? (msg.Payload.Value<string>("text") ?? "") : "";
            BroadcastEvent(room, "chat", new { from = session.PlayerName, text = text });
        }

        private void BroadcastRoomState(GameRoom room, string eventName, object evtPayload)
        {
            // 1) broadcast event
            BroadcastEvent(room, eventName, evtPayload);

            // 2) gửi state (public + personal) cho từng player
            var players = room.SnapshotPlayers();
            foreach (var p in players)
                SendRoomStateToPlayer(room, p);
        }

        private void BroadcastEvent(GameRoom room, string name, object payload)
        {
            var players = room.SnapshotPlayers();
            foreach (var p in players)
                p.SendEvent(name, payload);
        }

        private void SendRoomStateToPlayer(GameRoom room, IPlayerSession player)
        {
            var pub = room.BuildPublicState();
            var per = room.BuildPersonalState(player.PlayerId);

            player.SendState(new
            {
                publicState = pub,
                personalState = per
            });
        }
    }
}

