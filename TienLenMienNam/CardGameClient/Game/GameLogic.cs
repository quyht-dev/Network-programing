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

            Host = "127.0.0.1";
            PortText = "7777";
            PlayerName = "Player";
            RoomId = "ROOM-1";

            StatusText = "Not connected";
            ServerInfoText = "";

            _client.MessageReceived += OnMessage;
            _client.Disconnected += OnDisconnected;
        }

        // ===== Bindable properties =====
        private string _host;
        public string Host { get => _host; set { _host = value; Raise(); } }

        private string _portText;
        public string PortText { get => _portText; set { _portText = value; Raise(); } }

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
        public ObservableCollection<string> TableCards { get; } = new ObservableCollection<string>(); // list code string

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

        // ===== Public actions =====
        public async Task ConnectAsync()
        {
            if (IsConnected) return;

            int port;
            if (!int.TryParse(PortText, out port))
            {
                StatusText = "Port invalid";
                return;
            }

            try
            {
                StatusText = "Connecting...";
                await _client.ConnectAsync(Host, port);

                IsConnected = true;
                StatusText = "Connected";
                Log("Connected to server.");
            }
            catch (Exception ex)
            {
                StatusText = "Connect failed: " + ex.Message;
                Log("Connect failed: " + ex.Message);
            }
        }

        public async Task JoinAsync()
        {
            if (!IsConnected) return;

            await _client.SendAsync("join", new
            {
                name = PlayerName,
                roomId = RoomId
            });

            IsInRoom = true; // optimistic; server state sẽ chỉnh lại nếu fail
            Log($"Sent join: {PlayerName} -> {RoomId}");
        }

        public async Task ReadyAsync(bool ready)
        {
            if (!IsConnected || !IsInRoom) return;
            await _client.SendAsync("ready", new { ready = ready });
            Log("Sent ready=" + ready);
        }

        public async Task PlaySelectedAsync()
        {
            if (!CanPlay) return;

            var selected = Hand.Where(h => h.IsSelected).Select(h => h.Code).ToList();
            if (selected.Count == 0) return;

            await _client.SendAsync("play", new { cards = selected });
            Log("Sent play: " + string.Join(",", selected));

            // tạm thời clear selection, server sẽ gửi state update
            ClearSelection();
        }

        public async Task PassAsync()
        {
            if (!CanPass) return;
            await _client.SendAsync("pass", new { });
            Log("Sent pass");
        }

        public void Disconnect()
        {
            _client.Disconnect();
            ResetUiOnDisconnect("Disconnected");
        }

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

        // ===== Network callbacks =====
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

        private void OnMessage(NetMessage msg)
        {
            _ui.BeginInvoke(new Action(() =>
            {
                HandleMessageOnUi(msg);
            }));
        }

        private void HandleMessageOnUi(NetMessage msg)
        {
            if (msg == null) return;

            switch (msg.Type)
            {
                case "welcome":
                    {
                        var pid = msg.Payload?.Value<string>("playerId");
                        MyPlayerId = pid;
                        ServerInfoText = "YourId: " + pid;
                        Log("WELCOME playerId=" + pid);
                        break;
                    }

                case "state":
                    ApplyState(msg.Payload);
                    break;

                case "event":
                    ApplyEvent(msg.Payload);
                    break;

                case "error":
                    {
                        string code = msg.Payload?.Value<string>("code") ?? "ERR";
                        string message = msg.Payload?.Value<string>("message") ?? "";
                        Log($"ERROR {code}: {message}");
                        StatusText = $"Error: {code}";
                        break;
                    }

                case "ping":
                    // server ping -> reply pong
                    _ = _client.SendAsync("pong", msg.Payload ?? new JObject());
                    break;

                case "pong":
                    // có thể update latency nếu muốn
                    break;

                default:
                    Log("MSG " + msg.Type);
                    break;
            }
        }

        private void ApplyEvent(JObject payload)
        {
            if (payload == null) return;
            string name = payload.Value<string>("name") ?? "";
            Log("EVENT: " + name);
        }

        /// <summary>
        /// State format khớp server: { publicState: {...}, personalState: {...} }
        /// </summary>
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

                // currentTrick
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

                // players list
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

                // winner
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
                    // rebuild hand
                    var selectedBefore = Hand.Where(h => h.IsSelected).Select(h => h.Code).ToHashSet();

                    Hand.Clear();
                    foreach (var c in handArr)
                    {
                        var card = Card.FromCode(c.ToString());
                        var cvm = new CardViewModel(card)
                        {
                            IsSelected = selectedBefore.Contains(card.ToCode())
                        };
                        Hand.Add(cvm);
                    }

                    // sort by Power
                    var sorted = Hand.OrderBy(h => h.Card.Power).ToList();
                    Hand.Clear();
                    foreach (var x in sorted) Hand.Add(x);

                    HasSelection = Hand.Any(x => x.IsSelected);
                }
            }

            UpdateHint();
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

            // Play: phải đến lượt + có chọn bài
            CanPlay = IsConnected && IsInRoom && myTurn && HasSelection;

            // Pass: chỉ được pass nếu đến lượt và bàn đang có trick (server cũng check)
            CanPass = IsConnected && IsInRoom && myTurn && _tableHasTrick;
        }

        // ===== Gợi ý nước đi (rất tối thiểu) =====
        private void UpdateHint()
        {
            var selected = Hand.Where(h => h.IsSelected).Select(h => h.Code).ToList();
            if (selected.Count == 0)
            {
                HintText = "Select cards to play.";
                return;
            }

            // gợi ý rất đơn giản: số lá + có cùng rank hay sảnh
            try
            {
                var cards = selected.Select(Card.FromCode).OrderBy(c => c.Rank).ThenBy(c => c.Suit).ToList();
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
