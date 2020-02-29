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
    public class ComponentDesignUI : PulsarGuiWindow
    {
        private ComponentDesigner _componentDesigner;
        private int _designType;
        private string[] _designTypes;
        private ComponentTemplateSD[] _designables;
        private byte[] _nameInputBuffer = new byte[128];
        bool compactmod = false;

        private ComponentDesignUI()
        {
            //_flags = ImGuiWindowFlags.NoCollapse;
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

                GuiDesignUI();//Part design

                ImGui.Columns(2, "Main");//Col 1 contains list of comp types, col 2 contains the cost

                int numelements = Convert.ToInt32((ImGui.GetContentRegionAvail().Y - 20) / 17);  
                if (numelements < 4) {
                    numelements = 4;
                }
                else if (numelements > _designTypes.Length) {
                    numelements = _designTypes.Length;
                }
                ImGui.PushItemWidth(-1);
                if (ImGui.ListBox("", ref _designType, _designTypes, _designTypes.Length, numelements))//Lists the possible comp types
                {
                    var factionTech = _state.Faction.GetDataBlob<FactionTechDB>();
                    var staticdata = StaticRefLib.StaticData;
                    _componentDesigner = new ComponentDesigner(_designables[_designType], factionTech);
                    _nameInputBuffer = ImGuiSDL2CSHelper.BytesFromString(_componentDesigner.Name, 32);
                }

                ImGui.NextColumn();
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
                    foreach (ComponentDesignAttribute attribute in _componentDesigner.ComponentDesignAttributes.Values)//For each property of the comp type
                    {

                        switch (attribute.GuiHint)//Either
                        {
                            case GuiHint.None:
                                break;
                            case GuiHint.GuiTechSelectionList://Let the user pick a type from a list
                                GuiHintTechSelection(attribute);
                                break;
                            case GuiHint.GuiSelectionMaxMin://Set a value
                                GuiHintMaxMin(attribute);
                                break;
                            case GuiHint.GuiTextDisplay://Display a stat
                                GuiHintText(attribute);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }


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
                ImGui.BeginChild("Cost");
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

                    
                    ImGui.Text(_componentDesigner.MassValue.ToString());
                    ImGui.Text(_componentDesigner.VolumeValue.ToString());
                    ImGui.Text(_componentDesigner.CrewReqValue.ToString());
                    ImGui.Text(_componentDesigner.CreditCostValue.ToString());
                    ImGui.Text(_componentDesigner.ResearchCostValue.ToString());
                    ImGui.Text(_componentDesigner.IndustryPointCostsValue.ToString());
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

                    //Old Cost code I think
                    if (false)
                    {
                        /*
                        foreach (var mineral in _componentDesigner.MineralCostValues)
                        {
                            var mineralSD = StaticRefLib.StaticData.CargoGoods.GetMineral(mineral.Key);
                            var xpos = ImGui.GetCursorPosX();
                            ImGui.SetCursorPosX(xpos + 12);
                            ImGui.Text(mineralSD.Name);
                            ImGui.NextColumn();
                            ImGui.Text(mineral.Value.ToString());
                            ImGui.NextColumn();
                        }
                        foreach (var material in _componentDesigner.MaterialCostValues)
                        {
                            var matSD = StaticRefLib.StaticData.CargoGoods.GetMaterial(material.Key);
                            var xpos = ImGui.GetCursorPosX();
                            ImGui.SetCursorPosX(xpos + 12);
                            ImGui.Text(matSD.Name);
                            ImGui.NextColumn();
                            ImGui.Text(material.Value.ToString());
                            ImGui.NextColumn();
                        }
                        foreach (var component in _componentDesigner.ComponentCostValues)
                        {
                            var compSD = StaticRefLib.StaticData.CargoGoods.GetMaterial(component.Key);
                            var xpos = ImGui.GetCursorPosX();
                            ImGui.SetCursorPosX(xpos + 12);
                            ImGui.Text(compSD.Name);
                            ImGui.NextColumn();
                            ImGui.Text(component.Value.ToString());
                            ImGui.NextColumn();
                        }
                        */

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
                }
                
                ImGui.EndChild();
            }
            void GuiHintText(ComponentDesignAttribute attribute)
            {
                if(compactmod)
                {
                    ImGui.TextWrapped(attribute.Name + ": " + attribute.Value.ToString());
                    ImGui.NewLine();
                }
                else
                {
                    ImGui.TextWrapped(attribute.Name + ":");
                    ImGui.SameLine();
                    ImGui.TextWrapped(attribute.Value.ToString());
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
                //StaticRefLib.StaticData.Techs[attribute.Value]
                ImGui.TextWrapped(attribute.Value.ToString());
/*
                int techSelection;
                List<string> techs = attribute.GuidDictionary
                
                if (ImGui.ListBox("Tech" + attribute.Name, ref _designType, _designTypes, _designTypes.Length))
                {
                    var factionTech = _uiState.Faction.GetDataBlob<FactionTechDB>();
                    var staticdata = StaticRefLib.StaticData;
                    _componentDesign = GenericComponentFactory.StaticToDesign(_designables[_designType], factionTech, staticdata);
                }*/
                ImGui.NewLine();
            }
        }
    }
}