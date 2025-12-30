// CardGameServer/Network/GameServer.cs
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using CardGameServer.Game;
using Newtonsoft.Json;

namespace CardGameServer.Network
{
    public sealed class GameServer
    {
        private readonly int _port;
        private readonly GameEngine _engine;

        private TcpListener _listener;
        private Thread _acceptThread;

        private readonly object _gate = new object();
        private readonly Dictionary<string, PlayerSession> _sessions = new Dictionary<string, PlayerSession>();

        private volatile bool _running;

        // Heartbeat
        private Thread _heartbeatThread;
        private readonly TimeSpan _pingInterval = TimeSpan.FromSeconds(5);
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(15);

        public GameServer(int port, GameEngine engine)
        {
            _port = port;
            _engine = engine;
        }

        public void Start()
        {
            if (_running) return;
            _running = true;

            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();

            _acceptThread = new Thread(AcceptLoop) { IsBackground = true };
            _acceptThread.Start();

            _heartbeatThread = new Thread(HeartbeatLoop) { IsBackground = true };
            _heartbeatThread.Start();

            Console.WriteLine("Listening on 0.0.0.0:" + _port);
        }

        public void Stop()
        {
            _running = false;
            try { _listener.Stop(); } catch { }

            // close all
            List<PlayerSession> all;
            lock (_gate) all = new List<PlayerSession>(_sessions.Values);

            foreach (var s in all)
                s.Close();
        }

        private void AcceptLoop()
        {
            while (_running)
            {
                try
                {
                    var client = _listener.AcceptTcpClient();
                    var playerId = Guid.NewGuid().ToString("N");
                    var session = new PlayerSession(client, playerId);

                    lock (_gate) _sessions[playerId] = session;

                    Console.WriteLine("Client connected: " + playerId);
                    session.SendWelcome();

                    var t = new Thread(() => ClientReadLoop(session)) { IsBackground = true };
                    t.Start();
                }
                catch (SocketException)
                {
                    if (!_running) break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Accept error: " + ex.Message);
                }
            }
        }

        private void ClientReadLoop(PlayerSession session)
        {
            try
            {
                var stream = session.Stream;

                while (_running && session.IsConnected)
                {
                    // read header 4 bytes
                    byte[] header = ReadExact(stream, 4);
                    if (header == null) break;

                    int len = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(header, 0));
                    if (len <= 0 || len > 2_000_000)
                    {
                        session.SendError("BAD_FRAME", "Invalid frame length.");
                        break;
                    }

                    byte[] payload = ReadExact(stream, len);
                    if (payload == null) break;

                    session.Touch();

                    string json = Encoding.UTF8.GetString(payload);
                    NetMessage msg = null;

                    try
                    {
                        msg = JsonConvert.DeserializeObject<NetMessage>(json);
                    }
                    catch
                    {
                        session.SendError("BAD_JSON", "Cannot parse JSON.");
                        continue;
                    }

                    if (msg == null || string.IsNullOrWhiteSpace(msg.Type))
                    {
                        session.SendError("BAD_MSG", "Missing type.");
                        continue;
                    }

                    // dispatch
                    _engine.HandleMessage(session, msg);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Client loop error: " + ex.Message);
            }
            finally
            {
                HandleDisconnect(session);
            }
        }

        private void HandleDisconnect(PlayerSession session)
        {
            Console.WriteLine("Disconnected: " + session.PlayerId);

            lock (_gate)
            {
                _sessions.Remove(session.PlayerId);
            }

            try { _engine.OnDisconnected(session); } catch { }
            try { session.Close(); } catch { }
        }

        private void HeartbeatLoop()
        {
            DateTime lastPing = DateTime.UtcNow;

            while (_running)
            {
                try
                {
                    Thread.Sleep(500);

                    var now = DateTime.UtcNow;
                    if (now - lastPing >= _pingInterval)
                    {
                        lastPing = now;

                        List<PlayerSession> all;
                        lock (_gate) all = new List<PlayerSession>(_sessions.Values);

                        foreach (var s in all)
                        {
                            // ping
                            try
                            {
                                s.Send("ping", null, new { t = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });
                            }
                            catch { /* ignore */ }
                        }
                    }

                    // timeout sweep
                    {
                        List<PlayerSession> all;
                        lock (_gate) all = new List<PlayerSession>(_sessions.Values);

                        foreach (var s in all)
                        {
                            if (now - s.LastSeenUtc > _timeout)
                            {
                                Console.WriteLine("Timeout: " + s.PlayerId);
                                HandleDisconnect(s);
                            }
                        }
                    }
                }
                catch
                {
                    // ignore heartbeat errors
                }
            }
        }

        private static byte[] ReadExact(NetworkStream stream, int n)
        {
            byte[] buf = new byte[n];
            int offset = 0;

            while (offset < n)
            {
                int read = stream.Read(buf, offset, n - offset);
                if (read <= 0) return null; // remote closed
                offset += read;
            }
            return buf;
        }
    }
}

