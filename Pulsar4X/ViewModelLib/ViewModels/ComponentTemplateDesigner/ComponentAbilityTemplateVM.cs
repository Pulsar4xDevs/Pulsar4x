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
   public class ComponentAbilityTemplateVM : IViewModel
    {
        private string _name;
        public string Name { get { return _name; } set { _name = value; OnPropertyChanged(); } }
        private string _description;
        public string Description
        {
            get { return _description; }
            set { _description = value; OnPropertyChanged(); }
        }

        private ObservableCollection<ComponentAbilityTemplateVM> ParentList { get; set; }
        private int _index;
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

        private DictionaryVM<Type, string, string> _abilityDataBlobTypeSelection = new DictionaryVM<Type, string, string>();
        public DictionaryVM<Type,string, string> AbilityDataBlobTypeSelection
        {
            get { return _abilityDataBlobTypeSelection; }
            set { _abilityDataBlobTypeSelection = value; OnPropertyChanged(); }
        }

        private string _abilityDataBlobType;
        public string AbilityDataBlobType
        {
            get { return _abilityDataBlobType; }
            set { _abilityDataBlobType = value; OnPropertyChanged(); }
        }
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
        //private Dictionary
        public Dictionary<Guid, string> GuidDictionary;


        public ComponentAbilityTemplateVM(ObservableCollection<ComponentAbilityTemplateVM> parentList)
        {
            //SelectedGuiHint = new DictionaryVM<GuiHint, string>(DisplayMode.Value);
            ParentList = parentList;
            foreach (var item in Enum.GetValues(typeof(GuiHint)))
            {
                SelectedGuiHint.Add((GuiHint)item, Enum.GetName(typeof(GuiHint), item));
            }
            SelectedGuiHint.SelectedIndex = 0;
            AbilityDataBlobTypeSelection = GetTypeDict(AbilityTypes());
        }

        public ComponentAbilityTemplateVM(ComponentAbilitySD abilitySD, ObservableCollection<ComponentAbilityTemplateVM> parentList) : this(parentList)
        {
            Name = abilitySD.Name;
            Description = abilitySD.Description;
            SelectedGuiHint.SelectedIndex = (int)abilitySD.GuiHint;
            AbilityDataBlobType = abilitySD.AbilityDataBlobType;
            AbilityFormula = abilitySD.AbilityFormula;
            MinFormula = abilitySD.MinFormula;
            MaxFormula = abilitySD.MaxFormula;
            GuidDictionary = abilitySD.GuidDictionary;
        }

        private static List<Type> AbilityTypes()
        {
            List<Type> typelist = new List<Type>();
            typelist.Add(typeof(ConstructInstationsAbilityDB));
            typelist.Add(typeof(ConstructShipComponentsAbilityDB));
            typelist.Add(typeof(ConstructAmmoAbilityDB));
            typelist.Add(typeof(ConstructFightersAbilityDB));
            typelist.Add(typeof(EnginePowerDB));
            typelist.Add(typeof(FuelStorageDB));
            typelist.Add(typeof(FuelUseDB));
            typelist.Add(typeof(MineResourcesDB));
            typelist.Add(typeof(MissileLauncherSizeDB));
            typelist.Add(typeof(RefineResourcesDB));
            typelist.Add(typeof(ResearchPointsAbilityDB));
            typelist.Add(typeof(SensorSignatureDB));
            typelist.Add(typeof(ActiveSensorDB));

            return typelist;
        }

        private static DictionaryVM<Type, string, string> GetTypeDict(List<Type> abilityTypes)
        {
            DictionaryVM<Type, string, string> dict = new DictionaryVM<Type, string, string>(DisplayMode.Value);
            foreach (var type in abilityTypes)
            {
                dict.Add(type, type.Name);
            }

            return dict;

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
            sd.GuidDictionary = GuidDictionary;
            return sd;
                
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
