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
        public DictionaryVM<string, Guid, string> ComponentTypes { get; set; } 
        public ComponentDesignDB DesignDB { get; private set; }
        
        private readonly StaticDataStore _staticData;
        private readonly Entity _factionEntity;
        private readonly FactionTechDB _factionTech;
        private readonly GameVM _gameVM;
        //public event ValueChangedEventHandler ValueChanged;

        public List<ComponentAbilityDesignVM> AbilityList { get; private set; }


        public ComponentDesignVM()
        {
            AbilityList = new List<ComponentAbilityDesignVM>();
        }

        public ComponentDesignVM(GameVM gameVM):this()
        {
            _gameVM = gameVM;
            _staticData = gameVM.Game.StaticData;
            _factionEntity = gameVM.CurrentFaction;
            _factionTech = gameVM.CurrentFaction.GetDataBlob<FactionTechDB>();

            //ComponentTypes = new Dictionary<string, Guid>();
            ComponentTypes = new DictionaryVM<string, Guid, string>(DisplayMode.Key);
            foreach (var componentSD in gameVM.Game.StaticData.Components.Values)
            {
                ComponentTypes.Add(componentSD.Name, componentSD.ID);
            }
        }


        public static ComponentDesignVM Create(GameVM gameVM)
        {
            return new ComponentDesignVM(gameVM);            
        }

        public void SetComponent(Guid componentGuid)
        {
            ComponentSD componentSD = _staticData.Components[componentGuid];

            DesignDB = GenericComponentFactory.StaticToDesign(componentSD, _factionTech, _staticData);

            AbilityList = new List<ComponentAbilityDesignVM>();
            foreach (var componentAbility in DesignDB.ComponentDesignAbilities)
            {
                AbilityList.Add(new ComponentAbilityDesignVM(this, componentAbility, _staticData));
            }            
        }

        public void CreateComponent()
        {
            GenericComponentFactory.DesignToEntity(_gameVM.Game, _factionEntity, DesignDB);             
        }

        public string StatsText
        {
            get
            {
                string text = "";
                if (DesignDB != null)
                {
                    text = DesignDB.Name + Environment.NewLine;
                    text += "Size: " + DesignDB.SizeValue + Environment.NewLine;
                    text += "HTK: " + DesignDB.HTKValue + Environment.NewLine;
                    text += "Crew: " + DesignDB.CrewReqValue + Environment.NewLine;
                    text += "ResearchCost: " + DesignDB.ResearchCostValue + Environment.NewLine;
                    foreach (var kvp in DesignDB.MineralCostValues)
                    {
                        string mineralName = _staticData.Minerals.Find(item => item.ID == kvp.Key).Name;
                        text += mineralName + ": " + kvp.Value + Environment.NewLine;
                    }
                    text += "Credit Cost: " + DesignDB.CreditCostValue + Environment.NewLine;
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
        private ComponentDesignAbilityDB _designAbility;
        private StaticDataStore _staticData;
        private ComponentDesignVM _parentDesignVM;
        
        public event ValueChangedEventHandler ValueChanged;

        public List<TechSD> TechList { get; private set; }
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
        


        public ComponentAbilityDesignVM(ComponentDesignVM designVM, ComponentDesignAbilityDB designAbility, StaticDataStore staticData)
        {
            _designAbility = designAbility;
            _staticData = staticData;
            _parentDesignVM = designVM;


            switch (designAbility.GuiHint)
            {
                case GuiHint.GuiTechSelectionList:
                    TechList = new List<TechSD>();
                    foreach (var kvp in designAbility.GuidDictionary)
                    {
                        TechList.Add(_staticData.Techs[kvp.Key]);
                    }
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

        void MinMaxSlider_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnValueChanged( MinMaxSlider.Value);
        }


        private void OnValueChanged(double value)
        {
            if (GuiHint == GuiHint.GuiSelectionMaxMin)
                _designAbility.SetValueFromInput(value);
            else if (GuiHint == GuiHint.GuiTechSelectionList)
                _designAbility.SetValueFromGuidList(TechList[(int)value].ID);

            _parentDesignVM.Refresh();
        }

        public void OnValueChanged(GuiHint controlType, double value)
        {
            if (controlType == GuiHint.GuiSelectionMaxMin)
                _designAbility.SetValueFromInput(value);
            else if (controlType == GuiHint.GuiTechSelectionList)
                _designAbility.SetValueFromGuidList(TechList[(int)value].ID);

            //_parentDesignVM.Refresh();
            //if (ValueChanged != null) //bubble it up to ComponentDesignVM?
            //{
            //    ValueChanged.Invoke(controlType, value);
            //}
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
                OnValueChanged( _value );
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public delegate void ValueChangedEventHandler(GuiHint controlType, double value);
}
