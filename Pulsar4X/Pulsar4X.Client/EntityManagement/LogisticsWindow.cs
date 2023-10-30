using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Extensions;

namespace Pulsar4X.SDL2UI
{
    public class LogiShipWindow : PulsarGuiWindow
    {
        private int _factionID;
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
        internal static LogiShipWindow GetInstance(FactionDataStore staticData, EntityState state) {

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
                            var typename = _uiState.Faction.GetDataBlob<FactionInfoDB>().Data.CargoTypes[type.Key].Name;
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

                        var cargoLibrary = _uiState.Faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods;
                        var maxdv = OrbitMath.GetWetDV(_selectedEntity, maxMassVal, cargoLibrary);
                        var dv = OrbitMath.GetDV(_selectedEntity, maxMassVal, cargoLibrary);
                        ImGui.Text($"Max Dv:  {Stringify.Velocity(maxdv)}");
                        ImGui.Text($"Max Dv with current fuel: {Stringify.Velocity(dv)}");
                        
                        
                        if(ImGui.Button("Make it so"))
                        {
                            SetLogisticsOrder.CreateCommand_SetShipTypeAmounts(_selectedEntity, _changes);
                        }

                        ImGui.Text(_tradeshipDB.StateString);
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