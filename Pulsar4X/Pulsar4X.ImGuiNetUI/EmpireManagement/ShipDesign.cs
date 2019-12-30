using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Printing;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.ComponentFeatureSets.Damage;


namespace Pulsar4X.SDL2UI
{
    public class ShipDesignUI : PulsarGuiWindow
    {
        private byte[] _designName =  ImGuiSDL2CSHelper.BytesFromString("foo", 32);

        private string[] _exsistingDesigns;
        private List<ShipClass> _exsistingClasses;
        private int _selectedDesign = -1;
        
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
        
        private float _firstChildHeight = 350;

        private double _massDry;
        private double _massWet;
        private double _ttw;
        private double _dv;
        private double _wspd;
        private double _wcc;
        private double _wsc;
        private double _wec;
        private double _tn;
        private double _estor;
        private double _egen;
        private double _fuelStore;
        
        private ShipDesignUI()
        {
            _flags = ImGuiWindowFlags.NoCollapse;

            RefreshComponentDesigns();
            
            //TODO: this is temporary armor info, needs to be added to the game proper
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


            _exsistingClasses = _state.Faction.GetDataBlob<FactionInfoDB>().ShipDesigns.Values.ToList();
            _state.Game.GameLoop.GameGlobalDateChangedEvent += GameLoopOnGameGlobalDateChangedEvent;
        }

        private void GameLoopOnGameGlobalDateChangedEvent(DateTime newdate)
        {
            RefreshComponentDesigns();
            _exsistingClasses = _state.Faction.GetDataBlob<FactionInfoDB>().ShipDesigns.Values.ToList();
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

        void RefreshComponentDesigns()
        {
            _componentDesigns = _state.Faction.GetDataBlob<FactionInfoDB>().ComponentDesigns.Values.ToArray();
            _componentNames = new string[_componentDesigns.Length];
            for (int i = 0; i < _componentDesigns.Length; i++)
            {
                _componentNames[i] = _componentDesigns[i].Name;
            }
        }

        internal override void Display()
        {
            if (IsActive && ImGui.Begin("Ship Design", ref IsActive, _flags))
            {
                var designChanged = false;
                
                
                ImGui.Columns(4);
                ImGui.SetColumnWidth(0, 200);
                ImGui.SetColumnWidth(1, 350);
                ImGui.SetColumnWidth(2, 278);
                ImGui.SetColumnWidth(3, 278);
                if (ImGui.CollapsingHeader("Exsisting Designs"))
                {
                    ImGui.BeginChild("exsistingdesigns", new Vector2(200, _firstChildHeight));

                    for (int i = 0; i < _exsistingClasses.Count; i++)
                    {

                        string name = _exsistingClasses[i].Name;
                        if(ImGui.Selectable(name))
                        {
                            _selectedDesign = i;
                            _designName = ImGuiSDL2CSHelper.BytesFromString(_exsistingClasses[i].Name, 32);
                            _shipComponents = _exsistingClasses[i].Components;
                            _armor = _exsistingClasses[i].Armor;
                            _profile = new EntityDamageProfileDB(_shipComponents, _armor);
                            _rawShipImage = _profile.DamageProfile;
                            _shipImgPtr = SDL2Helper.CreateSDLTexture(_state.rendererPtr, _rawShipImage);

                            _armorNames.Contains(_armor.name);
                            _armorIndex = _armorSelection.FindIndex(foo => foo.name.Equals(_armor.name));
                            designChanged = true;
                        }
                    }
                    
                    ImGui.EndChild();
                }
                
                ImGui.NextColumn();
                
                ImGui.BeginChild( "ComponentSelection", new Vector2(350, _firstChildHeight));
                //ImGui.BeginGroup();
                ImGui.Columns(3);
                ImGui.SetColumnWidth(0, 150);
                ImGui.SetColumnWidth(1, 100);
                ImGui.SetColumnWidth(2, 100);
                
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
                
                ImGui.Columns(2);
                ImGui.Separator();
                ImGui.SetColumnWidth(0, 250);
                ImGui.SetColumnWidth(1, 50);
                var selectedComponent = _componentDesigns[_selectedDesignsIndex];
                
                ImGui.Text(selectedComponent.Name);
                ImGui.NextColumn();
                if (ImGui.Button("Add"))
                {
                    _shipComponents.Add((selectedComponent, 1));
                    designChanged = true;
                }
                ImGui.Columns(1);
                ImGui.NextColumn();
                ImGui.Text(selectedComponent.Description);
                
                //ImGui.Text();
                
                
                ImGui.EndChild();

                
                ImGui.NextColumn();


                ImGui.BeginChild("ShipDesign", new Vector2(280, _firstChildHeight));
                
                ImGui.Columns(2, "Ship Components", true);
                ImGui.SetColumnWidth(0, 150);
                ImGui.SetColumnWidth(1, 128);
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
                        if (i < _shipComponents.Count -1 )
                        {
                            ImGui.SameLine();
                            if (ImGui.SmallButton("v##" + i)) //todo: imagebutton
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
                ImGui.EndChild();

                ImGui.NextColumn();
                
                ImGui.BeginChild("Ship Stats", new Vector2(278, _firstChildHeight));
                
                ImGui.Columns(1);
                ImGui.Text("Ship Stats");
                
                ImGui.InputText("Design Name", _designName, (uint)_designName.Length);
                ImGui.Text("Mass: " + _massDry + " kg");
                ImGui.Text("Total Thrust: " + (_tn * 0.01) + " kN");
                ImGui.Text("Thrust To Weight: " + _ttw);
                ImGui.Text("Fuel Capacity: " + _fuelStore);
                
                ImGui.Text("Delta V: " + _dv);
                ImGui.Text("Warp Speed:" + _wspd + "m/s");
                ImGui.Text("Warp Bubble Creation: " + _wcc);
                ImGui.Text("Warp Bubble Sustain: " + _wsc);
                ImGui.Text("Warp Bubble Collapse: " + _wec);
                ImGui.Text("Energy Output: " + _egen);
                ImGui.Text("Energy Store:" + _estor);
                
                
                ImGui.Separator();
                
                ImGui.EndChild();
                
                ImGui.NextColumn();
                ImGui.Separator();
                //ImGui.EndChild();
                ImGui.Columns(1);
                
                if (designChanged)
                {
                    _profile = new EntityDamageProfileDB(_shipComponents, _armor);
                    _rawShipImage = _profile.DamageProfile;
                    
                    _shipImgPtr = SDL2Helper.CreateSDLTexture(_state.rendererPtr, _rawShipImage);

                    double mass = 0;
                    double fu = 0;
                    double tn = 0;
                    double ev = 0;

                    double wp = 0;
                    double wcc = 0;
                    double wsc = 0;
                    double wec = 0;
                    double egen = 0;
                    double estor = 0;
                    Guid thrusterFuel = Guid.Empty;
                    Dictionary<Guid, double> cstore = new Dictionary<Guid, double>();
                    
                    foreach (var component in _shipComponents)
                    {
                        mass += component.design.Mass * component.count;
                        if (component.design.HasAttribute<NewtonionThrustAtb>())
                        {
                            var atb = component.design.GetAttribute<NewtonionThrustAtb>();
                            ev = atb.ExhaustVelocity;
                            fu += atb.FuelBurnRate * component.count;
                            tn += ev * atb.FuelBurnRate * component.count;
                            thrusterFuel = atb.FuelType;
                        }

                        if (component.design.HasAttribute<WarpDriveAtb>())
                        {
                            var atb = component.design.GetAttribute<WarpDriveAtb>();
                             wp += atb.WarpPower * component.count;
                             wcc += atb.BubbleCreationCost * component.count;
                             wsc += atb.BubbleSustainCost * component.count;
                             wec += atb.BubbleCollapseCost * component.count;

                        }

                        if (component.design.HasAttribute<EnergyGenerationAtb>())
                        {
                            var atb = component.design.GetAttribute<EnergyGenerationAtb>();
                            egen += atb.PowerOutputMax * component.count;
                            
                        }

                        if (component.design.HasAttribute<EnergyStoreAtb>())
                        {
                            var atb = component.design.GetAttribute<EnergyStoreAtb>();
                            estor += atb.MaxStore * component.count;
                        }

                        if (component.design.HasAttribute<CargoStorageAtbDB>())
                        {
                            var atb = component.design.GetAttribute<CargoStorageAtbDB>();
                            var typeid = atb.CargoTypeGuid;
                            var amount = atb.StorageCapacity * component.count;
                            if (!cstore.ContainsKey(typeid))
                                cstore.Add(typeid, amount);
                            else
                                cstore[typeid] += amount;

                        }
                    }

                    _massDry = mass;
                    _tn = tn;
                    _ttw = tn / mass;
                    _wcc = wcc;
                    _wec = wec;
                    _wsc = wsc;
                    _wspd = ShipMovementProcessor.MaxSpeedCalc(wp, mass);
                    _egen = egen;
                    _estor = estor;
                    //double fuelMass = 0;
                    if (thrusterFuel != Guid.Empty)
                    {
                        var fuel = StaticRefLib.StaticData.GetICargoable(thrusterFuel);
                        if(cstore.ContainsKey(fuel.CargoTypeID))
                            _fuelStore = cstore[fuel.CargoTypeID];
                    }

                    _massWet = _massDry + _fuelStore;
                    _dv = OrbitMath.TsiolkovskyRocketEquation(_massWet, _massDry, ev);

                }

                if (_shipImgPtr != IntPtr.Zero)
                {
                    
                    float maxwidth = ImGui.GetWindowWidth();// ImGui.GetColumnWidth();;// 
                    float maxheight = 256;//ImGui.GetWindowHeight() / 2;
                    int w = _rawShipImage.Width; 
                    int h = _rawShipImage.Height;
                    float scalew = 1;
                    float scaleh = 1;
                    float scale = 1;
                    if (w > maxwidth)
                    {
                        scalew = maxwidth / w;
                    }

                    if (h > maxheight)
                    {
                        scaleh = maxheight / h;
                    }

                    scale = Math.Min(scaleh, scalew);
                    
                    ImGui.Image(_shipImgPtr, new Vector2(w * scale, h * scale));
                }
                //ImGui.NextColumn();
                

                if(ImGui.Button("Create Design"))
                {
                    int version = 0;
                    var strName = ImGuiSDL2CSHelper.StringFromBytes(_designName);
                    foreach (var shipclass in _exsistingClasses)
                    {
                        if (shipclass.Name == strName)
                        {
                            if (shipclass.DesignVersion >= version)
                                version = shipclass.DesignVersion + 1;
                        }
                    }
                    ShipClass shipClass = new ShipClass(_state.Faction.GetDataBlob<FactionInfoDB>(), strName, _shipComponents, _armor);
                    shipClass.DesignVersion = version;

                }
            }
        }
    }
}