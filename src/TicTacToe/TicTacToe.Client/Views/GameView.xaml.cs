using System.Windows.Controls;
using TicTacToe.Client.ViewModels;

namespace TicTacToe.Client.Views;

public partial class GameView : UserControl
{
    public GameView()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
