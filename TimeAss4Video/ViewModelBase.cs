using System.ComponentModel;
using System.Windows;

namespace TimeAss4Video
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public ViewModelBase(Window win)
        {
            win.DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}