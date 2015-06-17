using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    [JsonObject(MemberSerialization.OptOut)]
    public abstract class BaseDataBlob : ICloneable
    {
        [CanBeNull]
        [JsonIgnore]
        public virtual Entity OwningEntity
        {
            get { return _owningEntity; }
            set
            {
                if (Equals(value, _owningEntity))
                {
                    return;
                }
                _owningEntity = value;
                OnPropertyChanged();
            }
        }
        private Entity _owningEntity;

        [JsonIgnore]
        public readonly object LockObject = new object();

        // Partial implementation of INotifyPropertyChanged.
        // Note, derived datablobs are NOT fully required to implement.
        public event PropertyChangedEventHandler PropertyChanged;

        public abstract object Clone();

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
