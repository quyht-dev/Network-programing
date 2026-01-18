using System;
using System.Windows;

namespace CardGameClient
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnEnter_Click(object sender, RoutedEventArgs e)
        {
            // Lấy thông tin từ giao diện
            // (Nếu VS Code gạch đỏ TxtName, TxtRoom... thì KỆ NÓ, miễn Build được là OK)
            string name = TxtName.Text;
            string room = TxtRoom.Text;
            string url = TxtUrl.Text;

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Vui lòng nhập tên!");
                return;
            }

            try 
            {
                // 1. Thử tạo màn hình bàn chơi
                MainWindow gameWindow = new MainWindow(name, room, url);
                
                // 2. Nếu tạo thành công thì hiện lên
                gameWindow.Show();
                
                // 3. Đóng màn hình đăng nhập
                this.Close();
            }
            catch (Exception ex)
            {
                // NẾU CÓ LỖI: Nó sẽ hiện ra đây thay vì văng game
                MessageBox.Show($"Lỗi không thể vào bàn chơi:\n{ex.Message}\n\nChi tiết: {ex.InnerException?.Message}", "Lỗi Game");
                
                // Hiện lại màn hình login nếu lỡ ẩn
                this.Show();
            }
        }
    }
}