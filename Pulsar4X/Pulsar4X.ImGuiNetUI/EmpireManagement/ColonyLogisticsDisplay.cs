using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class ColonyLogisticsDisplay
    {
        private static ColonyLogisticsDisplay instance = null;
        private static readonly object padlock = new object();

        private Dictionary<ICargoable,(int count, int demandSupplyWeight)> _changes = new Dictionary<ICargoable, (int count, int demandSupplyWeight)>();

        private List<ICargoable> _allResources;
        private string[] _allResourceNames;
        private List<Guid> _allResourceID;
        private int _allResourceIndex = 0;
        private Dictionary<Guid, Dictionary<ICargoable, (int count, int demandSupplyWeight)>> _displayedStoredResources;
        private Dictionary<Guid, Dictionary<ICargoable, (int count, int demandSupplyWeight)>> _displayedUnstored;
        private EntityState _entityState;
        private Entity _selectedEntity;
        private LogiBaseDB _logisticsDB;
        private VolumeStorageDB _volStorageDB;
        private Dictionary<Guid, TypeStore> _stores;
        private StaticDataStore _staticData;
        private bool isEnabled;
        private ColonyLogisticsDisplay(EntityState entity)
        {
            _staticData = StaticRefLib.StaticData;
            var allgoods = _staticData.CargoGoods.GetAll();
            var allResourceNames = new List<string>();
            _allResourceID = new List<Guid>();
            _allResources = new List<ICargoable>();
            foreach (var item in allgoods)
            {
                allResourceNames.Add(item.Value.Name);
                _allResourceID.Add(item.Key);
                _allResources.Add(item.Value);
            }
            _allResourceNames = allResourceNames.ToArray();

            SetEntity(entity);
        }
        string _demandHint = "";
        string _demandBuff = "";
        internal static ColonyLogisticsDisplay GetInstance(StaticDataStore staticData, EntityState state) {
            lock(padlock)
            {
                if(instance == null)
                {
                    instance = new ColonyLogisticsDisplay(state);
                }
                instance.SetEntity(state);
            }

            return instance;
        }

        public void SetEntity(EntityState entityState)
        {
            _entityState = entityState;
            _selectedEntity = _entityState.Entity;
            if (entityState.DataBlobs.ContainsKey(typeof(OrderableDB)))
            {
                _selectedEntity.TryGetDatablob<LogiBaseDB>(out _logisticsDB);
                _selectedEntity.TryGetDatablob<VolumeStorageDB>(out _volStorageDB);

                Update();
            }
        }

        public void Update()
        {
            if (_volStorageDB == null || _logisticsDB == null) //if this colony does not have any storage.
                return;

            _changes = new Dictionary<ICargoable, (int count, int demandSupplyWeight)>();
            _displayedStoredResources = new Dictionary<Guid, Dictionary<ICargoable, (int count, int demandSupplyWeight)>>();
            _displayedUnstored = new Dictionary<Guid, Dictionary<ICargoable, (int count, int demandSupplyWeight)>>();
            //we do a deep copy clone so as to avoid a thread collision when we loop through.
            var newDict = new Dictionary<Guid, TypeStore>();

            ICollection ic = _volStorageDB.TypeStores;
            lock (ic.SyncRoot)
            {
                foreach (var kvp in _volStorageDB.TypeStores)
                {
                    newDict.Add(kvp.Key, kvp.Value.Clone());
                }
            }
            _stores = newDict;
            foreach (var kvp in _stores)
            {
                var stypeID = kvp.Key;
                _displayedStoredResources.Add(stypeID, new Dictionary<ICargoable, (int count, int demandSupplyWeight)>());
                var unitsInStore = kvp.Value.CurrentStoreInUnits.Get();
                foreach (var item in kvp.Value.GetCargoables())
                {
                    var ctypeID = item.Key;
                    var ctype = item.Value;
                    var numUnits = unitsInStore[ctypeID];
                    _displayedStoredResources[stypeID].Add(ctype, ((int)numUnits, 1));
                }
            }

            for (int i = 0; i < _allResources.Count; i++)
            {
                var ctypeID = _allResourceID[i];
                var ctype = _allResources[i];
                var stypeID = ctype.CargoTypeID;
                if (_stores.ContainsKey(stypeID))
                {
                    if (!_displayedUnstored.TryGetValue(stypeID, out Dictionary<ICargoable, (int count, int demandSupplyWeight)> indic))
                    {
                        indic = new Dictionary<ICargoable, (int count, int demandSupplyWeight)>();
                        _displayedUnstored.Add(stypeID, indic);
                    }
                    if (!_displayedStoredResources[stypeID].ContainsKey(ctype))//don't add anything already in stored
                    {
                        indic.TryAdd(ctype, (0, 1));
                    }
                }
            }
        }

        public void Display()
        {
            if(_volStorageDB == null || _logisticsDB == null)
                DisplayDisabledMessage();

            Vector2 topSize = ImGui.GetContentRegionAvail();
            if(ImGui.BeginChild("LogisticsHeader", new Vector2(topSize.X, 28f), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.Text("Logistics Capacity:");
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.HighlightColor);
                ImGui.SameLine();
                ImGui.Text(Stringify.Quantity(_logisticsDB.ListedItems.Count));
                ImGui.PopStyleColor();
                ImGui.SameLine();
                ImGui.Text("/");
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.HighlightColor);
                ImGui.SameLine();
                ImGui.Text(Stringify.Quantity(_logisticsDB.Capacity));
                ImGui.PopStyleColor();

                ImGui.SameLine();
                ImGui.Text(" Items in Transit:");
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.HighlightColor);
                ImGui.SameLine();
                ImGui.Text(Stringify.Quantity(_logisticsDB.ItemsInTransit.Count));
                ImGui.PopStyleColor();

                ImGui.SameLine();
                ImGui.Text(" Waiting for Pickup:");
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.HighlightColor);
                ImGui.SameLine();
                ImGui.Text(Stringify.Quantity(_logisticsDB.ItemsWaitingPickup.Count));
                ImGui.PopStyleColor();

                ImGui.EndChild();
            }

            Vector2 windowContentSize = ImGui.GetContentRegionAvail();
            var firstChildSize = new Vector2(windowContentSize.X * 0.33f, windowContentSize.Y);
            var secondChildSize = new Vector2(windowContentSize.X * 0.33f, windowContentSize.Y);
            var thirdChildSize = new Vector2(windowContentSize.X * 0.33f - (windowContentSize.X * 0.01f), windowContentSize.Y);
            if(ImGui.BeginChild("ColonyLogistics1", firstChildSize, true))
            {
                DisplayHelpers.Header("Imports");

                if(ImGui.BeginTable("LogisticsImportsTable", 3, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 1f);
                    ImGui.TableSetupColumn("Quantity Desired", ImGuiTableColumnFlags.None, 1f);
                    ImGui.TableSetupColumn("", ImGuiTableColumnFlags.None, 0.25f);
                    ImGui.TableHeadersRow();

                    foreach(var (cargoable, trade) in _logisticsDB.ListedItems)
                    {
                        // Greater than zero is for exports
                        if(trade.count >= 0) continue;

                        ImGui.TableNextColumn();
                        ImGui.Text(cargoable.Name);
                        ImGui.TableNextColumn();
                        ImGui.Text(Stringify.Quantity(trade.count * -1)); // display as a positive number
                        ImGui.TableNextColumn();
                        if(ImGui.SmallButton(">##impt"+cargoable.Name))
                        {
                            _changes = new Dictionary<ICargoable, (int count, int demandSupplyWeight)>();
                            _changes.TryAdd(cargoable, (0,0));
                            SetLogisticsOrder.CreateCommand_SetBaseItems(_selectedEntity, _changes);
                        }
                    }

                    ImGui.EndTable();
                }

                ImGui.EndChild();
            }
            ImGui.SameLine();
            if(ImGui.BeginChild("ColonyLogistics2", secondChildSize, true))
            {
                DisplayHelpers.Header("Goods Available to Import or Export");

                if(ImGui.BeginTable("LogisticsAvailableItemsTable", 4, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn("", ImGuiTableColumnFlags.None, 0.25f);
                    ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 1f);
                    ImGui.TableSetupColumn("In Local Storage", ImGuiTableColumnFlags.None, 1f);
                    ImGui.TableSetupColumn("", ImGuiTableColumnFlags.None, 0.25f);
                    ImGui.TableHeadersRow();

                    bool hasCapacityForMore = _logisticsDB.Capacity > _logisticsDB.ListedItems.Count;
                    // FIXME: this should show every item and component in the game? if so, it should
                    // allow the player to filter the list by searching
                    displayResources(_displayedStoredResources);

                    ImGui.TableNextColumn();
                    ImGui.Separator();
                    ImGui.TableNextColumn();
                    ImGui.Separator();
                    ImGui.TableNextColumn();
                    ImGui.Separator();
                    ImGui.TableNextColumn();
                    ImGui.Separator();

                    displayResources(_displayedUnstored);
                    void displayResources(Dictionary<Guid, Dictionary<ICargoable, (int count, int demandSupplyWeight)>> dict)
                    {
                        foreach (var typeStore in dict)
                        {
                            CargoTypeSD stype = _staticData.CargoTypes[typeStore.Key];
                            var stypeID = typeStore.Key;
                            var stypeName = stype.Name;
                            foreach (var item in _changes)
                            {
                                var typeID = item.Key.CargoTypeID;
                                if (dict.ContainsKey(typeID))
                                {
                                    if (!dict[typeID].ContainsKey(item.Key))
                                        dict[typeID].Add(item.Key, item.Value);
                                    else
                                        dict[typeID][item.Key] = item.Value;
                                }
                                else
                                {
                                    //this base cannot store this item.
                                }
                            }

                            var cargoables = _stores[stypeID].GetCargoables();
                            var unitsInStore = _stores[stypeID].CurrentStoreInUnits.Get();
                            foreach (var kvp in typeStore.Value)
                            {
                                var ctype = kvp.Key;

                                if (_logisticsDB.ListedItems.ContainsKey(ctype))
                                    continue;

                                var cname = ctype.Name;
                                var itemsStored = 0;
                                if (cargoables.ContainsKey(ctype.ID))
                                    itemsStored = (int)unitsInStore[ctype.ID];
                                var volumePerItem = ctype.VolumePerUnit;

                                ImGui.TableNextColumn();
                                if (!hasCapacityForMore)
                                    ImGui.BeginDisabled();
                                if (ImGui.SmallButton("<##"+cname))
                                {
                                    _changes = new Dictionary<ICargoable, (int count, int demandSupplyWeight)>();
                                    _changes.TryAdd(ctype, (-1,1));
                                    SetLogisticsOrder.CreateCommand_SetBaseItems(_selectedEntity, _changes);
                                }

                                if (!hasCapacityForMore)
                                    ImGui.EndDisabled();

                                ImGui.TableNextColumn();
                                ImGui.Text(cname);

                                ImGui.TableNextColumn();
                                ImGui.Text(Stringify.Number(itemsStored, "#,###"));

                                ImGui.TableNextColumn();
                                if (!hasCapacityForMore)
                                    ImGui.BeginDisabled();
                                if (ImGui.SmallButton(">##"+cname))
                                {
                                    _changes = new Dictionary<ICargoable, (int count, int demandSupplyWeight)>();
                                    _changes.TryAdd(ctype, (1,1));
                                    SetLogisticsOrder.CreateCommand_SetBaseItems(_selectedEntity, _changes);
                                }

                                if (!hasCapacityForMore)
                                    ImGui.EndDisabled();
                            }
                        }
                    }

                    ImGui.EndTable();
                }

                ImGui.EndChild();
            }
            ImGui.SameLine();
            if(ImGui.BeginChild("ColonyLogistics3", thirdChildSize, true))
            {
                DisplayHelpers.Header("Exports");

                if(ImGui.BeginTable("LogisticsExportsTable", 3, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn("", ImGuiTableColumnFlags.None, 0.25f);
                    ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 1f);
                    ImGui.TableSetupColumn("Quantity Available", ImGuiTableColumnFlags.None, 1f);
                    ImGui.TableHeadersRow();

                    foreach(var (cargoable, trade) in _logisticsDB.ListedItems)
                    {
                        // Less than zero is for imports
                        if(trade.count < 0) continue;

                        ImGui.TableNextColumn();
                        if(ImGui.SmallButton("<##xpt"+cargoable.Name))
                        {
                            _changes = new Dictionary<ICargoable, (int count, int demandSupplyWeight)>();
                            _changes.TryAdd(cargoable, (0,0));
                            SetLogisticsOrder.CreateCommand_SetBaseItems(_selectedEntity, _changes);
                        }
                        ImGui.TableNextColumn();
                        ImGui.Text(cargoable.Name);
                        ImGui.TableNextColumn();
                        ImGui.Text(Stringify.Quantity(trade.count));
                    }

                    ImGui.EndTable();
                }

                ImGui.EndChild();
            }

            /*
            if (ImGui.BeginChild("Colony Logistics Base"))
            {
                if(ImGui.Button("Disable this entity as an importer/exporter"))
                {
                    SetLogisticsOrder.CreateCommand(_selectedEntity, SetLogisticsOrder.OrderTypes.RemoveLogiBaseDB);
                }
                ImGui.Columns(4);
                foreach (var typeStore in _displayedStoredResources)
                {
                    CargoTypeSD stype = _staticData.CargoTypes[typeStore.Key];
                    var stypeID = typeStore.Key;
                    var stypeName = stype.Name;
                    foreach (var item in _changes)
                    {
                        var typeID = item.Key.CargoTypeID;
                        if(_displayedStoredResources.ContainsKey(typeID))
                        {
                            if(!_displayedStoredResources[typeID].ContainsKey(item.Key))
                                _displayedStoredResources[typeID].Add(item.Key, item.Value);
                            else
                                _displayedStoredResources[typeID][item.Key] = item.Value;
                        }
                        else
                            {//this base cannot store this item.
                            }
                    }

                    foreach (var kvp in typeStore.Value)
                    {
                        var ctype = kvp.Key;
                        var cname = ctype.Name;
                        var itemsStored = 0;
                        if(_stores[stypeID].Cargoables.ContainsKey(ctype.ID))
                            itemsStored = (int)_stores[stypeID].CurrentStoreInUnits[ctype.ID];
                        var volumePerItem = ctype.VolumePerUnit;
                        ImGui.Text(cname);
                        ImGui.NextColumn();
                        ImGui.Text(Stringify.Number(itemsStored, "#,###"));
                        ImGui.NextColumn();
                        if(ImGui.SmallButton("+##"+ctype.ID))
                        {
                            if(!_changes.ContainsKey(ctype))
                                _changes.Add(ctype, (1, 1));
                            else
                            {
                                _changes[ctype] = (_changes[ctype].count + 1, 1);
                            }
                        }
                        ImGui.SameLine();
                        if(ImGui.SmallButton("-##"+ctype.ID))
                        {
                            if(!_changes.ContainsKey(ctype))
                                _changes.Add(ctype, (-1, 1));
                            else
                            {
                                _changes[ctype] = (_changes[ctype].count - 1, 1);
                            }
                        }
                        if(_changes.ContainsKey(ctype) && _changes[ctype].count == 0)
                                {_changes.Remove(ctype);}
                        ImGui.NextColumn();
                        if(_logisticsDB.ListedItems.ContainsKey(ctype))
                        {
                            int total = _logisticsDB.ListedItems[ctype].count;

                            if(_changes.ContainsKey(ctype))
                            {
                                total += _changes[ctype].count;
                            }
                            ImGui.Text(Stringify.Number(total));
                        }
                        else if(_changes.ContainsKey(ctype))
                        {
                            double total = _changes[ctype].count;
                            ImGui.Text(Stringify.Number(total));
                        }

                        ImGui.NextColumn();
                    }
                }

                //ImGui.BeginCombo()
                if(ImGui.Combo("Add Demand", ref _allResourceIndex, _allResourceNames, 10))
                {
                    var resource = _allResources[_allResourceIndex];
                    if(!_changes.ContainsKey(resource))
                        _changes.Add(resource, (-1, 1));
                }

                if(_changes.Count > 0)
                {
                    if(ImGui.Button("Make it so"))
                    {
                        SetLogisticsOrder.CreateCommand_SetBaseItems(_selectedEntity, _changes);
                    }
                }
                ImGui.Columns(2);
                foreach (var item in _logisticsDB.ListedItems)
                {
                    ImGui.Text(item.Key.Name);
                    ImGui.NextColumn();
                    ImGui.Text(Stringify.Number(item.Value.count));
                    ImGui.NextColumn();
                }
                ImGui.Columns(1);

                ImGui.EndChild();
            }
            */
        }

        private void DisplayDisabledMessage()
        {
            Vector2 topSize = ImGui.GetContentRegionAvail();
            if(ImGui.BeginChild("LogisticsDisabled", new Vector2(topSize.X, 28f), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.OkColor);
                ImGui.Text("You need an Installation with a Logistics component to enable logistics on this colony.");
                ImGui.PopStyleColor();

                ImGui.EndChild();
            }
        }
    }
}