// CardGameClient/Game/GameLogic.cs
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using CardGameClient.Models;
using CardGameClient.Network;
using CardGameClient.ViewModels;
using Newtonsoft.Json.Linq;

namespace CardGameClient.Game
{
    // ViewModel cho từng người chơi trong danh sách
    internal sealed class PlayerInfoVM : ViewModelBase
    {
        public string PlayerId { get; set; }
        public string Name { get; set; }
        public bool Ready { get; set; }
        public int HandCount { get; set; }

        public string Display => $"{Name} ({HandCount} cards)";
        public string SubDisplay => $"Id: {PlayerId} | Ready: {Ready}";
    }

    internal sealed class GameLogic : ViewModelBase
    {
        private readonly Dispatcher _ui;
        private readonly GameClient _client = new GameClient();

        public GameLogic(Dispatcher ui)
        {
            _ui = ui;

            // --- CẤU HÌNH KẾT NỐI ---
            // LƯU Ý: Nếu tắt Ngrok bật lại, bạn phải cập nhật link này!
            ServerUrl = "https://jayda-sulfuric-medially.ngrok-free.dev/gameHub";
            
            PlayerName = "Player";
            RoomId = "ROOM-1";

            StatusText = "Not connected";
            ServerInfoText = "";

            // Đăng ký sự kiện từ SignalR
            _client.EventReceived += OnEventReceived;
            _client.Disconnected += OnDisconnected;
        }

        // ===== Bindable properties =====
        private string _serverUrl;
        public string ServerUrl { get => _serverUrl; set { _serverUrl = value; Raise(); } }

        private string _playerName;
        public string PlayerName { get => _playerName; set { _playerName = value; Raise(); } }

        private string _roomId;
        public string RoomId { get => _roomId; set { _roomId = value; Raise(); } }

        private bool _isConnected;
        public bool IsConnected { get => _isConnected; private set { _isConnected = value; Raise(); } }

        private bool _isInRoom;
        public bool IsInRoom { get => _isInRoom; private set { _isInRoom = value; Raise(); UpdateActionFlags(); } }

        private string _statusText;
        public string StatusText { get => _statusText; private set { _statusText = value; Raise(); } }

        private string _serverInfoText;
        public string ServerInfoText { get => _serverInfoText; private set { _serverInfoText = value; Raise(); } }

        private string _phaseText = "Phase: Lobby";
        public string PhaseText { get => _phaseText; private set { _phaseText = value; Raise(); } }

        private string _currentTurnText = "-";
        public string CurrentTurnText { get => _currentTurnText; private set { _currentTurnText = value; Raise(); } }

        private string _currentTrickText = "-";
        public string CurrentTrickText { get => _currentTrickText; private set { _currentTrickText = value; Raise(); } }

        private string _hintText = "";
        public string HintText { get => _hintText; private set { _hintText = value; Raise(); } }

        private string _myPlayerId;
        public string MyPlayerId { get => _myPlayerId; private set { _myPlayerId = value; Raise(); UpdateActionFlags(); } }

        // ===== Collections for UI =====
        public ObservableCollection<PlayerInfoVM> Players { get; } = new ObservableCollection<PlayerInfoVM>();
        public ObservableCollection<CardViewModel> Hand { get; } = new ObservableCollection<CardViewModel>();
        public ObservableCollection<string> TableCards { get; } = new ObservableCollection<string>();

        // ===== Buttons state =====
        private bool _canPlay;
        public bool CanPlay { get => _canPlay; private set { _canPlay = value; Raise(); } }

        private bool _canPass;
        public bool CanPass { get => _canPass; private set { _canPass = value; Raise(); } }

        private bool _hasSelection;
        public bool HasSelection { get => _hasSelection; private set { _hasSelection = value; Raise(); UpdateActionFlags(); } }

        // ===== State cache =====
        private string _currentTurnPlayerId;
        private bool _tableHasTrick;

        // ===== Logging =====
        private readonly StringBuilder _log = new StringBuilder();
        public string LogText { get => _log.ToString(); }

        private void Log(string s)
        {
            _log.AppendLine($"[{DateTime.Now:HH:mm:ss}] {s}");
            Raise(nameof(LogText));
        }

        // ===== Public actions (SignalR Calls) =====
        public async Task ConnectAsync()
        {
            if (IsConnected) return;

            try
            {
                StatusText = "Connecting...";
                Log($"Connecting to {ServerUrl}...");
                
                await _client.ConnectAsync(ServerUrl);

                IsConnected = true;
                StatusText = "Connected";
                Log("Connected to server via SignalR.");
            }
            catch (Exception ex)
            {
                StatusText = "Connect failed";
                Log("Connect failed: " + ex.Message);
            }
        }

        public async Task JoinAsync()
        {
            if (!IsConnected) return;

            // --- SỬA LỖI 1: Gọi đúng tên hàm "Join" và tách tham số ---
            // Server: public async Task Join(string name, string roomId)
            await _client.SendAsync("Join", PlayerName, RoomId);

            IsInRoom = true;
            Log($"Sent Join: {PlayerName} -> {RoomId}");
        }

        public async Task ReadyAsync(bool ready)
        {
            if (!IsConnected || !IsInRoom) return;
            
            // Hàm này tên "Ready" khớp rồi
            await _client.SendAsync("Ready", ready);
            Log("Sent Ready=" + ready);
        }

        public async Task PlaySelectedAsync()
        {
            if (!CanPlay) return;

            var selected = Hand.Where(h => h.IsSelected).Select(h => h.Card).ToList();
            if (selected.Count == 0) return;

            // --- SỬA LỖI 2: Gọi đúng tên hàm "Play" và chuyển sang mảng String ---
            // Server: public async Task Play(string[] cards)
            string[] cardCodes = selected.Select(c => c.ToCode()).ToArray();

            await _client.SendAsync("Play", (object)cardCodes); // Ép kiểu object để gọi hàm SendAsync chuẩn
            
            Log("Sent Play: " + string.Join(", ", cardCodes));
            ClearSelection();
        }

        public async Task PassAsync()
        {
            if (!CanPass) return;
            
            // --- SỬA LỖI 3: Gọi đúng tên hàm "Pass" ---
            // Server: public async Task Pass()
            await _client.SendAsync("Pass");
            Log("Sent Pass");
        }

        public async Task Disconnect()
        {
            await _client.DisconnectAsync();
            ResetUiOnDisconnect("Disconnected");
        }

        // ===== Logic chọn bài (Giữ nguyên) =====
        public void ToggleSelect(CardViewModel vm)
        {
            vm.IsSelected = !vm.IsSelected;
            HasSelection = Hand.Any(x => x.IsSelected);
            UpdateHint();
            UpdateActionFlags();
        }

        public void ClearSelection()
        {
            foreach (var c in Hand) c.IsSelected = false;
            HasSelection = false;
            UpdateHint();
            UpdateActionFlags();
        }

        // ===== Xử lý sự kiện từ Server (Giữ nguyên logic nhận) =====
        
        private void OnEventReceived(string eventName, object payload)
        {
            _ui.BeginInvoke(new Action(() =>
            {
                try 
                {
                    JToken json = payload != null ? JToken.FromObject(payload) : null;
                    HandleSignalREvent(eventName, json);
                }
                catch (Exception ex)
                {
                    Log($"Error parsing event {eventName}: {ex.Message}");
                }
            }));
        }

        private void HandleSignalREvent(string eventName, JToken data)
        {
            switch (eventName)
            {
                case "Welcome":
                    {
                        string pid = data.Type == JTokenType.String ? data.ToString() : data.Value<string>("playerId");
                        MyPlayerId = pid;
                        ServerInfoText = "YourId: " + pid;
                        Log($"WELCOME! My ID: {pid}");
                        break;
                    }

                case "UpdateGame":
                case "GameState":
                    {
                        ApplyState(data as JObject);
                        break;
                    }

                case "PlayerJoined":
                    Log($"Player joined: {data}");
                    break;

                case "ReceiveMessage":
                    Log($"SERVER: {data}");
                    break;

                case "Error":
                    Log($"ERROR: {data}");
                    StatusText = $"Error: {data}";
                    break;

                default:
                    Log($"EVENT {eventName}: {data?.ToString(Newtonsoft.Json.Formatting.None)}");
                    break;
            }
        }

        // ===== Logic tái tạo bàn chơi (Giữ nguyên) =====
        private void ApplyState(JObject payload)
        {
            if (payload == null) return;

            var pub = payload["publicState"] as JObject;
            var per = payload["personalState"] as JObject;

            if (pub != null)
            {
                string phase = pub.Value<string>("phase") ?? "Lobby";
                PhaseText = "Phase: " + phase;

                _currentTurnPlayerId = pub.Value<string>("currentTurn");
                CurrentTurnText = "Turn: " + DisplayNameOf(_currentTurnPlayerId);

                var trick = pub["currentTrick"] as JObject;
                TableCards.Clear();
                if (trick != null)
                {
                    var arr = trick["cards"] as JArray;
                    if (arr != null)
                    {
                        foreach (var t in arr) TableCards.Add(t.ToString());
                    }
                    _tableHasTrick = TableCards.Count > 0;
                    CurrentTrickText = $"{(trick.Value<string>("type") ?? "Unknown")} | {string.Join(",", TableCards)}";
                }
                else
                {
                    _tableHasTrick = false;
                    CurrentTrickText = "-";
                }

                var parr = pub["players"] as JArray;
                Players.Clear();
                if (parr != null)
                {
                    foreach (var p in parr.OfType<JObject>())
                    {
                        var vm = new PlayerInfoVM
                        {
                            PlayerId = p.Value<string>("playerId") ?? "",
                            Name = p.Value<string>("name") ?? "Player",
                            Ready = p.Value<bool?>("ready") ?? false,
                            HandCount = p.Value<int?>("handCount") ?? 0
                        };
                        Players.Add(vm);
                    }
                }

                var winner = pub.Value<string>("winner");
                if (!string.IsNullOrWhiteSpace(winner))
                {
                    StatusText = "Finished. Winner: " + DisplayNameOf(winner);
                    Log("WINNER: " + DisplayNameOf(winner));
                }
                else
                {
                    StatusText = IsConnected ? "Connected" : "Not connected";
                }
            }

            if (per != null)
            {
                var handArr = per["yourHand"] as JArray;
                if (handArr != null)
                {
                    var selectedBefore = Hand.Where(h => h.IsSelected).Select(h => h.Card.ToCode()).ToHashSet();

                    Hand.Clear();
                    foreach (var cToken in handArr)
                    {
                        Card card;
                        if (cToken.Type == JTokenType.String)
                        {
                            card = Card.FromCode(cToken.ToString());
                        }
                        else
                        {
                            card = cToken.ToObject<Card>();
                        }
                        
                        var cvm = new CardViewModel(card)
                        {
                            IsSelected = selectedBefore.Contains(card.ToCode())
                        };
                        Hand.Add(cvm);
                    }

                    var sorted = Hand.OrderBy(h => h.Card.Power).ToList();
                    Hand.Clear();
                    foreach (var x in sorted) Hand.Add(x);

                    HasSelection = Hand.Any(x => x.IsSelected);
                }
            }

            UpdateHint();
            UpdateActionFlags();
        }

        // ===== Helper Functions =====
        private void OnDisconnected(string reason)
        {
            _ui.BeginInvoke(new Action(() =>
            {
                Log("Disconnected: " + reason);
                ResetUiOnDisconnect("Disconnected");
            }));
        }

        private void ResetUiOnDisconnect(string status)
        {
            IsConnected = false;
            IsInRoom = false;
            MyPlayerId = null;
            _currentTurnPlayerId = null;
            _tableHasTrick = false;

            Players.Clear();
            Hand.Clear();
            TableCards.Clear();

            StatusText = status;
            ServerInfoText = "";
            PhaseText = "Phase: -";
            CurrentTurnText = "-";
            CurrentTrickText = "-";
            HintText = "";
            UpdateActionFlags();
        }

        private string DisplayNameOf(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId)) return "-";
            var p = Players.FirstOrDefault(x => x.PlayerId == playerId);
            return p != null ? p.Name : playerId;
        }

        private void UpdateActionFlags()
        {
            bool myTurn = !string.IsNullOrWhiteSpace(MyPlayerId) && MyPlayerId == _currentTurnPlayerId;
            CanPlay = IsConnected && IsInRoom && myTurn && HasSelection;
            CanPass = IsConnected && IsInRoom && myTurn && _tableHasTrick;
        }

        private void UpdateHint()
        {
            var selected = Hand.Where(h => h.IsSelected).Select(h => h.Card).ToList();
            if (selected.Count == 0)
            {
                HintText = "Select cards to play.";
                return;
            }

            try
            {
                var cards = selected.OrderBy(c => c.Rank).ThenBy(c => c.Suit).ToList();
                string kind = AnalyzeSimple(cards);
                HintText = $"Selected: {selected.Count} ({kind})";
            }
            catch
            {
                HintText = "Selected: " + selected.Count;
            }
        }

        private string AnalyzeSimple(System.Collections.Generic.List<Card> cards)
        {
            if (cards.Count == 1) return "Single";
            if (cards.Count == 2 && cards[0].Rank == cards[1].Rank) return "Pair";
            if (cards.Count == 3 && cards.All(c => c.Rank == cards[0].Rank)) return "Triple";
            if (cards.Count >= 3 && cards.All(c => c.Rank != 15))
            {
                bool consecutive = true;
                for (int i = 1; i < cards.Count; i++)
                    if (cards[i].Rank != cards[i - 1].Rank + 1) consecutive = false;
                if (consecutive) return "Straight";
            }
            return "Unknown";
        }
    }
}