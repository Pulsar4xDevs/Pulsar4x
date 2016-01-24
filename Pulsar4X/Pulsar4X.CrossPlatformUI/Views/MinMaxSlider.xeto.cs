using System;
using System.Collections.Generic;
using System.IO;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class MinMaxSlider : Panel
    {
        protected Slider Slider { get; set; }
        protected NumericUpDown NumericUpDown { get; set; }

        private double MaxValue { get; set; }
        private double MinValue { get; set; }

        //private double _value;
        //public double Value
        //{
        //    get { return _value; }
        //    set { _value = value; Slider.Value = (int)value * 10000; }
        //}

        public double Value { get; set; }

        private string Name { get; set; }


        private float _tickFrequency;
        

        public MinMaxSlider()
        {
            XamlReader.Load(this);
            DataContext = this;
            //Slider.ValueChanged += Slider_ValueChanged;
            NumericUpDown.DecimalPlaces = 4;
            
            NumericUpDown.ValueBinding.BindDataContext((MinMaxSlider m) => m.Value);
            Slider.BindDataContext(c => c.Value * 0.0001f, (MinMaxSlider m) => m.Value * 10000);

        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="viewModelObject"></param>
        public MinMaxSlider(dynamic viewModelObject):this()
        {
            //DataContext = viewModelObject;
            Name = viewModelObject.Name;
            SetMax = viewModelObject.MaxValue;
            SetMin = viewModelObject.MinValue;
            Value = viewModelObject.Value;


        }

        public double SetMax { set { MaxValue = value; Slider.MaxValue = (int)value * 10000;
            NumericUpDown.MaxValue = value;
        } }
        public double SetMin { set { MinValue = value; Slider.MinValue = (int)value * 10000;
            NumericUpDown.MinValue = value;
        }  }



        public float TickFrequency 
        { 
            get{return _tickFrequency;} 
            set
            {
                _tickFrequency = value;
                Slider.TickFrequency = (int)value * 10000;
            } 
        }
        public bool SnapToTick { get { return Slider.SnapToTick; } set {Slider.SnapToTick = value; } }

        //private void Slider_ValueChanged(object sender, EventArgs e)
        //{
        //    CurrentValue = Slider.Value * 0.0001f;
        //    NumericUpDown.Value = CurrentValue;
        //}
    }
}

