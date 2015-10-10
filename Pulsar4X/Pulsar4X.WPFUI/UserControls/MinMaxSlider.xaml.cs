using System.Windows;
using System.Windows.Controls;
using Pulsar4X.ECSLib; //not realy sure we should be referencing ecs lib in wpf...
using Pulsar4X.ViewModel;

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
        private ComponentAbilityDesignVM _designAbility;
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
                    _designAbility.OnValueChanged(GuiHint.GuiSelectionMaxMin, value);
                    ValueChanged.Invoke(GuiHint.GuiSelectionMaxMin, value);
                }
            }
        }

        public void GuiSliderSetup(ComponentAbilityDesignVM designAbility)
        {
            _designAbility = designAbility;
            NameLabel.Content = designAbility.Name;
            NameLabel.ToolTip = designAbility.Description;
            Maximum = designAbility.MaxValue;            
            Minimum = designAbility.MinValue;
            Value = designAbility.Value;
            designAbility.ValueChanged += ValueChanged;
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
