using System.Windows.Controls;

namespace TicTacToe.Client.Views
{
    // Quan trọng: Phải kế thừa từ UserControl nếu XAML của bạn là <UserControl>
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent(); // Lệnh này dùng để nạp giao diện từ XAML lên
        }
    }
}