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


    public abstract class ComponentTemplateDesignerBaseVM : INotifyPropertyChanged
    {
        
        private FocusedControl _subControlInFocus;
        public FocusedControl SubControlInFocus {
            get { return _subControlInFocus; }
            set { _subControlInFocus = value; FocusedText = FocusedText; } }

        protected ComponentTemplateParentVM ParentVM { get; private set; }

        public ComponentTemplateDesignerBaseVM(ComponentTemplateParentVM parent)
        {
            ParentVM = parent;
        }

        public abstract string FocusedText { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            ParentVM.FormulaEditor.RefreshFormula();
        }
    }

    public class ComponentTemplateMainPropertiesVM : ComponentTemplateDesignerBaseVM
    {


        private StaticDataStore _staticData;



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
                    case FocusedControl.MassControl:
                        return MassFormula;
                    case FocusedControl.VolumeControl:
                        return VolumeFormula;
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
                    case FocusedControl.MassControl:
                        MassFormula = value;              
                        break;
                    case FocusedControl.VolumeControl:
                        VolumeFormula = value;
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

                ParentVM.ControlInFocus = this;
                


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

        private string _massFormula;
        public string MassFormula
        {
            get { return _massFormula; }
            set { _massFormula = value; OnPropertyChanged(); }
        }

        private string _volumeFormula;
        public string VolumeFormula
        {
            get { return _volumeFormula; }
            set { _volumeFormula = value; OnPropertyChanged(); }
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
        private readonly ObservableDictionary<ComponentMountType, bool?> _mountType = new ObservableDictionary<ComponentMountType, bool?>();
        public ObservableDictionary<ComponentMountType, bool?> MountType { get { return _mountType; } }


        /// <summary>
        /// Constructor for empty VM
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="gameVM"></param>
        public ComponentTemplateMainPropertiesVM(ComponentTemplateParentVM parent, GameVM gameVM): base(parent)
        {
            _staticData = gameVM.Game.StaticData;
            SubControlInFocus = FocusedControl.MassControl;
            foreach (var item in Enum.GetValues(typeof(ComponentMountType)))
            {
                if ((ComponentMountType)item != ComponentMountType.None)
                {
                    MountType.Add((ComponentMountType)item, false);
                }
            }
        }

        /// <summary>
        /// Constructor for VM filled with componentSD
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="gameData"></param>
        /// <param name="designSD"></param>
        public ComponentTemplateMainPropertiesVM(ComponentTemplateParentVM parent, GameVM gameData, ComponentTemplateSD designSD) : this(parent, gameData)
        {
            SetDesignSD(designSD);
        }

        private void ComponentTemplateVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (MineralCostFormula.Last().Minerals.SelectedIndex >= 0 && MineralCostFormula[0].Minerals.SelectedIndex >= 0)
            {
                MineralCostFormula.Add(new MineralFormulaVM(ParentVM, _staticData));
                MineralCostFormula.Last().PropertyChanged += ComponentTemplateVM_PropertyChanged;
            }
        }





        public void ClearSelection()
        {
            Name = "";
            Description = "";
            _ID = Guid.NewGuid();
            MassFormula = "";
            VolumeFormula = "";
            HTKFormula = "";
            CrewReqFormula = "";
            MineralCostFormula.Clear();
            MineralCostFormula.Add(new MineralFormulaVM(ParentVM, _staticData));
            MineralCostFormula.Last().PropertyChanged += ComponentTemplateVM_PropertyChanged;
            ResearchCostFormula = "";
            CreditCostFormula = "";
            BuildPointCostFormula = "";
            //MountType.Clear();// = new ObservableDictionary<ComponentMountType, bool?>();


            foreach (var item in MountType.ToArray())
            {
                MountType[item.Key] = false;
            }
        }

        public void SetDesignSD(ComponentTemplateSD designSD)
        {
            Name = designSD.Name;
            Description = designSD.Description;
            _ID = designSD.ID;

            MassFormula = designSD.MassFormula;
            VolumeFormula = designSD.VolumeFormula;
            HTKFormula = designSD.HTKFormula;
            CrewReqFormula = designSD.CrewReqFormula;
            MineralCostFormula.Clear(); 
            foreach (var item in designSD.MineralCostFormula)
            {
                MineralCostFormula.Add(new MineralFormulaVM(ParentVM, _staticData, item));
            }
            MineralCostFormula.Add(new MineralFormulaVM(ParentVM, _staticData));
            
            ResearchCostFormula = designSD.ResearchCostFormula;
            CreditCostFormula = designSD.CreditCostFormula;
            BuildPointCostFormula = designSD.BuildPointCostFormula;

            foreach (object value in Enum.GetValues(typeof(ComponentMountType)))
            {
                var currentValue = (ComponentMountType)value;
                if ((currentValue & designSD.MountType) != 0)
                {
                    MountType[currentValue] = true;
                }
            }
        }


    }
}
