using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;


namespace Pulsar4X.CrossPlatformUI.Views
{
    public class MinMaxSlider : Panel
    {
        protected Slider Slider { get; set; }
        protected NumericUpDown NumericUpDown { get; set; }

        private MinMaxSliderVM _minMaxSLiderVM;

        public MinMaxSlider()
        {
            XamlReader.Load(this);           
            NumericUpDown.DecimalPlaces = 4;
            DataContextChanged += MinMaxSlider_DataContextChanged;
        }

        public MinMaxSlider (MinMaxSliderVM viewModel) : this()
        {
            _minMaxSLiderVM = viewModel;
            DataContext = viewModel;
        }

        private void MinMaxSlider_DataContextChanged(object sender, System.EventArgs e)
        {
            if (DataContext is MinMaxSliderVM)
            {
                MinMaxSliderVM vm = (MinMaxSliderVM)DataContext;
                vm.OnPropertyChanged(nameof(vm.Value)); //attempt to trigger propertychanged
            }
        }
    }
}

