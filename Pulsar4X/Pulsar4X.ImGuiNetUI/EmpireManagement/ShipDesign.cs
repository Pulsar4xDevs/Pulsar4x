using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;


namespace Pulsar4X.SDL2UI
{
    public class ShipDesignUI : PulsarGuiWindow
    {
        private byte[] _designName =  ImGuiSDL2CSHelper.BytesFromString("foo", 32);

        private string[] _exsistingDesigns;
        private List<ShipDesign> _exsistingClasses;
        private int _selectedDesign = -1;
        bool _imagecreated = false;
        
        private List<ComponentDesign> _componentDesigns;
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
            RefreshArmor();
            RefreshExistingClasses();
        }

        public override void OnSystemTickChange(DateTime newDateTime)
        {
            RefreshComponentDesigns();
            RefreshExistingClasses();
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
            _componentDesigns = _uiState.Faction.GetDataBlob<FactionInfoDB>().ComponentDesigns.Values.ToList();
            _componentDesigns.Sort((a, b) => a.Name.CompareTo(b.Name));
        }

        void RefreshExistingClasses()
        {
            _exsistingClasses = _uiState.Faction.GetDataBlob<FactionInfoDB>().ShipDesigns.Values.ToList();
            _exsistingClasses.Sort((a, b) => a.Name.CompareTo(b.Name));
        }

        void RefreshArmor()
        {
            _armorNames = new string[StaticRefLib.StaticData.ArmorTypes.Count];
            int i = 0;
            foreach (var kvp in StaticRefLib.StaticData.ArmorTypes)
            {
                var armorMat = _uiState.Game.StaticData.GetICargoable(kvp.Key);
                _armorSelection.Add(kvp.Value);
                
                _armorNames[i]= armorMat.Name;
                i++;
            }
            //TODO: bleed over from mod data to get a default armor...
            _armor = StaticRefLib.StaticData.ArmorTypes[new Guid("207af637-95a0-4b89-ac4a-6d66a81cfb2f")];
            _armorThickness = 3;
        }

        internal override void Display()
        {
            if (IsActive && ImGui.Begin("Ship Design", ref IsActive, _flags))
            {
                if(_exsistingClasses.Count != _uiState.Faction.GetDataBlob<FactionInfoDB>().ShipDesigns.Values.ToList().Count)
                {
                    _exsistingClasses = _uiState.Faction.GetDataBlob<FactionInfoDB>().ShipDesigns.Values.ToList();
                }
                if (_componentDesigns.Count != _uiState.Faction.GetDataBlob<FactionInfoDB>().ComponentDesigns.Values.ToArray().Length)
                {
                    RefreshComponentDesigns();
                }

                DisplayExistingDesigns();
                ImGui.SameLine();
                ImGui.SetCursorPosY(27f);

                Vector2 windowContentSize = ImGui.GetContentRegionAvail();
                var firstChildSize = new Vector2(windowContentSize.X * 0.33f, windowContentSize.Y);
                var secondChildSize = new Vector2(windowContentSize.X * 0.33f, windowContentSize.Y);
                var thirdChildSize = new Vector2(windowContentSize.X * 0.33f - (windowContentSize.X * 0.01f), windowContentSize.Y);
                if(ImGui.BeginChild("ShipDesign1", firstChildSize, true))
                {
                    DisplayComponentSelection();
                    ImGui.EndChild();
                }
                ImGui.SameLine();
                if(ImGui.BeginChild("ShipDesign2", secondChildSize, true))
                {
                    DisplayComponents();
                    ImGui.EndChild();
                }
                ImGui.SameLine();
                if(ImGui.BeginChild("ShipDesign3", thirdChildSize, true))
                {
                    DisplayStats();
                    ImGui.EndChild();
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

        internal void DisplayExistingDesigns()
        {
            Vector2 windowContentSize = ImGui.GetContentRegionAvail();

            if(ImGui.BeginChild("ComponentDesignSelection", new Vector2(204f, windowContentSize.Y - 24f), true))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                ImGui.Text("Existing Designs");
                // ImGui.SameLine();
                // ImGui.Text("[?]");
                // if(ImGui.IsItemHovered())
                //     ImGui.SetTooltip("Component Templates act as a framework for designing components.\n\n" +
                //         "Select a template and then design the attributes of the component to your specification.\n" +
                //         "Once the design is created it will be available to produce on the colonies with the appropriate\n" +
                //         "installations.");
                ImGui.PopStyleColor();
                ImGui.Separator();

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

            if(ImGui.Button("Create New Design", new Vector2(204f, 0f)))
            {
                _selectedDesign = -1;
                _designName = new byte[32];
                _shipComponents = new List<(ComponentDesign design, int count)>();
                GenImage();
                RefreshArmor();
                designChanged = true;
            }
        }

        internal void DisplayComponents()
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
            ImGui.Text("Current Design");
            // ImGui.SameLine();
            // ImGui.Text("[?]");
            // if(ImGui.IsItemHovered())
            //     ImGui.SetTooltip("Component Templates act as a framework for designing components.\n\n" +
            //         "Select a template and then design the attributes of the component to your specification.\n" +
            //         "Once the design is created it will be available to produce on the colonies with the appropriate\n" +
            //         "installations.");
            ImGui.PopStyleColor();
            ImGui.Separator();

            if(ImGui.BeginTable("CurrentShipDesignTable", 3, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 1.5f);
                ImGui.TableSetupColumn("Amount", ImGuiTableColumnFlags.None, 1f);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.None, 1f);
                ImGui.TableHeadersRow();

                int selectedItem = -1;
                for (int i = 0; i < _shipComponents.Count; i++)
                {
                    string name = _shipComponents[i].design.Name;
                    int number = _shipComponents[i].count;

                    ImGui.TableNextColumn();
                    ImGui.Text(name);

                    bool hovered = ImGui.IsItemHovered();
                    if (hovered)
                        selectedItem = i;

                    ImGui.TableNextColumn();
                    ImGui.Text(number.ToString());

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
                    ImGui.TableNextColumn();
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
                }

                ImGui.EndTable();
            }

            ImGui.NewLine();
            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
            ImGui.Text("Armor");
            // ImGui.SameLine();
            // ImGui.Text("[?]");
            // if(ImGui.IsItemHovered())
            //     ImGui.SetTooltip("Component Templates act as a framework for designing components.\n\n" +
            //         "Select a template and then design the attributes of the component to your specification.\n" +
            //         "Once the design is created it will be available to produce on the colonies with the appropriate\n" +
            //         "installations.");
            ImGui.PopStyleColor();
            ImGui.Separator();
            if(ImGui.BeginTable("CurrentShipDesignTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Attribute", ImGuiTableColumnFlags.None, 1.5f);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.None, 1f);
                ImGui.TableHeadersRow();

                ImGui.TableNextColumn();
                ImGui.Text("Type");
                ImGui.TableNextColumn();
                if (ImGui.Combo("##Armor Selection", ref _armorIndex, _armorNames, _armorNames.Length))
                {
                    _armor = _armorSelection[_armorIndex];
                    designChanged = true;
                }

                ImGui.TableNextColumn();
                ImGui.Text("Density");
                ImGui.TableNextColumn();
                ImGui.Text(_armorSelection[_armorIndex].Density.ToString());

                ImGui.TableNextColumn();
                ImGui.Text("Thickness");
                ImGui.TableNextColumn();
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

                ImGui.EndTable();
            }
        }

        internal void DisplayComponentSelection()
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
            ImGui.Text("Available Components");
            // ImGui.SameLine();
            // ImGui.Text("[?]");
            // if(ImGui.IsItemHovered())
            //     ImGui.SetTooltip("Component Templates act as a framework for designing components.\n\n" +
            //         "Select a template and then design the attributes of the component to your specification.\n" +
            //         "Once the design is created it will be available to produce on the colonies with the appropriate\n" +
            //         "installations.");
            ImGui.PopStyleColor();
            ImGui.Separator();

            if(ImGui.BeginTable("DesignStatsTables", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 2f);
                ImGui.TableSetupColumn("Mass", ImGuiTableColumnFlags.None, 0.7f);
                ImGui.TableHeadersRow();

                for (int i = 0; i < _componentDesigns.Count; i++)
                {
                    if(!_componentDesigns[i].ComponentMountType.HasFlag(ComponentMountType.ShipComponent))
                        continue;

                    var design = _componentDesigns[i];
                    string name = design.Name;

                    ImGui.TableNextColumn();
                    if (ImGui.Selectable(name, _selectedDesignsIndex == i, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick))
                    {
                        _selectedDesignsIndex = i;
                        if (ImGui.IsMouseDoubleClicked(0))
                        {
                            _shipComponents.Add((_componentDesigns[_selectedDesignsIndex], 1));
                            designChanged = true;
                        }
                    }

                    ImGui.TableNextColumn();
                    ImGui.Text(Stringify.Mass(design.MassPerUnit));
                }

                ImGui.EndTable();
            }
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
            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
            ImGui.Text("Statistics");
            // ImGui.SameLine();
            // ImGui.Text("[?]");
            // if(ImGui.IsItemHovered())
            //     ImGui.SetTooltip("Component Templates act as a framework for designing components.\n\n" +
            //         "Select a template and then design the attributes of the component to your specification.\n" +
            //         "Once the design is created it will be available to produce on the colonies with the appropriate\n" +
            //         "installations.");
            ImGui.PopStyleColor();
            ImGui.Separator();

            UpdateShipStats();
            if(ImGui.BeginTable("DesignStatsTables", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Attribute", ImGuiTableColumnFlags.None);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.None);
                ImGui.TableHeadersRow();

                ImGui.TableNextColumn();
                ImGui.Text("Mass");
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Mass(_massDry));

                ImGui.TableNextColumn();
                ImGui.Text("Total Thrust");
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Thrust(_tn));

                ImGui.TableNextColumn();
                ImGui.Text("Thrust to Mass Ratio");
                ImGui.TableNextColumn();
                ImGui.Text(_ttwr.ToString("0.####"));

                ImGui.TableNextColumn();
                ImGui.Text("Fuel Capacity");
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Mass(_fuelStore));

                ImGui.TableNextColumn();
                ImGui.Text("Delta V");
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Velocity(_dv));

                ImGui.TableNextColumn();
                ImGui.Text("Warp Speed");
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Velocity(_wspd));

                ImGui.TableNextColumn();
                ImGui.Text("Warp Bubble Creation");
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Power(_wcc));

                ImGui.TableNextColumn();
                ImGui.Text("Warp Bubble Sustain");
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Power(_wsc));

                ImGui.TableNextColumn();
                ImGui.Text("Warp Bubble Collapse");
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Power(_wec));

                ImGui.TableNextColumn();
                ImGui.Text("Energy Output");
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Power(_egen));

                ImGui.TableNextColumn();
                ImGui.Text("Energy Storage");
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Energy(_estor));

                ImGui.EndTable();
            }

            ImGui.InputText("Design Name", _designName, (uint)_designName.Length);
            NewShipButton();
            ImGui.SameLine();
            ImGui.Checkbox("Show Pic", ref displayimage);
            ImGui.NewLine();

            var size = ImGui.GetContentRegionAvail();
            DisplayImage(size.X, size.Y);

            ImGui.EndChild();
        }

        private void UpdateShipStats()
        {
            if(!designChanged) return;

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
                mass += component.design.MassPerUnit * component.count;
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

                if (component.design.HasAttribute<VolumeStorageAtb>())
                {
                    var atb = component.design.GetAttribute<VolumeStorageAtb>();
                    var typeid = atb.StoreTypeID;
                    var amount = atb.MaxVolume * component.count;
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

            designChanged = false;
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

                ImGui.Image(_shipImgPtr, new System.Numerics.Vector2(rawimagewidth * scale, rawimageheight * scale));
            }
        }
    }
}