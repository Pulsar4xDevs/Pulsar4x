using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ViewModel
{
    public class MissileDesignVM : ViewModelBase
    {
        public string DesignName
        {
            get { return _designName; }
            set { _designName = value; OnPropertyChanged(); }
        }
        private string _designName;
        
        public float MissileSize
        {
            get { return (float)ChainedSliders.MaxValue; }
            set { ChainedSliders.MaxValue = value; OnPropertyChanged(); }
        }

        public ChainedSliders ChainedSliders { get; private set; }


        #region Payload
        public ObservableCollection<object> PayloadTypes { get; } = new ObservableCollection<object>();
        public int PayloadCount { get { return _payloadCount; } set { _payloadCount = value; OnPropertyChanged(); }}
        private int _payloadCount;
        public int SeperationDistance { get { return _seperationDistance; } set { _seperationDistance = value; OnPropertyChanged(); }}
        private int _seperationDistance;
        #endregion


        #region Engine

        public ObservableCollection<object> EngineTypes { get; } = new ObservableCollection<object>();
        public int EngineCount { get { return _engineCount; } set { _engineCount = value; OnPropertyChanged(); } }
        private int _engineCount;

        #endregion

        #region EWandDefence

        public ObservableCollection<object> ArmorTypes { get; } = new ObservableCollection<object>();
        public float ArmorAmount { get; set; }

        public float ECMAmount { get; set; }

        public float ECCMAmount { get; set; }




        #endregion


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
