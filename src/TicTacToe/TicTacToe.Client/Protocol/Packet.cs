using System.Text.Json;

namespace TicTacToe.Client.Protocol;

public record Packet(string type, JsonElement data);
