using System.Windows;
using TicTacToe.Client.Views;

namespace TicTacToe.Client;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var home = new HomeView();
        home.PlayRequested += () =>
        {
            MainContent.Content = new GameView();
        };

        MainContent.Content = home;
    }
}

