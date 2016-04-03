using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;

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
        public DictionaryVM<Payload, string> PayloadTypes { get; } = new DictionaryVM<Payload, string>();
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


            //TODO: how are we going to define the warheads? can they just be another design entity with just an explosive datablob or something? below is just a placeholder. 
            Payload explosive = new Payload();
            explosive.Name = "Explosive Charge";
            explosive.Damage = 1;
            PayloadTypes.Add(explosive, explosive.Name);


            foreach (var design in gameVM.CurrentFaction.GetDataBlob<FactionInfoDB>().MissileDesigns)
            {
                Payload payload = new Payload(design.Value);
                PayloadTypes.Add(payload, payload.Name);
            } 

        }
    }

    public class Payload
    {
        public string Name { get; set; }       
        public float Damage { get; set; }
        public float PayloadAmount { get; set; }
              
        public Entity PayloadEntity { get; set; }

        public float Range { get; set; }
        public float Speed { get; set; }

        public Payload SubMunition { get; set; }

        public Payload()
        { }

        public Payload(Entity missileEntity)
        {
            Name = missileEntity.GetDataBlob<NameDB>().DefaultName;
            Speed = missileEntity.GetDataBlob<PropulsionDB>().MaximumSpeed;

        }


    }

}
