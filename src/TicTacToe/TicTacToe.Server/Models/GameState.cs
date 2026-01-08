namespace TicTacToe.Server.Models;

public class GameState
{
    public string RoomId { get; }
    public Board Board { get; } = new();

    public Player? PlayerX { get; private set; }
    public Player? PlayerO { get; private set; }

    public CellValue Turn { get; private set; } = CellValue.X;
    public GameStatus Status { get; private set; } = GameStatus.WaitingPlayers;

    public GameState(string roomId)
    {
        RoomId = roomId;
    }

    public void SetPlayers(Player x, Player o)
    {
        PlayerX = x;
        PlayerO = o;
        Turn = CellValue.X;
        Status = GameStatus.Playing;
    }

    public bool TryApplyMove(string playerId, Move move, out string error)
    {
        error = "";

        if (Status != GameStatus.Playing)
        {
            error = "Game not in playing state";
            return false;
        }

        var currentPlayer = Turn == CellValue.X ? PlayerX : PlayerO;
        if (currentPlayer == null)
        {
            error = "Players not ready";
            return false;
        }

        if (currentPlayer.Id != playerId)
        {
            error = "Not your turn";
            return false;
        }

        if (!Board.TryPlace(move.Row, move.Col, currentPlayer.Symbol, out error))
            return false;

        // cập nhật trạng thái thắng/hòa
        var winner = Board.GetWinner();
        if (winner == CellValue.X) { Status = GameStatus.WinX; return true; }
        if (winner == CellValue.O) { Status = GameStatus.WinO; return true; }

        if (Board.IsFull()) { Status = GameStatus.Draw; return true; }

        // đổi lượt
        Turn = Turn == CellValue.X ? CellValue.O : CellValue.X;
        return true;
    }
}
