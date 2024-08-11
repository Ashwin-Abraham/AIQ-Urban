using RhinoWindows.ViewModels;
using System.ComponentModel;

namespace ArchSitePlan.UI
{
    public class ViewModelBase : Rhino.UI.ViewModel, INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
