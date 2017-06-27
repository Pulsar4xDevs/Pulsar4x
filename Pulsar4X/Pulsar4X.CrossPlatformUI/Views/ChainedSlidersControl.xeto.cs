using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;
namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ChainedSlidersControl : Panel
    {
        protected StackLayout SlidersStack { get; set; }
        public ChainedSlidersControl()
        {
            XamlReader.Load(this);
        }

        public void SetViewModel(ChainedSliders chainedSliders)
        {
            DataContext = chainedSliders;

            foreach (var item in chainedSliders.SliderVMs)
            {
                MinMaxSlider mms = new MinMaxSlider(item);
                SlidersStack.Items.Add(mms);
            }
        }
    }
}
