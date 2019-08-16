using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Pulsar4X.ECSLib;
using SDL2;

namespace Pulsar4X.SDL2UI
{
    public class ComponentDesignUI : PulsarGuiWindow
    {
        private ComponentDesigner _componentDesigner;
        private int _designType;
        private string[] _designTypes;
        private ComponentTemplateSD[] _designables;

        private ComponentDesignUI()
        {
            _flags = ImGuiWindowFlags.NoCollapse;
        }

        internal static ComponentDesignUI GetInstance()
        {
            ComponentDesignUI thisitem;
            if (!_state.LoadedWindows.ContainsKey(typeof(ComponentDesignUI)))
            {
                thisitem = new ComponentDesignUI();
            }
            thisitem = (ComponentDesignUI)_state.LoadedWindows[typeof(ComponentDesignUI)];

            //TODO: pull this from faction info and have designables unlocked via tech.
            thisitem._designables = StaticRefLib.StaticData.ComponentTemplates.Values.ToArray();
            int count = thisitem._designables.Length;
            thisitem._designTypes = new string[count];
            for (int i = 0; i < count ; i++)
            {
                thisitem._designTypes[i] = thisitem._designables[i].Name;
            }
            return thisitem;
        }
        
        internal override void Display()
        {
            if (IsActive && ImGui.Begin("Component Design", ref IsActive, _flags))
            {

                ImGui.Columns(2);
                if (ImGui.ListBox("Type", ref _designType, _designTypes, _designTypes.Length))
                {
                    var factionTech = _state.Faction.GetDataBlob<FactionTechDB>();
                    var staticdata = StaticRefLib.StaticData;
                    _componentDesigner = new ComponentDesigner(_designables[_designType], factionTech);
                }

                if (_componentDesigner != null)
                {

                    foreach (ComponentDesignAttribute attribute in _componentDesigner.ComponentDesignAttributes)
                    {
 
                        switch (attribute.GuiHint)
                        {
                            case GuiHint.None:
                                break;
                            case GuiHint.GuiTechSelectionList:
                                GuiHintTechSelection(attribute);
                                break;
                            case GuiHint.GuiSelectionMaxMin:
                                GuiHintMaxMin(attribute);
                                break;
                            case GuiHint.GuiTextDisplay:
                                GuiHintText(attribute);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        
                    }

                    var _nameInputBuffer = _componentDesigner.Name.ToByteArray();
                    ImGui.InputText("Component Name", _nameInputBuffer, (uint)_nameInputBuffer.Length);
                    if (ImGui.Button("Create Design"))
                    {
                         _componentDesigner.CreateDesign(_state.Faction);
                    }
                ImGui.NextColumn();
                ImGui.BeginChild("ComponentData");
                ImGui.Columns(2);
                ImGui.Text("Mass");
                ImGui.NextColumn();
                ImGui.Text(_componentDesigner.MassValue.ToString());
                ImGui.NextColumn();
                
                ImGui.Text("Volume");
                ImGui.NextColumn();
                ImGui.Text(_componentDesigner.VolumeValue.ToString());
                ImGui.NextColumn();
                
                ImGui.Text("Crew Requred");
                ImGui.NextColumn();
                ImGui.Text(_componentDesigner.CrewReqValue.ToString());
                ImGui.NextColumn();
                
                ImGui.Text("Cost");
                ImGui.NextColumn();
                ImGui.Text(_componentDesigner.CreditCostValue.ToString());
                ImGui.NextColumn();
                
                ImGui.Text("Research Cost");
                ImGui.NextColumn();
                ImGui.Text(_componentDesigner.ResearchCostValue.ToString());
                ImGui.NextColumn();
                
                ImGui.Text("Build Cost");
                ImGui.NextColumn();
                ImGui.Text(_componentDesigner.BuildPointCostValue.ToString());
                ImGui.NextColumn();
                
                ImGui.Text("Minerals");
                ImGui.NextColumn();
                ImGui.NextColumn();
                foreach (var mineral in _componentDesigner.MineralCostValues)
                {
                    var mineralSD = StaticRefLib.StaticData.CargoGoods.GetMineral(mineral.Key);
                    ImGui.Text(mineralSD.Name);
                    ImGui.NextColumn();
                    ImGui.Text(mineral.Value.ToString());
                    ImGui.NextColumn();
                }
/*
                ImGui.Text("Materials");
                ImGui.NextColumn();
                ImGui.Text(_componentDesigner.MassValue.ToString());
                ImGui.NextColumn();
                
                ImGui.Text("Components");
                ImGui.NextColumn();
                ImGui.Text(_componentDesigner.MassValue.ToString());
                ImGui.NextColumn();
*/

                }
                


                ImGui.End();
            }

            void GuiHintText(ComponentDesignAttribute attribute)
            {
                ImGui.Text(attribute.Name);
                ImGui.Text(attribute.Description);
                ImGui.Text(attribute.Value.ToString());

            }
            void GuiHintMaxMin(ComponentDesignAttribute attribute)
            {
                ImGui.Text(attribute.Name);
                ImGui.Text(attribute.Description);

                attribute.SetMax();
                attribute.SetMin();
                attribute.SetValue();
                attribute.SetStep();
                
                var max = attribute.MaxValue;
                var min = attribute.MinValue;
                double val = attribute.Value;
                double step = attribute.StepValue;
                double fstep = step * 10;
                IntPtr valPtr;
                IntPtr maxPtr;
                IntPtr minPtr;
                IntPtr stepPtr;
                IntPtr fstepPtr;
                
                unsafe
                {
                    valPtr = new IntPtr(&val);
                    maxPtr = new IntPtr(&max);
                    minPtr = new IntPtr(&min);
                    stepPtr = new IntPtr(&step);
                    fstepPtr =  new IntPtr(&fstep);
                }
                //ImGui.DragScalar("##slider" + attribute.Name, ImGuiDataType.Double, valPtr, 1f, minPtr, maxPtr);
                if (ImGui.SliderScalar("##scaler" + attribute.Name, ImGuiDataType.Double, valPtr, minPtr, maxPtr))
                {
                    attribute.SetValueFromInput(val);
                    
                    
                }
                if(ImGui.InputScalar("##input" + attribute.Name, ImGuiDataType.Double, valPtr, stepPtr, fstepPtr))
                    attribute.SetValueFromInput(val);
            }
            void GuiHintTechSelection(ComponentDesignAttribute attribute)
            {
                ImGui.Text(attribute.Name);
                ImGui.Text(attribute.Description);
                //StaticRefLib.StaticData.Techs[attribute.Value]
                ImGui.Text(attribute.Value.ToString());
/*
                int techSelection;
                List<string> techs = attribute.GuidDictionary
                
                if (ImGui.ListBox("Tech" + attribute.Name, ref _designType, _designTypes, _designTypes.Length))
                {
                    var factionTech = _uiState.Faction.GetDataBlob<FactionTechDB>();
                    var staticdata = StaticRefLib.StaticData;
                    _componentDesign = GenericComponentFactory.StaticToDesign(_designables[_designType], factionTech, staticdata);
                }*/

            }
        }
    }
}