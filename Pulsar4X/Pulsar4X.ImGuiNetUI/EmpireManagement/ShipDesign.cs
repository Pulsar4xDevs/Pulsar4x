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
        private List<ShipDesign> _exsistingClasses;
        private int _selectedDesign = -1;
        bool _imagecreated = false;
        
        private ComponentDesign[] _componentDesigns;
        private string[] _componentNames;
        private int _selectedDesignsIndex;
        
        private string[] _shipComponentNames;
        private int _selectedShipIndex;
 
        List<(ComponentDesign design, int count)> _shipComponents = new List<(ComponentDesign design, int count)>();
        
        private IntPtr _shipImgPtr;
        
        //TODO: armor, temporary, maybe density should be an "equvelent" and have a different mass? (damage calcs use density for penetration)
        List<ArmorSD> _armorSelection = new List<ArmorSD>();
        private string[] _armorNames;
        private int _armorIndex = 0;
        private float _armorThickness = 10;
        private ArmorSD _armor;

        private int rawimagewidth;
        private int rawimageheight;

        private double _massDry;
        private double _massWet;
        private double _ttwr;
        private double _dv;
        private double _wspd;
        private double _wcc;
        private double _wsc;
        private double _wec;
        private double _tn;
        private double _estor;
        private double _egen;
        private double _fuelStore;
        bool displayimage = true;

        private bool existingdesignsstatus = true;
        bool designChanged = false;

        private ShipDesignUI()
        {
            //_flags = ImGuiWindowFlags.NoCollapse;


            RefreshComponentDesigns();

            _armorNames = new string[StaticRefLib.StaticData.ArmorTypes.Count];
            int i = 0;
            foreach (var kvp in StaticRefLib.StaticData.ArmorTypes)
            {
                var armorMat = _uiState.Game.StaticData.GetICargoable(kvp.Key);
                _armorSelection.Add(kvp.Value);
                
                _armorNames[i]= armorMat.Name;
                i++;
            }
            
            _exsistingClasses = _uiState.Faction.GetDataBlob<FactionInfoDB>().ShipDesigns.Values.ToList();
        }

        public override void OnSystemTickChange(DateTime newDateTime)
        {
            RefreshComponentDesigns();
            _exsistingClasses = _uiState.Faction.GetDataBlob<FactionInfoDB>().ShipDesigns.Values.ToList();
            //TODO: bleed over from mod data to get a default armor...
            _armor = StaticRefLib.StaticData.ArmorTypes[new Guid("207af637-95a0-4b89-ac4a-6d66a81cfb2f")];
            _armorThickness = 3;
        }

        internal static ShipDesignUI GetInstance()
        {
            ShipDesignUI thisitem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(ShipDesignUI)))
            {
                thisitem = new ShipDesignUI();
            }
            else
                thisitem = (ShipDesignUI)_uiState.LoadedWindows[typeof(ShipDesignUI)];
            
            return thisitem;
        }

        void RefreshComponentDesigns()
        {
            _componentDesigns = _uiState.Faction.GetDataBlob<FactionInfoDB>().ComponentDesigns.Values.ToArray();
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

                

                if(_exsistingClasses.Count != _uiState.Faction.GetDataBlob<FactionInfoDB>().ShipDesigns.Values.ToList().Count)
                {
                    _exsistingClasses = _uiState.Faction.GetDataBlob<FactionInfoDB>().ShipDesigns.Values.ToList();
                }
                if (_componentDesigns.Length != _uiState.Faction.GetDataBlob<FactionInfoDB>().ComponentDesigns.Values.ToArray().Length)
                {
                    RefreshComponentDesigns();
                }

                

                float imageheight = ImGui.GetContentRegionAvail().Y / 3;
                float height = ImGui.GetContentRegionAvail().Y - imageheight;
                float partlistwidth = 350;
                float shortwindowwidth = ImGui.GetContentRegionAvail().X - partlistwidth;
                bool compactimage = CheckDisplayImage(1, imageheight, shortwindowwidth);

                if (compactimage || !displayimage)
                {
                    ImGui.BeginChild("ShipDesign", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y));
                    imageheight = ImGui.GetContentRegionAvail().Y / 2;
                    height = ImGui.GetContentRegionAvail().Y - imageheight;
                }
                else
                {
                    ImGui.BeginChild("ShipDesign", new Vector2(ImGui.GetContentRegionAvail().X, height));
                }

                //imageheight = imageheight * 0.9f;
                ImGui.Columns(2);
                ImGui.SetColumnWidth(0, shortwindowwidth);
                //ImGui.SetColumnWidth(1, 350);
                //ImGui.SetColumnWidth(2, 278);
                //ImGui.SetColumnWidth(3, 278);
                    if (compactimage)
                    {
                        ImGui.BeginChild("Small Design Windows", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y- imageheight));
                    }
                    else
                    {
                        ImGui.BeginChild("Small Design Windows");
                    }
                    
                    ImGui.Columns(3);
                        DisplayShips();             
                
                        ImGui.NextColumn();

                        DisplayStats();

                        ImGui.NextColumn();

                        DisplayComponents();
                    ImGui.EndChild();

                    if (compactimage)
                    {
                        DisplayImage(shortwindowwidth, imageheight);
                    }

                    ImGui.NextColumn();
                    
                    DisplayComponentSelection();

                    ImGui.NextColumn();
                ImGui.EndChild();

                if(!compactimage && displayimage)
                {
                    ImGui.Separator();
                    ImGui.Columns(1);
                    DisplayImage(ImGui.GetWindowWidth(), imageheight);
                }
                
            }
        }

        internal void NewShipButton()
        {
            if (ImGui.Button("Create Design"))
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
                ShipDesign shipDesign = new ShipDesign(_uiState.Faction.GetDataBlob<FactionInfoDB>(), strName, _shipComponents, (_armor, _armorThickness));
                shipDesign.DesignVersion = version;

            }
        }

        internal void DisplayShips()
        {
            if (ImGui.CollapsingHeader("Exsisting Designs", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.BeginChild("exsistingdesigns");

                for (int i = 0; i < _exsistingClasses.Count; i++)
                {

                    string name = _exsistingClasses[i].Name;
                    if (ImGui.Selectable(name))
                    {
                        _selectedDesign = i;
                        _designName = ImGuiSDL2CSHelper.BytesFromString(_exsistingClasses[i].Name, 32);
                        _shipComponents = _exsistingClasses[i].Components;
                        _armor = _exsistingClasses[i].Armor.type;
                        GenImage();
                        _armorIndex = _armorSelection.IndexOf(_armor);
                        _armorThickness = _exsistingClasses[i].Armor.thickness;
                        designChanged = true;
                    }
                }

                ImGui.EndChild();
            }
        }

        internal void DisplayComponents()
        {
            ImGui.BeginChild("ShipDesign");

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
                if (i < _shipComponents.Count - 1)
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
            if (ImGui.Combo("##Armor Selection", ref _armorIndex, _armorNames, _armorNames.Length))
            {
                _armor = _armorSelection[_armorIndex];
                designChanged = true;
            }
            ImGui.SameLine();
            ImGui.Text(_armorSelection[_armorIndex].Density.ToString());
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
            if (ImGui.SmallButton("-##armor") && _armorThickness > 0) //todo: imagebutton
            {
                _armorThickness--;
                designChanged = true;
            }

            ImGui.NextColumn();
            ImGui.EndChild();
        }

        internal void DisplayComponentSelection()
        {
            ImGui.BeginChild("ComponentSelection");
            //ImGui.BeginGroup();
            ImGui.Columns(3);
            ImGui.SetColumnWidth(0, 150);
            ImGui.SetColumnWidth(1, 100);
            ImGui.SetColumnWidth(2, 100);

            ImGui.Text("Component");
            ImGui.NextColumn();
            ImGui.Text("Mass");
            ImGui.NextColumn();
            ImGui.Text("Volume_km3");
            ImGui.NextColumn();
            ImGui.Separator();

            for (int i = 0; i < _componentDesigns.Length; i++)
            {
                var design = _componentDesigns[i];
                string name = design.Name;

                if (ImGui.Selectable(name, _selectedDesignsIndex == i, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick))
                {
                    _selectedDesignsIndex = i;
                    if (ImGui.IsMouseDoubleClicked(0))
                    {
                        _shipComponents.Add((_componentDesigns[_selectedDesignsIndex], 1));
                        designChanged = true;
                    }
                }

                ImGui.NextColumn();
                ImGui.Text(design.Mass.ToString());
                ImGui.NextColumn();
                ImGui.Text(design.Volume_m3.ToString());
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
            ImGui.Text(selectedComponent.Description ?? "");

            //ImGui.Text();


            ImGui.EndChild();
        }

        internal void GenImage()
        {
            EntityDamageProfileDB _profile = new EntityDamageProfileDB(_shipComponents, (_armor, _armorThickness));
            _shipImgPtr = SDL2Helper.CreateSDLTexture(_uiState.rendererPtr, _profile.DamageProfile, _imagecreated);
            rawimagewidth = _profile.DamageProfile.Width;
            rawimageheight = _profile.DamageProfile.Height;
            _imagecreated = true;
        }

        internal void DisplayStats()
        {
            

            ImGui.BeginChild("Ship Stats");

            ImGui.Columns(1);

            ImGui.InputText("Design Name", _designName, (uint)_designName.Length);
            NewShipButton();
            ImGui.SameLine();
            ImGui.Checkbox("Show Pic", ref displayimage);
            ImGui.NewLine();

            

            ImGui.Text("Ship Stats");
            if (designChanged)
            {
                if(displayimage)
                {
                    GenImage();
                }

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
                _ttwr = (tn / mass) * 0.01;
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
                    if (cstore.ContainsKey(fuel.CargoTypeID))
                        _fuelStore = cstore[fuel.CargoTypeID];
                }

                _massWet = _massDry + _fuelStore;
                _dv = OrbitMath.TsiolkovskyRocketEquation(_massWet, _massDry, ev);

            }
            designChanged = false;
            ImGui.Text("Mass: " + Stringify.Mass(_massDry));
            ImGui.Text("Total Thrust: " + Stringify.Thrust(_tn));
            ImGui.Text("Thrust To Mass Ratio: " + _ttwr);
            ImGui.Text("Fuel Capacity: " + Stringify.Mass(_fuelStore));

            ImGui.Text("Delta V: " + Stringify.Velocity(_dv));
            ImGui.Text("Warp Speed:" + Stringify.Velocity(_wspd));
            ImGui.Text("Warp Bubble Creation: " + _wcc);
            ImGui.Text("Warp Bubble Sustain: " + _wsc);
            ImGui.Text("Warp Bubble Collapse: " + _wec);
            ImGui.Text("Energy Output: " + _egen);
            ImGui.Text("Energy Store:" + _estor);


            ImGui.Separator();

            ImGui.EndChild();

        }

        internal bool CheckDisplayImage(float maxwidth, float maxheight, float checkwidth)
        {
            if (_shipImgPtr != IntPtr.Zero && displayimage)
            {

                maxwidth = ImGui.GetWindowWidth();// ImGui.GetColumnWidth();;// 
                int maxheightint = (int)(maxheight / 4);
                maxheight = maxheightint * 4;//ImGui.GetWindowHeight() * _imageratio;
                float scalew = 1;
                float scaleh = 1;
                float scale;
                scalew = maxwidth / rawimagewidth;
                scaleh = maxheight / rawimageheight;

                scale = Math.Min(scaleh, scalew);

                if (rawimagewidth * scale < checkwidth)
                {
                    return true;
                }
            }
            return false;
        }

        internal void DisplayImage(float maxwidth, float maxheight)
        {
            if (_shipImgPtr != IntPtr.Zero && displayimage)
            {
                int maxheightint = (int)(maxheight / 4);
                maxheight = maxheightint*4;//ImGui.GetWindowHeight() * _imageratio;
                float scalew = 1;
                float scaleh = 1;
                float scale;

                scalew = maxwidth / rawimagewidth;
                scaleh = maxheight / rawimageheight;

                scale = Math.Min(scaleh, scalew);

                ImGui.Image(_shipImgPtr, new Vector2(rawimagewidth * scale, rawimageheight * scale));
            }
        }
    }
}