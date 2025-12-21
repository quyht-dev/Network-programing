namespace TicTacToe.Client.ViewModels;

public class CellViewModel : ViewModelBase
{
    public int Row { get; }
    public int Col { get; }

    private string _text = "";
    public string Text
    {
        get => _text;
        set => Set(ref _text, value);
    }

    private bool _isEnabled = true;
    public bool IsEnabled
    {
        get => _isEnabled;
        set => Set(ref _isEnabled, value);
    }

    public CellViewModel(int row, int col)
    {
        Row = row;
        Col = col;
    }
}
