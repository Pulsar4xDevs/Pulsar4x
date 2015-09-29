using System.ComponentModel;

namespace Pulsar4X.WPFUI.ViewModels
{
    public interface IViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Refreshes the properties of this ViewModel.
        /// 
        /// If partialRefresh is set to true, the ViewModel will try to update only data that changes during a pulse.
        /// </summary>
        void Refresh(bool partialRefresh = false);
    }
}