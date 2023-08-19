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
        private Dictionary<Guid,IndustryAbilityDB.ProductionLine> _prodLines;
        private Guid _selectedProdLine;
        private int _selectedExistingIndex = -1;
        private IndustryJob _lastClickedJob { get; set; }
        private IConstrucableDesign _lastClickedDesign;
        private Entity Entity;
        private IndustryAbilityDB _industryDB;
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
                //_constructableDesigns[i] = kvp.Value;
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
                return;

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
                    ImGui.Text("Add job to " + _prodLines[_selectedProdLine].Name);
                }
                else
                {
                    ImGui.Text("Select a production line on the left");
                }

                ImGui.Separator();
                //EditButtonsDisplay(state);
                NewJobDisplay(state);
                if(_lastClickedJob != null)
                    CostsDisplay(_lastClickedJob, state);
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
                        if(ImGui.Button("Add New Job"))
                        {
                            _selectedProdLine = id;
                            _newjobSelectionIndex = (_selectedProdLine, 0);
                            _lastClickedJob = _selectedExistingConJob;
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
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.25f, 1f, 0.25f, 0.9f));
                            ImGui.Text(rate.ToString());
                            ImGui.PopStyleColor();
                            if(ImGui.IsItemHovered())
                                ImGui.SetTooltip("Assuming all resources needed are available.");

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
                                    //ImGui.ProgressBar(percent, progsize, "");
                                    ImGui.TableNextColumn();
                                    //ImGui.SetCursorPos(cpos);
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
                                    ImGui.Text((batchJob.ProductionPointsCost - batchJob.ProductionPointsLeft) + "/" + batchJob.ProductionPointsCost);
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
                                    }

                                    if(ji < line.Jobs.Count - 1)
                                    {
                                        ImGui.SameLine();
                                        if (ImGui.ImageButton(state.Img_Down(), new Vector2(16, 16)))
                                        {
                                            var cmd = IndustryOrder2.CreateChangePriorityOrder(_factionID, Entity, id, line.Jobs[ji].JobID, 1);
                                            StaticRefLib.OrderHandler.HandleOrder(cmd);
                                        }
                                    }

                                    ImGui.SameLine();
                                    if (ImGui.ImageButton(state.Img_Cancel(), new Vector2(16, 16)))
                                    {
                                        //new ConstructCancelJob(_uiState.Faction.Guid, _selectedEntity.Guid, _selectedEntity.StarSysDateTime, _selectedExistingConJob.JobID);
                                        var cmd = IndustryOrder2.CreateCancelJobOrder(_factionID, Entity, id, line.Jobs[ji].JobID);

                                        StaticRefLib.OrderHandler.HandleOrder(cmd);
                                    }
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

        void EditButtonsDisplay(GlobalUIState state)
        {
            //ImGui.BeginChild("Buttons", new Vector2(116, 100), true, ImGuiWindowFlags.ChildWindow);
            ImGui.BeginGroup();

            if (ImGui.ImageButton(state.Img_Up(), new Vector2(16, 8)) && _selectedExistingConJob != null)
            {
                var cmd = IndustryOrder2.CreateChangePriorityOrder(_factionID, Entity, _selectedProdLine, _selectedExistingConJob.JobID, -1);
                StaticRefLib.OrderHandler.HandleOrder(cmd);
            }

            if (ImGui.ImageButton(state.Img_Down(), new Vector2(16, 8)) && _selectedExistingConJob != null)
            {
                var cmd = IndustryOrder2.CreateChangePriorityOrder(_factionID, Entity, _selectedProdLine, _selectedExistingConJob.JobID, 1);
                StaticRefLib.OrderHandler.HandleOrder(cmd);
            }

            ImGui.EndGroup();
            ImGui.SameLine();
            if (ImGui.ImageButton(state.Img_Repeat(), new Vector2(16, 16)) && _selectedExistingConJob != null)
            {

                var jobcount = _selectedExistingConJob.NumberOrdered;
                var jobrepeat = _selectedExistingConJob.Auto;

                var cmd = IndustryOrder2.CreateEditJobOrder(_factionID, Entity, _selectedProdLine,_selectedExistingConJob.JobID, jobcount, !jobrepeat);
                StaticRefLib.OrderHandler.HandleOrder(cmd);
            }

            ImGui.SameLine();
            if (ImGui.ImageButton(state.Img_Cancel(), new Vector2(16, 16)) && _selectedExistingConJob != null)
            {
                //new ConstructCancelJob(_uiState.Faction.Guid, _selectedEntity.Guid, _selectedEntity.StarSysDateTime, _selectedExistingConJob.JobID);
                var cmd = IndustryOrder2.CreateCancelJobOrder(_factionID, Entity, _selectedProdLine, _selectedExistingConJob.JobID);

                StaticRefLib.OrderHandler.HandleOrder(cmd);
            }

            if (_lastClickedDesign != null)
            {
                if (_lastClickedDesign.GuiHints == ConstructableGuiHints.CanBeInstalled)
                {
                    ImGui.Checkbox("Auto Install on colony", ref _newJobAutoInstall);

                    if (_newJobAutoInstall)
                        _lastClickedJob.InstallOn = Entity;
                    else
                        _lastClickedJob.InstallOn = null;

                }

                if (_lastClickedDesign.GuiHints == ConstructableGuiHints.CanBeLaunched)
                {
                    if (Entity.HasDataBlob<ColonyInfoDB>())
                    {
                        var s = (ShipDesign)_lastClickedDesign;
                        var planet = Entity.GetDataBlob<ColonyInfoDB>().PlanetEntity;
                        var lowOrbit = planet.GetDataBlob<MassVolumeDB>().RadiusInM * 0.33333;

                        var mass = s.MassPerUnit;

                        var fuelCost = OrbitMath.FuelCostToLowOrbit(planet, mass);


                        if (ImGui.Button("Launch to Low Orbit"))
                        {
                            LaunchShipCmd.CreateCommand(_factionID, Entity, _selectedProdLine, _lastClickedJob.JobID);
                        }
                        //ImGui.SameLine();


                        ImGui.Text("Fuel Cost: " + fuelCost);

                    }
                }
            }

            //ImGui.EndGroup();

            //ImGui.Button("Install On Parent")

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
                }

                ImGui.Text("Enter the quantity:");
                ImGui.InputInt("##batchcount", ref _newJobbatchCount);
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("The production line will move to the next job in the queue\nafter finished the number of items requested.");

                ImGui.Text("Repeat this job?");
                ImGui.Checkbox("##repeat", ref _newJobRepeat);
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("A repeat job will run until cancelled.");

                if (ImGui.Button("Add Job to " + _prodLines[_selectedProdLine].Name))
                {

                    _newConJob = new IndustryJob(state.Faction.GetDataBlob<FactionInfoDB>(), SelectedConstrucableID);

                    var cmd = IndustryOrder2.CreateNewJobOrder(_factionID, Entity, _selectedProdLine, _newConJob);
                    _newConJob.InitialiseJob((ushort)_newJobbatchCount, _newJobRepeat);
                    if(_newJobAutoInstall)
                        _newConJob.InstallOn = Entity;
                    _lastClickedJob = _newConJob;
                    _lastClickedDesign = _factionInfoDB.IndustryDesigns[SelectedConstrucableID];
                    StaticRefLib.OrderHandler.HandleOrder(cmd);
                }

            }

            //ImGui.EndChild();
        }

        void CostsDisplay(JobBase selectedJob, GlobalUIState state)
        {
            //ImGui.BeginChild("Resources Requred", new Vector2(294, 184 ), true, ImGuiWindowFlags.ChildWindow);
            ImGui.NewLine();
            ImGui.Text("Job Cost:");
            ImGui.Separator();
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 140);
            ImGui.SetColumnWidth(1, 128);
            ImGui.Text("Industry Points");
            ImGui.NextColumn();
            ImGui.Text(selectedJob.ProductionPointsLeft.ToString());
            ImGui.NextColumn();

            foreach (var item in selectedJob.ResourcesRequired)
            {
                ICargoable cargoItem = StaticRefLib.StaticData.CargoGoods.GetAny(item.Key);
                if (cargoItem == null)
                    cargoItem = state.Faction.GetDataBlob<FactionInfoDB>().ComponentDesigns[item.Key];
                ImGui.Text(cargoItem.Name);
                ImGui.NextColumn();
                ImGui.Text(item.Value.ToString());
                ImGui.NextColumn();
            }

            //ImGui.EndChild();
        }
    }
}