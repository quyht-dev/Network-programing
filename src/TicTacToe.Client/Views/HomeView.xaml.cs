using System;
using System.Windows;
using System.Windows.Controls;

namespace TicTacToe.Client.Views;

public partial class HomeView : UserControl
{
    public event Action? PlayRequested;

    public HomeView()
    {
        InitializeComponent(); // chỉ gọi, không tự định nghĩa
    }

    private void Play_Click(object sender, RoutedEventArgs e)
    {
        PlayRequested?.Invoke();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
