using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;
using SDL2;

namespace Pulsar4X.SDL2UI
{
    public class PartDesignUI : NonUniquePulsarGuiWindow
    {
        private ComponentDesigner _componentDesigner;
        private int _designType;
        private string[] _designTypes;
        private ComponentTemplateSD[] _designables;
        private byte[] _nameInputBuffer = new byte[128];
        bool compactmod = false;
        string _windowname;
        

        private TechSD[] _techSDs;
        private string[] _techNames;
        private int _techSelectedIndex = -1;
        
        //private TechSD[] _techSDs;
        private string[] _enumNames;
        
        //new internal static GlobalUIState _state;

        public PartDesignUI(int designType, GlobalUIState state)
        {

            _designType = designType;
            _state = state;
            SetName("Part Designer " + _designType.ToString());
            //_flags = ImGuiWindowFlags.Popup;

            var factionTech = _state.Faction.GetDataBlob<FactionTechDB>();
            var staticdata = StaticRefLib.StaticData;
            IsActive = true;                     
            _designables = StaticRefLib.StaticData.ComponentTemplates.Values.ToArray();
            int count = _designables.Length;
            _designTypes = new string[count];
            for (int i = 0; i < count; i++)
            {
                _designTypes[i] = _designables[i].Name;
            }
            _componentDesigner = new ComponentDesigner(_designables[_designType], factionTech);
            _windowname = _componentDesigner.Name;
            StartDisplay();

        }




        internal override void Display()
        {
            
            if (IsActive && ImGui.Begin(_windowname, ref IsActive, _flags))
            {             

                GuiDesignUI();//Part design

                GuiCostText();//Print cost
                ImGui.End();
            }

            void GuiDesignUI()//Creates all UI elements need for designing the Component
            {
                ImGui.Text("Component Specifications");
                ImGui.SameLine(ImGui.GetWindowWidth() - 70);
                if (ImGui.Button("Compact"))
                {
                    compactmod = !compactmod;
                }


                ImGui.NewLine();

                if (_componentDesigner != null)//Make sure comp is selected
                {
                    foreach (ComponentDesignAttribute attribute in _componentDesigner.ComponentDesignAttributes.Values) //For each property of the comp type
                    {
                        ImGui.PushID(attribute.Name);
                        
                        
                        if (!attribute.IsEnabled)
                        {
                            break;
                        }

                        switch (attribute.GuiHint) //Either
                        {
                            case GuiHint.None:
                                break;
                            case GuiHint.GuiTechSelectionList: //Let the user pick a type from a list
                                GuiHintTechSelection(attribute);
                                break;
                            case GuiHint.GuiSelectionMaxMin: //Set a value
                                GuiHintMaxMin(attribute);
                                break;
                            case GuiHint.GuiTextDisplay: //Display a stat
                                GuiHintText(attribute);
                                break;
                            case GuiHint.GuiEnumSelectionList: //Let the user pick a type from a hard coded list
                                GuiHintEnumSelection(attribute);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        
                        
                        
                        ImGui.PopID();
                    }


                
                    ImGui.Text("Name");
                    ImGui.InputText("", _nameInputBuffer, 32);
                    if (ImGui.Button("Create Design"))
                    {
                        _componentDesigner.Name = ImGuiSDL2CSHelper.StringFromBytes(_nameInputBuffer);
                        _componentDesigner.CreateDesign(_state.Faction);
                        //we reset the designer here, so we don't end up trying to edit the precious design. 
                        var factionTech = _state.Faction.GetDataBlob<FactionTechDB>();
                        _componentDesigner = new ComponentDesigner(_designables[_designType], factionTech);
                    }
                    ImGui.NewLine();
                } 
                else//Tell the user they don't have a comp type selected
                {
                    ImGui.NewLine();
                    ImGui.Text("No component type selected");
                    ImGui.NewLine();
                }
            }

            




            void GuiCostText()//Prints a 2 col table with the costs of the part
            {
                //ImGui.BeginChild("Cost");
                if (_componentDesigner != null)//If a part time is selected
                {
                    ImGui.Columns(2);
                    ImGui.BeginTabItem("Cost");
                    
                    ImGui.Text("Mass");
                    ImGui.Text("Volume");
                    ImGui.Text("Crew Requred"); 
                    ImGui.Text("Cost");
                    ImGui.Text("Research Cost");
                    ImGui.Text("Build Cost");
                    ImGui.Text("Resource Costs");
                    ImGui.NextColumn();//Add all the cost names to col 1

                    
                    ImGui.Text(Misc.StringifyMass(_componentDesigner.MassValue));
                    ImGui.Text(Misc.StringifyVolume(_componentDesigner.VolumeValue));
                    ImGui.Text(_componentDesigner.CrewReqValue.ToString());
                    ImGui.Text(_componentDesigner.CreditCostValue.ToString());
                    ImGui.Text(_componentDesigner.ResearchCostValue.ToString() + " RP");
                    ImGui.Text(_componentDesigner.IndustryPointCostsValue.ToString() + " BP");
                    ImGui.NextColumn();//Add all the price values to col 2


                    foreach (var kvp in _componentDesigner.ResourceCostValues)
                    {
                        var resource = StaticRefLib.StaticData.CargoGoods.GetAny(kvp.Key);
                        if (resource == null)
                            resource = (ICargoable)_state.Faction.GetDataBlob<FactionInfoDB>().IndustryDesigns[kvp.Key];
                        var xpos = ImGui.GetCursorPosX();
                        ImGui.SetCursorPosX(xpos + 12);
                        ImGui.Text(resource.Name);
                        ImGui.NextColumn();
                        ImGui.Text(kvp.Value.ToString());
                        ImGui.NextColumn();
                    }



                }
                
                //ImGui.EndChild();
            }
            void GuiHintText(ComponentDesignAttribute attribute)
            {
                if(compactmod)
                {
                    ImGui.TextWrapped(attribute.Name + ": " + attribute.Value.ToString() + " " + attribute.Unit);
                    ImGui.NewLine();
                }
                else
                {
                    ImGui.TextWrapped(attribute.Name + ":");
                    ImGui.SameLine();
                    ImGui.TextWrapped(attribute.Value.ToString() + " " + attribute.Unit);
                    ImGui.NewLine();
                }
            }
            void GuiHintMaxMin(ComponentDesignAttribute attribute)
            {
                if (compactmod)
                {
                    ImGui.TextWrapped(attribute.Name + ": " + attribute.Description);
                    ImGui.NewLine();
                }
                else
                {
                    ImGui.TextWrapped(attribute.Name + ":");
                    ImGui.SameLine();
                    ImGui.TextWrapped(attribute.Description);
                    ImGui.NewLine();
                }

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
                

                if (compactmod)
                {
                }
                else
                {
                    ImGui.PushItemWidth(-1);
                    if (ImGui.SliderScalar("##scaler" + attribute.Name, ImGuiDataType.Double, valPtr, minPtr, maxPtr))
                    {
                        attribute.SetValueFromInput(val);
                    }
                }
                ImGui.PushItemWidth(-1);
                if (ImGui.InputScalar("##input" + attribute.Name, ImGuiDataType.Double, valPtr, stepPtr, fstepPtr))
                    attribute.SetValueFromInput(val);
                ImGui.NewLine();
            }
            void GuiHintTechSelection(ComponentDesignAttribute attribute)
            {
                if (compactmod)
                {
                    ImGui.TextWrapped(attribute.Name + ": " + attribute.Description);
                    ImGui.NewLine();
                }
                else
                {
                    ImGui.TextWrapped(attribute.Name + ":");
                    ImGui.SameLine();
                    ImGui.TextWrapped(attribute.Description);
                    ImGui.NewLine();
                }

                int i = 0;
                _techSDs = new TechSD[attribute.GuidDictionary.Count];
                _techNames = new string[attribute.GuidDictionary.Count];
                foreach (var kvp in attribute.GuidDictionary)
                {
                    TechSD sd = StaticRefLib.StaticData.Techs[Guid.Parse((string)kvp.Key)];
                    _techSDs[i] = sd;
                    _techNames[i] = sd.Name;
                    i++;
                }
                
                ImGui.TextWrapped(attribute.Value.ToString());



                if (ImGui.Combo("Select Tech", ref _techSelectedIndex, _techNames, _techNames.Length))
                {
                    attribute.SetValueFromGuidList(_techSDs[_techSelectedIndex].ID);
                }
                
                ImGui.NewLine();
            }
            void GuiHintEnumSelection(ComponentDesignAttribute attribute)
            {
                if (compactmod)
                {
                    ImGui.TextWrapped(attribute.Name + ": " + attribute.Description);
                    ImGui.NewLine();
                }
                else
                {
                    ImGui.TextWrapped(attribute.Name + ":");
                    ImGui.SameLine();
                    ImGui.TextWrapped(attribute.Description);
                    ImGui.NewLine();
                }
                
                
                int i = 0;
                //_techSDs = new TechSD[attribute.GuidDictionary.Count];
                _enumNames = Enum.GetNames(attribute.EnumType);

                
                ImGui.TextWrapped(attribute.Value.ToString());

                if (ImGui.Combo("Select", ref attribute.EnumSelection, _enumNames, (int)attribute.MaxValue + 1))
                {
                    int enumVal = (int)Enum.Parse(attribute.EnumType, _enumNames[attribute.EnumSelection]);
                    attribute.SetValueFromInput(enumVal);
                }
                
                ImGui.NewLine();
            }
        }
    }
}