// CardGameClient/ViewModels/ViewModelBase.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CardGameClient.ViewModels
{
    internal class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void Raise([CallerMemberName] string prop = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
