using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using ImGuiNET;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.Industry;

namespace Pulsar4X.SDL2UI
{
    public sealed class IndustryDisplay
    {
        private static IndustryDisplay instance = null;
        private static readonly object padlock = new object();

        public EntityState EntityState { get; private set; }

        private Guid _factionID;
        private FactionInfoDB _factionInfoDB;
        Dictionary<Guid, (Guid[] itemIDs, string[] itemNames) > _contructablesByPline = new Dictionary<Guid, (Guid[], string[])>();
        private IndustryJob _newConJob;
        private (Guid pline, int item) _newjobSelectionIndex = (Guid.Empty, 0);
        private int _newJobbatchCount = 1;
        private bool _newJobRepeat = false;
        private bool _newJobAutoInstall = true;
        private bool _newJobCanAutoInstall = false;
        private Entity _newJobAutoInstallEntity = null;
        private Dictionary<Guid,IndustryAbilityDB.ProductionLine> _prodLines;
        private Guid _selectedProdLine;
        private int _selectedExistingIndex = -1;
        private IndustryJob _lastClickedJob { get; set; }
        private IConstrucableDesign _lastClickedDesign;
        private Entity Entity;
        private IndustryAbilityDB _industryDB;
        private VolumeStorageDB _volStorageDB;
        private IndustryJob _selectedExistingConJob
        {
            get
            {
                if (_selectedProdLine != Guid.Empty
                    && _selectedExistingIndex > -1
                    && _prodLines[_selectedProdLine].Jobs.Count > _selectedExistingIndex)
                {
                    return _prodLines[_selectedProdLine].Jobs[_selectedExistingIndex];
                }

                return null;
            }
        }

        Guid SelectedConstrucableID
        {
            get
            {
                if(_contructablesByPline.Count != _industryDB.ProductionLines.Count)
                    Update();
                return _contructablesByPline[_newjobSelectionIndex.pline].itemIDs[_newjobSelectionIndex.item];
            }
        }

        private IndustryDisplay() { }

        internal static IndustryDisplay GetInstance(EntityState state) {
            lock(padlock)
            {
                if(instance == null)
                {
                    instance = new IndustryDisplay();
                }
                instance.SetEntity(state);
            }

            return instance;
        }

        public void SetEntity(EntityState state)
        {
            if(state == EntityState) return;

            EntityState = state;
            //Update();
        }

        private void Update()
        {
            _prodLines = _industryDB.ProductionLines;

            int count = _factionInfoDB.IndustryDesigns.Count;
            //_constructableDesigns = new IConstrucableDesign[count];
            var constructablesNames = new string[count];
            var constructablesIDs = new Guid[count];

            int i = 0;
            Dictionary<Guid, List<int>> _constructablesIndexesByType = new ();
            foreach (var (id, design) in _factionInfoDB.IndustryDesigns)
            {
                if(!design.IsValid) continue;

                constructablesNames[i] = design.Name;
                constructablesIDs[i] = id;
                Guid typeID = design.IndustryTypeID;

                if(!_constructablesIndexesByType.ContainsKey(typeID))
                    _constructablesIndexesByType.Add(typeID, new List<int>());
                _constructablesIndexesByType[typeID].Add(i);
                i++;
            }

            foreach (var (id, productionLine) in _prodLines)
            {
                List<Guid> itemIDs = new ();
                List<string> itemNames = new ();

                foreach (var typeID in productionLine.IndustryTypeRates.Keys)
                {
                    if(_constructablesIndexesByType.ContainsKey(typeID))
                    {
                        foreach (var index in _constructablesIndexesByType[typeID])
                        {
                            itemIDs.Add(constructablesIDs[index]);
                            itemNames.Add(constructablesNames[index]);
                        }
                    }
                }
                _contructablesByPline[id] = (itemIDs.ToArray(), itemNames.ToArray());
            }
        }

        public void Display(GlobalUIState state)
        {
            Entity = EntityState.Entity;
            if(!Entity.TryGetDatablob<IndustryAbilityDB>(out _industryDB) || !state.Faction.TryGetDatablob<FactionInfoDB>(out _factionInfoDB))
            {
                Vector2 topSize = ImGui.GetContentRegionAvail();
                if(ImGui.BeginChild("NoProductionAvailable", new Vector2(topSize.X, 56f), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                {
                    ImGui.Text("You need an installation capable of production. Consider importing one.\n\nExamples: Factory, Shipyard or Refinery");
                    ImGui.EndChild();
                }
                return;
            }

            Entity.TryGetDatablob(out _volStorageDB);
            _factionID = state.Faction.Guid;
            Update();

            Vector2 windowContentSize = ImGui.GetContentRegionAvail();
            ProductionLineDisplay(state);
            ImGui.SameLine();

            if(_selectedProdLine == Guid.Empty)
                return;

            if(ImGui.BeginChild("JobDescriptionPane", new Vector2(windowContentSize.X * 0.5f - 8f, windowContentSize.Y), true))
            {
                if(_prodLines.ContainsKey(_selectedProdLine) &&_prodLines[_selectedProdLine] != null)
                {
                    ImGui.Text("Create a new job for: " + _prodLines[_selectedProdLine].Name);
                }
                else
                {
                    ImGui.Text("Select a production line on the left");
                }

                ImGui.Separator();
                NewJobDisplay(state);
                ImGui.EndChild();
            }
        }

        public void ProductionLineDisplay(GlobalUIState state)
        {
            if(_prodLines == null)
            {
                ImGui.Text("No capacity for construction at this colony.");
                return;
            }

            Vector2 windowContentSize = ImGui.GetContentRegionAvail();
            if(ImGui.BeginChild("ColonyProductionLines", new Vector2(windowContentSize.X * 0.5f, windowContentSize.Y), true))
            {
                ImGui.Text("Production Lines");
                ImGui.Separator();

                foreach (var (id, line) in _prodLines)
                {
                    ImGui.PushID(id.ToString());
                    // ImGui.Text(Stringify.Volume(line.MaxVolume));
                    // foreach(var (rid, rate) in line.IndustryTypeRates)
                    // {
                    //     ImGui.Text(rid.ToString());
                    //     ImGui.SameLine();
                    //     ImGui.Text(rate.ToString());
                    // }

                    string headerTitle = line.Name;
                    if(line.Jobs.Count == 0)
                        headerTitle += " (Idle)";

                    if (ImGui.CollapsingHeader(headerTitle, ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        Vector2 topSize = ImGui.GetContentRegionAvail();
                        if(ImGui.BeginChild("", new Vector2(topSize.X, 36f), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                        {
                            if(ImGui.Button("Add New Job"))
                            {
                                _selectedProdLine = id;
                                _newjobSelectionIndex = (_selectedProdLine, 0);

                                _newConJob = new IndustryJob(state.Faction.GetDataBlob<FactionInfoDB>(), SelectedConstrucableID);
                                _lastClickedJob = _newConJob;
                                _lastClickedDesign = _factionInfoDB.IndustryDesigns[SelectedConstrucableID];
                            }

                            ImGui.SameLine();
                            if(ImGui.Button("Upgrade " + line.Name))
                            {
                                // TODO: add upgrade functionality
                            }

                            if(line.Jobs.Count > 0)
                            {
                                IConstrucableDesign designInfo = _factionInfoDB.IndustryDesigns[line.Jobs[0].ItemGuid];
                                var rate = line.IndustryTypeRates[designInfo.IndustryTypeID];
                                ImGui.SameLine();
                                ImGui.Text("Progress per day:");
                                ImGui.SameLine();
                                ImGui.PushStyleColor(ImGuiCol.Text, Styles.HighlightColor);
                                ImGui.Text(rate.ToString());
                                ImGui.PopStyleColor();
                                if(ImGui.IsItemHovered())
                                    ImGui.SetTooltip("Assuming all resources needed are available.");
                            }

                            ImGui.EndChild();
                        }

                        if(line.Jobs.Count > 0)
                        {
                            if(ImGui.BeginTable(line.Name, 4, ImGuiTableFlags.BordersV | ImGuiTableFlags.BordersOuterH | ImGuiTableFlags.RowBg))
                            {
                                ImGui.TableSetupColumn("Job");
                                ImGui.TableSetupColumn("Batch Size");
                                ImGui.TableSetupColumn("Progress");
                                ImGui.TableSetupColumn("");
                                ImGui.TableHeadersRow();
                                var progsize = new Vector2(128, ImGui.GetTextLineHeight());
                                for (int ji = 0; ji < line.Jobs.Count; ji++)
                                {
                                    var cpos = ImGui.GetCursorPos();
                                    var batchJob = line.Jobs[ji];
                                    string jobname = line.Jobs[ji].Name;

                                    bool selected = _selectedExistingIndex ==  ji && id == _selectedProdLine;
                                    float percent = 1 - (float)batchJob.ProductionPointsLeft / batchJob.ProductionPointsCost;

                                    ImGui.TableNextColumn();
                                    if (ImGui.Selectable(jobname, ref selected))
                                    {
                                        _selectedExistingIndex =  ji;
                                        _selectedProdLine = id;
                                        _lastClickedJob = _selectedExistingConJob;
                                        _lastClickedDesign = _factionInfoDB.IndustryDesigns[SelectedConstrucableID];
                                    }

                                    ImGui.TableNextColumn();
                                    ImGui.Text(batchJob.NumberCompleted + "/" + batchJob.NumberOrdered);

                                    if (batchJob.Auto)
                                    {
                                        ImGui.SameLine();
                                        ImGui.Image(state.Img_Repeat(), new Vector2(16, 16));
                                    }

                                    ImGui.TableNextColumn();
                                    ImGui.Text("IP " + (batchJob.ProductionPointsCost - batchJob.ProductionPointsLeft) + "/" + batchJob.ProductionPointsCost);

                                    string hoverText = "";
                                    foreach(var (rId, amountRemaining) in line.Jobs[ji].ResourcesRequiredRemaining)
                                    {
                                        ICargoable cargoItem = StaticRefLib.StaticData.CargoGoods.GetAny(rId);
                                        if (cargoItem == null)
                                            cargoItem = state.Faction.GetDataBlob<FactionInfoDB>().ComponentDesigns[rId];
                                        hoverText += cargoItem.Name;
                                        hoverText += " x" + amountRemaining.ToString() + "\n";
                                    }
                                    ImGui.Text(hoverText);
                                    ImGui.TableNextColumn();
                                    ImGui.PushID(line.Jobs[ji].JobID.ToString());
                                    ImGui.Text("");
                                    if(ji > 0)
                                    {
                                        ImGui.SameLine();
                                        if (ImGui.ImageButton(state.Img_Up(), new Vector2(16, 16)))
                                        {
                                            var cmd = IndustryOrder2.CreateChangePriorityOrder(_factionID, Entity, id, line.Jobs[ji].JobID, -1);
                                            StaticRefLib.OrderHandler.HandleOrder(cmd);
                                        }
                                        if(ImGui.IsItemHovered())
                                            ImGui.SetTooltip("Move up in the produciton queue.");
                                    }

                                    if(ji < line.Jobs.Count - 1)
                                    {
                                        ImGui.SameLine();
                                        if (ImGui.ImageButton(state.Img_Down(), new Vector2(16, 16)))
                                        {
                                            var cmd = IndustryOrder2.CreateChangePriorityOrder(_factionID, Entity, id, line.Jobs[ji].JobID, 1);
                                            StaticRefLib.OrderHandler.HandleOrder(cmd);
                                        }
                                        if(ImGui.IsItemHovered())
                                            ImGui.SetTooltip("Move down in the produciton queue.");
                                    }

                                    ImGui.SameLine();
                                    if (ImGui.ImageButton(state.Img_Cancel(), new Vector2(16, 16)))
                                    {
                                        //new ConstructCancelJob(_uiState.Faction.Guid, _selectedEntity.Guid, _selectedEntity.StarSysDateTime, _selectedExistingConJob.JobID);
                                        var cmd = IndustryOrder2.CreateCancelJobOrder(_factionID, Entity, id, line.Jobs[ji].JobID);

                                        StaticRefLib.OrderHandler.HandleOrder(cmd);
                                    }
                                    if(ImGui.IsItemHovered())
                                        ImGui.SetTooltip("Cancel the job.");
                                    ImGui.PopID();
                                    ImGui.TableNextRow();
                                }
                                ImGui.EndTable();
                            }
                        }
                        ImGui.Columns(1);
                    }
                    ImGui.PopID();
                }
                ImGui.EndChild();
            }
        }

        void NewJobDisplay(GlobalUIState state)
        {
            //ImGui.BeginChild("InitialiseJob", new Vector2(404, 84), true, ImGuiWindowFlags.ChildWindow);
            if(_newjobSelectionIndex.pline != Guid.Empty)
            {
                int curItemIndex = _newjobSelectionIndex.item;

                // TODO: improve this, preferably store the array of names and update it during Update()
                var constructableNames = _contructablesByPline[_selectedProdLine].itemNames;
                // Flatten the dictionary
                // var flattened = _contructablesByPline.SelectMany(
                //     kvp => kvp.Value.itemNames.Zip(kvp.Value.itemIDs, (name, id) => new { Id = id, Name = name, ParentKey = kvp.Key }))
                //     .ToList();

                // // Sort by itemNames
                // var sorted = flattened.OrderBy(x => x.Name).ToList();

                // // Group back (if you need)
                // var groupedBack = sorted.GroupBy(x => x.ParentKey)
                //     .ToDictionary(g => g.Key, g => (itemIDs: g.Select(x => x.Id).ToArray(), itemNames: g.Select(x => x.Name).ToArray()));

                ImGui.Text("Select a production method:");
                if (ImGui.Combo("", ref curItemIndex, constructableNames, constructableNames.Length))
                {
                    _newjobSelectionIndex = (_newjobSelectionIndex.pline, curItemIndex);
                    _newConJob = new IndustryJob(state.Faction.GetDataBlob<FactionInfoDB>(), SelectedConstrucableID);
                    _lastClickedJob = _newConJob;
                    _lastClickedDesign = _factionInfoDB.IndustryDesigns[SelectedConstrucableID];
                    _newConJob.NumberOrdered = (ushort)_newJobbatchCount;
                    _newJobCanAutoInstall = _lastClickedDesign.GuiHints == ConstructableGuiHints.CanBeInstalled;
                    if(_lastClickedDesign is ComponentDesign)
                    {
                        ComponentDesign cDesign = (ComponentDesign)_lastClickedDesign;
                        // TODO: check the mount type and display options for the player to auto-install on
                    }
                }

                ImGui.Text("Enter the quantity:");
                if (ImGui.InputInt("##batchcount", ref _newJobbatchCount))
                {
                    if(_newJobbatchCount < 1)
                        _newJobbatchCount = 1;

                    _newConJob.NumberOrdered = (ushort)_newJobbatchCount;
                }
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("The production line will move to the next job in the queue\nafter finishing the number of items requested.");

                if(_lastClickedJob != null)
                    CostsDisplay(_lastClickedJob, state);

                ImGui.Columns(1);
                ImGui.NewLine();
                ImGui.Text("Repeat this job?");
                ImGui.Checkbox("##repeat", ref _newJobRepeat);
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("A repeat job will run until cancelled.");

                if(_newJobCanAutoInstall)
                {
                    ImGui.Text("Auto-install on completion?");
                    ImGui.Checkbox("##autoinstall", ref _newJobAutoInstall);

                    // TODO: need to allow the player to select what to install the component on
                    // depending on the mount type in the component design
                }

                ImGui.NewLine();

                if (ImGui.Button("Queue the job to " + _prodLines[_selectedProdLine].Name))
                {
                    _newConJob = new IndustryJob(state.Faction.GetDataBlob<FactionInfoDB>(), SelectedConstrucableID);

                    if(_newJobCanAutoInstall && _newJobAutoInstall && _lastClickedDesign is ComponentDesign design)
                    {
                        if(design.ComponentMountType.HasFlag(ComponentMountType.PlanetInstallation))
                        {
                            _newConJob.InstallOn = Entity;
                        }
                        else if(design.ComponentMountType.HasFlag(ComponentMountType.ShipComponent)
                            || design.ComponentMountType.HasFlag(ComponentMountType.ShipCargo)
                            || design.ComponentMountType.HasFlag(ComponentMountType.Fighter)
                            || design.ComponentMountType.HasFlag(ComponentMountType.Missile))
                        {
                            _newConJob.InstallOn = _newJobAutoInstallEntity;
                        }
                    }

                    var cmd = IndustryOrder2.CreateNewJobOrder(_factionID, Entity, _selectedProdLine, _newConJob);
                    _newConJob.InitialiseJob((ushort)_newJobbatchCount, _newJobRepeat);
                    _lastClickedJob = _newConJob;
                    _lastClickedDesign = _factionInfoDB.IndustryDesigns[SelectedConstrucableID];
                    StaticRefLib.OrderHandler.HandleOrder(cmd);
                }

            }

            //ImGui.EndChild();
        }

        void CostsDisplay(JobBase selectedJob, GlobalUIState state)
        {
            ImGui.NewLine();
            var sizeAvailable = ImGui.GetContentRegionAvail();
            string inputs = "Inputs Needed";
            float textSize = ImGui.CalcTextSize(inputs).X / 2;

            ImGui.PushStyleColor(ImGuiCol.Text, Styles.HighlightColor);
            ImGui.SetCursorPosX(sizeAvailable.X / 2 - textSize);
            ImGui.Text(inputs);
            ImGui.PopStyleColor();
            if(ImGui.BeginTable("JobCostsTables", 4, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 1.5f);
                ImGui.TableSetupColumn("Cost Per Quantity", ImGuiTableColumnFlags.None, 1f);
                ImGui.TableSetupColumn("Total Cost", ImGuiTableColumnFlags.None, 1f);
                ImGui.TableSetupColumn("Available", ImGuiTableColumnFlags.None, 1f);
                ImGui.TableHeadersRow();

                ImGui.TableNextColumn();
                ImGui.Text("");
                ImGui.SameLine();
                ImGui.Text("Industry Points");
                ImGui.TableNextColumn();
                ImGui.Text(selectedJob.ProductionPointsLeft.ToString());
                ImGui.TableNextColumn();
                ImGui.Text((selectedJob.ProductionPointsLeft * selectedJob.NumberOrdered).ToString());
                if(ImGui.IsItemHovered())
                        ImGui.SetTooltip("Total Cost = Cost Per Quantity * Quantity Ordered");
                ImGui.TableNextColumn();
                ImGui.Text("-");
                ImGui.TableNextRow();

                foreach (var item in selectedJob.ResourcesRequiredRemaining)
                {
                    ICargoable cargoItem = StaticRefLib.StaticData.CargoGoods.GetAny(item.Key);
                    if (cargoItem == null)
                        cargoItem = state.Faction.GetDataBlob<FactionInfoDB>().ComponentDesigns[item.Key];
                    var totalCost = selectedJob.NumberOrdered * item.Value;

                    ImGui.TableNextColumn();
                    ImGui.Text("");
                    ImGui.SameLine();
                    ImGui.Text(cargoItem.Name);
                    ImGui.TableNextColumn();
                    ImGui.Text(item.Value.ToString());
                    ImGui.TableNextColumn();
                    ImGui.Text(totalCost.ToString());
                    if(ImGui.IsItemHovered())
                        ImGui.SetTooltip("Total Cost = Cost Per Output * Quantity Ordered\n" + totalCost + " = " + item.Value + " * " + selectedJob.NumberOrdered);
                    ImGui.TableNextColumn();
                    if (_volStorageDB != null)
                    {
                        var stored = CargoExtensionMethods.GetUnitsStored(_volStorageDB, cargoItem);
                        if(stored < totalCost)
                            ImGui.PushStyleColor(ImGuiCol.Text, Styles.BadColor);

                        ImGui.Text(Stringify.Quantity(stored));

                        if(stored < totalCost)
                        {
                            if(ImGui.IsItemHovered())
                                ImGui.SetTooltip("Not enough " + cargoItem.Name + " available on this colony.\nImport or produce some!");
                            ImGui.PopStyleColor();
                        }
                    }
                    else
                    {
                        ImGui.Text("No Local Storage Available");
                    }
                    ImGui.TableNextRow();
                }

                ImGui.EndTable();
            }

            ImGui.NewLine();
            sizeAvailable = ImGui.GetContentRegionAvail();
            string outputs = "Outputs";
            textSize = ImGui.CalcTextSize(outputs).X / 2;

            ImGui.PushStyleColor(ImGuiCol.Text, Styles.HighlightColor);
            ImGui.SetCursorPosX(sizeAvailable.X / 2 - textSize);
            ImGui.Text(outputs);
            ImGui.PopStyleColor();

            if(ImGui.BeginTable("JobOutputsTables", 3, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 1.5f);
                ImGui.TableSetupColumn("Amount Per Quantity", ImGuiTableColumnFlags.None, 1f);
                ImGui.TableSetupColumn("Total", ImGuiTableColumnFlags.None, 1f);
                ImGui.TableHeadersRow();

                ImGui.TableNextColumn();
                ImGui.Text("");
                ImGui.SameLine();
                ImGui.Text(_lastClickedDesign.Name);
                ImGui.TableNextColumn();
                ImGui.Text(_lastClickedDesign.OutputAmount.ToString());
                ImGui.TableNextColumn();
                ImGui.Text((_lastClickedDesign.OutputAmount * selectedJob.NumberOrdered).ToString());

                ImGui.EndTable();
            }
        }
    }
}