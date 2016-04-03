using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Pulsar4X.ViewModel
{
    public class ComponentTemplateParentVM : INotifyPropertyChanged
    {
        private StaticDataStore _staticData;
        private GameVM _gameVM;

        private readonly DictionaryVM<ComponentTemplateSD, string> _components = new DictionaryVM<ComponentTemplateSD, string>();
        public DictionaryVM<ComponentTemplateSD, string> Components { get { return _components; } }

        private ComponentTemplateMainPropertiesVM _selectedComponent;
        public ComponentTemplateMainPropertiesVM SelectedComponent
        {
            get { return _selectedComponent; }
            set { _selectedComponent = value; OnPropertyChanged(); }
        }

        private readonly RangeEnabledObservableCollection<ComponentAbilityTemplateVM> _componentAbilitySDs = new RangeEnabledObservableCollection<ComponentAbilityTemplateVM>();
        public RangeEnabledObservableCollection<ComponentAbilityTemplateVM> ComponentAbilitySDs
        {
            get { return _componentAbilitySDs; }
        }

        private FormulaEditorVM _formulaEditor;
        public FormulaEditorVM FormulaEditor
        {
            get { return _formulaEditor; }
            set { _formulaEditor = value; OnPropertyChanged(); }
        }


        private ComponentTemplateDesignerBaseVM _controlInFocus;

        public ComponentTemplateDesignerBaseVM ControlInFocus
        {
            get { return _controlInFocus; }
            set
            {
                if (_controlInFocus != value)
                {
                    _controlInFocus = value;
                    FormulaEditor.RefreshFormula();
                }
            }
        }

        public ICommand NewComponentCommand { get { return new RelayCommand<object>(obj => StartNewComponent()); } }
        public ICommand NewAbilityCommand { get { return new RelayCommand<object>(obj => AddNewAbility()); } }
        public ICommand SaveCommand { get { return new RelayCommand<object>(obj => SaveToStaticData()); } }
        public ICommand ExportCommand { get { return new RelayCommand<object>(obj => ExportToFile()); } }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="gameVM"></param>
        public ComponentTemplateParentVM(GameVM gameVM)
        {
            _staticData = gameVM.Game.StaticData;
            _gameVM = gameVM;
            FormulaEditor = new FormulaEditorVM(this, _staticData);
            
            foreach (var item in _staticData.Components.Values)
            {
                Components.Add(item, item.Name);
            }
            SelectedComponent = new ComponentTemplateMainPropertiesVM(this, _gameVM);
            Components.SelectionChangedEvent += Components_SelectionChangedEvent;
            Components.SelectedIndex = 0;
        }

        private void Components_SelectionChangedEvent(int oldSelection, int newSelection)
        {

            SelectedComponent.SetDesignSD(Components.GetKey());// = mainTemplateVM;
            ComponentAbilitySDs.Clear();
            var tmp = new List<ComponentAbilityTemplateVM>();
            foreach (var item in Components.GetKey().ComponentAbilitySDs)
            {
                var vm = new ComponentAbilityTemplateVM(this, item, ComponentAbilitySDs, _staticData);               
                tmp.Add(vm);
            }
            ComponentAbilitySDs.AddRange(tmp);
        }

        public void StartNewComponent()
        {

            SelectedComponent.ClearSelection();
            SelectedComponent.ID = Guid.NewGuid().ToString();
            SelectedComponent.Name = "NewComponent";
            ComponentAbilitySDs.Clear();
        }

        public void AddNewAbility()
        {
            ComponentAbilitySDs.Add(new ComponentAbilityTemplateVM(this, ComponentAbilitySDs, _staticData));
        }

        public void SaveToStaticData()
        {
            ComponentTemplateSD sd = new ComponentTemplateSD();
            sd.Name = SelectedComponent.Name;
            sd.Description = SelectedComponent.Description;
            sd.ID = Guid.Parse(SelectedComponent.ID);

            sd.SizeFormula = SelectedComponent.SizeFormula;
            sd.HTKFormula = SelectedComponent.HTKFormula;
            sd.CrewReqFormula = SelectedComponent.CrewReqFormula;
            sd.MineralCostFormula = new Dictionary<Guid, string>();
            foreach (var item in SelectedComponent.MineralCostFormula)
            {
                if(item.MineralFormula != null)
                    sd.MineralCostFormula.Add(item.Minerals.GetKey(), item.MineralFormula);
            }
            sd.ResearchCostFormula = SelectedComponent.ResearchCostFormula;
            sd.CreditCostFormula = SelectedComponent.CreditCostFormula;
            sd.BuildPointCostFormula = SelectedComponent.BuildPointCostFormula;
            sd.MountType = ComponentMountType.None;
            foreach (KeyValuePair<ComponentMountType, bool?> mountTypePair in SelectedComponent.MountType)
            {
                if (mountTypePair.Value != null && mountTypePair.Value == true)
                {
                    sd.MountType = sd.MountType | mountTypePair.Key;
                }
            }

            sd.ComponentAbilitySDs = new List<ComponentTemplateAbilitySD>();
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

        public void ExportToFile()
        {
            StaticDataManager.ExportStaticData(_staticData.Components, "NewComponentData.json");
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


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
        StepControl,
        AbilityFormulaControl
    }
}
