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
    }

    public class ComponentTemplateVM : IViewModel
    {
        private StaticDataStore _staticData;

        public DictionaryVM<ComponentSD, string, string> Components { get; set; }

        public FormulaEditorVM FormulaEditor { get; set; }
        private FocusedControl _controlInFocus;
        public FocusedControl ControlInFocus { get { return _controlInFocus; } set { _controlInFocus = value; OnPropertyChanged("FocusText"); }  }
        public string FocusedText
        {
            get
            {
                switch (ControlInFocus)
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
                    default:
                        return "";
                }
            }
            set
            {
                switch (ControlInFocus)
                {
                    case FocusedControl.NameControl:
                        Name = value;
                        OnPropertyChanged();
                        break;
                    case FocusedControl.DescriptionControl:
                        Description = value;
                        OnPropertyChanged();
                        break;
                    case FocusedControl.SizeControl:
                        SizeFormula = value;
                        OnPropertyChanged();
                        break;
                    case FocusedControl.CrewReqControl:
                        CrewReqFormula = value;
                        OnPropertyChanged();
                        break;
                    case FocusedControl.HTKControl:
                        HTKFormula = value;
                        OnPropertyChanged();
                        break;
                    case FocusedControl.BPCostControl:
                        BuildPointCostFormula = value;
                        OnPropertyChanged();
                        break;
                    case FocusedControl.ResearchCostControl:
                        ResearchCostFormula = value;
                        OnPropertyChanged();
                        break;
                }
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
        public RangeEnabledObservableCollection<ComponentAbilityTemplateVM> ComponentAbilitySDs
        {
            get { return _componentAbilitySDs; }
        }



        public ComponentTemplateVM(GameVM gameData)
        {
            _staticData = gameData.Game.StaticData;
            ControlInFocus = FocusedControl.SizeControl;
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
            ComponentAbilitySDs.Add(new ComponentAbilityTemplateVM(ComponentAbilitySDs, _staticData));
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
                tmp.Add(new ComponentAbilityTemplateVM(item, ComponentAbilitySDs, _staticData));
                
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
