using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;

namespace Pulsar4X.ViewModel
{
    public class ComponentTemplateVM : IViewModel
    {
        private StaticDataStore _staticData;

        public string Name { get; set; }
        public string Description { get; set; }
        private Guid _ID;
        public string ID { get { return _ID.ToString(); } }

        public string SizeFormula { get; set; }
        public string HTKFormula { get; set; }
        public string CrewReqFormula { get; set; }
        public ObservableCollection<MineralFormulaVM> MineralCostFormula { get; set; }
        public string ResearchCostFormula { get; set; }
        public string CreditCostFormula { get; set; }
        public string BuildPointCostFormula { get; set; }
        //if it can be fitted to a ship as a ship component, on a planet as an installation, can be cargo etc.
        public Dictionary<ComponentMountType, bool> MountType { get; set; }

        public ObservableCollection<ComponentAbilityTemplateVM> ComponentAbilitySDs { get; set; }



        public ComponentTemplateVM(GameVM gameData)
        {
            _staticData = gameData.Game.StaticData;
            Name = "";
            Description = "";
            //ID = "";
            SizeFormula = "";
            HTKFormula = "";
            CrewReqFormula = "";
            MineralCostFormula = new ObservableCollection<MineralFormulaVM>();
            MineralCostFormula.Add(new MineralFormulaVM(_staticData));
            MineralCostFormula.Last().PropertyChanged += ComponentTemplateVM_PropertyChanged;
            ResearchCostFormula = "";
            CreditCostFormula = "";
            BuildPointCostFormula = "";
            MountType = new Dictionary<ComponentMountType, bool>();
            ComponentAbilitySDs = new ObservableCollection<ComponentAbilityTemplateVM>();

        }

        private void ComponentTemplateVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //if (MineralCostFormula.Last().selectedMineralKVP.Key != null)
            //{
            //    MineralCostFormula.Add(new MineralFormulaVM(_staticData));
            //}
        }

        public ComponentTemplateVM(GameVM gameData, ComponentSD designSD) : this(gameData)
        { SetDesignSD(designSD); }

        public void SetDesignSD(ComponentSD designSD)
        {
            Name = designSD.Name;
            Description = designSD.Description;
            //ID = designSD.ID;

            SizeFormula = designSD.SizeFormula;
            HTKFormula = designSD.HTKFormula;
            CrewReqFormula = designSD.CrewReqFormula;
            MineralCostFormula = new ObservableCollection<MineralFormulaVM>();//clear the list
            foreach (var item in designSD.MineralCostFormula)
            {
                MineralCostFormula.Add(new MineralFormulaVM(_staticData, item));
            }
            MineralCostFormula.Add(new MineralFormulaVM(_staticData));
            
            ResearchCostFormula = designSD.ResearchCostFormula;
            CreditCostFormula = designSD.CreditCostFormula;
            BuildPointCostFormula = designSD.BuildPointCostFormula;
            MountType = designSD.MountType;
            ComponentAbilitySDs = new ObservableCollection<ComponentAbilityTemplateVM>();
            foreach (var item in designSD.ComponentAbilitySDs)
            {
                ComponentAbilitySDs.Add(new ComponentAbilityTemplateVM());
            }
        }

        public void Create(GameVM game)
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void Refresh(bool partialRefresh = false)
        {
            throw new NotImplementedException();
        }
    }
}
