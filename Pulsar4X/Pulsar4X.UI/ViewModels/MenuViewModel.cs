using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using System.Linq.Expressions;
using Pulsar4X;

namespace Pulsar4X.UI.ViewModels
{
    public class MenuViewModel : INotifyPropertyChanged
    {
        private string _GameDateTime;
        public string GameDateTime
        {
            get
            {
                return _GameDateTime;
            }
            set
            {
                _GameDateTime = value;
                OnPropertyChanged(() => GameDateTime);
            }
        }


        public MenuViewModel()
        {
            // get the current gameDateTime:
            
            _GameDateTime = "Pulsar4X - " + GameState.Instance.GameDateTime.ToString(); // note that date should be formated according to system local!
        }

        private void OnPropertyChanged(Expression<Func<object>> property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this,
                    new PropertyChangedEventArgs(BindingHelper.Name(property)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
