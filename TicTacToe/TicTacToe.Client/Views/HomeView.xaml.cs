using System;
using System.Windows;
using System.Windows.Controls;

namespace TicTacToe.Client.Views;

public partial class HomeView : UserControl
{
    // Sự kiện để MainWindow có thể lắng nghe và chuyển màn hình
    public event Action? PlayRequested;

    public HomeView()
    {
        InitializeComponent();
    }

    private void Play_Click(object sender, RoutedEventArgs e)
    {
        // Khi nhấn nút, gọi sự kiện để báo cho lớp cha (MainWindow)
        PlayRequested?.Invoke();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        // Đóng ứng dụng hoàn toàn
        Application.Current.Shutdown();
    }
}