using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using TicTacToe.Client.Protocol;
using TicTacToe.Client.Services;

namespace TicTacToe.Client.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly SocketClientService _net = new();

    public ObservableCollection<CellViewModel> Cells { get; } =
        new ObservableCollection<CellViewModel>(
            Enumerable.Range(0, 9).Select(i => new CellViewModel(i / 3, i % 3)));

    private string _roomId = "";
    public string RoomId { get => _roomId; set => Set(ref _roomId, value); }

    private string _mySymbol = ""; // "X" or "O"
    public string MySymbol
    {
        get => _mySymbol;
        set
        {
            Set(ref _mySymbol, value);
            OnPropertyChanged(nameof(IsMyTurn));
            OnPropertyChanged(nameof(StatusText));
        }
    }

    private string _turn = "X";
    public string Turn
    {
        get => _turn;
        set
        {
            Set(ref _turn, value);
            OnPropertyChanged(nameof(IsMyTurn));
            OnPropertyChanged(nameof(StatusText));
        }
    }

    private string _status = "Idle"; // Idle/Connected/Queuing/Playing/WinX/WinO/Draw/Disconnected
    public string Status
    {
        get => _status;
        set
        {
            Set(ref _status, value);
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(IsMyTurn));
        }
    }

    private bool _isConnected;
    public bool IsConnected { get => _isConnected; set => Set(ref _isConnected, value); }

    public string StatusText => $"Status: {Status} | Turn: {Turn} | You: {MySymbol}";
    public bool IsMyTurn => Status == "Playing" && !string.IsNullOrEmpty(MySymbol) && Turn == MySymbol;

    // Commands
    public ICommand ConnectCommand { get; }
    public ICommand FindMatchCommand { get; }
    public ICommand CellClickCommand { get; }

    public MainViewModel()
    {
        // Khởi tạo: khoá hết ô
        SetAllCellsEnabled(false);

        _net.PacketReceived += OnPacket;
        _net.Disconnected += msg => UI(() =>
        {
            IsConnected = false;
            Status = "Disconnected";
            MessageBox.Show("Mất kết nối: " + msg);
            SetAllCellsEnabled(false);
        });

        ConnectCommand = new RelayCommand(async _ =>
        {
            try
            {
                await _net.ConnectAsync("127.0.0.1", 5000);
                IsConnected = true;
                Status = "Connected";
            }
            catch (Exception ex)
            {
                IsConnected = false;
                MessageBox.Show("Connect lỗi: " + ex.Message);
            }
        });

        FindMatchCommand = new RelayCommand(async _ =>
        {
            try
            {
                if (!IsConnected)
                {
                    MessageBox.Show("Chưa connect server.");
                    return;
                }

                // reset thông tin ván cũ
                RoomId = "";
                MySymbol = "";
                Turn = "X";
                ClearBoard();
                SetAllCellsEnabled(false);

                await _net.SendAsync(new { type = "queue", data = new { } });
                Status = "Queuing";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Queue lỗi: " + ex.Message);
            }
        });

        CellClickCommand = new RelayCommand(async param =>
        {
            try
            {
                if (!IsMyTurn) return;
                if (string.IsNullOrWhiteSpace(RoomId)) return;
                if (param is not CellViewModel cell) return;

                await _net.SendAsync(new
                {
                    type = "move",
                    data = new { roomId = RoomId, r = cell.Row, c = cell.Col }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Move lỗi: " + ex.Message);
            }
        });
    }

    // Helper: invoke lên UI thread an toàn
    private void UI(Action action)
    {
        var app = Application.Current;
        if (app?.Dispatcher == null)
        {
            // design-time hoặc trường hợp hiếm: cứ chạy thẳng
            action();
            return;
        }

        if (app.Dispatcher.CheckAccess()) action();
        else app.Dispatcher.Invoke(action);
    }

    private void OnPacket(Packet pkt)
    {
        UI(() =>
        {
            try
            {
                switch (pkt.type)
                {
                    case "queued":
                        // server có thể gửi queued cho client đang chờ
                        Status = "Queuing";
                        break;

                    case "matched":
                        // expect: { roomId, symbol }
                        RoomId = GetString(pkt.data, "roomId") ?? "";
                        MySymbol = GetString(pkt.data, "symbol") ?? "";

                        // Sau matched, server thường sẽ gửi state ngay sau đó
                        Status = "Playing";
                        ApplyEnabledFromState();
                        break;

                    case "state":
                        // expect: { roomId, board:[ "X..", ".O.", "..." ], turn:"X", status:"Playing" }
                        Turn = GetString(pkt.data, "turn") ?? "X";
                        Status = GetString(pkt.data, "status") ?? "Playing";

                        var rows = ReadBoardRows(pkt.data);
                        ApplyBoard(rows);
                        ApplyEnabledFromState();
                        break;

                    case "error":
                        MessageBox.Show(GetString(pkt.data, "message") ?? "Error", "Server");
                        break;

                    default:
                        // bỏ qua packet lạ để tránh crash
                        break;
                }

                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(IsMyTurn));
            }
            catch (Exception ex)
            {
                // Quan trọng: không cho crash => không bị tắt cửa sổ
                MessageBox.Show(
                    $"Packet parse lỗi!\nType: {pkt.type}\nData: {pkt.data}\n\n{ex}",
                    "Client Error"
                );
            }
        });
    }

    private static string? GetString(JsonElement data, string prop)
    {
        if (data.ValueKind != JsonValueKind.Object) return null;
        if (!data.TryGetProperty(prop, out var v)) return null;
        return v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString();
    }

    private static string[] ReadBoardRows(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object) return new[] { "...", "...", "..." };
        if (!data.TryGetProperty("board", out var board) || board.ValueKind != JsonValueKind.Array)
            return new[] { "...", "...", "..." };

        var list = board.EnumerateArray()
            .Select(x => x.GetString() ?? "...")
            .ToList();

        // đảm bảo đủ 3 dòng
        while (list.Count < 3) list.Add("...");
        if (list.Count > 3) list = list.Take(3).ToList();

        // đảm bảo mỗi dòng dài 3
        for (int i = 0; i < 3; i++)
        {
            var s = list[i];
            if (string.IsNullOrEmpty(s)) s = "...";
            if (s.Length < 3) s = s.PadRight(3, '.');
            if (s.Length > 3) s = s.Substring(0, 3);
            list[i] = s;
        }

        return list.ToArray();
    }

    private void ApplyBoard(string[] rows)
    {
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                var ch = rows[r][c];
                Cells[r * 3 + c].Text = (ch == '.') ? "" : ch.ToString();
            }
        }
    }

    private void ApplyEnabledFromState()
    {
        bool enableClick = IsMyTurn;

        foreach (var cell in Cells)
        {
            bool empty = string.IsNullOrEmpty(cell.Text);
            cell.IsEnabled = enableClick && empty;
        }

        if (Status != "Playing")
        {
            SetAllCellsEnabled(false);
        }
    }

    private void SetAllCellsEnabled(bool enabled)
    {
        foreach (var cell in Cells) cell.IsEnabled = enabled;
    }

    private void ClearBoard()
    {
        foreach (var cell in Cells) cell.Text = "";
    }
}
