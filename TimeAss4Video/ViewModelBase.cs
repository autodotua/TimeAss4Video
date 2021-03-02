using System.ComponentModel;
using System.Windows;

namespace TimeAss4Video
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
    }
}