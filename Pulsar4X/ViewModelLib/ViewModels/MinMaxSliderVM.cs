using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Pulsar4X.ViewModel
{
    public class MinMaxSliderVM : INotifyPropertyChanged
    {

        public string Name { get; set; } = "MinMaxSlider";

        /// <summary>
        /// Max value of slider;
        /// </summary>
        public double MaxValue
        {
            get { return _maxValue; }
            set { _maxValue = value; OnPropertyChanged(nameof(SliderMaxValue)); OnPropertyChanged(); }
        }
        private double _maxValue = 1;

        /// <summary>
        /// Min value of slider
        /// </summary>
        public double MinValue
        {
            get { return _minValue; }
            set { _minValue = value; OnPropertyChanged(nameof(SliderMinValue)); OnPropertyChanged(); }
        }
        private double _minValue = 0;

        public double StepValue { get; set; } = 0.0001;
        public double Value { get {return _value_;}
            set { _value_ = value; OnPropertyChanged(nameof(SliderValue)); OnValueChanged(); OnPropertyChanged(); } }
        protected double _value_ = 50;
        
        public int SliderMaxValue { get { return (int)(MaxValue * 10000); }}
        public int SliderMinValue { get { return (int)(MinValue * 10000); }}
        public int SliderStepValue { get { return (int)(StepValue * 10000);}}
        public int SliderValue { get { return (int)(Value * 10000); }
            set { Value = value * 0.0001f; }}

        /// <summary>
        /// Gets / Sets whether the step/tick is enabled TODO use a 'bool?' and have off, slider, everything? 
        /// </summary>
        public bool StrictStepValue { get; set; } = true;

        /// <summary>
        /// whether the control can be locked by the user.
        /// </summary>
        public bool IsLockable { get; set; } = false;

        private bool _isLocked = false;
        /// <summary>
        /// used with IsLockable. locks the control so it can't be changed. 
        /// </summary>
        public bool IsLocked
        {
            get { return _isLocked; }
            set { _isLocked = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsUnLocked));}
        }
        public bool IsUnLocked
        {
            get { return !_isLocked; }
        }

        //#region eto.forms bug workaround, remove once https://github.com/picoe/Eto/issues/563 and another bug I've not created an issue for yet  has been resolved
        //public double PreLoadedValue { get; set; } = 50;
        //public void SetValueFromPreLoadedValue()
        //{
        //    Value = PreLoadedValue;
        //}
        //#endregion

        private double _oldValue;
        public event ValuePropertyChangedEventHandler ValueChanged;
        public void OnValueChanged()
        {            
            if (_oldValue != Value)
            {
                double delta = _oldValue - Value;
                _oldValue = Value;
                ValueChanged?.Invoke(this, delta);            
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public delegate void ValuePropertyChangedEventHandler(MinMaxSliderVM sender, double delta);
    public delegate void LockPropertyChangedEventHandler();
    public class ChainedSliderVM : MinMaxSliderVM
    {
        public double SoftUpperValue { get; set; }

        
        public event LockPropertyChangedEventHandler LockChanged;

        public ChainedSliderVM()
        {
            IsLockable = true;
            
        }

        /// <summary>
        /// adds delta to the slider value.
        /// </summary>
        /// <param name="delta"></param>
        public void Add(double delta)
        {

            double maxdelta = SoftUpperValue - Value;
            double mindelta = 0 - Value;

            if (delta > maxdelta)
            {
                delta = maxdelta;
            }
            else if (delta < mindelta)
            {
                delta = mindelta;
            }
            
            _value_ += delta;           
        }
    }

    public class ChainedSliders : INotifyPropertyChanged
    {
        private double _maxValue = 5;
        public double MaxValue { get { return _maxValue; } set
            {
                _maxValue = value;
                foreach (var slider in SliderVMs)
                {
                    slider.MaxValue = value;
                }
                OnPropertyChanged(); }}

        public List<ChainedSliderVM> SliderVMs { get; } = new List<ChainedSliderVM>();
        
        public ChainedSliders(List<ChainedSliderVM> listOfSliders)
        {
            SliderVMs.AddRange(listOfSliders);
            foreach (var slider in SliderVMs)
            {
                slider.MaxValue = MaxValue;
                slider.ValueChanged += Slider_ValueChanged;
                slider.LockChanged += OnAnySliderLockChanged;
            }            
            OnAnySliderLockChanged();
            Slider_ValueChanged(null, MaxValue);
        }

        private void Slider_ValueChanged(MinMaxSliderVM sender, double delta)
        {
            ChainedSliderVM thisSlider = (ChainedSliderVM)sender;

            List<ChainedSliderVM> sliderstochange = UnlockedSliders.Where(T => T != thisSlider).ToList();
            double eachperslider = delta / sliderstochange.Count;
            foreach (var slider in sliderstochange)
            {
                slider.Add(eachperslider);
            }
            if (thisSlider != null)
                thisSlider.Add(MaxValue - TotalValues);

            foreach (var item in UnlockedSliders)
            {
                item.OnPropertyChanged(nameof(ChainedSliderVM.Value));
                item.OnPropertyChanged(nameof(ChainedSliderVM.SliderValue));
            }
        }

        public void OnAnySliderLockChanged()
        {
            double upperlimit = (MaxValue - LockedValues);
            foreach (var unlockedslider in UnlockedSliders)
            {
                unlockedslider.SoftUpperValue = upperlimit;
                unlockedslider.StepValue = 0.0001f * (UnlockedSliders.Count - 1);
            }
        }

        private double TotalValues
        {
            get
            {
                double value = 0;
                foreach (var slider in SliderVMs)
                {
                    value += slider.Value;
                }
                return value;
              
            }
        }

        private double LockedValues 
        {
            get
            {
                double value = 0;
                foreach (var slider in LockedSliders)
                {
                    value += slider.Value;
                }
                return value;
            }
        }

        private List<ChainedSliderVM> UnlockedSliders { get { return SliderVMs.Where(T => !T.IsLocked).ToList(); } } 

        private List<ChainedSliderVM> LockedSliders { get { return SliderVMs.Where(T => T.IsLocked).ToList(); } }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
