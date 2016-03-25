using Pulsar4X.ECSLib;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Pulsar4X.ViewModel
{
    public class MinMaxSliderVM : IViewModel
    {

        public string Name { get; set; } = "MinMaxSlider";
        public double MaxValue { get; set; } = 100;
        public double MinValue { get; set; } = 0;
        public double StepValue { get; set; } = 0.0001;
        public double Value { get {return _value;} set { _value = value; OnPropertyChanged(); OnPropertyChanged("SliderValue"); } }
        private double _value;

        public int SliderMaxValue { get { return (int)(MaxValue * 10000); } set { OnPropertyChanged(); }}
        public int SliderMinValue { get { return (int)(MinValue * 10000); } set { OnPropertyChanged(); }}
        public int SliderStepValue { get { return (int)(StepValue * 10000); } set { OnPropertyChanged(); }}
        public int SliderValue { get { return (int)(Value * 10000); } set { Value = value * 0.0001f; OnPropertyChanged(); } }

        public bool StrictStepValue { get; set; } = true;


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
