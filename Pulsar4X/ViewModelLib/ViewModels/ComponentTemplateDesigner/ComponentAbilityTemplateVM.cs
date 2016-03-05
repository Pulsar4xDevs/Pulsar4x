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

        private string _toolTipText ="";
        public string ToolTipText
        {
            get { return _toolTipText; }
            set { _toolTipText = value; OnPropertyChanged(); }
        }

        private ObservableCollection<ComponentAbilityTemplateVM> ParentList { get; set; }
        public int Index
        {
            get { return ParentList.IndexOf(this); }
        }

        private DictionaryVM<GuiHint, string> _selectedGuiHint = new DictionaryVM<GuiHint, string>();
        public DictionaryVM<GuiHint, string> SelectedGuiHint
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
        public DictionaryVM<Type, string> ItemDictTypes { get; } = new DictionaryVM<Type, string>();
        public ObservableCollection<ItemDictVM<object>> ItemDict { get; } = new ObservableCollection<ItemDictVM<object>>();
        //public ItemDictVM<object> ItemDict { get; } = new ItemDictVM<object>();

        
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
            SelectedGuiHint.SelectionChangedEvent += SelectedGuiHint_SelectionChangedEvent;
            SelectedGuiHint.SelectedIndex = 0;
            _abilityDataBlobTypeSelection = AbilityTypes();

            foreach (var item in EnumTypes())
            {
                ItemDictTypes.Add(item, item.Name);
            }
            ItemDictTypes.SelectionChangedEvent += ItemDictTypes_SelectionChangedEvent;
            ItemDictTypes.SelectedIndex = 0;
            
        }

        private void ItemDictTypes_SelectionChangedEvent(int oldSelection, int newSelection)
        {
            ItemDict.Clear();
            ItemDictVM<object> itdvm = new ItemDictVM<object>(ParentVM, ItemDictTypes.SelectedKey);
            itdvm.PropertyChanged += Itdvm_PropertyChanged;
            ItemDict.Add(itdvm );

        }

        private void Itdvm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            foreach (var item in ItemDict.ToList())
            {
                if (string.IsNullOrWhiteSpace(item.Formula) && ItemDict.IndexOf(item) < ItemDict.Count -1)
                    ItemDict.Remove(item);
            }

            if (!string.IsNullOrWhiteSpace(ItemDict.Last().Formula))
            {
                ItemDictVM<object> itdvm = new ItemDictVM<object>(ParentVM, ItemDictTypes.SelectedKey);
                itdvm.PropertyChanged += Itdvm_PropertyChanged;
                ItemDict.Add(itdvm);
            }
        }

        private void SelectedGuiHint_SelectionChangedEvent(int oldSelection, int newSelection)
        {
            SetToolTipText();
        }

        /// <summary>
        /// Constructor for filled
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="abilitySD"></param>
        /// <param name="parentList"></param>
        /// <param name="staticData"></param>
        public ComponentAbilityTemplateVM(ComponentTemplateParentVM parent, ComponentTemplateAbilitySD abilitySD, ObservableCollection<ComponentAbilityTemplateVM> parentList, StaticDataStore staticData) : this(parent, parentList, staticData)
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
            DictionaryVM<Guid, string> techSelected = new DictionaryVM<Guid, string>();
            if (abilitySD.GuiHint == GuiHint.GuiTechSelectionList)
            {
                foreach (var item in abilitySD.GuidDictionary)
                {
                    techSelected.Add(item.Key, _staticData.Techs[item.Key].Name);
                }
                GuidDict = new TechListVM(techSelected, _staticData);
            }
        }

        private void SetToolTipText()
        {
            switch (SelectedGuiHint.GetKey())
            {
                case GuiHint.GuiSelectionMaxMin:
                    ToolTipText = _minMaxTTT;
                    break;
                case GuiHint.GuiTechSelectionList:
                    ToolTipText = _techSelectionTTT;
                    break;
                case GuiHint.GuiTextDisplay:
                    ToolTipText = _textDisplayTTT;
                    break;
                case GuiHint.None:
                    ToolTipText = _noneTTT;
                    break;

            }

        }

        private string _minMaxTTT = "In this mode the component designer will display a slider for the user to select a value between the min and max values.";
        private string _techSelectionTTT = "In this mode the component designer will display a combo box for the user to select from a list of tech, only tech researched will be shown.";
        private string _textDisplayTTT = "In this mode the component designer will display the AbilityFormula";
        private string _noneTTT = "In this mode the component designer will not display anything, used for; \r\nUse for hidden AbilityFormula calcs. \r\nUse in conjunction with Datablob type and args. \r\nUse with ItemDictionary and EnumDict(myEnum)";

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
            var typelist = new List<string>
            {
                typeof(ActiveSensorAbilityDB).ToString(),
                typeof(BeamFireControlAbilityDB).ToString(),
                typeof(BeamWeaponAbilityDB).ToString(),
                typeof(CargoStorageAbilityDB).ToString(),
                typeof(CloakAbilityDB).ToString(),
                typeof(CommandAbilityDB).ToString(),
                typeof(ConstructionAbilityDB).ToString(),
                typeof(DamageControlAbilityDB).ToString(),
                typeof(ElectronicDACAbilityDB).ToString(),
                typeof(EnginePowerAbilityDB).ToString(),
                typeof(ExplosionChanceAbilityDB).ToString(),
                typeof(FailureRateMitigationAbilityDB).ToString(),
                typeof(FuelConsumptionAbilityDB).ToString(),
                typeof(FuelStorageAbilityDB).ToString(),
                typeof(JumpDriveAbilityDB).ToString(),
                typeof(JumpPointStabilizationAbilityDB).ToString(),
                typeof(LifeSupportAbilityDB).ToString(),
                typeof(MineResourcesDB).ToString(),
                typeof(MissileLauncherAbilityDB).ToString(),
                typeof(MissileStorageAbilityDB).ToString(),
                typeof(MSPCapacityAbilityDB).ToString(),
                typeof(PassiveEMSensorAbilityDB).ToString(),
                typeof(PassiveThermalSensorAbilityDB).ToString(),
                typeof(PowerGeneratorAbilityDB).ToString(),
                typeof(RefineResourcesDB).ToString(),
                typeof(ResearchPointsAbilityDB).ToString(),
                typeof(SensorSignatureDB).ToString(),
                typeof(StandardShieldAbilityDB).ToString()
            };

            return typelist;
        }

        public enum None { };
        private static List<Type> EnumTypes()
        {
            var typelist = new List<Type>
            {
                typeof(None),
                typeof(ConstructionType)
            };

            return typelist;
        }



        public ComponentTemplateAbilitySD CreateSD()
        {
            ComponentTemplateAbilitySD sd = new ComponentTemplateAbilitySD();
            sd.Name = Name;
            sd.Description = Description;
            sd.AbilityDataBlobType = AbilityDataBlobType;
            sd.GuiHint = SelectedGuiHint.GetKey();
            sd.AbilityFormula = AbilityFormula;
            sd.MinFormula = MinFormula;
            sd.MaxFormula = MaxFormula;
            if (GuidDict != null)
            {
                Dictionary<Guid, string> guidict = new Dictionary<Guid, string>();
                foreach (var item in GuidDict.SelectedItems)
                {
                    guidict.Add(item.Key, item.Value);
                }
                sd.GuidDictionary = guidict;
            }
            return sd;                
        }
    }


    public class ItemDictVM<T1> : ComponentTemplateDesignerBaseVM
    {
        public override string FocusedText
        {
            get
            {
                switch (SubControlInFocus)
                {
                    case FocusedControl.AbilityFormulaControl:
                        return Formula;
                    default:
                        return "";
                }
            }

            set
            {
                switch (SubControlInFocus)
                {

                    case FocusedControl.AbilityFormulaControl:
                        Formula = value;
                        break;
                }
                ParentVM.ControlInFocus = this;
            }
        }

        private string _formula;
        public string Formula
        {
            get { return _formula; }
            set { _formula = value; OnPropertyChanged(); }
        }

        public DictionaryVM<T1, string> Items { get; } = new DictionaryVM<T1, string>();

        public ItemDictVM(ComponentTemplateParentVM parent, Type enumtype): base(parent)
        {
            foreach (var item in Enum.GetValues(enumtype))
            {
                Items.Add((T1)item, item.ToString());
            }
        }
    }
}
