// CardGameClient/MainWindow.xaml.cs
using System.Windows;
using CardGameClient.Game;
using CardGameClient.ViewModels;

namespace CardGameClient
{
    public partial class MainWindow : Window
    {
        private readonly GameLogic _logic;

        public MainWindow()
        {
            InitializeComponent();

            _logic = new GameLogic(Dispatcher);
            DataContext = _logic;
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            await _logic.ConnectAsync();
        }

        private async void Join_Click(object sender, RoutedEventArgs e)
        {
            await _logic.JoinAsync();
        }

        private async void Ready_Click(object sender, RoutedEventArgs e)
        {
            await _logic.ReadyAsync(true);
        }

        private async void Play_Click(object sender, RoutedEventArgs e)
        {
            await _logic.PlaySelectedAsync();
        }

        private async void Pass_Click(object sender, RoutedEventArgs e)
        {
            await _logic.PassAsync();
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            _logic.Disconnect();
        }

        private void ClearSelection_Click(object sender, RoutedEventArgs e)
        {
            _logic.ClearSelection();
        }

        private void Card_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as FrameworkElement;
            var vm = btn?.Tag as CardViewModel;
            if (vm != null) _logic.ToggleSelect(vm);
        }
    }
}
