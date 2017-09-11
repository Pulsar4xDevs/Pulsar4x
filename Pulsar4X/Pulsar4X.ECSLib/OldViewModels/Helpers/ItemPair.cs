using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace Pulsar4X.ECSLib
{
    public class ItemPair<T1, T2> : INotifyPropertyChanged
    {
        private T1 _item1;
        public T1 Item1
        {
            get { return _item1; }
            set { _item1 = value; OnPropertyChanged(); }
        }

        private T2 _item2;
        public T2 Item2
        {
            get { return _item2; }
            set { _item2 = value; OnPropertyChanged(); }
        }

        public ItemPair(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
