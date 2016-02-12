using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;
using System.Runtime.CompilerServices;

namespace Pulsar4X.ViewModel
{
   public class ComponentAbilityTemplateVM : IViewModel
    {

        public string Name { get; set; }
        public string Description { get; set; }
        //public DictionaryVM<GuiHint, string> GuiHint { get; set; }
        public DictionaryVM<GuiHint,string> SelectedGuiHint{ get; set; }
        public DictionaryVM<Type,string> AbilityDataBlobTypeSelection { get; set; }
        public string AbilityDataBlobType { get; set; }
        public string AbilityFormula { get; set; }
        public string MinFormula { get; set; }
        public string MaxFormula { get; set; }
        public Dictionary<Guid, string> GuidDictionary;
        public ComponentAbilityTemplateVM()
        {
            SelectedGuiHint = new DictionaryVM<GuiHint, string>(DisplayMode.Value);
            foreach (var item in Enum.GetValues(typeof(GuiHint)))
            {
                SelectedGuiHint.Add((GuiHint)item, Enum.GetName(typeof(GuiHint), item));
            }
            
            //GuiHint = Enum.GetNames(typeof(ECSLib.GuiHint)).ToList<string>();
            //GuiHint = new DictionaryVM<ECSLib.GuiHint, string>(DisplayMode.Value);
            //GuiHint = typeof(GuiHint);
            AbilityDataBlobTypeSelection = GetTypeDict(AbilityTypes());
        }

        public ComponentAbilityTemplateVM(ComponentAbilitySD abilitySD)
        {
            Name = abilitySD.Name;
            Description = abilitySD.Description;

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

        private static DictionaryVM<Type, string> GetTypeDict(List<Type> abilityTypes)
        {
            DictionaryVM<Type, string> dict = new DictionaryVM<Type, string>(DisplayMode.Key);
            foreach (var type in abilityTypes)
            {
                dict.Add(type, type.Name);
            }

            return dict;

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
