using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;


namespace Pulsar4X.CrossPlatformUI.Views
{
    public class MinMaxSlider : Panel
    {
        protected Slider Slider { get; set; }
        protected NumericUpDown NumericUpDown { get; set; }
        public event ValueChangedEventHandler ValueChanged;


        private float _tickFrequency;
        

        public MinMaxSlider()
        {
            XamlReader.Load(this);
            DataContext = this;
            
            NumericUpDown.DecimalPlaces = 4;
            
            //NumericUpDown.ValueBinding.BindDataContext((MinMaxSliderVM m) => m.Value);
            //NumericUpDown.BindDataContext(c => c.MaxValue, (MinMaxSliderVM m) => m.MaxValue);
            //NumericUpDown.BindDataContext(c => c.MinValue, (MinMaxSliderVM m) => m.MinValue);
            //Slider.BindDataContext(c => c.Value, (MinMaxSliderVM m) => m.SliderValue);
            //Slider.BindDataContext(c => c.MaxValue, (MinMaxSliderVM m) => m.SliderMaxValue);
            //Slider.BindDataContext(c => c.MinValue, (MinMaxSliderVM m) => m.SliderMinValue);
            //Slider.BindDataContext(c => c.TickFrequency, (MinMaxSliderVM m) => m.SliderStepValue);
            

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

