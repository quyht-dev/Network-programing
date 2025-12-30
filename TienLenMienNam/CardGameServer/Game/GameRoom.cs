// CardGameServer/Game/GameRoom.cs
using System;
using System.Collections.Generic;
using System.Linq;
using CardGameServer.Models;
using CardGameServer.Network;

namespace CardGameServer.Game
{
    public enum RoomPhase
    {
        Lobby = 0,
        Playing = 1,
        Finished = 2
    }

    public enum MoveType
    {
        Invalid = 0,
        Single = 1,
        Pair = 2,
        Triple = 3,
        Straight = 4
    }

    public sealed class Move
    {
        public MoveType Type { get; private set; }
        public List<Card> Cards { get; private set; }

        public Move(MoveType type, List<Card> cards)
        {
            Type = type;
            Cards = cards ?? new List<Card>();
        }

        public int Strength
        {
            get
            {
                if (Cards == null || Cards.Count == 0) return -1;
                return Cards.Max(c => c.Power);
            }
        }
    }

    public sealed class GameRoom
    {
        private readonly object _gate = new object();

        public string RoomId { get; private set; }
        public RoomPhase Phase { get; private set; }

        // Tối đa 4 người chơi
        private readonly List<PlayerSession> _players = new List<PlayerSession>();

        // Ready flag theo playerId
        private readonly Dictionary<string, bool> _ready = new Dictionary<string, bool>();

        // Hand theo playerId
        private readonly Dictionary<string, List<Card>> _hands = new Dictionary<string, List<Card>>();

        // Ván hiện tại
        public string CurrentTurnPlayerId { get; private set; }
        public string LastPlayedByPlayerId { get; private set; }
        public Move CurrentTrick { get; private set; } // nước hiện tại trên bàn, null nếu bàn trống
        public int PassCount { get; private set; }

        // Kết quả
        public string WinnerPlayerId { get; private set; }

        public GameRoom(string roomId)
        {
            RoomId = roomId;
            Phase = RoomPhase.Lobby;
        }

        public List<PlayerSession> SnapshotPlayers()
        {
            lock (_gate) return _players.ToList();
        }

        public bool IsFull
        {
            get { lock (_gate) return _players.Count >= 4; }
        }

        public bool Contains(PlayerSession s)
        {
            lock (_gate) return _players.Contains(s);
        }

        public bool Join(PlayerSession s, string playerName, out string error)
        {
            error = null;
            lock (_gate)
            {
                if (Phase != RoomPhase.Lobby)
                {
                    error = "Room already started.";
                    return false;
                }
                if (_players.Count >= 4)
                {
                    error = "Room is full.";
                    return false;
                }
                if (_players.Contains(s))
                {
                    error = "Already in room.";
                    return false;
                }

                s.PlayerName = playerName;
                s.RoomId = RoomId;

                _players.Add(s);
                _ready[s.PlayerId] = false;
                _hands[s.PlayerId] = new List<Card>();
                return true;
            }
        }

        public void Leave(PlayerSession s)
        {
            lock (_gate)
            {
                _players.Remove(s);
                _ready.Remove(s.PlayerId);
                _hands.Remove(s.PlayerId);

                if (Phase == RoomPhase.Playing)
                {
                    // đơn giản: có người rời khi đang chơi => kết thúc ván
                    Phase = RoomPhase.Finished;
                }
            }
        }

        public bool SetReady(PlayerSession s, bool ready, out string error)
        {
            error = null;
            lock (_gate)
            {
                if (!_players.Contains(s))
                {
                    error = "Not in room.";
                    return false;
                }
                if (Phase != RoomPhase.Lobby)
                {
                    error = "Room not in lobby.";
                    return false;
                }
                _ready[s.PlayerId] = ready;
                return true;
            }
        }

        public bool CanStart(out string error)
        {
            error = null;
            lock (_gate)
            {
                if (_players.Count != 4)
                {
                    error = "Need exactly 4 players.";
                    return false;
                }
                foreach (var p in _players)
                {
                    bool r;
                    if (!_ready.TryGetValue(p.PlayerId, out r) || !r)
                    {
                        error = "Not all players ready.";
                        return false;
                    }
                }
                return true;
            }
        }

        public void StartGame()
        {
            lock (_gate)
            {
                Phase = RoomPhase.Playing;
                WinnerPlayerId = null;

                var deck = new Deck();
                deck.Shuffle();

                foreach (var p in _players)
                    _hands[p.PlayerId] = deck.Draw(13);

                // Tìm người có 3 bích (3S) đi trước
                var threeSpades = new Card(3, Suit.Spades);
                string first = _players[0].PlayerId;

                foreach (var p in _players)
                {
                    var hand = _hands[p.PlayerId];
                    if (hand.Any(c => c.Rank == threeSpades.Rank && c.Suit == threeSpades.Suit))
                    {
                        first = p.PlayerId;
                        break;
                    }
                }

                CurrentTurnPlayerId = first;
                LastPlayedByPlayerId = null;
                CurrentTrick = null;
                PassCount = 0;
            }
        }

        public List<Card> GetHand(string playerId)
        {
            lock (_gate)
            {
                List<Card> hand;
                if (_hands.TryGetValue(playerId, out hand))
                    return hand.ToList();
                return new List<Card>();
            }
        }

        public bool Play(PlayerSession s, List<Card> cards, out string error, out string info)
        {
            error = null;
            info = null;

            lock (_gate)
            {
                if (Phase != RoomPhase.Playing)
                {
                    error = "Game not started.";
                    return false;
                }
                if (s.PlayerId != CurrentTurnPlayerId)
                {
                    error = "Not your turn.";
                    return false;
                }

                if (cards == null || cards.Count == 0)
                {
                    error = "No cards.";
                    return false;
                }

                var hand = _hands[s.PlayerId];
                // check cards belong to hand
                foreach (var c in cards)
                {
                    bool found = hand.Any(h => h.Rank == c.Rank && h.Suit == c.Suit);
                    if (!found)
                    {
                        error = "Card not in hand: " + c.ToCode();
                        return false;
                    }
                }

                var move = AnalyzeMove(cards);
                if (move.Type == MoveType.Invalid)
                {
                    error = "Invalid move.";
                    return false;
                }

                // nếu bàn đang có bài thì phải chặn được
                if (CurrentTrick != null)
                {
                    if (!Beats(move, CurrentTrick))
                    {
                        error = "Move does not beat current trick.";
                        return false;
                    }
                }

                // apply: remove cards from hand
                foreach (var c in cards)
                {
                    var idx = hand.FindIndex(h => h.Rank == c.Rank && h.Suit == c.Suit);
                    if (idx >= 0) hand.RemoveAt(idx);
                }

                CurrentTrick = move;
                LastPlayedByPlayerId = s.PlayerId;
                PassCount = 0;

                // thắng nếu hết bài
                if (hand.Count == 0)
                {
                    Phase = RoomPhase.Finished;
                    WinnerPlayerId = s.PlayerId;
                    info = "WINNER";
                }
                else
                {
                    // chuyển lượt sang người kế tiếp
                    CurrentTurnPlayerId = NextPlayerId_NoLock(s.PlayerId);
                }

                return true;
            }
        }

        public bool Pass(PlayerSession s, out string error)
        {
            error = null;

            lock (_gate)
            {
                if (Phase != RoomPhase.Playing)
                {
                    error = "Game not started.";
                    return false;
                }
                if (s.PlayerId != CurrentTurnPlayerId)
                {
                    error = "Not your turn.";
                    return false;
                }
                if (CurrentTrick == null)
                {
                    error = "Cannot pass on empty table.";
                    return false;
                }

                PassCount++;
                // nếu 3 người pass (với 4 players) => bàn trống, người đánh cuối được đánh tiếp
                if (PassCount >= (_players.Count - 1))
                {
                    CurrentTrick = null;
                    PassCount = 0;
                    // lượt về người đánh cuối
                    CurrentTurnPlayerId = LastPlayedByPlayerId;
                }
                else
                {
                    CurrentTurnPlayerId = NextPlayerId_NoLock(s.PlayerId);
                }

                return true;
            }
        }

        private string NextPlayerId_NoLock(string currentId)
        {
            int idx = _players.FindIndex(p => p.PlayerId == currentId);
            if (idx < 0) return _players[0].PlayerId;
            int next = (idx + 1) % _players.Count;
            return _players[next].PlayerId;
        }

        // ====== Rules: tối thiểu (rác/đôi/sám/sảnh) ======
        public static Move AnalyzeMove(List<Card> cards)
        {
            var sorted = cards.OrderBy(c => c.Rank).ThenBy(c => c.Suit).ToList();
            if (sorted.Count == 1)
                return new Move(MoveType.Single, sorted);

            if (sorted.Count == 2 && sorted[0].Rank == sorted[1].Rank)
                return new Move(MoveType.Pair, sorted);

            if (sorted.Count == 3 && sorted.All(c => c.Rank == sorted[0].Rank))
                return new Move(MoveType.Triple, sorted);

            if (sorted.Count >= 3)
            {
                // sảnh: liên tiếp, không chứa 2
                if (sorted.Any(c => c.Rank == 15))
                    return new Move(MoveType.Invalid, sorted);

                bool consecutive = true;
                for (int i = 1; i < sorted.Count; i++)
                {
                    if (sorted[i].Rank != sorted[i - 1].Rank + 1)
                    {
                        consecutive = false;
                        break;
                    }
                }
                if (consecutive)
                    return new Move(MoveType.Straight, sorted);
            }

            return new Move(MoveType.Invalid, sorted);
        }

        public static bool Beats(Move challenger, Move current)
        {
            if (current == null) return true;
            if (challenger == null || challenger.Type == MoveType.Invalid) return false;

            if (challenger.Type != current.Type) return false;
            if (challenger.Cards.Count != current.Cards.Count) return false;

            // so theo Strength (lá mạnh nhất)
            return challenger.Strength > current.Strength;
        }

        // ====== Build state for sending ======
        public object BuildPublicState()
        {
            lock (_gate)
            {
                return new
                {
                    roomId = RoomId,
                    phase = Phase.ToString(),
                    players = _players.Select(p => new
                    {
                        playerId = p.PlayerId,
                        name = p.PlayerName,
                        ready = _ready.ContainsKey(p.PlayerId) ? _ready[p.PlayerId] : false,
                        handCount = _hands.ContainsKey(p.PlayerId) ? _hands[p.PlayerId].Count : 0
                    }).ToList(),
                    currentTurn = CurrentTurnPlayerId,
                    lastPlayedBy = LastPlayedByPlayerId,
                    currentTrick = CurrentTrick == null ? null : new
                    {
                        type = CurrentTrick.Type.ToString(),
                        cards = CurrentTrick.Cards.Select(c => c.ToCode()).ToList()
                    },
                    passCount = PassCount,
                    winner = WinnerPlayerId
                };
            }
        }

        public object BuildPersonalState(string playerId)
        {
            lock (_gate)
            {
                var hand = _hands.ContainsKey(playerId) ? _hands[playerId] : new List<Card>();
                return new
                {
                    yourHand = hand.Select(c => c.ToCode()).ToList()
                };
            }
        }
    }
}
