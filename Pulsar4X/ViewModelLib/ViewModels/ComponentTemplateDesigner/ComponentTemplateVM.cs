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
    public enum FocusedControl
    {
        NameControl,
        DescriptionControl,
        SizeControl,
        HTKControl,
        CrewReqControl,
        MinCostControl,
        BPCostControl,
        ResearchCostControl,
        CreditCostControl,
        MinControl,
        MaxControl,
        AbilityFormulaControl
    }

    public abstract class ComponentTemplateDesignerBaseVM : INotifyPropertyChanged
    {
        
        private FocusedControl _subControlInFocus;
        public FocusedControl SubControlInFocus {
            get { return _subControlInFocus; }
            set { _subControlInFocus = value; FocusedText = FocusedText; } }

        //public virtual string FocusedText { get { return ""; } set { OnPropertyChanged(); } }
        public abstract string FocusedText { get; set; }
        public abstract event PropertyChangedEventHandler PropertyChanged;
        internal abstract void OnPropertyChanged([CallerMemberName] string propertyName = null);

    }

    public class ComponentTemplateVM : ComponentTemplateDesignerBaseVM
    {
        private StaticDataStore _staticData;

        public DictionaryVM<ComponentSD, string, string> Components { get; set; }

        public FormulaEditorVM FormulaEditor { get; set; }

        private ComponentTemplateDesignerBaseVM _controlInFocus;
        public ComponentTemplateDesignerBaseVM ControlInFocus
        {
            get { return _controlInFocus; }
            set
            {
                if (_controlInFocus != value)
                {
                    _controlInFocus = value;
                    if(FormulaEditor != null)
                        FormulaEditor.Formula = FormulaEditor.Formula; //force propertyUpdate
                }
            }
        }
        //public FocusedControl SubControlInFocus { get { return _controlInFocus; } set { _controlInFocus = value; OnPropertyChanged("FocusText"); }  }
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
                    case FocusedControl.SizeControl:
                        return SizeFormula;
                    case FocusedControl.CrewReqControl:
                        return CrewReqFormula;
                    case FocusedControl.HTKControl:
                        return HTKFormula;
                    case FocusedControl.BPCostControl:
                        return BuildPointCostFormula;
                    case FocusedControl.ResearchCostControl:
                        return ResearchCostFormula;
                    case FocusedControl.CreditCostControl:
                        return CreditCostFormula;
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
                    case FocusedControl.SizeControl:
                        SizeFormula = value;              
                        break;
                    case FocusedControl.CrewReqControl:
                        CrewReqFormula = value;             
                        break;
                    case FocusedControl.HTKControl:
                        HTKFormula = value;                 
                        break;
                    case FocusedControl.BPCostControl:
                        BuildPointCostFormula = value;            
                        break;
                    case FocusedControl.ResearchCostControl:
                        ResearchCostFormula = value;                    
                        break;
                    case FocusedControl.CreditCostControl:
                        CreditCostFormula = value;                        
                        break;
                }
                OnPropertyChanged();
            }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged(); }
        }
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

        private readonly ObservableCollection<MineralFormulaVM> _mineralCostFormula = new ObservableCollection<MineralFormulaVM>();
        public ObservableCollection<MineralFormulaVM> MineralCostFormula
        {
            get { return _mineralCostFormula; }
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

        private readonly RangeEnabledObservableCollection<ComponentAbilityTemplateVM> _componentAbilitySDs = new RangeEnabledObservableCollection<ComponentAbilityTemplateVM>();

        public override event PropertyChangedEventHandler PropertyChanged;

        public RangeEnabledObservableCollection<ComponentAbilityTemplateVM> ComponentAbilitySDs
        {
            get { return _componentAbilitySDs; }
        }



        public ComponentTemplateVM(GameVM gameData)
        {
            _staticData = gameData.Game.StaticData;
            SubControlInFocus = FocusedControl.SizeControl;
            ControlInFocus = this;
            FormulaEditor = new FormulaEditorVM(this);
            
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

            foreach (var item in Enum.GetValues(typeof(ComponentMountType)))
            {
                MountType.Add((ComponentMountType)item, false);
            }

            ComponentAbilitySDs.Clear();
            ComponentAbilitySDs.Add(new ComponentAbilityTemplateVM(this, ComponentAbilitySDs, _staticData));
        }

        public void SetDesignSD(ComponentSD designSD)
        {
            Name = designSD.Name;
            Description = designSD.Description;
            _ID = designSD.ID;

            SizeFormula = designSD.SizeFormula;
            HTKFormula = designSD.HTKFormula;
            CrewReqFormula = designSD.CrewReqFormula;
            MineralCostFormula.Clear(); 
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

            ComponentAbilitySDs.Clear();
            var tmp = new List<ComponentAbilityTemplateVM>();
            foreach (var item in designSD.ComponentAbilitySDs)
            {
                var vm = new ComponentAbilityTemplateVM(this, item, ComponentAbilitySDs, _staticData);
                tmp.Add(vm);
                
            }
            ComponentAbilitySDs.AddRange(tmp);
        }



        public void CreateSD()
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
                sd.MineralCostFormula.Add(item.Minerals.GetKey(), item.Formula);
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

            if (_staticData.Components.Keys.Contains(sd.ID))
            {
                _staticData.Components[sd.ID] = sd;
            }
            else
            {
                _staticData.Components.Add(sd.ID, sd);
            }
        }

        public void SaveToFile()
        {
            StaticDataManager.ExportStaticData(_staticData.Components, "./NewComponentData.json");
        }

        internal override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            ControlInFocus = this;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
