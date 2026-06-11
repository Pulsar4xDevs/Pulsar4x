using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Engine;
using Pulsar4X.Components;
using Pulsar4X.Blueprints;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Extensions;
using Pulsar4X.DataStructures;
using Pulsar4X.Energy;
using Pulsar4X.Factions;
using Pulsar4X.Damage;
using Pulsar4X.Ships;
using Pulsar4X.Storage;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using SDL3;

namespace Pulsar4X.Client
{
    public class ShipDesignWindow : PulsarGuiWindow
    {
        private bool ShowNoDesigns = false;
        private byte[] SelectedDesignName =  Utils.BytesFromString("foo", 32);
        private List<string> _existingShipDesignNames = new();
        private List<string> _existingShipDesignIDs = new();
        private string SelectedExistingDesignID = String.Empty;
        private ShipDesign _workingDesign;
        private bool SelectedDesignObsolete;
        bool _imagecreated = false;

        private List<ComponentDesign> AvailableShipComponents = new();
        private List<ComponentDesign> AllShipComponents = new();
        private static string[]? _sortedComponentNames;
        private int _componentFilterIndex = 0;

        List<(ComponentDesign design, int count)> SelectedComponents = new List<(ComponentDesign design, int count)>();

        private IntPtr _shipImgPtr;

        //TODO: armor, temporary, maybe density should be an "equvelent" and have a different mass? (damage calcs use density for penetration)
        List<ArmorBlueprint> _armorSelection = new List<ArmorBlueprint>();
        private string[]? _armorNames;
        private int _armorIndex = 0;
        private float _armorThickness = 10;
        private ArmorBlueprint? _armor;
        private double _armorMass = 0;

        private int rawimagewidth;
        private int rawimageheight;




        //energy
        private double _estor;
        private double _egen;

        //mass
        private double _massDry;
        private double _massWet;
        private double _grossTonnage;
        //warp
        private double _wcc;
        private double _wsc;
        private double _wec;
        private double _wspd;
        //newt
        private double _tn;
        private double _ttwr;
        private double _dv;
        //fuel
        private double _fuelStoreMass;
        private double _fuelStoreVolume;
        private ICargoable? _fuelType;
        //cargo
        private double _cvol = 0;
        private double _trnge = 0;
        private double _trate = 0;


        bool displayimage = true;
        private EntityDamageProfileDB? _profile;
        private bool existingdesignsstatus = true;
        bool DesignChanged = false;
        // Set when the server rejects a save; shown so a failed save isn't silent.
        private string? _saveError;
        // True while editing a not-yet-saved design (it has no server-side id), so the per-tick
        // refresh doesn't clobber it by auto-selecting the first existing design.
        private bool _editingNewDesign;

        private FactionInfoDB _factionInfoDB;

        private ShipDesignWindow()
        {
            //_flags = ImGuiWindowFlags.NoCollapse;
            // The interactive designer evaluates client-side against the faction's design-time data
            // (components, ship designs, armor) exposed by the adapter; writes go through commands.
            if (_uiState.GameClient is not IDesignDataProvider provider
                || !provider.TryGetDesignData(out _factionInfoDB!, out _))
                throw new NullReferenceException("The game client cannot provide design data");

            RefreshComponentDesigns();
            RefreshArmor();
            RefreshExistingClasses();
        }

        // ExecuteSynchronously: in-process the command completes inline, so the continuation (which
        // touches UI state) runs on the UI thread before the next frame.
        private void SubmitCommand(GameCommand command, Action<CommandResult>? onResult = null)
        {
            _uiState.GameClient?.SubmitCommandAsync(command).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully && onResult != null)
                    onResult(task.Result);
            }, System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously);
        }

        public override void OnSystemTickChange(DateTime newDateTime)
        {
            RefreshComponentDesigns();
            RefreshExistingClasses();
        }

        internal static ShipDesignWindow GetInstance()
        {
            ShipDesignWindow thisitem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(ShipDesignWindow)))
            {
                thisitem = new ShipDesignWindow();
                thisitem.RefreshComponentDesigns();
                thisitem.RefreshExistingClasses();
            }
            else
                thisitem = (ShipDesignWindow)_uiState.LoadedWindows[typeof(ShipDesignWindow)];

            return thisitem;
        }

        void RefreshComponentDesigns()
        {
            AllShipComponents = _factionInfoDB.ComponentDesigns.Values.ToList();
            AllShipComponents.Sort((a, b) => a.Name.CompareTo(b.Name));

            var templatesByGroup = AllShipComponents.GroupBy(t => t.ComponentType);
            var groupNames = templatesByGroup.Select(g => g.Key).ToList();
            var sortedTempGroupNames = groupNames.OrderBy(name => name).ToArray();
            _sortedComponentNames = new string[sortedTempGroupNames.Length + 1];
            _sortedComponentNames[0] = "All";
            Array.Copy(sortedTempGroupNames, 0, _sortedComponentNames, 1, sortedTempGroupNames.Length);

            if(_componentFilterIndex == 0)
            {
                AvailableShipComponents = new List<ComponentDesign>(AllShipComponents);
            }
            else
            {
                AvailableShipComponents = AllShipComponents.Where(t => t.ComponentType.Equals(_sortedComponentNames[_componentFilterIndex])).ToList();
            }
        }

        void RefreshExistingClasses()
        {
            var designs = _factionInfoDB.ShipDesigns.Values.Where(d => !d.IsObsolete).ToList();
            designs.Sort((a, b) => a.Name.CompareTo(b.Name));
            _existingShipDesignNames = new List<string>();
            _existingShipDesignIDs = new List<string>();
            foreach (var design in designs)
            {
                _existingShipDesignIDs.Add(design.UniqueID);
                _existingShipDesignNames.Add(design.Name);
            }

            if(_existingShipDesignNames.Count == 0)
            {
                ShowNoDesigns = true;
                return;
            }
            if(SelectedExistingDesignID.IsNullOrEmpty() && !_editingNewDesign && _existingShipDesignNames.Count > 0)
                Select(_factionInfoDB.ShipDesigns[_existingShipDesignIDs[0]]);

            ShowNoDesigns = false;
        }

        void RefreshArmor()
        {
            var factionData = _factionInfoDB.Data;
            _armorNames = new string[factionData.Armor.Count];
            int i = 0;
            foreach (var kvp in factionData.Armor)
            {
                var armorMat = factionData.CargoGoods.GetAny(kvp.Value.ResourceID);
                _armorSelection.Add(kvp.Value);

                _armorNames[i]= armorMat?.Name ?? "Unknown";
                i++;
            }
            //TODO: bleed over from mod data to get a default armor...
            _armor = factionData.Armor["plastic-armor"];
            _armorThickness = 3;
        }

        void Select(ShipDesign design)
        {
            // Edit a local clone; track the ORIGINAL id (the clone gets a fresh one) so the list
            // highlight matches and a save updates the right design server-side.
            _workingDesign = design.Clone(_factionInfoDB);
            SelectedExistingDesignID = design.UniqueID;
            _editingNewDesign = false;
            SelectedDesignName = Utils.BytesFromString(_workingDesign.Name, 32);
            SelectedComponents = _workingDesign.Components;
            SelectedDesignObsolete = _workingDesign.IsObsolete;
            _armor = _workingDesign.Armor.type;
            _armorIndex = _armorSelection.IndexOf(_armor);
            _armorThickness = _workingDesign.Armor.thickness;
            DesignChanged = true;
            UpdateShipStats();
        }

        internal override void Display()
        {
            if(!IsActive) return;

            if (Window.Begin("Ship Design", ref IsActive, _flags))
            {
                if(_existingShipDesignNames.Count != _factionInfoDB.ShipDesigns.Values.Count(d => !d.IsObsolete))
                {
                    RefreshExistingClasses();
                }
                if (AllShipComponents.Count != _factionInfoDB.ComponentDesigns.Values.Count)
                {
                    RefreshComponentDesigns();
                }

                DisplayExistingDesigns();
                ImGui.SameLine();
                ImGui.SetCursorPosY(27f);

                if(ShowNoDesigns)
                {
                    ImGui.Text("Create a new design to begin editing.");
                    return;
                }

                Vector2 windowContentSize = ImGui.GetContentRegionAvail();
                var firstChildSize = new Vector2(windowContentSize.X * 0.33f, windowContentSize.Y);
                var secondChildSize = new Vector2(windowContentSize.X * 0.33f, windowContentSize.Y);
                var thirdChildSize = new Vector2(windowContentSize.X * 0.33f - (windowContentSize.X * 0.01f), windowContentSize.Y);
                if(ImGui.BeginChild("ShipDesign1", firstChildSize, ImGuiChildFlags.Borders))
                {
                    DisplayComponentSelection();
                }
                ImGui.EndChild();
                ImGui.SameLine();
                ImGui.SetCursorPosY(27f);
                if(ImGui.BeginChild("ShipDesign2", secondChildSize, ImGuiChildFlags.Borders))
                {
                    DisplayComponents();
                }
                ImGui.EndChild();
                ImGui.SameLine();
                ImGui.SetCursorPosY(27f);
                if(ImGui.BeginChild("ShipDesign3", thirdChildSize, ImGuiChildFlags.Borders))
                {
                    DisplayStats();
                }
                ImGui.EndChild();
            }
            Window.End();
        }

        internal void NewShipButton()
        {
            if (ImGui.Button("Save Design"))
            {
                var name = Utils.StringFromBytes(SelectedDesignName);

                if(name.IsNotNullOrEmpty() && _armor != null)
                {
                    // The working design is a local edit buffer; the server resolves the referenced
                    // component/armor ids, recalculates, validates and registers the design.
                    _saveError = null;
                    bool wasObsolete = SelectedDesignObsolete;
                    string? designId = SelectedExistingDesignID.IsNullOrEmpty() ? null : SelectedExistingDesignID;
                    var components = SelectedComponents
                        .Select(c => new ShipComponentCount(c.design.UniqueID, c.count))
                        .ToList();

                    SubmitCommand(new SaveShipDesignCommand(
                        _uiState.GameClient!.Session.FactionId,
                        designId,
                        name,
                        components,
                        _armor.UniqueID,
                        _armorThickness,
                        SelectedDesignObsolete),
                        result =>
                        {
                            if (!result.Accepted)
                            {
                                _saveError = result.RejectionReason ?? "The server rejected the design.";
                                return;
                            }

                            _editingNewDesign = false;
                            if (wasObsolete)
                                SelectedExistingDesignID = String.Empty;
                            else if (designId == null)
                            {
                                // A new design gets its id server-side; pick it up by name.
                                var saved = _factionInfoDB.ShipDesigns.Values.FirstOrDefault(d => d.Name.Equals(name));
                                if (saved != null)
                                    SelectedExistingDesignID = saved.UniqueID;
                            }

                            RefreshExistingClasses();
                        });
                }
            }
        }

        internal void DisplayExistingDesigns()
        {
            Vector2 windowContentSize = ImGui.GetContentRegionAvail();
            if(ImGui.BeginChild("ComponentDesignSelection", new Vector2(Styles.LeftColumnWidth, windowContentSize.Y - 24f), ImGuiChildFlags.Borders, ImGuiWindowFlags.ChildWindow))
            {
                DisplayHelpers.Header("Existing Designs", "Select an existing ship design to edit it.");
                ImGui.Columns(2);
                ImGui.SetColumnWidth(0, Styles.LeftColumnWidth - 24);
                ImGui.SetColumnWidth(1, 24);
                for (int index = 0; index < _existingShipDesignNames.Count; index++)
                {
                    string? designID = _existingShipDesignIDs[index];
                    string designName = _existingShipDesignNames[index];
                    if (ImGui.Selectable(designName + "###existing-design-" + designID, designID.Equals(SelectedExistingDesignID)))
                    {
                        Select(_factionInfoDB.ShipDesigns[designID]);
                    }

                    if (ImGui.BeginPopupContextItem())
                    {
                        if (ImGui.MenuItem("Delete###delete-" + designID))
                        {
                            SubmitCommand(new DeleteShipDesignCommand(_uiState.GameClient!.Session.FactionId, designID),
                                _ =>
                                {
                                    SelectedExistingDesignID = String.Empty;
                                    RefreshExistingClasses();
                                });
                        }
                        if (ImGui.MenuItem("Obsolete###obsolete-" + designID))
                        {
                            SubmitCommand(new SetShipDesignObsoleteCommand(_uiState.GameClient!.Session.FactionId, designID),
                                _ =>
                                {
                                    SelectedExistingDesignID = String.Empty;
                                    RefreshExistingClasses();
                                });
                        }

                        ImGui.EndPopup();
                    }
                    ImGui.NextColumn();
                    string versionText = "P";
                    if(_factionInfoDB.ShipDesigns[designID].DesignVersion > 0)
                        versionText = _factionInfoDB.ShipDesigns[designID].DesignVersion.ToString();
                    ImGui.Text(versionText);
                    ImGui.NextColumn();
                }
                ImGui.Columns(1);
            }
            ImGui.EndChild();

            if(ImGui.Button("Create New Design", new Vector2(204f, 0f)))
            {
                string originalName = NameFactory.GetShipName(_uiState.Game), name = originalName;
                int counter = 1;
                while(_factionInfoDB.ShipDesigns.Values.Any(d => d.Name.Equals(name)))
                {
                    name = originalName + " " + counter.ToString();
                    counter++;
                }
                SelectedDesignName = Utils.BytesFromString(name);
                SelectedComponents = new List<(ComponentDesign design, int count)>();
                RefreshArmor();
                DesignChanged = true;

                if(_armor == null)
                    throw new NullReferenceException();

                // A new design stays a local working copy until it's saved — the server assigns
                // its real id then, so nothing is selected in the existing list yet.
                _workingDesign = new ShipDesign(_factionInfoDB, name, SelectedComponents, (_armor, _armorThickness))
                {
                    IsValid = false
                };
                SelectedDesignObsolete = false;
                SelectedExistingDesignID = String.Empty;
                _editingNewDesign = true;
                ShowNoDesigns = false;
                _saveError = null;
            }
        }

        internal void DisplayComponents()
        {
            DisplayHelpers.Header("Current Design");

            if(SelectedComponents.Count == 0)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.TerribleColor);
                ImGui.Text("Add components from the available components list");
                ImGui.PopStyleColor();
            }
            else
            {
                DisplayComponentsTable();
            }

            ImGui.NewLine();
            DisplayHelpers.Header("Armor");
            if(ImGui.BeginTable("CurrentShipDesignTable", 2, Styles.TableFlags | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Attribute", ImGuiTableColumnFlags.None, 0.6f);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.None, 0.4f);
                ImGui.TableHeadersRow();

                ImGui.TableNextColumn();
                ImGui.Text("Type");
                ImGui.TableNextColumn();

                if(_armorNames == null)
                    throw new NullReferenceException();

                if (ImGui.Combo("##Armor Selection", ref _armorIndex, _armorNames, _armorNames.Length))
                {
                    _armor = _armorSelection[_armorIndex];
                    DesignChanged = true;
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
                    DesignChanged = true;
                }
                ImGui.SameLine();
                if (ImGui.SmallButton("-##armor") && _armorThickness > 0) //todo: imagebutton
                {
                    _armorThickness--;
                    DesignChanged = true;
                }

                ImGui.TableNextColumn();
                ImGui.Text("Mass");
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Mass(_armorMass));

                ImGui.SameLine();
                ImGui.EndTable();
            }
        }

        internal void DisplayComponentsTable()
        {
            if(ImGui.BeginTable("CurrentShipDesignTable", 3, Styles.TableFlags | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 0.5f);
                ImGui.TableSetupColumn("Amount", ImGuiTableColumnFlags.None, 0.25f);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.None, 0.25f);
                ImGui.TableHeadersRow();

                int selectedItem = -1;
                for (int i = 0; i < SelectedComponents.Count; i++)
                {
                    string name = SelectedComponents[i].design.Name;
                    int number = SelectedComponents[i].count;

                    ImGui.TableNextColumn();
                    ImGui.Text(name);

                    bool hovered = ImGui.IsItemHovered();
                    if (hovered)
                    {
                        selectedItem = i;
                        DisplayHelpers.DescriptiveTooltip(SelectedComponents[i].design.Name, SelectedComponents[i].design.TemplateName, SelectedComponents[i].design.Description);
                    }

                    ImGui.TableNextColumn();
                    ImGui.Text(number.ToString());

                    ImGui.SameLine();
                    if (ImGui.SmallButton("+##" + i)) //todo: imagebutton
                    {
                        SelectedComponents[i] = (SelectedComponents[i].design, SelectedComponents[i].count + 1);
                        DesignChanged = true;
                    }
                    ImGui.SameLine();
                    if (ImGui.SmallButton("-##" + i) && number > 0) //todo: imagebutton
                    {
                        SelectedComponents[i] = (SelectedComponents[i].design, SelectedComponents[i].count - 1);
                        DesignChanged = true;
                    }
                    ImGui.TableNextColumn();
                    if (ImGui.SmallButton("x##" + i)) //todo: imagebutton
                    {
                        SelectedComponents.RemoveAt(i);
                        DesignChanged = true;
                    }

                    if (i > 0)
                    {
                        ImGui.SameLine();
                        if (ImGui.SmallButton("^##" + i)) //todo: imagebutton
                        {

                            (ComponentDesign design, int count) item = SelectedComponents[i];
                            SelectedComponents.RemoveAt(i);
                            SelectedComponents.Insert(i - 1, item);

                            DesignChanged = true;
                        }
                    }
                    if (i < SelectedComponents.Count - 1)
                    {
                        ImGui.SameLine();
                        if (ImGui.SmallButton("v##" + i)) //todo: imagebutton
                        {
                            (ComponentDesign design, int count) item = SelectedComponents[i];
                            SelectedComponents.RemoveAt(i);
                            SelectedComponents.Insert(i + 1, item);
                            DesignChanged = true;
                        }
                    }
                }

                ImGui.EndTable();
            }
        }

        internal void DisplayComponentSelection()
        {
            if(_sortedComponentNames == null)
                throw new NullReferenceException();

            DisplayHelpers.Header("Available Components");

            var availableSize = ImGui.GetContentRegionAvail();
            ImGui.SetNextItemWidth(availableSize.X);
            if(ImGui.Combo("###component-filter", ref _componentFilterIndex, _sortedComponentNames, _sortedComponentNames.Length))
            {
                if(_componentFilterIndex == 0)
                {
                    AvailableShipComponents = new List<ComponentDesign>(AllShipComponents);
                }
                else
                {
                    AvailableShipComponents = AllShipComponents.Where(t => t.ComponentType.Equals(_sortedComponentNames[_componentFilterIndex])).ToList();
                }
                ImGui.EndCombo();
            }

            if(ImGui.BeginTable("DesignStatsTables", 3, Styles.TableFlags | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 0.5f);
                ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.None, 0.3f);
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.None, 0.2f);
                ImGui.TableHeadersRow();

                for (int i = 0; i < AvailableShipComponents.Count; i++)
                {
                    if(!AvailableShipComponents[i].ComponentMountType.HasFlag(ComponentMountType.ShipComponent))
                        continue;

                    var design = AvailableShipComponents[i];
                    string name = design.Name;

                    ImGui.TableNextColumn();
                    ImGui.Text(name);
                    if(ImGui.IsItemHovered())
                    {
                        void TooltipExtension()
                        {
                            ImGui.Text("Mass: " + Stringify.Mass(AvailableShipComponents[i].MassPerUnit));
                            ImGui.Text("Volume: " + Stringify.Volume(AvailableShipComponents[i].VolumePerUnit));
                            ImGui.Text("Crew Required: " + AvailableShipComponents[i].CrewReq);
                        }

                        DisplayHelpers.DescriptiveTooltip(AvailableShipComponents[i].Name, AvailableShipComponents[i].TemplateName, AvailableShipComponents[i].Description, TooltipExtension);
                    }
                    ImGui.TableNextColumn();
                    ImGui.Text(design.ComponentType);
                    ImGui.TableNextColumn();
                    ImGui.InvisibleButton($"{i}", new Vector2(4, 8));
                    ImGui.SameLine();
                    if(ImGui.SmallButton("+ Add###add-component-" + i))
                    {
                        SelectedComponents.Add((AvailableShipComponents[i], 1));
                        DesignChanged = true;
                    }
                }

                ImGui.EndTable();
            }
        }

        internal void GenImage()
        {
            if(_profile == null)
                throw new NullReferenceException();

            Textures.CreateTexture(_uiState.ViewPort.Renderer, _profile.DamageProfile, ref _shipImgPtr, SDL.PixelFormat.ARGB8888);
            rawimagewidth = _profile.DamageProfile.Width;
            rawimageheight = _profile.DamageProfile.Height;
            _imagecreated = true;
        }

        internal void DisplayStats()
        {
            DisplayHelpers.Header("Statisitcs", "The attributes of the ship are calculated based on the components you have added to the design.");

            UpdateShipStats();
            if(ImGui.BeginTable("DesignStatsTables", 2, Styles.TableFlags | ImGuiTableFlags.SizingStretchSame))
            {
                ImGui.TableSetupColumn("Attribute", ImGuiTableColumnFlags.None);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.None);
                ImGui.TableHeadersRow();

                ImGui.TableNextColumn();
                ImGui.Text("Gross Tonnage");
                ImGui.TableNextColumn();
                ImGui.Text(_grossTonnage.ToString(Styles.IntFormat));

                ImGui.TableNextColumn();
                ImGui.Text("Mass (Dry)");
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Mass(_massDry));
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Wet: " + Stringify.Mass(_massDry + _fuelStoreMass));
                }

                ImGui.TableNextColumn();
                ImGui.Text("Total Thrust");
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Thrust(_tn));

                ImGui.TableNextColumn();
                ImGui.Text("Thrust to Mass Ratio");
                ImGui.TableNextColumn();
                ImGui.Text(_ttwr.ToString(Styles.DecimalFormat));

                ImGui.TableNextColumn();
                var fuelName = _fuelType?.Name ?? "Unknown";
                ImGui.Text("Fuel Capacity (" + fuelName + ")");
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Mass(_fuelStoreMass));
                ImGui.SameLine();
                ImGui.Text(Stringify.VolumeLtr(_fuelStoreVolume));

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

                if (_cvol > 0)
                {
                    ImGui.TableNextColumn();
                    ImGui.Text("Cargo Storage");
                    ImGui.TableNextColumn();
                    ImGui.Text(Stringify.VolumeLtr(_cvol));


                    ImGui.TableNextColumn();
                    ImGui.Text("Cargo Transfer Rate");
                    ImGui.TableNextColumn();
                    if(_trate == 0)
                        ImGui.PushStyleColor(ImGuiCol.Text, Styles.MediocreColor);
                    ImGui.Text(Stringify.Mass(_trate));
                    if(_trate == 0)
                        ImGui.PopStyleColor();
                    ImGui.TableNextColumn();
                    ImGui.Text("Cargo Transfer Range");
                    ImGui.TableNextColumn();
                    if(_trnge == 0)
                        ImGui.PushStyleColor(ImGuiCol.Text, Styles.MediocreColor);
                    ImGui.Text(Stringify.Velocity(_trnge));
                    if(_trnge == 0)
                        ImGui.PopStyleColor();

                }

                ImGui.EndTable();
            }

            ImGui.NewLine();
            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
            ImGui.Text("Details");
            ImGui.PopStyleColor();
            ImGui.Separator();

            ImGui.Text("Design Name:");
            ImGui.InputText("###Design Name", SelectedDesignName, (uint)SelectedDesignName.Length);
            ImGui.NewLine();
            ImGui.Text("Is Obsolete?");
            ImGui.Checkbox("###IsObsolete", ref SelectedDesignObsolete);

            if(!IsDesignValid())
            {
                ImGui.NewLine();
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.BadColor);
                ImGui.Text("Current design is invalid!");
                // TODO: tell the player what is invalid about their design
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("You will not be able to construct ships with an invalid design.");
                ImGui.PopStyleColor();
            }

            foreach (var warning in Warnings())
            {
                ImGui.Text(warning);
            }

            if (_saveError != null)
            {
                ImGui.NewLine();
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.BadColor);
                ImGui.TextWrapped("Save failed: " + _saveError);
                ImGui.PopStyleColor();
            }

            ImGui.NewLine();
            NewShipButton();
            ImGui.SameLine();
            ImGui.Checkbox("Show Pic", ref displayimage);
            ImGui.NewLine();

            var size = ImGui.GetContentRegionAvail();
            DisplayImage(size.X, size.Y);
        }

        private void UpdateShipStats()
        {
            if(!DesignChanged) return;

            if(_armor == null)
                throw new NullReferenceException();

            _profile = new EntityDamageProfileDB(SelectedComponents, (_armor, _armorThickness));
            if(displayimage)
            {
                GenImage();
            }

            long mass = 0;
            double fu = 0;
            double tn = 0;
            double ev = 0;

            double wp = 0;
            double wcc = 0;
            double wsc = 0;
            double wec = 0;
            double egen = 0;
            double estor = 0;
            string thrusterFuel = String.Empty;
            Dictionary<string, double> cstore = new Dictionary<string, double>();

            double volume = 0;

            foreach (var component in SelectedComponents)
            {
                mass += component.design.MassPerUnit * component.count;
                volume += component.design.VolumePerUnit * component.count;
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

                /*
                if (component.design.HasAttribute<CargoStorageAtb>())
                {
                    var atb = component.design.GetAttribute<CargoStorageAtb>();
                    var typeid = atb.StoreTypeID;
                    var amount = atb.MaxVolume * component.count;
                    if (!cstore.ContainsKey(typeid))
                        cstore.Add(typeid, amount);
                    else
                        cstore[typeid] += amount;
                }

                if (component.design.HasAttribute<CargoTransferAtb>())
                {
                    var atb = component.design.GetAttribute<CargoTransferAtb>();
                    //atb.TransferRange_ms

                }*/
            }

            cstore = StorageSpaceProcessor.CalculatedMaxStorage(_workingDesign);
            var cargoTransfer = StorageSpaceProcessor.CalcRateAndRange(_workingDesign);




            _armorMass = ShipDesign.GetArmorMass(_profile, _factionInfoDB.Data.CargoGoods);
            mass += (long)Math.Round(_armorMass);

            var K = 0.2 + 0.02 * Math.Log10(volume);

            _grossTonnage = volume * K; // GT = V * K from: https://en.wikipedia.org/wiki/Gross_tonnage
            _massDry = mass;
            _tn = tn;
            _ttwr = (tn / mass) * 0.01;
            _wcc = wcc;
            _wec = wec;
            _wsc = wsc;
            _wspd = WarpMath.MaxSpeedCalc(wp, mass);
            _egen = egen;
            _estor = estor;
            _trate = cargoTransfer.rate;
            if(double.IsNaN(cargoTransfer.range))
                _trnge = 0;
            else
                _trnge = cargoTransfer.range;
            //double fuelMass = 0;
            if (thrusterFuel.IsNotNullOrEmpty())
            {
                _fuelType = _factionInfoDB.Data.CargoGoods.GetAny(thrusterFuel);
                if (_fuelType != null && cstore.ContainsKey(_fuelType.CargoTypeID))
                {
                    _fuelStoreVolume = cstore[_fuelType.CargoTypeID];
                    var fuelDensity = _fuelType.MassPerUnit / _fuelType.VolumePerUnit;
                    _fuelStoreMass = _fuelStoreVolume * fuelDensity;

                }
            }

            _cvol = 0;
            foreach (var store in cstore)
            {
                if (_fuelType == null || store.Key != _fuelType.CargoTypeID)
                    _cvol += store.Value;
            }

            _massWet = _massDry + _fuelStoreMass;
            _dv = OrbitMath.TsiolkovskyRocketEquation(_massWet, _massDry, ev);

            DesignChanged = false;
        }

        private bool IsDesignValid()
        {
            return _massDry > 0 &&
                    _tn > 0 &&
                    _ttwr > 0 &&
                    _egen > 0 &&
                    _estor > 0;
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

                ImGui.Image(_shipImgPtr.ToTextureRef(), new System.Numerics.Vector2(rawimagewidth * scale, rawimageheight * scale));
            }
        }

        private List<string> Warnings()
        {
            List<string> warnings = new List<string>();
            if (_cvol > 0 && _trate == 0 || _trnge == 0)
            {
                warnings.Add("This ship has cargo space but no way to transfer cargo by itself");
            }
            if (_wspd == 0)
            {
                warnings.Add("This ship has no warp ability");
            }

            if (_ttwr == 0)
            {
                warnings.Add("This ship has no newtonion thrust");
            }
            return warnings;
        }
    }
}
