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
            DataContext = this;
            
            NumericUpDown.DecimalPlaces = 4;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="viewModelObject"></param>
        public MinMaxSlider(MinMaxSliderVM minMaxSliderVM)
            : this()
        {
            DataContext = minMaxSliderVM;

        }
    }
}

