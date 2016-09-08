using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;


namespace Pulsar4X.CrossPlatformUI.Views
{
    public class MinMaxSlider : Panel
    {
        protected Slider Slider { get; set; }
        protected NumericUpDown NumericUpDown { get; set; }

        public MinMaxSlider()
        {
            XamlReader.Load(this);           
            NumericUpDown.DecimalPlaces = 4;
            DataContextChanged += MinMaxSlider_DataContextChanged;
        }

        private void MinMaxSlider_DataContextChanged(object sender, System.EventArgs e)
        {
            if (DataContext is MinMaxSliderVM)
            { }
        }
    }
}

