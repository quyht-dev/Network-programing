using System.Windows;
using System.Windows.Controls;
using TicTacToe.Client.ViewModels;

namespace TicTacToe.Client.Views
{
    public partial class GameView : UserControl
    {
        public GameView()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Nếu chiều rộng nhỏ hơn 650px thì chuyển sang giao diện dọc
            if (e.NewSize.Width < 650)
            {
                VisualStateManager.GoToState(this, "NarrowState", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "WideState", true);
            }
        }
    }
}