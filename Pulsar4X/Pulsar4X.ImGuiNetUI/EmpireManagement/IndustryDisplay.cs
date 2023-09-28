using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Industry;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Extensions;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Interfaces;
using Pulsar4X.Components;

namespace Pulsar4X.SDL2UI
{
    public sealed class IndustryDisplay
    {
        private static IndustryDisplay instance = null;
        private static readonly object padlock = new object();

        public EntityState EntityState { get; private set; }

        private string _factionID;
        private FactionInfoDB _factionInfoDB;
        Dictionary<string, (string[] itemIDs, string[] itemNames) > _contructablesByPline = new ();
        private IndustryJob _newConJob;
        private (string pline, int item) _newjobSelectionIndex = (string.Empty, 0);
        private int _newJobbatchCount = 1;
        private bool _newJobRepeat = false;
        private bool _newJobAutoInstall = true;
        private bool _newJobCanAutoInstall = false;
        private Entity _newJobAutoInstallEntity = null;
        private Dictionary<string, IndustryAbilityDB.ProductionLine> _prodLines;
        private string _selectedProdLine;
        private int _selectedExistingIndex = -1;
        private IndustryJob _lastClickedJob { get; set; }
        private IConstructableDesign _lastClickedDesign;
        private Entity Entity;
        private IndustryAbilityDB _industryDB;
        private VolumeStorageDB _volStorageDB;
        private IndustryJob _selectedExistingConJob
        {
            get
            {
                if (_selectedProdLine != string.Empty
                    && _selectedExistingIndex > -1
                    && _prodLines[_selectedProdLine].Jobs.Count > _selectedExistingIndex)
                {
                    return _prodLines[_selectedProdLine].Jobs[_selectedExistingIndex];
                }

                return null;
            }
        }

        string SelectedConstrucableID
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
            var constructablesIDs = new string[count];

            int i = 0;
            Dictionary<string, List<int>> _constructablesIndexesByType = new ();
            var sortedDesigns = _factionInfoDB.IndustryDesigns.Values.ToList();
            sortedDesigns.Sort((a, b) => a.Name.CompareTo(b.Name));

            foreach(var (id, productionLine) in _prodLines)
            {
                List<string> itemIDs = new ();
                List<string> itemNames = new ();

                foreach (var design in sortedDesigns)
                {
                    if(!design.IsValid) continue;

                    if(productionLine.IndustryTypeRates.ContainsKey(design.IndustryTypeID))
                    {
                        itemIDs.Add(design.UniqueID);
                        itemNames.Add(design.Name);
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

            if(_selectedProdLine.IsNullOrEmpty())
                return;

            if(ImGui.BeginChild("JobDescriptionPane", new Vector2(windowContentSize.X * 0.5f - 8f, windowContentSize.Y), true))
            {
                if(_prodLines.ContainsKey(_selectedProdLine) &&_prodLines[_selectedProdLine] != null)
                {
                    DisplayHelpers.Header("Create a new job for: " + _prodLines[_selectedProdLine].Name);
                }
                else
                {
                    DisplayHelpers.Header("Select a production line on the left");
                }

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
                DisplayHelpers.Header("Production Lines");

                foreach (var (id, line) in _prodLines)
                {
                    var jobs = line.Jobs.ToList();
                    string headerTitle = line.Name;
                    if(jobs.Count == 0)
                        headerTitle += " (Idle)";
                    ImGui.PushID(id.ToString());
                    if(_selectedProdLine == id)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Header, Styles.DescriptiveColor);
                    }
                    if (ImGui.CollapsingHeader(headerTitle, ImGuiTreeNodeFlags.DefaultOpen ))
                    {
                        if(ImGui.Button("+ New Job"))
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

                        if(jobs.Count > 0)
                        {
                            IConstructableDesign designInfo = _factionInfoDB.IndustryDesigns[line.Jobs[0].ItemGuid];
                            var rate = line.IndustryTypeRates[designInfo.IndustryTypeID];
                            ImGui.SameLine();
                            ImGui.Text("Progress per day:");
                            ImGui.SameLine();
                            ImGui.PushStyleColor(ImGuiCol.Text, Styles.HighlightColor);
                            ImGui.Text(rate.ToString());
                            ImGui.PopStyleColor();
                            if(ImGui.IsItemHovered())
                                ImGui.SetTooltip("Assuming all resources needed are available.");

                            if(ImGui.BeginTable(line.Name, 4, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
                            {
                                ImGui.TableSetupColumn("Job", ImGuiTableColumnFlags.None, 1f);
                                ImGui.TableSetupColumn("Batch", ImGuiTableColumnFlags.None, 0.5f);
                                ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.None, 1f);
                                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.None, 1f);
                                ImGui.TableHeadersRow();
                                var progsize = new Vector2(128, ImGui.GetTextLineHeight());
                                for (int ji = 0; ji < jobs.Count; ji++)
                                {
                                    var cpos = ImGui.GetCursorPos();
                                    var batchJob = jobs[ji];
                                    string jobname = jobs[ji].Name;

                                    //bool selected = _selectedExistingIndex ==  ji && id == _selectedProdLine;
                                    float percent = (1 - (float)batchJob.ProductionPointsLeft / batchJob.ProductionPointsCost) * 100;

                                    ImGui.TableNextColumn();
                                    ImGui.Text(jobname);

                                    ImGui.TableNextColumn();
                                    ImGui.Text(batchJob.NumberCompleted + "/" + batchJob.NumberOrdered);

                                    if (batchJob.Auto)
                                    {
                                        ImGui.SameLine();
                                        ImGui.Image(state.Img_Repeat(), new Vector2(16, 16));
                                    }

                                    ImGui.TableNextColumn();
                                    var color = batchJob.Status == IndustryJobStatus.MissingResources ? Styles.BadColor : Styles.GoodColor;

                                    ImGui.PushStyleColor(ImGuiCol.Text, color);
                                    switch(batchJob.Status)
                                    {
                                        case IndustryJobStatus.Processing:
                                            string status = "Processing (" + percent.ToString("0.#") + "%%)";
                                            ImGui.Text(status);
                                            break;
                                        default:
                                            ImGui.Text(batchJob.Status.ToString());
                                            break;
                                    }
                                    //ImGui.Text("IP " + (batchJob.ProductionPointsCost - batchJob.ProductionPointsLeft) + "/" + batchJob.ProductionPointsCost);
                                    ImGui.PopStyleColor();

                                    if(ImGui.IsItemHovered())
                                    {
                                        ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize, 0f);
                                        ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, 0f);
                                        ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(0.1f, 0.1f, 0.1f, 1f));
                                        ImGui.BeginTooltip();
                                        if(ImGui.BeginTable(jobs[ji].ItemGuid.ToString(), 2, ImGuiTableFlags.Borders))
                                        {
                                            ImGui.TableSetupColumn("Resource Required");
                                            ImGui.TableSetupColumn("Quantity Needed");
                                            ImGui.TableHeadersRow();
                                            ImGui.TableNextColumn();
                                            ImGui.Text("Industry Points");
                                            ImGui.TableNextColumn();
                                            ImGui.Text(batchJob.ProductionPointsLeft.ToString());

                                            foreach(var (rId, amountRemaining) in jobs[ji].ResourcesRequiredRemaining)
                                            {
                                                ICargoable cargoItem = state.Faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny(rId);
                                                if (cargoItem == null)
                                                    cargoItem = state.Faction.GetDataBlob<FactionInfoDB>().ComponentDesigns[rId];

                                                ImGui.TableNextColumn();
                                                ImGui.Text(cargoItem.Name);
                                                ImGui.TableNextColumn();
                                                ImGui.Text(amountRemaining.ToString());
                                            }
                                            ImGui.EndTable();
                                        }
                                        ImGui.EndTooltip();
                                        ImGui.PopStyleColor();
                                        ImGui.PopStyleVar(2);
                                    }
                                    ImGui.TableNextColumn();
                                    ActionButtons(id, line.Jobs[ji].JobID, ji, line.Jobs.Count, state);
                                    ImGui.TableNextRow();
                                }
                                ImGui.EndTable();
                            }
                        }
                    }
                    if(_selectedProdLine == id)
                    {
                        ImGui.PopStyleColor();
                    }
                    ImGui.PopID();
                }
                ImGui.EndChild();
            }
        }

        void NewJobDisplay(GlobalUIState state)
        {
            //ImGui.BeginChild("InitialiseJob", new Vector2(404, 84), true, ImGuiWindowFlags.ChildWindow);
            if(_newjobSelectionIndex.pline != String.Empty)
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

                ImGui.NewLine();
                ImGui.Text("Select a design:");
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

                ImGui.NewLine();
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
                ImGui.Checkbox("##repeat", ref _newJobRepeat);
                ImGui.SameLine();
                ImGui.Text("Repeat this job?");
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("A repeat job will run until cancelled.");

                if(_newJobCanAutoInstall)
                {
                    // TODO: need to allow the player to select what to install the component on
                    // depending on the mount type in the component design
                    ImGui.Checkbox("##autoinstall", ref _newJobAutoInstall);
                    ImGui.SameLine();
                    ImGui.Text("Auto-install on completion?");
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
                    state.Game.OrderHandler.HandleOrder(cmd);

                    // Reset the displayed construction job
                    _newConJob = new IndustryJob(state.Faction.GetDataBlob<FactionInfoDB>(), SelectedConstrucableID);
                    _lastClickedJob = _newConJob;
                    _lastClickedDesign = _factionInfoDB.IndustryDesigns[SelectedConstrucableID];
                }

            }

            //ImGui.EndChild();
        }

        void CostsDisplay(JobBase selectedJob, GlobalUIState state)
        {
            ImGui.NewLine();
            ImGui.Text("Inputs Needed:");
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
                    ICargoable cargoItem = state.Faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny(item.Key);
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
                        var stored = _volStorageDB.GetUnitsStored(cargoItem);
                        if(stored < totalCost)
                        {
                            if(_factionInfoDB.IndustryDesigns.ContainsKey(cargoItem.UniqueID))
                                ImGui.PushStyleColor(ImGuiCol.Text, Styles.BadColor);
                            else
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, Styles.TerribleColor);
                            }
                        }

                        ImGui.Text(Stringify.Quantity(stored));

                        if(stored < totalCost)
                        {
                            if (ImGui.IsItemHovered())
                            {
                                if (_factionInfoDB.IndustryDesigns.ContainsKey(cargoItem.UniqueID) || state.Faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.IsMineral(cargoItem.UniqueID))
                                {
                                    ImGui.SetTooltip("Not enough " + cargoItem.Name + " available on this colony.\nImport or produce some!");
                                }
                                else
                                {
                                    ImGui.SetTooltip("Not enough " + cargoItem.Name + " available on this colony.\nAnd we can't build this item!");
                                }
                            }

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
            ImGui.Text("Outputs:");

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

        private void ActionButtons(string productionLineID, string jobID, int index, int count, GlobalUIState state)
        {
            var invisButtonSize = new Vector2(15, 15);
            ImGui.PushID(jobID.ToString());
            if(index > 0)
            {
                if (ImGui.SmallButton("^"))
                {
                    var cmd = IndustryOrder2.CreateChangePriorityOrder(_factionID, Entity, productionLineID, jobID, -1);
                    state.Game.OrderHandler.HandleOrder(cmd);
                }
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("Move up in the produciton queue.");
            }
            else
            {
                ImGui.InvisibleButton("invis", invisButtonSize);
            }
            ImGui.SameLine();

            if(index < count - 1)
            {
                if (ImGui.SmallButton("v"))
                {
                    var cmd = IndustryOrder2.CreateChangePriorityOrder(_factionID, Entity, productionLineID, jobID, 1);
                    state.Game.OrderHandler.HandleOrder(cmd);
                }
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("Move down in the produciton queue.");
            }
            else
            {
                ImGui.InvisibleButton("invis", invisButtonSize);
            }

            ImGui.SameLine();
            if (ImGui.SmallButton("x"))
            {
                //new ConstructCancelJob(_uiState.Faction.Guid, _selectedEntity.Guid, _selectedEntity.StarSysDateTime, _selectedExistingConJob.JobID);
                var cmd = IndustryOrder2.CreateCancelJobOrder(_factionID, Entity, productionLineID, jobID);

                state.Game.OrderHandler.HandleOrder(cmd);
            }
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Cancel the job.");
            ImGui.PopID();
        }
    }
}