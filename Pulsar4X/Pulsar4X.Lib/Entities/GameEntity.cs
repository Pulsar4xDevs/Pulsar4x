using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities
{
    /// <summary>
    /// Base Game Entity. All objects/entities in the game should inherit from this.
    /// </summary>
    public class GameEntity : INotifyPropertyChanged
    {
        public Guid Id { get; set; }

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        public GameEntity()
        {
            Id = Guid.NewGuid();
        }

        public override string ToString()
        {
            if (Name != null)
            {
                return Name;
            }

            return this.GetType().FullName;
        }

        public virtual void OnPropertyChanged(string propertyName)
        {
            var propertyChanged = PropertyChanged;
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
