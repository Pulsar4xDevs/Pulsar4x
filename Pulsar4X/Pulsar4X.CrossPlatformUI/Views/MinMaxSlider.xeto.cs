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
        

        protected float MaxValue { get; set; }
        protected float MinValue { get; set; }
        
        protected float CurrentValue { get; set; }

        protected string Name { get; set; }


        private float _tickFrequency;
        

        public MinMaxSlider()
        {
            XamlReader.Load(this);
            DataContext = this;
            Slider.ValueChanged += Slider_ValueChanged;           
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="viewModelObject"></param>
        public MinMaxSlider(dynamic viewModelObject):this()
        {
            //Name = viewModelObject.Name;
            //MaxValue = viewModelObject.MaxValue;
            //MinValue = viewModelObject.MinValue;


        }

        public float SetMax { set { MaxValue = value; Slider.MaxValue = (int)value * 10000; } }
        public float SetMin { set { MinValue = value; Slider.MinValue = (int)value * 10000;} }
        public float Value { get { return CurrentValue; } set { CurrentValue = value; Slider.Value = (int)value * 10000; } }

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

        private void Slider_ValueChanged(object sender, EventArgs e)
        {
            CurrentValue = Slider.Value * 0.0001f; 
        }
    }
}

