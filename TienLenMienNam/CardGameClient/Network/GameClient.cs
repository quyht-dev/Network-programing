// CardGameClient/Network/GameClient.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CardGameClient.Network
{
    public sealed class NetMessage
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("payload")]
        public JObject Payload { get; set; }
    }

    /// <summary>
    /// Client giao tiếp với server qua SignalR
    /// Thay thế TCP bằng HubConnection
    /// </summary>
    internal sealed class GameClient
    {
        private HubConnection _hubConnection;
        private CancellationTokenSource _cts;

        // Semaphore vẫn giữ để đảm bảo thread safety khi gửi
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        public event Action<NetMessage> MessageReceived;
        public event Action<string> Disconnected;

        /// <summary>
        /// Kết nối đến SignalR server
        /// </summary>
        public async Task ConnectAsync(string host, int port)
        {
            try
            {
                _cts = new CancellationTokenSource();

                // Xây dựng URL SignalR 
                // Lưu ý: SignalR mặc định dùng /gameHub (chứ không phải /gamehub)
                string url = $"http://{host}:{port}/gameHub";

                // Tạo HubConnection với cấu hình đơn giản
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(url)
                    .WithAutomaticReconnect() // Tự động reconnect nếu mất kết nối
                    .Build();

                // Đăng ký handler nhận message từ server
                _hubConnection.On<NetMessage>("ReceiveMessage", (msg) =>
                {
                    MessageReceived?.Invoke(msg);
                });

                // Xử lý sự kiện ngắt kết nối
                _hubConnection.Closed += OnConnectionClosed;

                // Bắt đầu kết nối
                await _hubConnection.StartAsync(_cts.Token);
            }
            catch (Exception ex)
            {
                throw new Exception($"SignalR connection failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gửi message đến server qua SignalR
        /// </summary>
        // CardGameClient/Network/GameClient.cs
        public async Task SendAsync(string type, object payload, string requestId = null)
        {
            if (_hubConnection?.State != HubConnectionState.Connected)
                return;

            // Tạo message object
            var msg = new NetMessage
            {
                Type = type,
                RequestId = requestId,
                Payload = payload == null ? new JObject() : JObject.FromObject(payload)
            };

            await _sendLock.WaitAsync();
            try
            {
                // Gọi method tương ứng với type
                switch (type)
                {
                    case "join":
                        await _hubConnection.InvokeAsync("Join",
                            msg.Payload.Value<string>("name"),
                            msg.Payload.Value<string>("roomId"));
                        break;

                    case "ready":
                        await _hubConnection.InvokeAsync("Ready",
                            msg.Payload.Value<bool>("ready"));
                        break;

                    case "play":
                        var cards = msg.Payload["cards"]?.ToObject<string[]>() ?? new string[0];
                        await _hubConnection.InvokeAsync("Play", cards);
                        break;

                    case "pass":
                        await _hubConnection.InvokeAsync("Pass");
                        break;

                    case "chat":
                        await _hubConnection.InvokeAsync("Chat",
                            msg.Payload.Value<string>("text"));
                        break;

                    case "ping":
                        await _hubConnection.InvokeAsync("Ping", msg.Payload);
                        break;

                    default:
                        // Fallback: gửi nguyên message nếu không biết type
                        await _hubConnection.InvokeAsync("SendMessage", msg);
                        break;
                }
            }
            finally
            {
                _sendLock.Release();
            }
        }

        /// <summary>
        /// Ngắt kết nối
        /// </summary>
        public async void Disconnect()
        {
            try
            {
                _cts?.Cancel();
                if (_hubConnection != null)
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                }
            }
            catch
            {
                // Ignore errors during disconnect
            }
            finally
            {
                Disconnected?.Invoke("Client disconnected");
            }
        }

        /// <summary>
        /// Xử lý khi kết nối bị đóng
        /// </summary>
        private Task OnConnectionClosed(Exception ex)
        {
            string reason = ex?.Message ?? "Connection closed";
            Disconnected?.Invoke(reason);
            return Task.CompletedTask;
        }
    }
}