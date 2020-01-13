using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Pulsar4X.ECSLib
{




    public class ComponentDesignVM : IViewModel
    {
        //public Dictionary<string, ID> ComponentTypes { get; set; }
        public DictionaryVM<string, Guid> ComponentTypes { get; } = new DictionaryVM<string, Guid>(DisplayMode.Key);
        public ComponentDesigner Designer { get; private set; }
        
        private readonly StaticDataStore _staticData;
        private readonly Entity _factionEntity;
        private readonly FactionTechDB _factionTech;
        private readonly GameVM _gameVM;
        //public event ValueChangedEventHandler ValueChanged;

        public List<ComponentAbilityDesignVM> AbilityList { get; private set; } = new List<ComponentAbilityDesignVM>();

        public ComponentDesignVM(GameVM gameVM)
        {
            _gameVM = gameVM;
            _staticData = gameVM.Game.StaticData;
            _factionEntity = gameVM.CurrentFaction;
            _factionTech = gameVM.CurrentFaction.GetDataBlob<FactionTechDB>();


            foreach (var componentSD in gameVM.Game.StaticData.ComponentTemplates.Values)
            {
                ComponentTypes.Add(componentSD.Name, componentSD.ID);
            }
            ComponentTypes.SelectedIndex = 0;
        }

        public static ComponentDesignVM Create(GameVM gameVM)
        {
            return new ComponentDesignVM(gameVM);            
        }

        public void SetComponent(Guid componentGuid)
        {
            ComponentTemplateSD componentSD = _staticData.ComponentTemplates[componentGuid];

            Designer = new ComponentDesigner(componentSD, _factionTech);

            AbilityList = new List<ComponentAbilityDesignVM>();
            foreach (var componentAbility in Designer.ComponentDesignAttributes.Values)
            {
                AbilityList.Add(new ComponentAbilityDesignVM(this, componentAbility, _staticData));
            }            
        }



        public string StatsText
        {
            get
            {
                string text = "";
                if (Designer != null)
                {
                    text = Designer.Name + Environment.NewLine;
                    text += "Size: " + Designer.MassValue + Environment.NewLine;
                    text += "HTK: " + Designer.HTKValue + Environment.NewLine;
                    text += "Crew: " + Designer.CrewReqValue + Environment.NewLine;
                    text += "ResearchCost: " + Designer.ResearchCostValue + Environment.NewLine;
                    foreach (var kvp in Designer.ResourceCostValues)
                    {
                        string resourceName = _staticData.CargoGoods.GetAny(kvp.Key).Name;
                        text += resourceName + ": " + kvp.Value + Environment.NewLine;
                    }
                    text += "Credit Cost: " + Designer.CreditCostValue + Environment.NewLine;
                }
                return text;
            }
        }

        public string AbilityStatsText
        {
            get
            {
                string text = "Ability Stats:" + Environment.NewLine;

                foreach (var abilty in AbilityList)
                {
                    text += abilty.AbilityStat;
                }
                return text;
            }
        }

        
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Refresh(bool partialRefresh = false)
        {
            OnPropertyChanged("AbilityStatsText");
            OnPropertyChanged("StatsText");
        }
    }


    public class ComponentAbilityDesignVM : IViewModel
    {
        private ComponentDesignAttribute _designAbility;
        private StaticDataStore _staticData;
        private ComponentDesignVM _parentDesignVM;
        
        //public event ValueChangedEventHandler ValueChanged;

        public DictionaryVM<TechSD, string> TechList { get; } = new DictionaryVM<TechSD, string>();
        public string Name { get { return _designAbility.Name; } }
        public string Description { get { return _designAbility.Description; } }

        public double MaxValue { get { return _designAbility.MaxValue; } }
        public double MinValue { get { return _designAbility.MinValue; } }
        public double StepValue { get { return _designAbility.StepValue; } }
        private double _value;
        public double Value { get { return _designAbility.Value; }set
        {
            _value = value; OnPropertyChanged();} }

        public GuiHint GuiHint { get { return _designAbility.GuiHint; }}
        
        public MinMaxSliderVM minMaxSliderVM { get; set; }
        


        public ComponentAbilityDesignVM(ComponentDesignVM designVM, ComponentDesignAttribute designAbility, StaticDataStore staticData)
        {
            _designAbility = designAbility;
            _staticData = staticData;
            _parentDesignVM = designVM;


            switch (designAbility.GuiHint)
            {
                case GuiHint.GuiTechSelectionList:
                  
                    foreach (var kvp in designAbility.GuidDictionary)
                    {
                        TechSD sd = _staticData.Techs[Guid.Parse((string)kvp.Key)];
                        TechList.Add(sd, sd.Name );
                    }
                    TechList.SelectedIndex = 0;
                    TechList.SelectionChangedEvent += TechList_SelectionChangedEvent;
                    break;
                case GuiHint.GuiSelectionMaxMin:
                    {
                        minMaxSliderVM = new MinMaxSliderVM();

                        designAbility.SetMax();
                        designAbility.SetMin();
                        designAbility.SetValue();
                        designAbility.SetStep();
                        minMaxSliderVM.Name = Name;
                        minMaxSliderVM.MaxValue = MaxValue;
                        minMaxSliderVM.MinValue = MinValue;
                        minMaxSliderVM.StepValue = StepValue;
                        minMaxSliderVM.Value = Value; //.PreLoadedValue = Value; //hack due to eto bug. MinMaxSlider.Value = Value; 
                        minMaxSliderVM.PropertyChanged += MinMaxSlider_PropertyChanged;

                    }
                    break;
            }
        }

        private void TechList_SelectionChangedEvent(int oldSelection, int newSelection)
        {
            _designAbility.SetValueFromGuidList(TechList.SelectedKey.ID);
            _parentDesignVM.Refresh();
        }

        void MinMaxSlider_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            _designAbility.SetValueFromInput(minMaxSliderVM.Value);
            _parentDesignVM.Refresh();
        }



        public string AbilityStat {
            get
            {
                string text = null;
                if (_designAbility.GuiHint == GuiHint.GuiTextDisplay)
                {
                    text += _designAbility.Name + ": ";
                    text += _designAbility.Value + Environment.NewLine;
                }
                return text;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void Refresh(bool partialRefresh = false)
        {
            throw new NotImplementedException();
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    //public delegate void ValueChangedEventHandler(GuiHint controlType, double value);
}
