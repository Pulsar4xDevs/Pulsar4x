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

        public DictionaryVM<ComponentSD, string, string> Components { get; set; }
        

        public string Name { get; set; }
        private string _description;
        public string Description
        {
            get { return _description; }
            set { _description = value; OnPropertyChanged(); }
        }
        private Guid _ID;
        public string ID
        {
            get { return _ID.ToString(); }
            set { _ID = Guid.Parse(value); OnPropertyChanged(); }
        }

        private string _sizeFormula;
        public string SizeFormula
        {
            get { return _sizeFormula; }
            set { _sizeFormula = value; OnPropertyChanged(); }
        }
        private string _hTKFormula;
        public string HTKFormula
        {
            get { return _hTKFormula; }
            set { _hTKFormula = value; OnPropertyChanged(); }
        }
        private string _crewReqFormula;
        public string CrewReqFormula
        {
            get { return _crewReqFormula; }
            set { _crewReqFormula = value; OnPropertyChanged(); }
        }

        private ObservableCollection<MineralFormulaVM> _mineralCostFormula = new ObservableCollection<MineralFormulaVM>();
        public ObservableCollection<MineralFormulaVM> MineralCostFormula
        {
            get { return _mineralCostFormula; }
            set { _mineralCostFormula = value; OnPropertyChanged(); }
        }
        private string _researchCostFormula;
        public string ResearchCostFormula
        {
            get { return _researchCostFormula; }
            set { _researchCostFormula = value; OnPropertyChanged(); }
        }

        private string _creditCostFormula;
        public string CreditCostFormula
        {
            get { return _creditCostFormula; }
            set { _creditCostFormula = value; OnPropertyChanged(); }
        }

        private string _buildPointCostFormula;
        public string BuildPointCostFormula
        {
            get { return _buildPointCostFormula; }
            set { _buildPointCostFormula = value; OnPropertyChanged(); }
        }
        //if it can be fitted to a ship as a ship component, on a planet as an installation, can be cargo etc.

        public ObservableDictionary<ComponentMountType, bool?> MountType { get; set; }

        //private DictionaryVM<ComponentMountType, bool?, bool?> _mountType = new DictionaryVM<ComponentMountType, bool?, bool?>();
        //public DictionaryVM<ComponentMountType, bool?, bool?> MountType { get; set; }

        //private ObservableCollection<ItemPair<ComponentMountType, bool?>> _mountType;
        //public ObservableCollection<ItemPair<ComponentMountType, bool?>> MountType
        //{
        //    get { return _mountType; }
        //    set { _mountType = value; OnPropertyChanged(); }
        //}
        //public ObservableCollection<ItemPair<ComponentMountType, bool?>> MountType { get; set; }

        private ObservableCollection<ComponentAbilityTemplateVM> _componentAbilitySDs = new ObservableCollection<ComponentAbilityTemplateVM>();
        public ObservableCollection<ComponentAbilityTemplateVM> ComponentAbilitySDs
        {
            get { return _componentAbilitySDs; }
            set { _componentAbilitySDs = value; OnPropertyChanged(); }
        }



        public ComponentTemplateVM(GameVM gameData)
        {
            _staticData = gameData.Game.StaticData;
            Components = new DictionaryVM<ComponentSD, string, string>();
            foreach (var item in _staticData.Components.Values)
            {
                Components.Add(item, item.Name);
            }
            Components.SelectionChangedEvent += Components_SelectionChangedEvent;

            ClearSelection();
        }

        private void Components_SelectionChangedEvent(int oldSelection, int newSelection)
        {
            //todo if current selection has changed in any way, warn.
            SetDesignSD(Components.GetKey());
        }

        private void ComponentTemplateVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (MineralCostFormula.Last().Minerals.SelectedIndex >= 0 && MineralCostFormula[0].Minerals.SelectedIndex >= 0)
            {
                MineralCostFormula.Add(new MineralFormulaVM(_staticData));
                MineralCostFormula.Last().PropertyChanged += ComponentTemplateVM_PropertyChanged;
            }
        }

        public ComponentTemplateVM(GameVM gameData, ComponentSD designSD) : this(gameData)
        { SetDesignSD(designSD); }

        public void ClearSelection()
        {
            Name = "";
            Description = "";
            _ID = Guid.NewGuid();
            SizeFormula = "";
            HTKFormula = "";
            CrewReqFormula = "";
            MineralCostFormula.Clear();
            MineralCostFormula.Add(new MineralFormulaVM(_staticData));
            MineralCostFormula.Last().PropertyChanged += ComponentTemplateVM_PropertyChanged;
            ResearchCostFormula = "";
            CreditCostFormula = "";
            BuildPointCostFormula = "";
            MountType = new ObservableDictionary<ComponentMountType, bool?>();
            //MountType = new DictionaryVM<ComponentMountType, bool?, bool?>();
            foreach (var item in Enum.GetValues(typeof(ComponentMountType)))
            {
                MountType.Add((ComponentMountType)item, false);
            }
            //MountType = new ObservableCollection<ItemPair<ComponentMountType, bool?>>();
            //foreach (var item in Enum.GetValues(typeof(ComponentMountType)))
            //{
            //    MountType.Add(new ItemPair<ComponentMountType, bool?>((ComponentMountType)item, false));
            //}
            ComponentAbilitySDs.Clear();
            ComponentAbilitySDs.Add(new ComponentAbilityTemplateVM());
        }

        public void SetDesignSD(ComponentSD designSD)
        {
            Name = designSD.Name;
            Description = designSD.Description;
            _ID = designSD.ID;

            SizeFormula = designSD.SizeFormula;
            HTKFormula = designSD.HTKFormula;
            CrewReqFormula = designSD.CrewReqFormula;
            MineralCostFormula.Clear(); // = new ObservableCollection<MineralFormulaVM>();//clear the list
            foreach (var item in designSD.MineralCostFormula)
            {
                MineralCostFormula.Add(new MineralFormulaVM(_staticData, item));
            }
            MineralCostFormula.Add(new MineralFormulaVM(_staticData));
            
            ResearchCostFormula = designSD.ResearchCostFormula;
            CreditCostFormula = designSD.CreditCostFormula;
            BuildPointCostFormula = designSD.BuildPointCostFormula;

            foreach (var item in designSD.MountType)
            {

                MountType[item.Key] = item.Value;
            }
            //for (int i = 0; i < MountType.Count; i++)
            //{
            //    MountType[i].Item2 = designSD.MountType.ElementAt(i).Value;//not sure this will work, didn't think dictionarys were ordered?
            //}
            //MountType.Clear();
            //foreach (var item in designSD.MountType)
            //{
            //    ItemPair<ComponentMountType, bool?> ipr = new ItemPair<ComponentMountType, bool?>(item.Key, item.Value);
            //    MountType.Add(ipr);
            //}

            ComponentAbilitySDs.Clear();
            foreach (var item in designSD.ComponentAbilitySDs)
            {
                ComponentAbilitySDs.Add(new ComponentAbilityTemplateVM(item));
            }
        }

        public void CreateSD(GameVM game)
        {
            ComponentSD sd = new ComponentSD();
            sd.Name = Name;
            sd.Description = Description;
            sd.ID = _ID;

            sd.SizeFormula = SizeFormula;
            sd.HTKFormula = HTKFormula;
            sd.CrewReqFormula = CrewReqFormula;
            sd.MineralCostFormula = new Dictionary<Guid, string>();
            foreach (var item in MineralCostFormula)
            {
                sd.MineralCostFormula.Add(item.Minerals.GetKey(), item.Forumula);
            }
            sd.ResearchCostFormula = ResearchCostFormula;
            sd.CreditCostFormula = CreditCostFormula;
            sd.BuildPointCostFormula = BuildPointCostFormula;
            sd.MountType = new Dictionary<ComponentMountType, bool>();
            sd.ComponentAbilitySDs = new List<ComponentAbilitySD>();
            foreach (var item in ComponentAbilitySDs)
            {
                sd.ComponentAbilitySDs.Add(item.CreateSD());
            }

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
