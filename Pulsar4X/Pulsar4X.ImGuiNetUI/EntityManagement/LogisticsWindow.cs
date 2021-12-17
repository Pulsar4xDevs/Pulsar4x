using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using ImGuiNET;
using Pulsar4X.ECSLib;


namespace Pulsar4X.SDL2UI
{
    //a do nothing helper class that is plugged into generics for static checks
    public class LogiBaseWindow : PulsarGuiWindow
    {
        private Dictionary<ICargoable,(int count, int demandSupplyWeight)> _changes = new Dictionary<ICargoable, (int count, int demandSupplyWeight)>();

        private List<ICargoable> _allResources;
        private string[] _allResourceNames;
        private List<Guid> _allResourceID;
        private int _allResourceIndex = 0;
        private Dictionary<Guid, Dictionary<ICargoable, (int count, int demandSupplyWeight)>> _displayedResources;
        private EntityState _entityState;
        private Entity _selectedEntity;
        private LogiBaseDB _tradebaseDB;

        private VolumeStorageDB _volStorageDB;

        private Dictionary<Guid, TypeStore> _stores;
        private StaticDataStore _staticData;
        private LogiBaseWindow(EntityState entity)
        {
            SetEntity(entity);
            var allgoods = StaticRefLib.StaticData.CargoGoods.GetAll();
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
            _staticData = StaticRefLib.StaticData;
        }
        string _demandHint = "";
        string _demandBuff = "";
        internal static LogiBaseWindow GetInstance(StaticDataStore staticData, EntityState state) {

            LogiBaseWindow instance;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(LogiBaseWindow)))
            {
                instance = new LogiBaseWindow(_uiState.LastClickedEntity);
            }
            else
            {
                instance = (LogiBaseWindow)_uiState.LoadedWindows[typeof(LogiBaseWindow)];
            }
            if(instance._entityState != _uiState.LastClickedEntity)
                instance.SetEntity(_uiState.LastClickedEntity);
            
            return instance;
        } 

        public void SetEntity(EntityState entityState)
        {
            _entityState = entityState;
            _selectedEntity = _entityState.Entity;
            if (entityState.DataBlobs.ContainsKey(typeof(OrderableDB)))
            {
                if(_selectedEntity.HasDataBlob<LogiBaseDB>())
                    _tradebaseDB = _selectedEntity.GetDataBlob<LogiBaseDB>();
                else{_tradebaseDB = null;}
                if(_selectedEntity.HasDataBlob<VolumeStorageDB>())
                    _volStorageDB = _selectedEntity.GetDataBlob<VolumeStorageDB>();
                else{_volStorageDB = null;}
                Update();
                CanActive = true;//And note if that it can be displayed
            }
            else
            {
                //CanActive = false;
                //_entityState = null;
            }
        }

        public void Update()
        {
            if (_volStorageDB == null) //if this colony does not have any storage. 
                return;

            _changes = new Dictionary<ICargoable, (int count, int demandSupplyWeight)>();
            _displayedResources = new Dictionary<Guid, Dictionary<ICargoable, (int count, int demandSupplyWeight)>>();

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
                _displayedResources.Add(stypeID, new Dictionary<ICargoable, (int count, int demandSupplyWeight)>());
                foreach (var item in kvp.Value.Cargoables)
                {
                    var ctypeID = item.Key;
                    var ctype = item.Value;
                    var numUnits = kvp.Value.CurrentStoreInUnits[ctypeID];
                    _displayedResources[stypeID].Add(ctype, ((int)numUnits, 1));
                }
            }
            

            
            
        }

        internal override void EntityClicked(EntityState entity, MouseButtons button)
        {
            if (button == MouseButtons.Primary)
                SetEntity(entity);
        }

        internal override void Display()
        {
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(264, 325), ImGuiCond.Once);
            if(IsActive)
            if (ImGui.Begin("Logisitcs Base", ref IsActive, _flags))
            {
                if(!_selectedEntity.HasDataBlob<LogiBaseDB>())
                {
                    if(ImGui.Button("Set this entity as an importer/exporter"))
                    {
                        SetLogisticsOrder.CreateCommand(_selectedEntity, SetLogisticsOrder.OrderTypes.AddLogiBaseDB);
                        _tradebaseDB = _selectedEntity.GetDataBlob<LogiBaseDB>();
                    }
                }
                else
                {
                    if(ImGui.Button("Disable this entity as an inporter/exporter"))
                    {
                        SetLogisticsOrder.CreateCommand(_selectedEntity, SetLogisticsOrder.OrderTypes.RemoveLogiBaseDB);
                    }
                    ImGui.Columns(4);
                    foreach (var typeStore in _displayedResources)
                    {
                        CargoTypeSD stype = _staticData.CargoTypes[typeStore.Key];
                        var stypeID = typeStore.Key;
                        var stypeName = stype.Name;
                        foreach (var item in _changes)
                        {
                            var typeID = item.Key.CargoTypeID;
                            if(_displayedResources.ContainsKey(typeID))
                            {
                                if(!_displayedResources[typeID].ContainsKey(item.Key))
                                    _displayedResources[typeID].Add(item.Key, item.Value);
                                else
                                    _displayedResources[typeID][item.Key] = item.Value;
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
                            ImGui.Text(ctype.Name);
                            ImGui.NextColumn();
                            ImGui.Text(Stringify.Number(itemsStored));
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
                            if(_tradebaseDB.ListedItems.ContainsKey(ctype))
                            {
                                int total = _tradebaseDB.ListedItems[ctype].count;
                                
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

                    /*
                    if(ImGui.InputTextWithHint("Add Demand", _demandHint, _demandBuff, 32 ))
                    {

                    }
                    */
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
                    foreach (var item in _tradebaseDB.ListedItems)
                    {
                        ImGui.Text(item.Key.Name);
                        ImGui.NextColumn();
                        ImGui.Text(Stringify.Number(item.Value.count));
                        ImGui.NextColumn();
                        
                    }
                    ImGui.Columns(1);
                }
            
            
            }   
        }
    }
    

    public class LogiShipWindow : PulsarGuiWindow
    {
        private System.Guid _factionID;
        private EntityState _entityState;
        private Entity _selectedEntity;
        private LogiShipperDB _tradeshipDB;
        private VolumeStorageDB _cargoDB;

        private SetLogisticsOrder.Changes _changes;
        private double _changeMass = 1;

        private LogiShipWindow(EntityState entity)
        {
            SetEntity(entity);
        }
        internal static LogiShipWindow GetInstance(StaticDataStore staticData, EntityState state) {

            LogiShipWindow instance;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(LogiShipWindow)))
            {
                instance = new LogiShipWindow(_uiState.LastClickedEntity);
            }
            else
            {
                instance = (LogiShipWindow)_uiState.LoadedWindows[typeof(LogiShipWindow)];
            }
            if(instance._entityState != _uiState.LastClickedEntity)
                instance.SetEntity(_uiState.LastClickedEntity);
            return instance;
        } 

        public void SetEntity(EntityState entityState)
        {
            _entityState = entityState;
            _selectedEntity = _entityState.Entity;
            _changes = new SetLogisticsOrder.Changes();
            if (_selectedEntity.HasDataBlob<OrderableDB>())
            {
                if(_selectedEntity.HasDataBlob<LogiShipperDB>())
                    _tradeshipDB = _selectedEntity.GetDataBlob<LogiShipperDB>();
                else{_tradeshipDB = null;}
                if(_selectedEntity.HasDataBlob<VolumeStorageDB>())
                    _cargoDB = _selectedEntity.GetDataBlob<VolumeStorageDB>();
                else{_cargoDB = null;}
                CanActive = true;//And note if that it can be displayed
                _factionID = _selectedEntity.FactionOwnerID;
            }
            else
            {
                //CanActive = false;
                //_entityState = null;
            }
        }

        internal override void EntityClicked(EntityState entity, MouseButtons button)
        {
            if (button == MouseButtons.Primary)
                SetEntity(entity);
        }
        internal override void Display()
        {
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(264, 325), ImGuiCond.Once);
            if(IsActive)
            {
                if (ImGui.Begin("Logisitcs Ship", ref IsActive, _flags))
                {
                    if(!_selectedEntity.HasDataBlob<LogiShipperDB>())
                    {
                        if(ImGui.Button("Set this entity as an independant Trade Ship"))
                        {
                            SetLogisticsOrder.CreateCommand(_selectedEntity, SetLogisticsOrder.OrderTypes.AddLogiShipDB);
                            _tradeshipDB = _selectedEntity.GetDataBlob<LogiShipperDB>();
                        }

                    }
                    else
                    {
                        if(ImGui.Button("Disable this entity as an independant Trade Ship"))
                        {
                            SetLogisticsOrder.CreateCommand(_selectedEntity, SetLogisticsOrder.OrderTypes.RemoveLogiShipDB);
                        }

                        ImGui.Text("Allocate amount of cargo space for trade");
                        double totalVol = 0;
                        foreach (var type in _cargoDB.TypeStores)
                        {
                            var typename = StaticRefLib.StaticData.CargoTypes[type.Key].Name;
                            var typeVol = type.Value.MaxVolume;
                            totalVol += typeVol;
                            var currentVal = _tradeshipDB.TradeSpace[type.Key];
                            var volAmounts = _changes.VolumeAmounts;
                            if(volAmounts.ContainsKey(type.Key))
                                currentVal = volAmounts[type.Key];
                            
                            if(ImGuiExt.SliderDouble(typename, ref currentVal, 0, typeVol))
                            {
                                if(!volAmounts.ContainsKey(type.Key))
                                    volAmounts.Add(type.Key, currentVal);
                                else
                                    volAmounts[type.Key] = currentVal;
                                if (_changes.MaxMass == 0)
                                    _changes.MaxMass = (int)totalVol;
                            }
                        }

                        int maxMassVal = _changes.MaxMass;
                        if(ImGui.SliderInt("MassConstraint", ref maxMassVal, 0, (int)totalVol))
                        {
                            _changes.MaxMass = maxMassVal;
                        }


                        var maxdv = OrbitMath.GetWetDV(_selectedEntity, maxMassVal);
                        var dv = OrbitMath.GetDV(_selectedEntity, maxMassVal);
                        ImGui.Text($"Max Dv:  {Stringify.Velocity(maxdv)}");
                        ImGui.Text($"Max Dv with current fuel: {Stringify.Velocity(dv)}");
                        
                        
                        if(ImGui.Button("Make it so"))
                        {
                            SetLogisticsOrder.CreateCommand_SetShipTypeAmounts(_selectedEntity, _changes);
                        }
                        ImGui.Columns(2);
                        foreach (var item in _tradeshipDB.ActiveCargoTasks)
                        {
                            ImGui.Text("From: " + item.Source.GetName(_factionID));
                            ImGui.NextColumn();
                            ImGui.Text("To: " + item.Destination.GetName(_factionID));
                            ImGui.NextColumn();
                            ImGui.Text(item.item.Name);
                            ImGui.NextColumn();
                            ImGui.Text(item.NumberOfItems.ToString());
                            ImGui.NextColumn();
                            
                        }
                        ImGui.Columns(1);


                        ImGui.Text("Current Cargo Manafest");
                        //ImGui.Text("Maybe this should show entire cargo, not just trade cargo, and use colours to diffentiate?");
                        ImGui.Columns(2);
                        foreach (var item in _tradeshipDB.ItemsToShip)
                        {
                            ImGui.Text(item.item.Name);
                            ImGui.NextColumn();
                            ImGui.Text(item.count.ToString());
                            ImGui.NextColumn();   
                        }
                        ImGui.Columns(1);
                    }
                }
            }
        }
    }

    public class AutoComplete
    {
        List<string> SugestionList = new List<string>();


        public AutoComplete(List<string> sugestionList)
        {
            SugestionList = sugestionList;
        }

        public string Sugestion(string fromInput)
        {
            
            string hint = SugestionList[0];
            int j = 0;
            

            for (int i = 0; i < fromInput.Length; i++)
            {
                
                var chr = fromInput[i];
                if(hint[j] == chr)
                {
                    j++;
                    continue;
                }
            
            
            
            }
            return hint;
        }




    }

}