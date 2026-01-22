using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

Console.OutputEncoding = Encoding.UTF8;

var listener = new TcpListener(IPAddress.Any, 5000);
listener.Start();
Console.WriteLine("✅ Server listening on port 5000");

var rooms = new ConcurrentDictionary<string, Room>();
ClientConn? waiting = null;
object matchLock = new();

while (true)
{
    var tcp = await listener.AcceptTcpClientAsync();
    var conn = new ClientConn(tcp);
    Console.WriteLine($"🔌 Client connected: {conn.Id}");

    _ = Task.Run(() => HandleClient(conn));
}

async Task HandleClient(ClientConn c)
{
    try
    {
        while (true)
        {
            var line = await c.Reader.ReadLineAsync();
            if (line == null) break;

            Packet? pkt;
            try { pkt = JsonSerializer.Deserialize<Packet>(line); }
            catch
            {
                await c.SendAsync(new { type = "error", data = new { message = "Bad JSON" } });
                continue;
            }
            if (pkt == null) continue;

            if (pkt.type == "queue")
            {
                await HandleQueue(c);
            }
            else if (pkt.type == "move")
            {
                await HandleMove(c, pkt.data);
            }
            else
            {
                await c.SendAsync(new { type = "error", data = new { message = "Unknown type" } });
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Client {c.Id} error: {ex.Message}");
    }
    finally
    {
        Console.WriteLine($"❌ Client disconnected: {c.Id}");
        CleanupDisconnect(c);
        try { c.Tcp.Close(); } catch { }
    }
}

async Task HandleQueue(ClientConn c)
{
    Room? created = null;

    lock (matchLock)
    {
        if (waiting == null || waiting.Id == c.Id)
        {
            waiting = c;
        }
        else
        {
            created = new Room(waiting, c);
            rooms[created.Id] = created;
            waiting = null;
        }
    }

    if (created == null)
    {
        await c.SendAsync(new { type = "queued", data = new { message = "Waiting opponent..." } });
        Console.WriteLine($"🕒 {c.Id} queued");
        return;
    }

    Console.WriteLine($"🎮 Room created: {created.Id} ({created.X.Id}=X vs {created.O.Id}=O)");

    await created.X.SendAsync(new { type = "matched", data = new { roomId = created.Id, symbol = "X" } });
    await created.O.SendAsync(new { type = "matched", data = new { roomId = created.Id, symbol = "O" } });

    await BroadcastState(created);
}

async Task HandleMove(ClientConn c, JsonElement data)
{
    string roomId;
    int r, col;

    try
    {
        roomId = data.GetProperty("roomId").GetString()!;
        r = data.GetProperty("r").GetInt32();
        col = data.GetProperty("c").GetInt32();
    }
    catch
    {
        await c.SendAsync(new { type = "error", data = new { message = "Bad move payload" } });
        return;
    }

    if (!rooms.TryGetValue(roomId, out var room))
    {
        await c.SendAsync(new { type = "error", data = new { message = "Room not found" } });
        return;
    }

    if (!room.TryMove(c, r, col, out var err))
    {
        await c.SendAsync(new { type = "error", data = new { message = err } });
        return;
    }

    await BroadcastState(room);
}

async Task BroadcastState(Room room)
{
    var state = new
    {
        type = "state",
        data = new
        {
            roomId = room.Id,
            board = room.BoardRows(),
            turn = room.Turn.ToString(),
            status = room.Status
        }
    };

    Console.WriteLine("📤 " + JsonSerializer.Serialize(state));
    await room.X.SendAsync(state);
    await room.O.SendAsync(state);
}

void CleanupDisconnect(ClientConn c)
{
    lock (matchLock)
    {
        if (waiting?.Id == c.Id) waiting = null;
    }

    if (c.RoomId != null && rooms.TryRemove(c.RoomId, out var room))
    {
        var other = room.X.Id == c.Id ? room.O : room.X;
        _ = other.SendAsync(new { type = "error", data = new { message = "Opponent disconnected" } });
    }
}

// ====== helper types ======

record Packet(string type, JsonElement data);

class ClientConn
{
    public string Id { get; } = Guid.NewGuid().ToString("N")[..8];
    public TcpClient Tcp { get; }
    public StreamReader Reader { get; }
    public StreamWriter Writer { get; }

    public string? RoomId { get; set; }
    public char Symbol { get; set; } = '?';

    public ClientConn(TcpClient tcp)
    {
        Tcp = tcp;
        var stream = tcp.GetStream();
        Reader = new StreamReader(stream, Encoding.UTF8);
        Writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
    }

    public Task SendAsync(object obj)
        => Writer.WriteLineAsync(JsonSerializer.Serialize(obj));
}

class Room
{
    public string Id { get; } = "R" + Guid.NewGuid().ToString("N")[..6];
    public ClientConn X { get; }
    public ClientConn O { get; }

    private readonly char[,] _b = new char[3, 3];
    public char Turn { get; private set; } = 'X';
    public string Status { get; private set; } = "Playing"; // Playing/WinX/WinO/Draw

    public Room(ClientConn x, ClientConn o)
    {
        X = x; O = o;
        x.RoomId = Id; o.RoomId = Id;
        x.Symbol = 'X'; o.Symbol = 'O';

        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                _b[r, c] = '.';
    }

    public bool TryMove(ClientConn p, int r, int c, out string err)
    {
        err = "";

        if (Status != "Playing") { err = "Game ended"; return false; }
        if (p.Symbol != Turn) { err = "Not your turn"; return false; }
        if (r < 0 || r > 2 || c < 0 || c > 2) { err = "Out of range"; return false; }
        if (_b[r, c] != '.') { err = "Cell occupied"; return false; }

        _b[r, c] = Turn;

        if (IsWin(Turn)) Status = Turn == 'X' ? "WinX" : "WinO";
        else if (IsDraw()) Status = "Draw";
        else Turn = Turn == 'X' ? 'O' : 'X';

        return true;
    }

    bool IsDraw()
    {
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                if (_b[r, c] == '.') return false;
        return true;
    }

    bool IsWin(char s)
    {
        for (int i = 0; i < 3; i++)
        {
            if (_b[i, 0] == s && _b[i, 1] == s && _b[i, 2] == s) return true;
            if (_b[0, i] == s && _b[1, i] == s && _b[2, i] == s) return true;
        }
        if (_b[0, 0] == s && _b[1, 1] == s && _b[2, 2] == s) return true;
        if (_b[0, 2] == s && _b[1, 1] == s && _b[2, 0] == s) return true;
        return false;
    }

    public string[] BoardRows()
        => new[]
        {
            new string(new[]{_b[0,0],_b[0,1],_b[0,2]}),
            new string(new[]{_b[1,0],_b[1,1],_b[1,2]}),
            new string(new[]{_b[2,0],_b[2,1],_b[2,2]})
        };
}

