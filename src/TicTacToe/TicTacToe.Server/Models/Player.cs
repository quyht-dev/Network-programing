namespace TicTacToe.Server.Models;

public class Player
{
    public string Id { get; }          // thường là ClientId từ socket
    public string Name { get; }        // có thể để mặc định
    public CellValue Symbol { get; }   // X hoặc O

    public Player(string id, CellValue symbol, string? name = null)
    {
        if (symbol == CellValue.Empty) throw new ArgumentException("Symbol must be X or O");
        Id = id;
        Symbol = symbol;
        Name = string.IsNullOrWhiteSpace(name) ? id : name;
    }
}
