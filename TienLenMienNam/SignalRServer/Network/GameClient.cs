// CardGameServer/Network/GameClient.cs
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CardGameServer.Network
{
    public sealed class GameClient
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _readThread;

        public event Action<NetMessage> OnMessage;

        public void Connect(string host, int port)
        {
            _client = new TcpClient();
            _client.Connect(host, port);
            _client.NoDelay = true;

            _stream = _client.GetStream();
            _readThread = new Thread(ReadLoop) { IsBackground = true };
            _readThread.Start();
        }

        public void Send(string type, object payload)
        {
            var msg = new NetMessage
            {
                Type = type,
                RequestId = null,
                Payload = payload == null ? new JObject() : JObject.FromObject(payload)
            };

            byte[] json = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msg));
            byte[] header = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(json.Length));

            _stream.Write(header, 0, 4);
            _stream.Write(json, 0, json.Length);
            _stream.Flush();
        }

        private void ReadLoop()
        {
            try
            {
                while (_client.Connected)
                {
                    byte[] header = ReadExact(4);
                    if (header == null) break;

                    int len = System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(header, 0));
                    if (len <= 0 || len > 2_000_000) break;

                    byte[] payload = ReadExact(len);
                    if (payload == null) break;

                    var json = Encoding.UTF8.GetString(payload);
                    var msg = JsonConvert.DeserializeObject<NetMessage>(json);

                    var cb = OnMessage;
                    if (cb != null) cb(msg);
                }
            }
            catch
            {
                // ignore
            }
        }

        private byte[] ReadExact(int n)
        {
            byte[] buf = new byte[n];
            int offset = 0;
            while (offset < n)
            {
                int read = _stream.Read(buf, offset, n - offset);
                if (read <= 0) return null;
                offset += read;
            }
            return buf;
        }

        public void Close()
        {
            try { _stream.Close(); } catch { }
            try { _client.Close(); } catch { }
        }
    }
}
