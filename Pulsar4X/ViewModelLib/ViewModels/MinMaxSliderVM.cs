using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ViewModel
{
    public class MinMaxSliderVM : IViewModel
    {

        public string Name { get; set; }
        public double MaxValue { get; set; }
        public double MinValue { get; set; }
        public double Value { get {return _value;} set { _value = value; OnPropertyChanged(); OnPropertyChanged("SliderValue"); } }
        private double _value;

        public int SliderMaxValue { get { return (int)MaxValue * 10000; } set { OnPropertyChanged(); }}
        public int SliderMinValue { get { return (int)MinValue * 10000; } set { OnPropertyChanged(); } }
        public int SliderValue { get { return (int)Value * 10000; } set { Value = value * 0.0001f; OnPropertyChanged(); } }





        //public float TickFrequency
        //{
        //    get { return _tickFrequency; }
        //    set
        //    {
        //        _tickFrequency = value;
        //        Slider.TickFrequency = (int)value * 10000;
        //    }
        //}
        //public bool SnapToTick { get { return Slider.SnapToTick; } set { Slider.SnapToTick = value; } }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void Refresh(bool partialRefresh = false)
        {
            throw new NotImplementedException();
        }
    }
}
