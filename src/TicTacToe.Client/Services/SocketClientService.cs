using System;
using System.IO;              // <- QUAN TRỌNG
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TicTacToe.Client.Protocol;

namespace TicTacToe.Client.Services;

public class SocketClientService
{
    private TcpClient? _tcp;
    private StreamReader? _reader;
    private StreamWriter? _writer;

    public event Action<Packet>? PacketReceived;
    public event Action<string>? Disconnected;

    public async Task ConnectAsync(string host, int port)
    {
        _tcp = new TcpClient();
        await _tcp.ConnectAsync(host, port);

        var stream = _tcp.GetStream();
        _reader = new StreamReader(stream, Encoding.UTF8);
        _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        _ = Task.Run(ReceiveLoop);
    }

    public async Task SendAsync(object obj)
    {
        if (_writer == null) throw new InvalidOperationException("Not connected");

        var json = JsonSerializer.Serialize(obj);
        await _writer.WriteLineAsync(json);   // đúng: WriteLineAsync
    }

    private async Task ReceiveLoop()
    {
        try
        {
            while (true)
            {
                if (_reader == null) break;

                var line = await _reader.ReadLineAsync(); // đúng: ReadLineAsync
                if (line == null) break;

                var pkt = JsonSerializer.Deserialize<Packet>(line);
                if (pkt != null) PacketReceived?.Invoke(pkt);
            }
        }
        catch (Exception ex)
        {
            Disconnected?.Invoke(ex.Message);
        }
        finally
        {
            Disconnected?.Invoke("Server closed");
        }
    }
}
