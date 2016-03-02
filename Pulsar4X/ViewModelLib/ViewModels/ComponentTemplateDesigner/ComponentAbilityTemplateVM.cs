using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Input;

namespace Pulsar4X.ViewModel
{
   public class ComponentAbilityTemplateVM : ComponentTemplateDesignerBaseVM
    {
        private StaticDataStore _staticData;

        private string _name;
        public string Name { get { return _name; } set { _name = value; OnPropertyChanged(); } }
        private string _description;
        public string Description
        {
            get { return _description; }
            set { _description = value; OnPropertyChanged(); }
        }

        private ObservableCollection<ComponentAbilityTemplateVM> ParentList { get; set; }
        public int Index
        {
            get { return ParentList.IndexOf(this); }
        }

        private DictionaryVM<GuiHint, string, string> _selectedGuiHint = new DictionaryVM<GuiHint, string, string>();
        public DictionaryVM<GuiHint, string, string> SelectedGuiHint
        {
            get { return _selectedGuiHint; }
            set { _selectedGuiHint = value; OnPropertyChanged(); }
        }

        private List<string> _abilityDataBlobTypeSelection = new List<string>();
        public List<string> AbilityDataBlobTypeSelection
        {
            get { return _abilityDataBlobTypeSelection; }
        }

        private string _abilityDataBlobType;
        public string AbilityDataBlobType { get { return _abilityDataBlobType; } set { _abilityDataBlobType = value; OnPropertyChanged(); } }

        private string _abilityFormula;
        public string AbilityFormula
        {
            get { return _abilityFormula; }
            set { _abilityFormula = value; OnPropertyChanged(); }
        }
        private string _minFormula;
        public string MinFormula
        {
            get { return _minFormula; }
            set { _minFormula = value; OnPropertyChanged(); }
        }
        private string _maxFormula;



        public string MaxFormula
        {
            get { return _maxFormula; }
            set { _maxFormula = value; OnPropertyChanged(); }
        }

        public override string FocusedText
        {

            get
            {
                switch (SubControlInFocus)
                {
                    case FocusedControl.NameControl:
                        return Name;
                    case FocusedControl.DescriptionControl:
                        return Description;
                    case FocusedControl.MinControl:
                        return MinFormula;
                    case FocusedControl.MaxControl:
                        return MaxFormula;
                    case FocusedControl.AbilityFormulaControl:
                        return AbilityFormula;
                    default:
                        return "";
                }
            }
            set
            {                
                switch (SubControlInFocus)
                {
                    case FocusedControl.NameControl:
                        Name = value;
                        break;
                    case FocusedControl.DescriptionControl:
                        Description = value;
                        break;
                    case FocusedControl.MinControl:
                        MinFormula = value;
                        break;
                    case FocusedControl.MaxControl:
                        MaxFormula = value;
                        break;
                    case FocusedControl.AbilityFormulaControl:
                        AbilityFormula = value;
                        break;
                }
                ParentVM.ControlInFocus = this;
            }
        }


        public TechListVM GuidDict { get; set; }


        
        public ICommand AddToEditCommand { get { return new RelayCommand<object>(obj => AddMe()); } }
        public ICommand DeleteCommand { get { return new RelayCommand<object>(obj => DeleteMe()); } }

        /// <summary>
        /// Constructor for empty
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="parentList"></param>
        /// <param name="staticData"></param>
        public ComponentAbilityTemplateVM(ComponentTemplateParentVM parent, ObservableCollection<ComponentAbilityTemplateVM> parentList, StaticDataStore staticData) : base(parent)
        {

            _staticData = staticData;
          
            //SelectedGuiHint = new DictionaryVM<GuiHint, string>(DisplayMode.Value);
            ParentList = parentList;
            foreach (var item in Enum.GetValues(typeof(GuiHint)))
            {
                SelectedGuiHint.Add((GuiHint)item, Enum.GetName(typeof(GuiHint), item));
            }
            SelectedGuiHint.SelectedIndex = 0;
            _abilityDataBlobTypeSelection = AbilityTypes();
        }

        /// <summary>
        /// Constructor for filled
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="abilitySD"></param>
        /// <param name="parentList"></param>
        /// <param name="staticData"></param>
        public ComponentAbilityTemplateVM(ComponentTemplateParentVM parent, ComponentAbilitySD abilitySD, ObservableCollection<ComponentAbilityTemplateVM> parentList, StaticDataStore staticData) : this(parent, parentList, staticData)
        {
            Name = abilitySD.Name;
            Description = abilitySD.Description;
            SelectedGuiHint.SelectedIndex = (int)abilitySD.GuiHint;
            if (abilitySD.AbilityDataBlobType != null)
            {
                AbilityDataBlobType = abilitySD.AbilityDataBlobType;
            }
            AbilityFormula = abilitySD.AbilityFormula;
            MinFormula = abilitySD.MinFormula;
            MaxFormula = abilitySD.MaxFormula;
            //GuidDictionary = abilitySD.GuidDictionary;
            DictionaryVM<Guid, string, string> techSelected = new DictionaryVM<Guid, string, string>();
            if (abilitySD.GuiHint == GuiHint.GuiTechSelectionList)
            {
                foreach (var item in abilitySD.GuidDictionary)
                {
                    techSelected.Add(item.Key, _staticData.Techs[item.Key].Name);
                }
                GuidDict = new TechListVM(techSelected, _staticData);
            }
        }


        private void DeleteMe()
        {
            ParentList.RemoveAt(Index);
        }
        private void AddMe()
        {
            ParentVM.FormulaEditor.AddParam("Ability(" + Index + ")");
            ParentVM.FormulaEditor.RefreshFormula();
        }
        private static List<string> AbilityTypes()
        {
            List<string> typelist = new List<string>();
            typelist.Add(typeof(ConstructInstationsAbilityDB).ToString());
            typelist.Add(typeof(ConstructShipComponentsAbilityDB).ToString());
            typelist.Add(typeof(ConstructAmmoAbilityDB).ToString());
            typelist.Add(typeof(ConstructFightersAbilityDB).ToString());
            typelist.Add(typeof(EnginePowerDB).ToString());
            typelist.Add(typeof(FuelStorageDB).ToString());
            typelist.Add(typeof(FuelUseDB).ToString());
            typelist.Add(typeof(MineResourcesDB).ToString());
            typelist.Add(typeof(MissileLauncherSizeDB).ToString());
            typelist.Add(typeof(RefineResourcesDB).ToString());
            typelist.Add(typeof(ResearchPointsAbilityDB).ToString());
            typelist.Add(typeof(SensorSignatureDB).ToString());
            typelist.Add(typeof(ActiveSensorDB).ToString());

            return typelist;
        }



        public ComponentAbilitySD CreateSD()
        {
            ComponentAbilitySD sd = new ComponentAbilitySD();
            sd.Name = Name;
            sd.Description = Description;
            sd.AbilityDataBlobType = AbilityDataBlobType;
            sd.GuiHint = SelectedGuiHint.GetKey();
            sd.AbilityFormula = AbilityFormula;
            sd.MinFormula = MinFormula;
            sd.MaxFormula = MaxFormula;
            Dictionary<Guid, string> guidict = new Dictionary<Guid, string>();
            foreach (var item in GuidDict.SelectedItems)
            {
                guidict.Add(item.Key, item.Value);
            }
            sd.GuidDictionary = guidict;
            return sd;                
        }
    }
}
