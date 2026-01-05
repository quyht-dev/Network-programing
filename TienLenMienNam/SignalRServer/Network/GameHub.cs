using System;
using System.Threading.Tasks;
using CardGameServer.Game;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;

namespace CardGameServer.Network
{
    public class GameHub : Hub
    {
        private readonly GameEngine _engine;
        private readonly ConnectionManager _connectionManager;

        public GameHub(GameEngine engine, ConnectionManager connectionManager)
        {
            _engine = engine;
            _connectionManager = connectionManager;
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var session = new SignalRPlayerSession(
                    Context.ConnectionId,
                    Guid.NewGuid().ToString("N"),
                    Clients.Caller
                );

                _connectionManager.AddSession(session);
                session.SendWelcome();

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OnConnected error: {ex.Message}");
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var session = _connectionManager.GetSession(Context.ConnectionId);
                if (session != null)
                {
                    _engine.OnDisconnected(session);
                    _connectionManager.RemoveSession(Context.ConnectionId);
                }

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OnDisconnected error: {ex.Message}");
            }
        }

        // Client methods
        public async Task Join(string name, string roomId)
        {
            var session = _connectionManager.GetSession(Context.ConnectionId);
            if (session == null) return;

            var msg = new NetMessage
            {
                Type = "join",
                Payload = JObject.FromObject(new { name, roomId })
            };
            _engine.HandleMessage(session, msg);
        }

        public async Task Ready(bool ready)
        {
            var session = _connectionManager.GetSession(Context.ConnectionId);
            if (session == null) return;

            var msg = new NetMessage
            {
                Type = "ready",
                Payload = JObject.FromObject(new { ready })
            };
            _engine.HandleMessage(session, msg);
        }

        public async Task Play(string[] cards)
        {
            var session = _connectionManager.GetSession(Context.ConnectionId);
            if (session == null) return;

            var msg = new NetMessage
            {
                Type = "play",
                Payload = JObject.FromObject(new { cards })
            };
            _engine.HandleMessage(session, msg);
        }

        public async Task Pass()
        {
            var session = _connectionManager.GetSession(Context.ConnectionId);
            if (session == null) return;

            var msg = new NetMessage
            {
                Type = "pass"
            };
            _engine.HandleMessage(session, msg);
        }

        public async Task Chat(string text)
        {
            var session = _connectionManager.GetSession(Context.ConnectionId);
            if (session == null) return;

            var msg = new NetMessage
            {
                Type = "chat",
                Payload = JObject.FromObject(new { text })
            };
            _engine.HandleMessage(session, msg);
        }

        public async Task Ping(JObject payload)
        {
            var session = _connectionManager.GetSession(Context.ConnectionId);
            if (session == null) return;

            var msg = new NetMessage
            {
                Type = "ping",
                Payload = payload
            };
            _engine.HandleMessage(session, msg);
        }
    }
}