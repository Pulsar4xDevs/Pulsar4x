using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ViewModel
{
    public class MissileDesignVM : ViewModelBase
    {
        
        public float MissileSize
        {
            get { return (float)ChainedSliders.MaxValue; }
            set { ChainedSliders.MaxValue = value; OnPropertyChanged(); }
        }

        public ChainedSliders ChainedSliders { get; private set; }
   

        public MissileDesignVM(GameVM gameVM)
        {
            List<ChainedSliderVM> chainedSlidersVM = new List<ChainedSliderVM>();
            chainedSlidersVM.Add(new ChainedSliderVM());
            chainedSlidersVM[0].Name = "Payload";
            chainedSlidersVM.Add(new ChainedSliderVM());
            chainedSlidersVM[1].Name = "Engine";
            chainedSlidersVM.Add(new ChainedSliderVM());
            chainedSlidersVM[2].Name = "Agility";
            chainedSlidersVM.Add(new ChainedSliderVM());
            chainedSlidersVM[3].Name = "Fuel";
            chainedSlidersVM.Add(new ChainedSliderVM());
            chainedSlidersVM[4].Name = "EW and Armor";


            ChainedSliders = new ChainedSliders(chainedSlidersVM);

        }
    }
}
