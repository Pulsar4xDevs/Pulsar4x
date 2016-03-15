using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Pulsar4X.ViewModel
{




    public class ComponentDesignVM : IViewModel
    {
        //public Dictionary<string, Guid> ComponentTypes { get; set; }
        public DictionaryVM<string, Guid> ComponentTypes { get; } = new DictionaryVM<string, Guid>(DisplayMode.Key);
        public ComponentDesign Design { get; private set; }
        
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


            foreach (var componentSD in gameVM.Game.StaticData.Components.Values)
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
            ComponentTemplateSD componentSD = _staticData.Components[componentGuid];

            Design = GenericComponentFactory.StaticToDesign(componentSD, _factionTech, _staticData);

            AbilityList = new List<ComponentAbilityDesignVM>();
            foreach (var componentAbility in Design.ComponentDesignAbilities)
            {
                AbilityList.Add(new ComponentAbilityDesignVM(this, componentAbility, _staticData));
            }            
        }

        public void CreateComponent()
        {
            GenericComponentFactory.DesignToEntity(_gameVM.Game, _factionEntity, Design);             
        }

        public string StatsText
        {
            get
            {
                string text = "";
                if (Design != null)
                {
                    text = Design.Name + Environment.NewLine;
                    text += "Size: " + Design.SizeValue + Environment.NewLine;
                    text += "HTK: " + Design.HTKValue + Environment.NewLine;
                    text += "Crew: " + Design.CrewReqValue + Environment.NewLine;
                    text += "ResearchCost: " + Design.ResearchCostValue + Environment.NewLine;
                    foreach (var kvp in Design.MineralCostValues)
                    {
                        string mineralName = _staticData.Minerals.Find(item => item.ID == kvp.Key).Name;
                        text += mineralName + ": " + kvp.Value + Environment.NewLine;
                    }
                    text += "Credit Cost: " + Design.CreditCostValue + Environment.NewLine;
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
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void Refresh(bool partialRefresh = false)
        {
            OnPropertyChanged("AbilityStatsText");
            OnPropertyChanged("StatsText");
        }
    }


    public class ComponentAbilityDesignVM : IViewModel
    {
        private ComponentDesignAbility _designAbility;
        private StaticDataStore _staticData;
        private ComponentDesignVM _parentDesignVM;
        
        public event ValueChangedEventHandler ValueChanged;

        public DictionaryVM<TechSD, string> TechList { get; } = new DictionaryVM<TechSD, string>();
        public string Name { get { return _designAbility.Name; } }
        public string Description { get { return _designAbility.Description; } }

        public double MaxValue { get { return _designAbility.MaxValue; } }
        public double MinValue { get { return _designAbility.MinValue; } }
        private double _value;
        public double Value { get { return _designAbility.Value; }set
        {
            _value = value; OnPropertyChanged();} }

        public GuiHint GuiHint { get { return _designAbility.GuiHint; }}
        
        public MinMaxSliderVM MinMaxSlider { get; set; }
        


        public ComponentAbilityDesignVM(ComponentDesignVM designVM, ComponentDesignAbility designAbility, StaticDataStore staticData)
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
                        MinMaxSlider = new MinMaxSliderVM();

                        designAbility.SetMax();
                        designAbility.SetMin();
                        designAbility.SetValue();
                        MinMaxSlider.Name = Name;
                        MinMaxSlider.MaxValue = MaxValue;
                        MinMaxSlider.MinValue = MinValue;
                        MinMaxSlider.Value = Value;
                        MinMaxSlider.PropertyChanged += MinMaxSlider_PropertyChanged;

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
            _designAbility.SetValueFromInput(MinMaxSlider.Value);
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
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public delegate void ValueChangedEventHandler(GuiHint controlType, double value);
}
