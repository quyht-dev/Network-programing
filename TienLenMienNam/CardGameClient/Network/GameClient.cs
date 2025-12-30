// CardGameClient/Network/GameClient.cs
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

    internal sealed class GameClient
    {
        private TcpClient _tcp;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;

        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        public bool IsConnected => _tcp != null && _tcp.Connected;

        public event Action<NetMessage> MessageReceived;
        public event Action<string> Disconnected;

        public async Task ConnectAsync(string host, int port)
        {
            _cts = new CancellationTokenSource();
            _tcp = new TcpClient();
            _tcp.NoDelay = true;

            await _tcp.ConnectAsync(host, port);
            _stream = _tcp.GetStream();

            _ = Task.Run(() => ReadLoopAsync(_cts.Token));
        }

        public void Disconnect()
        {
            try { _cts?.Cancel(); } catch { }
            try { _stream?.Close(); } catch { }
            try { _tcp?.Close(); } catch { }
        }

        public async Task SendAsync(string type, object payload, string requestId = null)
        {
            if (_stream == null) return;

            var msg = new NetMessage
            {
                Type = type,
                RequestId = requestId,
                Payload = payload == null ? new JObject() : JObject.FromObject(payload)
            };

            byte[] jsonBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msg));
            byte[] header = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(jsonBytes.Length));

            await _sendLock.WaitAsync();
            try
            {
                await _stream.WriteAsync(header, 0, 4);
                await _stream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
                await _stream.FlushAsync();
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private async Task ReadLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    byte[] header = await ReadExactAsync(4, ct);
                    if (header == null) break;

                    int len = System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(header, 0));
                    if (len <= 0 || len > 2_000_000)
                        throw new Exception("Bad frame length: " + len);

                    byte[] payload = await ReadExactAsync(len, ct);
                    if (payload == null) break;

                    string json = Encoding.UTF8.GetString(payload);
                    NetMessage msg = null;
                    try
                    {
                        msg = JsonConvert.DeserializeObject<NetMessage>(json);
                    }
                    catch
                    {
                        // ignore bad message
                    }

                    if (msg != null)
                        MessageReceived?.Invoke(msg);
                }
            }
            catch (Exception ex)
            {
                Disconnected?.Invoke(ex.Message);
            }
            finally
            {
                Disconnected?.Invoke("Connection closed");
                Disconnect();
            }
        }

        private async Task<byte[]> ReadExactAsync(int n, CancellationToken ct)
        {
            byte[] buf = new byte[n];
            int offset = 0;

            while (offset < n)
            {
                int read = await _stream.ReadAsync(buf, offset, n - offset, ct);
                if (read <= 0) return null;
                offset += read;
            }
            return buf;
        }
    }
}
