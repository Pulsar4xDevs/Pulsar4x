using System.Windows;
using System.Windows.Controls;
using Pulsar4X.ECSLib; //not realy sure we should be referencing ecs lib in wpf...
using Pulsar4X.WPFUI.ViewModels;

namespace Pulsar4X.WPFUI.UserControls
{
    //public delegate void ValueChangedEventHandler(object sender, double value);
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
                    ValueChanged.Invoke(GuiHint.GuiSelectionMaxMin, value);
                }
            }
        }

        public void GuiSliderSetup(ComponentAbilityDesignVM designAbility)
        {
            MinMaxSlider guiSliderControl = new MinMaxSlider
            {
                NameLabel = { Content = designAbility.Name },
                ToolTip = designAbility.Description
            };

            guiSliderControl.Maximum = designAbility.MaxValue;            
            guiSliderControl.Minimum = designAbility.MinValue;
            guiSliderControl.Value = designAbility.Value;

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
