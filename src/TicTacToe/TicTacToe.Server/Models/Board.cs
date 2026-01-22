using System.Text;

namespace TicTacToe.Server.Models;

public class Board
{
    private readonly CellValue[,] _cells = new CellValue[3, 3];

    public CellValue Get(int r, int c) => _cells[r, c];

    public bool InRange(int r, int c) => r >= 0 && r < 3 && c >= 0 && c < 3;

    public bool IsEmpty(int r, int c) => _cells[r, c] == CellValue.Empty;

    public bool TryPlace(int r, int c, CellValue symbol, out string error)
    {
        error = "";
        if (symbol == CellValue.Empty) { error = "Invalid symbol"; return false; }
        if (!InRange(r, c)) { error = "Out of range"; return false; }
        if (!IsEmpty(r, c)) { error = "Cell occupied"; return false; }

        _cells[r, c] = symbol;
        return true;
    }

    public bool IsFull()
    {
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                if (_cells[r, c] == CellValue.Empty) return false;
        return true;
    }

    public CellValue GetWinner()
    {
        // rows
        for (int r = 0; r < 3; r++)
        {
            if (_cells[r, 0] != CellValue.Empty &&
                _cells[r, 0] == _cells[r, 1] &&
                _cells[r, 1] == _cells[r, 2])
                return _cells[r, 0];
        }

        // cols
        for (int c = 0; c < 3; c++)
        {
            if (_cells[0, c] != CellValue.Empty &&
                _cells[0, c] == _cells[1, c] &&
                _cells[1, c] == _cells[2, c])
                return _cells[0, c];
        }

        // diagonals
        if (_cells[0, 0] != CellValue.Empty &&
            _cells[0, 0] == _cells[1, 1] &&
            _cells[1, 1] == _cells[2, 2])
            return _cells[0, 0];

        if (_cells[0, 2] != CellValue.Empty &&
            _cells[0, 2] == _cells[1, 1] &&
            _cells[1, 1] == _cells[2, 0])
            return _cells[0, 2];

        return CellValue.Empty;
    }

    // Gửi cho client dạng 3 dòng "X..", ".O.", "..."
    public string[] ToRowStrings()
    {
        string Map(CellValue v) => v switch
        {
            CellValue.X => "X",
            CellValue.O => "O",
            _ => "."
        };

        var rows = new string[3];
        for (int r = 0; r < 3; r++)
        {
            var sb = new StringBuilder(3);
            for (int c = 0; c < 3; c++)
                sb.Append(Map(_cells[r, c]));
            rows[r] = sb.ToString();
        }
        return rows;
    }
}
