// CardGameClient/ViewModels/CardViewModel.cs
using System.Windows.Media;
using CardGameClient.Models;

namespace CardGameClient.ViewModels
{
    internal class CardViewModel : ViewModelBase
    {
        public Card Card { get; }

        private bool _isSelected;

        public CardViewModel(Card card)
        {
            Card = card;
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                Raise();
                Raise(nameof(BorderBrush));
            }
        }

        public Brush BorderBrush => IsSelected ? Brushes.Gold : Brushes.Transparent;

        public string Code => Card.ToCode();
    }
}
