using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CardGameClient.Game;
using CardGameClient.ViewModels;
using CardGameClient.Models;

namespace CardGameClient
{
    // 1. Converter hiệu ứng nhô bài (Giữ nguyên)
    public class BoolToOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? -30.0 : 0.0;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    // 2. Converter tìm ảnh (Giữ nguyên - Đã fix hiển thị bài trên bàn)
    public class ImagePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int rank = 0;
            string suitChar = "";

            // Trường hợp 1: Bài trên tay (Object)
            if (value is CardViewModel cvm) { rank = cvm.Card.Rank; suitChar = GetSuitChar(cvm.Card.Suit); }
            else if (value is Card card) { rank = card.Rank; suitChar = GetSuitChar(card.Suit); }
            
            // Trường hợp 2: Bài trên bàn (String Code "10H", "3S"...)
            else if (value is string code && !string.IsNullOrEmpty(code))
            {
                string rankStr = code.Length > 2 && code.StartsWith("10") ? "10" : code.Substring(0, 1);
                suitChar = code.Substring(code.Length - 1);

                if (rankStr == "J") rank = 11;
                else if (rankStr == "Q") rank = 12;
                else if (rankStr == "K") rank = 13;
                else if (rankStr == "A") rank = 14;
                else if (rankStr == "2") rank = 15;
                else int.TryParse(rankStr, out rank);
            }
            else return null;

            // Xử lý tên file ảnh
            string rankName = rank.ToString();
            switch (rank)
            {
                case 11: rankName = "jack"; break;
                case 12: rankName = "queen"; break;
                case 13: rankName = "king"; break;
                case 14: rankName = "ace"; break;
                case 15: rankName = "2"; break;
            }

            string suitName = "";
            switch (suitChar)
            {
                case "S": suitName = "spades"; break;
                case "C": suitName = "clubs"; break;
                case "D": suitName = "diamonds"; break;
                case "H": suitName = "hearts"; break;
                default: return null;
            }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = Path.Combine(baseDir, "Resources", "Cards", $"{rankName}_of_{suitName}.png");

            if (File.Exists(fullPath)) return new Uri(fullPath, UriKind.Absolute);
            return null;
        }

        private string GetSuitChar(Suit suit)
        {
            switch (suit)
            {
                case Suit.Spades: return "S";
                case Suit.Clubs: return "C";
                case Suit.Diamonds: return "D";
                case Suit.Hearts: return "H";
                default: return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    // 3. MAIN WINDOW (CODE BEHIND)
    public partial class MainWindow : Window
    {
        private GameLogic _logic;

        public MainWindow() { InitializeComponent(); }

        // Constructor nhận tham số từ LoginWindow
        public MainWindow(string playerName, string roomId, string url) : this()
        {
            _logic = new GameLogic(this.Dispatcher);
            _logic.PlayerName = playerName;
            _logic.RoomId = roomId;
            _logic.ServerUrl = url;
            this.DataContext = _logic;
            
            // Tự động kết nối và vào phòng
            AutoConnectAndJoin();
        }

        private async void AutoConnectAndJoin()
        {
            try
            {
                await _logic.ConnectAsync();
                await Task.Delay(500); 
                await _logic.JoinAsync();
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
        }

        // --- SỰ KIỆN NÚT BẤM CŨ ---
        private void Card_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is CardViewModel cardVM) _logic.ToggleSelect(cardVM);
        }

        private async void Play_Click(object sender, RoutedEventArgs e) => await _logic.PlaySelectedAsync();
        private async void Pass_Click(object sender, RoutedEventArgs e) => await _logic.PassAsync();
        private async void Ready_Click(object sender, RoutedEventArgs e) => await _logic.ReadyAsync(true);
        
        private async void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            await _logic.Disconnect();
            new LoginWindow().Show(); // Quay về màn hình đăng nhập
            this.Close();
        }

        // --- SỰ KIỆN MỚI: RESET GAME (THÊM VÀO ĐÂY) ---
        private async void RequestReset_Click(object sender, RoutedEventArgs e)
        {
            if (_logic != null) await _logic.SendRequestReset();
        }

        private async void AcceptReset_Click(object sender, RoutedEventArgs e)
        {
            if (_logic != null) await _logic.SendAcceptReset();
        }

        private void DeclineReset_Click(object sender, RoutedEventArgs e)
        {
            if (_logic != null) _logic.CloseResetAlert();
        }
    }
}