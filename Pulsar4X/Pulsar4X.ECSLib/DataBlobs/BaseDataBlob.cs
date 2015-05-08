using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    [JsonObject(MemberSerialization.OptOut)]
    public abstract class BaseDataBlob : ICloneable, INotifyPropertyChanged
    {
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

        public event PropertyChangedEventHandler PropertyChanged;

        public abstract object Clone();

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
