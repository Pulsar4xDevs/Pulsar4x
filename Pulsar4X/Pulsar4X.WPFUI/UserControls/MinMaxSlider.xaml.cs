using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Pulsar4X.WPFUI.UserControls
{
    public delegate void ValueChangedEventHandler(double value);
    /// <summary>
    /// Interaction logic for MaxMinSlider.xaml
    /// </summary>
    public partial class MinMaxSlider : UserControl
    {
        public double Maximum
        {
            get
            {
                return Slider.Maximum;
            }
            set
            {
                Slider.Maximum = value;
                Max.Content = value;
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
                Min.Content = value;
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
                    ValueChanged.Invoke(value);
                }
            }
        }

        public event ValueChangedEventHandler ValueChanged;
       

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
