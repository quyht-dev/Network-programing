using System;
using System.Windows;

namespace CardGameClient
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Đăng ký bắt lỗi toàn bộ ứng dụng
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                // Khi có lỗi sập game, hiện bảng thông báo
                MessageBox.Show("Lỗi nghiêm trọng: " + ((Exception)args.ExceptionObject).Message);
            };

            base.OnStartup(e);
        }
    }
}