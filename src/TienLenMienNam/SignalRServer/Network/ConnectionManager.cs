using System;
using System.Collections.Concurrent;

namespace CardGameServer.Network
{
    public class ConnectionManager
    {
        private readonly ConcurrentDictionary<string, SignalRPlayerSession> _sessions =
            new ConcurrentDictionary<string, SignalRPlayerSession>();

        public void AddSession(SignalRPlayerSession session)
        {
            _sessions[session.ConnectionId] = session;
        }

        public SignalRPlayerSession GetSession(string connectionId)
        {
            _sessions.TryGetValue(connectionId, out var session);
            return session;
        }

        public void RemoveSession(string connectionId)
        {
            _sessions.TryRemove(connectionId, out _);
        }

        // Heartbeat - kiểm tra session timeout
        public void CleanupInactiveSessions(TimeSpan timeout)
        {
            var now = DateTime.UtcNow;
            foreach (var session in _sessions.Values)
            {
                if (now - session.LastSeenUtc > timeout)
                {
                    // Đánh dấu hoặc xóa session timeout
                    // (Trong thực tế cần thông báo cho GameEngine)
                }
            }
        }
    }
}