using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using CardGameClient.Models;
using CardGameClient.Network;
using CardGameClient.ViewModels;
using Newtonsoft.Json.Linq;

namespace CardGameClient.Game
{
    // =========================================================
    // 1. VIEW MODEL: NGƯỜI CHƠI
    // =========================================================
    internal sealed class PlayerInfoVM : ViewModelBase, IEquatable<PlayerInfoVM>
    {
        public string PlayerId { get; set; }
        public string Name { get; set; }

        private bool _ready;
        public bool Ready { get => _ready; set { _ready = value; Raise(); } }

        private int _handCount;
        public int HandCount { get => _handCount; set { _handCount = value; Raise(); } }

        private bool _isTurn;
        public bool IsTurn { get => _isTurn; set { _isTurn = value; Raise(); } }

        public string Display => $"{Name} ({HandCount})";

        public bool Equals(PlayerInfoVM other) => other != null && PlayerId == other.PlayerId;
    }

    // =========================================================
    // 2. LOGIC TRUNG TÂM (GAME LOGIC)
    // =========================================================
    internal sealed class GameLogic : ViewModelBase
    {
        private readonly Dispatcher _ui;
        private readonly GameClient _client = new GameClient();

        // --- CẤU HÌNH SERVER ---
        public string ServerUrl { get; set; } = "https://jayda-sulfuric-medially.ngrok-free.dev/gameHub"; 
        public string PlayerName { get; set; } = "Player";
        public string RoomId { get; set; } = "ROOM-1";

        public GameLogic(Dispatcher ui)
        {
            _ui = ui;
            StatusText = "Sẵn sàng kết nối...";
            
            _client.EventReceived += OnEventReceived;
            _client.Disconnected += OnDisconnected;
        }

        // --- VỊ TRÍ NGỒI ---
        private PlayerInfoVM _playerLeft;
        public PlayerInfoVM PlayerLeft { get => _playerLeft; private set { _playerLeft = value; Raise(); } }

        private PlayerInfoVM _playerTop;
        public PlayerInfoVM PlayerTop { get => _playerTop; private set { _playerTop = value; Raise(); } }

        private PlayerInfoVM _playerRight;
        public PlayerInfoVM PlayerRight { get => _playerRight; private set { _playerRight = value; Raise(); } }

        // --- TRẠNG THÁI UI ---
        private bool _isConnected;
        public bool IsConnected { get => _isConnected; private set { _isConnected = value; Raise(); } }

        private bool _isInRoom;
        public bool IsInRoom { get => _isInRoom; private set { _isInRoom = value; Raise(); UpdateActionFlags(); } }

        private bool _showCountdown;
        public bool ShowCountdown { get => _showCountdown; private set { _showCountdown = value; Raise(); } }

        private string _statusText;
        public string StatusText { get => _statusText; private set { _statusText = value; Raise(); } }

        private string _serverInfoText;
        public string ServerInfoText { get => _serverInfoText; private set { _serverInfoText = value; Raise(); } }

        // --- TÍNH NĂNG RESET GAME ---
        private bool _showResetAlert;
        public bool ShowResetAlert { get => _showResetAlert; set { _showResetAlert = value; Raise(); } }

        private string _resetRequesterName;
        public string ResetRequesterName { get => _resetRequesterName; set { _resetRequesterName = value; Raise(); } }

        // --- TRẠNG THÁI GAME ---
        private string _myPlayerId;
        public string MyPlayerId { get => _myPlayerId; private set { _myPlayerId = value; Raise(); UpdateActionFlags(); UpdateSeating(); } }

        private bool _amIReady;
        public bool AmIReady { get => _amIReady; private set { _amIReady = value; Raise(); } }

        private string _currentTurnPlayerId;
        private bool _tableHasTrick;

        // Biến theo dõi trạng thái cũ để kích hoạt đếm ngược
        private string _lastPhase = "Lobby"; 

        // --- DỮ LIỆU ---
        public ObservableCollection<PlayerInfoVM> Players { get; } = new ObservableCollection<PlayerInfoVM>();
        public ObservableCollection<CardViewModel> Hand { get; } = new ObservableCollection<CardViewModel>();
        public ObservableCollection<string> TableCards { get; } = new ObservableCollection<string>();

        // --- ACTION FLAGS ---
        private bool _canPlay;
        public bool CanPlay { get => _canPlay; private set { _canPlay = value; Raise(); } }

        private bool _canPass;
        public bool CanPass { get => _canPass; private set { _canPass = value; Raise(); } }

        private bool _hasSelection;
        public bool HasSelection { get => _hasSelection; private set { _hasSelection = value; Raise(); UpdateActionFlags(); } }

        private string _hintText = "Chọn bài để đánh";
        public string HintText { get => _hintText; private set { _hintText = value; Raise(); } }

        // =========================================================
        // PHẦN 3: XỬ LÝ MẠNG & SỰ KIỆN
        // =========================================================
        private void OnEventReceived(string eventName, object payload)
        {
            _ui.BeginInvoke(new Action(() =>
            {
                try
                {
                    JToken json = null;
                    if (payload != null)
                    {
                        string jsonStr = System.Text.Json.JsonSerializer.Serialize(payload);
                        json = JToken.Parse(jsonStr);
                    }
                    HandleSignalREvent(eventName, json);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Error] Event {eventName}: {ex.Message}");
                }
            }));
        }

        private void HandleSignalREvent(string eventName, JToken data)
        {
            switch (eventName)
            {
                case "Welcome":
                    MyPlayerId = data.Type == JTokenType.String ? data.ToString() : data.Value<string>("playerId");
                    ServerInfoText = $"ID: {MyPlayerId}";
                    StatusText = "Đã kết nối máy chủ";
                    break;

                case "UpdateGame":
                case "GameState":
                    ApplyState(data as JObject);
                    break;

                case "Error":
                    StatusText = $"Lỗi: {data}";
                    break;

                case "ask_reset":
                    ResetRequesterName = data.Value<string>("requester");
                    ShowResetAlert = true; 
                    break;

                case "game_reset":
                    ShowResetAlert = false;
                    TableCards.Clear();
                    Hand.Clear();
                    _tableHasTrick = false;
                    AmIReady = false;
                    ShowCountdown = false;
                    _lastPhase = "Lobby"; // Reset phase về Lobby
                    StatusText = "Ván mới - Hãy Sẵn Sàng";
                    UpdateActionFlags();
                    break;
            }
        }

        // =========================================================
        // PHẦN 4: LOGIC ĐỒNG BỘ TRẠNG THÁI
        // =========================================================
        private void ApplyState(JObject payload)
        {
            if (payload == null) return;

            var pub = payload["publicState"] as JObject;
            var per = payload["personalState"] as JObject;

            if (pub != null)
            {
                // --- [MỚI] LOGIC KÍCH HOẠT ĐẾM NGƯỢC ---
                string currentPhase = pub.Value<string>("phase"); // "Lobby", "Playing", "Finished"
                
                // Nếu trước đó là Lobby (đang chờ) mà giờ chuyển sang Playing (đang chơi) -> Bắt đầu game
                if (_lastPhase == "Lobby" && currentPhase == "Playing")
                {
                    TriggerGameStartCountdown();
                }
                _lastPhase = currentPhase; 
                // ---------------------------------------

                _currentTurnPlayerId = pub.Value<string>("currentTurn");

                // Cập nhật bài trên bàn
                var trick = pub["currentTrick"] as JObject;
                List<string> incomingTable = new List<string>();
                if (trick != null)
                {
                    var arr = trick["cards"] as JArray;
                    if (arr != null) foreach (var t in arr) incomingTable.Add(t.ToString());
                }
                
                if (incomingTable.Count > 0)
                {
                    SyncCollection(TableCards, incomingTable, (code) => code, (a, b) => a == b);
                    _tableHasTrick = true;
                }
                else
                {
                    _tableHasTrick = false;
                }

                // Cập nhật người chơi
                var parr = pub["players"] as JArray;
                if (parr != null)
                {
                    var newPlayerList = new List<PlayerInfoVM>();
                    foreach (var p in parr.OfType<JObject>())
                    {
                        string pid = p.Value<string>("playerId");
                        newPlayerList.Add(new PlayerInfoVM
                        {
                            PlayerId = pid,
                            Name = p.Value<string>("name") ?? "Unknown",
                            Ready = p.Value<bool?>("ready") ?? false,
                            HandCount = p.Value<int?>("handCount") ?? 0,
                            IsTurn = (pid == _currentTurnPlayerId)
                        });
                        
                        if (pid == MyPlayerId) AmIReady = p.Value<bool?>("ready") ?? false;
                    }

                    SyncCollection(Players, newPlayerList, 
                        (vm) => vm, 
                        (oldItem, newItem) => oldItem.PlayerId == newItem.PlayerId,
                        (oldItem, newItem) => {
                            oldItem.Name = newItem.Name;
                            oldItem.Ready = newItem.Ready;
                            oldItem.HandCount = newItem.HandCount;
                            oldItem.IsTurn = newItem.IsTurn;
                        }
                    );
                }
                UpdateSeating();

                // Thông báo trạng thái
                var winner = pub.Value<string>("winner");
                if (!string.IsNullOrEmpty(winner)) 
                {
                    StatusText = $"CHIẾN THẮNG: {DisplayNameOf(winner)}";
                    TableCards.Clear(); 
                }
                else if (pub.Value<string>("phase") == "Playing") 
                {
                    StatusText = $"Lượt: {DisplayNameOf(_currentTurnPlayerId)}";
                }
                else 
                {
                    StatusText = "Đang chờ người chơi...";
                }
            }

            if (per != null)
            {
                var handArr = per["yourHand"] as JArray;
                if (handArr != null)
                {
                    List<string> newCodes = new List<string>();
                    foreach (var t in handArr)
                        newCodes.Add(t.Type == JTokenType.String ? t.ToString() : (t["code"]?.ToString() ?? ""));

                    var sortedCodes = newCodes
                        .Select(c => Card.FromCode(c))
                        .OrderBy(c => c.Power)
                        .Select(c => c.ToCode())
                        .ToList();

                    SyncCollection(Hand, sortedCodes,
                        (code) => new CardViewModel(Card.FromCode(code)),
                        (vm, code) => vm.Card.ToCode() == code
                    );

                    HasSelection = Hand.Any(x => x.IsSelected);
                }
            }

            UpdateHint();
            UpdateActionFlags();
        }

        // --- HÀM BẬT ĐẾM NGƯỢC ---
        private async void TriggerGameStartCountdown()
        {
            ShowCountdown = true;
            await Task.Delay(4000); // Chờ 4 giây (3-2-1-Start trong XAML)
            ShowCountdown = false;
        }

        private void SyncCollection<TVM, TData>(
            ObservableCollection<TVM> currentList, 
            List<TData> newList, 
            Func<TData, TVM> creator, 
            Func<TVM, TData, bool> comparer,
            Action<TVM, TData> updater = null)
        {
            for (int i = currentList.Count - 1; i >= 0; i--)
            {
                if (!newList.Any(data => comparer(currentList[i], data)))
                    currentList.RemoveAt(i);
            }

            for (int i = 0; i < newList.Count; i++)
            {
                var data = newList[i];
                var existingItem = currentList.FirstOrDefault(vm => comparer(vm, data));

                if (existingItem == null)
                {
                    var newItem = creator(data);
                    if (currentList.Count > i) currentList.Insert(i, newItem);
                    else currentList.Add(newItem);
                }
                else
                {
                    int oldIndex = currentList.IndexOf(existingItem);
                    if (oldIndex != i) currentList.Move(oldIndex, i);
                    updater?.Invoke(existingItem, data);
                }
            }
        }

        // =========================================================
        // PHẦN 6: HELPERS
        // =========================================================
        private void UpdateSeating()
        {
            PlayerLeft = null; PlayerTop = null; PlayerRight = null;
            if (string.IsNullOrEmpty(MyPlayerId) || Players.Count == 0) return;

            var me = Players.FirstOrDefault(p => p.PlayerId == MyPlayerId);
            if (me == null) return;

            int myIndex = Players.IndexOf(me);
            int count = Players.Count;

            if (count >= 2) PlayerRight = Players[(myIndex + 1) % count];
            if (count >= 3) PlayerTop = Players[(myIndex + 2) % count];
            if (count >= 4) PlayerLeft = Players[(myIndex + 3) % count];
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
            if (selected.Count == 0) { HintText = "Chọn bài..."; return; }
            HintText = AnalyzeHand(selected);
        }

        private string AnalyzeHand(List<Card> cards)
        {
            var sorted = cards.OrderBy(c => c.Power).ToList();
            int n = sorted.Count;
            if (n == 0) return "";

            if (sorted.All(c => c.Rank == sorted[0].Rank))
            {
                if (n == 1) return "Rác (Lẻ)";
                if (n == 2) return "Đôi";
                if (n == 3) return "Sám cô";
                if (n == 4) return "Tứ Quý (Bomb)";
            }

            bool has2 = sorted.Any(c => c.Rank == 15);
            bool isStraight = true;
            for (int i = 0; i < n - 1; i++) 
                if (sorted[i + 1].Rank != sorted[i].Rank + 1) { isStraight = false; break; }
            
            if (isStraight && !has2 && n >= 3) return $"Sảnh {n} lá";

            if (n >= 6 && n % 2 == 0 && !has2)
            {
                bool isPine = true;
                for (int i = 0; i < n; i += 2) if (sorted[i].Rank != sorted[i+1].Rank) isPine = false;
                for (int i = 0; i < n - 2; i += 2) if (sorted[i+2].Rank != sorted[i].Rank + 1) isPine = false;
                if (isPine) return $"{n/2} Đôi Thông";
            }

            return "Không hợp lệ";
        }

        private string DisplayNameOf(string id) => Players.FirstOrDefault(x => x.PlayerId == id)?.Name ?? id;

        private void ResetUiOnDisconnect(string msg)
        {
            IsConnected = false; IsInRoom = false; Players.Clear(); Hand.Clear(); TableCards.Clear();
            StatusText = msg; ShowCountdown = false; ShowResetAlert = false;
        }

        // =========================================================
        // PHẦN 7: GIAO TIẾP SERVER (ACTIONS)
        // =========================================================
        public async Task ConnectAsync() { if (!IsConnected) { StatusText="Đang kết nối..."; await _client.ConnectAsync(ServerUrl); IsConnected=true; StatusText="Đã kết nối"; } }
        
        public async Task JoinAsync() { 
            if (IsConnected) { 
                await _client.SendAsync("Join", PlayerName, RoomId); 
                IsInRoom = true; 
                // ĐÃ XÓA ĐẾM NGƯỢC Ở ĐÂY -> CHUYỂN SANG ApplyState
            } 
        }
        
        public async Task ReadyAsync(bool r) { if (IsConnected) await _client.SendAsync("Ready", !AmIReady); }
        
        public async Task PlaySelectedAsync() {
            if (CanPlay) {
                var codes = Hand.Where(h=>h.IsSelected).Select(h=>h.Card.ToCode()).ToArray();
                await _client.SendAsync("Play", (object)codes);
                ClearSelection();
            }
        }
        
        public async Task PassAsync() { if (CanPass) await _client.SendAsync("Pass"); }
        
        public async Task Disconnect() { await _client.DisconnectAsync(); ResetUiOnDisconnect("Đã thoát"); }

        // --- CÁC HÀM RESET GAME ---
        public async Task SendRequestReset()
        {
            if (IsConnected) await _client.SendAsync("RequestReset");
        }

        public async Task SendAcceptReset()
        {
            ShowResetAlert = false; 
            if (IsConnected) await _client.SendAsync("AcceptReset");
        }

        public void CloseResetAlert()
        {
            ShowResetAlert = false; 
        }
        
        public void ToggleSelect(CardViewModel vm) { vm.IsSelected = !vm.IsSelected; HasSelection = Hand.Any(x => x.IsSelected); UpdateHint(); UpdateActionFlags(); }
        public void ClearSelection() { foreach (var c in Hand) c.IsSelected = false; HasSelection = false; UpdateHint(); UpdateActionFlags(); }
        private void OnDisconnected(string reason) => _ui.BeginInvoke(new Action(() => ResetUiOnDisconnect("Mất kết nối")));
    }
}