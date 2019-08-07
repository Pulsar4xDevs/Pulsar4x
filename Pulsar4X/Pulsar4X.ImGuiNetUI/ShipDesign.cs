using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.ComponentFeatureSets.Damage;

namespace Pulsar4X.SDL2UI
{
    public class ShipDesignUI : PulsarGuiWindow
    {
        private ComponentDesign[] _componentDesigns;
        private string[] _componentNames;
        private int _selectedDesignsIndex;


        private string[] _shipComponentNames;
        private int _selectedShipIndex;
 
        List<(ComponentDesign design, int count)> _shipComponents = new List<(ComponentDesign design, int count)>();
        
        private EntityDamageProfileDB _profile;
        private RawBmp _rawShipImage;
        private IntPtr _shipImgPtr;
        
        //TODO: armor, temporary, maybe density should be an "equvelent" and have a different mass? (damage calcs use density for penetration)
        List<(string name, double density)> _armorSelection = new List<(string name, double density)>();
        private string[] _armorNames;
        private int _armorIndex = 0;
        private double _armorThickness = 10;
        private (string name, double density, float thickness) _armor = ("Polyprop", 1175f, 10);
        private ShipDesignUI()
        {
            _flags = ImGuiWindowFlags.NoCollapse;
            _componentDesigns = _state.Faction.GetDataBlob<FactionInfoDB>().ComponentDesigns.Values.ToArray();
            _componentNames = new string[_componentDesigns.Length];
            for (int i = 0; i < _componentDesigns.Length; i++)
            {
                _componentNames[i] = _componentDesigns[i].Name;
            }
            
            _armorSelection.Add(("None", 0)    );
            _armorSelection.Add(("Polyprop", 1175f)    );
            _armorSelection.Add(("Aluminium", 2700f)    );
            _armorSelection.Add(("Titanium", 4540f)    );
            _armorSelection.Add(("SteelCarbon", 7860)    );
            _armorSelection.Add(("SteelStainless", 7900)    );

            _armorNames = new string[_armorSelection.Count];
            for (int i = 0; i < _armorSelection.Count; i++)
            {
                _armorNames[i]= _armorSelection[i].name;
            }
            

            
        }

        internal static ShipDesignUI GetInstance()
        {
            ShipDesignUI thisitem;
            if (!_state.LoadedWindows.ContainsKey(typeof(ShipDesignUI)))
            {
                thisitem = new ShipDesignUI();
            }
            else
                thisitem = (ShipDesignUI)_state.LoadedWindows[typeof(ShipDesignUI)];


            return thisitem;
        }


        internal override void Display()
        {
            if (IsActive && ImGui.Begin("Ship Design", ref IsActive, _flags))
            {
                var designChanged = false;
                //ImGui.BeginChild("Component Selection");
                
                ImGui.Columns(2);
                ImGui.BeginChild("ComponentSelection");
                //ImGui.BeginGroup();
                ImGui.Columns(3);
                
                //ImGui.ListBox("##Components", ref _selectedDesignsIndex, _componentNames, _componentNames.Length);

                
                ImGui.Text("Component");
                ImGui.NextColumn();
                ImGui.Text("Mass");
                ImGui.NextColumn();
                ImGui.Text("Volume");
                ImGui.NextColumn();
                ImGui.Separator();
                
                for (int i = 0; i < _componentDesigns.Length; i++)
                {
                    var design = _componentDesigns[i];
                    string name = design.Name;
                    
                    if (ImGui.Selectable(name, _selectedDesignsIndex == i, ImGuiSelectableFlags.SpanAllColumns))
                    {
                        _selectedDesignsIndex = i;
                    }
                    ImGui.NextColumn();
                    ImGui.Text(design.Mass.ToString());
                    ImGui.NextColumn();
                    ImGui.Text(design.Volume.ToString());
                    ImGui.NextColumn();
                    
                }
                var selectedComponent = _componentDesigns[_selectedDesignsIndex];
                if (ImGui.Button("Add"))
                {
                    _shipComponents.Add((selectedComponent, 1));
                    designChanged = true;
                }
                
                ImGui.Separator();
                ImGui.EndChild();

                ImGui.NextColumn();

                ImGui.Text(selectedComponent.Name);
                ImGui.Text(selectedComponent.Description);


                
                
                
                
                
                ImGui.BeginChild("ShipDesign");
                
                ImGui.Columns(2, "Ship Components", true);
                ImGui.Text("Component");
                ImGui.NextColumn();
                ImGui.Text("Count"); ImGui.NextColumn();
                ImGui.Separator();
                int selectedItem = -1;
                for (int i = 0; i < _shipComponents.Count; i++)
                {
                    string name = _shipComponents[i].design.Name;
                    int number = _shipComponents[i].count;
                    
                    /*
                    if (ImGui.Selectable(name, selectedItem == i, ImGuiSelectableFlags.SpanAllColumns))
                    {
                        selectedItem = i;
                    }
                    */
                    ImGui.Text(name);
                    
                    bool hovered = ImGui.IsItemHovered();
                    if (hovered)
                        selectedItem = i;
                    
                    ImGui.NextColumn();
                    ImGui.Text(number.ToString());
                    
                    //if (hovered)
                    //{
                        ImGui.SameLine();
                        if (ImGui.SmallButton("+##" + i)) //todo: imagebutton
                        {
                            _shipComponents[i] = (_shipComponents[i].design, _shipComponents[i].count + 1);
                            designChanged = true;
                        }
                        ImGui.SameLine();
                        if (ImGui.SmallButton("-##" + i) && number > 0) //todo: imagebutton
                        {
                            _shipComponents[i] = (_shipComponents[i].design, _shipComponents[i].count - 1);
                            designChanged = true;
                        }
                        ImGui.SameLine();
                        if (ImGui.SmallButton("x##" + i)) //todo: imagebutton
                        {
                            _shipComponents.RemoveAt(i);
                            designChanged = true;
                        }

                        if (i > 0)
                        {
                            ImGui.SameLine();
                            if (ImGui.SmallButton("^##" + i)) //todo: imagebutton
                            {

                                (ComponentDesign design, int count) item = _shipComponents[i];
                                _shipComponents.RemoveAt(i);
                                _shipComponents.Insert(i - 1, item);

                                designChanged = true;
                            }
                        }


                        if (_shipComponents.Count <= i)
                        {
                            ImGui.SameLine();
                            if (ImGui.SmallButton("V##" + i)) //todo: imagebutton
                            {
                                (ComponentDesign design, int count) item = _shipComponents[i];
                                _shipComponents.RemoveAt(i);
                                _shipComponents.Insert(i + 1, item);
                                designChanged = true;
                            }
                        }

                        //}


                    ImGui.NextColumn();
                    
                }
                ImGui.Separator();
                //ImGui.BeginChild("armorchild");
                //ImGui.Columns(2);
                ImGui.Text("Armor: Density");
                ImGui.NextColumn(); 
                ImGui.Text("Thickness ");
                ImGui.NextColumn();
                if (ImGui.Combo("##Armor Selection", ref _armorIndex, _armorNames, 6))
                {
                    designChanged = true;
                }
                ImGui.SameLine();
                ImGui.Text(_armorSelection[_armorIndex].density.ToString());
                //ImGui.EndChild();
                ImGui.NextColumn();
                ImGui.Text(_armorThickness.ToString());
                
                ImGui.SameLine();
                if (ImGui.SmallButton("+##armor")) //todo: imagebutton
                {
                    _armorThickness++;
                    designChanged = true;
                }
                ImGui.SameLine();
                if (ImGui.SmallButton("-##armor" ) && _armorThickness > 0) //todo: imagebutton
                {
                    _armorThickness--;
                    designChanged = true;
                }
 
                ImGui.NextColumn();
                
                ImGui.Separator();
                //ImGui.EndChild();

                if (designChanged)
                {
                    _profile = ComponentPlacement.CreateDamageProfileDB(_shipComponents, _armor);
                    _rawShipImage = _profile.ShipDamageProfile;
                    
                    _shipImgPtr = SDL2Helper.CreateSDLTexture(_state.rendererPtr, _rawShipImage);
                }

                if (_shipImgPtr != IntPtr.Zero)
                {
                    
                    float maxwidth = ImGui.GetColumnWidth();;// ImGui.GetWindowWidth();
                    float maxheigh = 512;//ImGui.GetWindowHeight() / 2;
                    int w = _rawShipImage.Width; 
                    int h = _rawShipImage.Height;
                    float scalew = 1;
                    float scaleh = 1;
                    float scale = 1;
                    if (w > maxwidth)
                    {
                        scalew = maxwidth / w;
                    }

                    if (h > maxheigh)
                    {
                        scaleh = maxheigh / h;
                    }

                    scale = Math.Max(scaleh, scalew);
                    
                    ImGui.Image(_shipImgPtr, new System.Numerics.Vector2(w * scale, h * scale));
                }
                ImGui.NextColumn();
                
                ImGui.Text("Ship Stats");
                
                
                
                ImGui.Columns(1);



            }

        }
    }
}