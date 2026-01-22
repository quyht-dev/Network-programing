namespace TicTacToe.Server.Models;

public enum CellValue
{
    Empty = 0,
    X = 1,
    O = 2
}

public enum GameStatus
{
    WaitingPlayers = 0,
    Playing = 1,
    Draw = 2,
    WinX = 3,
    WinO = 4
}
