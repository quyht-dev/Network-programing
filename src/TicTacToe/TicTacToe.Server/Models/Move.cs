namespace TicTacToe.Server.Models;

public readonly struct Move
{
    public int Row { get; }
    public int Col { get; }

    public Move(int row, int col)
    {
        Row = row;
        Col = col;
    }
}
