using System.Windows;
using System.Windows.Controls;

namespace Pulsar4X.WPFUI.UserControls
{
    public delegate void ValueChangedEventHandler(object sender, double value);
    /// <summary>
    /// Interaction logic for MaxMinSlider.xaml
    /// </summary>
    // ReSharper disable once RedundantExtendsListEntry
    public partial class MinMaxSlider : UserControl
    {
        
        public event ValueChangedEventHandler ValueChanged; 
        
        public double Maximum
        {
            get
            {
                return Slider.Maximum;
            }
            set
            {
                Slider.Maximum = value;
                MaxLabel.Content = value;
            }
        }

        public double Minimum
        {
            get
            {
                return Slider.Minimum;
            }
            set
            {
                Slider.Minimum = value;
                MinLabel.Content = value;
            }
        }

        public double Value
        {
            get
            {
                return Slider.Value;
            }
            set
            {
                Slider.Value = value;
                if (ValueChanged != null)
                {
                    ValueChanged.Invoke(this, value);
                }
            }
        }

        
       

        public MinMaxSlider()
        {
            InitializeComponent();            
        }

        private void NumericEntry_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if(NumericEntry.Value != null)
                Value = (double)NumericEntry.Value;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            NumericEntry.Value = Value;
        }
    }
}
