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
    // 1. Converter hiệu ứng nhô bài
    public class BoolToOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? -30.0 : 0.0;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    // 2. Converter tìm ảnh (ĐÃ SỬA LỖI ĐỂ HIỆN BÀI TRÊN BÀN)
    public class ImagePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int rank = 0;
            string suitChar = "";

            // --- TRƯỜNG HỢP 1: Bài trên tay (Là đối tượng Card/CardViewModel) ---
            if (value is CardViewModel cvm) { rank = cvm.Card.Rank; suitChar = GetSuitChar(cvm.Card.Suit); }
            else if (value is Card card) { rank = card.Rank; suitChar = GetSuitChar(card.Suit); }
            
            // --- TRƯỜNG HỢP 2: Bài trên bàn (Là chuỗi "3S", "10H"...) ---
            else if (value is string code && !string.IsNullOrEmpty(code))
            {
                // Cắt chuỗi: Ví dụ "10H" -> Rank="10", Suit="H"
                string rankStr = code.Substring(0, code.Length - 1);
                suitChar = code.Substring(code.Length - 1);

                if (rankStr == "J") rank = 11;
                else if (rankStr == "Q") rank = 12;
                else if (rankStr == "K") rank = 13;
                else if (rankStr == "A") rank = 14;
                else if (rankStr == "2") rank = 15;
                else int.TryParse(rankStr, out rank);
            }
            else return null;

            // --- XỬ LÝ TÊN FILE ---
            string rankName = rank.ToString();
            switch (rank)
            {
                case 11: rankName = "jack"; break;
                case 12: rankName = "queen"; break;
                case 13: rankName = "king"; break;
                case 14: rankName = "ace"; break;
                case 15: rankName = "2"; break; // File ảnh là 2_of_...
            }

            string suitName = "";
            switch (suitChar)
            {
                case "S": suitName = "spades"; break;   // Bích
                case "C": suitName = "clubs"; break;    // Chuồn
                case "D": suitName = "diamonds"; break; // Rô
                case "H": suitName = "hearts"; break;   // Cơ
                default: return null;
            }

            // Tạo đường dẫn tuyệt đối
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

    // 3. MAIN WINDOW
    public partial class MainWindow : Window
    {
        private GameLogic _logic;

        public MainWindow() { InitializeComponent(); }

        public MainWindow(string playerName, string roomId, string url) : this()
        {
            _logic = new GameLogic(this.Dispatcher);
            _logic.PlayerName = playerName;
            _logic.RoomId = roomId;
            _logic.ServerUrl = url;
            this.DataContext = _logic;
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
            new LoginWindow().Show();
            this.Close();
        }
    }
}