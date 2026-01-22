// CardGameClient/Network/GameClient.cs
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using System.Windows; 

namespace CardGameClient.Network
{
    // Giữ lại class này để tránh lỗi compile (dù có thể không dùng tới)
    public sealed class NetMessage
    {
        public string Type { get; set; }
        public object Payload { get; set; }
    }

    public sealed class GameClient
    {
        private HubConnection _connection;

        // Sự kiện báo mất kết nối
        public event Action<string> Disconnected;
        
        // Sự kiện tổng để bắn mọi tin từ Server ra cho GameLogic xử lý
        public event Action<string, object> EventReceived; 

        public bool IsConnected => _connection?.State == HubConnectionState.Connected;

        public async Task ConnectAsync(string url)
        {
            // 1. Cấu hình kết nối (Tự động kết nối lại nếu rớt)
            _connection = new HubConnectionBuilder()
                .WithUrl(url)
                .WithAutomaticReconnect()
                .Build();

            // 2. Đăng ký các kênh lắng nghe từ Server
            // Đây là các sự kiện mà Server (GameHub) sẽ bắn về
            
            // Nhận thông báo chào mừng (kèm ID người chơi)
            _connection.On<object>("Welcome", (data) => EventReceived?.Invoke("Welcome", data));
            
            // Nhận trạng thái bàn chơi (quan trọng nhất)
            _connection.On<object>("UpdateGame", (data) => EventReceived?.Invoke("UpdateGame", data));
            _connection.On<object>("GameState", (data) => EventReceived?.Invoke("GameState", data));
            
            // Các sự kiện phụ
            _connection.On<object>("PlayerJoined", (data) => EventReceived?.Invoke("PlayerJoined", data));
            _connection.On<string>("ReceiveMessage", (msg) => EventReceived?.Invoke("ReceiveMessage", msg));
            _connection.On<string>("Error", (msg) => EventReceived?.Invoke("Error", msg));

            // Xử lý khi mất kết nối hẳn
            _connection.Closed += (exception) =>
            {
                Disconnected?.Invoke(exception?.Message ?? "Mất kết nối server");
                return Task.CompletedTask;
            };

            // 3. Bắt đầu kết nối
            try 
            {
                await _connection.StartAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể kết nối Server: {ex.Message}");
                throw; // Ném lỗi để bên ngoài biết
            }
        }

        public async Task DisconnectAsync()
        {
            if (_connection != null)
            {
                await _connection.StopAsync();
                await _connection.DisposeAsync();
                _connection = null;
            }
        }

        // --- PHẦN QUAN TRỌNG NHẤT: SỬA HÀM GỬI ---
        // Thêm từ khóa "params" để gửi được nhiều tham số (VD: gửi cả Tên và RoomID)
        public async Task SendAsync(string methodName, params object[] args)
        {
            if (!IsConnected) return;

            try 
            {
                // InvokeCoreAsync giúp gửi mảng tham số động lên Server
                await _connection.InvokeCoreAsync(methodName, args);
            }
            catch (Exception ex)
            {
                // In lỗi ra cửa sổ Output của Visual Studio để debug
                System.Diagnostics.Debug.WriteLine($"Lỗi gửi tin '{methodName}': {ex.Message}");
            }
        }
    }
}