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

        public double Value { get { return Slider.Value; } set { Slider.Value = value; } }

       

        public MinMaxSlider()
        {
            InitializeComponent();            
        }
    }
}
