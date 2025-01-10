using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Blueprints;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.SDL2UI;
public delegate void CargoItemSelectedHandler(CargoListPanelComplex cargoPannel);
public class CargoListPanelComplex
{
    FactionDataStore _staticData;
    EntityState _entityState;
    CargoStorageDB _volStorageDB;
    SafeDictionary<string, TypeStore> _stores = new ();
    Dictionary<ICargoable, long> _cargoToMove = new ();
    Dictionary<ICargoable, long> _cargoToMoveUI = new ();
    Dictionary<ICargoable, long> _cargoToMoveOrders = new ();
    Dictionary<ICargoable, long> _cargoToMoveDatablob = new ();

    //Dictionary<Guid, CargoTypeStoreVM> _cargoResourceStoresDict = new Dictionary<Guid, CargoTypeStoreVM>();
    //public List<CargoTypeStoreVM> CargoResourceStores { get; } = new List<CargoTypeStoreVM>();
    public ICargoable? selectedCargo;
    internal Dictionary<Guid,bool> HeadersIsOpenDict { get; set; }

    public CargoListPanelComplex(FactionDataStore staticData, EntityState entity, Dictionary<Guid,bool> headersOpenDict)
    {
        _staticData = staticData;
        _entityState = entity;
        _volStorageDB = entity.Entity.GetDataBlob<CargoStorageDB>();
        HeadersIsOpenDict = headersOpenDict;

        Update();
    }


    public event CargoItemSelectedHandler? CargoItemSelectedEvent;

    public void Update()
    {

        _stores = _volStorageDB.TypeStores;
        

        if (_entityState.Entity.TryGetDatablob<CargoTransferDB>(out var db))
        {
            var itemsToXfer = db.GetItemsToTransfer();
            var newxferDict = new Dictionary<ICargoable, long>();
            foreach (var tuple in itemsToXfer)
            {
                newxferDict.Add(tuple.item, tuple.unitCount);
            }
            _cargoToMoveDatablob = newxferDict;
        }

        /*
        if (_entityState.Entity.HasDataBlob<OrderableDB>())
        {
            var orders = _entityState.Entity.GetDataBlob<OrderableDB>().ActionList.ToArray();
            var newxferDict = new Dictionary<ICargoable, long>();
            foreach (var order in orders)
            {
                if (order is CargoTransferOrder)
                {
                    var xferOrder = (CargoTransferOrder)order;

                    bool isPrimary = xferOrder.IsPrimaryEntity;
                    foreach (var tuple in xferOrder.)
                    {
                        var cargoItem = tuple.item;
                        var cargoAmount = tuple.amount;
                        if (!isPrimary)
                            cargoAmount *= -1;
                        if (!newxferDict.ContainsKey(cargoItem))
                            newxferDict.Add(cargoItem, cargoAmount);
                        else
                            newxferDict[cargoItem] += cargoAmount;
                    }
                }
            }

            _cargoToMoveOrders = newxferDict;
        }*/
        UpdateTotalMoving();

    }

    internal List<(ICargoable, long)> GetAllToMoveOut()
    {
        var listToMove = new List<(ICargoable, long)>();

        foreach (var item in _cargoToMoveUI)
        {
            if(item.Value < 0)
                listToMove.Add((item.Key, item.Value * -1));
        }
        return listToMove;
    }

    internal List<(ICargoable, long)> GetAllToMove()
    {
        var listToMove = new List<(ICargoable, long)>();

        foreach (var item in _cargoToMoveUI)
        {
            listToMove.Add((item.Key, item.Value));
        }
        return listToMove;
    }

    internal void ClearUINumbers()
    {
        _cargoToMoveUI = new Dictionary<ICargoable, long>();
        Update();
    }

    internal bool CanStore(string cargoTypeID)
    {
        return _stores.ContainsKey(cargoTypeID);

    }

    internal bool CanInstall(ICargoable cargoItem)
    {
        if (_entityState.Entity.HasDataBlob<ColonyInfoDB>())
        {
            if (cargoItem is ComponentDesign)
            {
                var componentDesign = (ComponentDesign)cargoItem;
                if ((componentDesign.ComponentMountType & ComponentMountType.PlanetInstallation) == ComponentMountType.PlanetInstallation)
                {
                    return true;
                }
            }
            
        }
        if (_entityState.Entity.HasDataBlob<ShipInfoDB>())
        {
            if (cargoItem is ComponentDesign)
            {
                var componentDesign = (ComponentDesign)cargoItem;
                if ((componentDesign.ComponentMountType & ComponentMountType.ShipComponent) == ComponentMountType.ShipComponent)
                {
                    return true;
                }
            }
            
        }
        return false;
    }

    internal void AddUICargoIn(ICargoable cargoItem, long itemCount)
    {
        if(!_cargoToMoveUI.ContainsKey(cargoItem))
            _cargoToMoveUI.Add(cargoItem, itemCount);
        else
            _cargoToMoveUI[cargoItem] += itemCount;

        UpdateTotalMoving();

    }

    public void UpdateTotalMoving()
    {
        var newDict = new Dictionary<ICargoable, long>();
        
        foreach (var kvp in _cargoToMoveDatablob)
        {
            if(!newDict.ContainsKey(kvp.Key))
                newDict.Add(kvp.Key, kvp.Value);
            else
                newDict[kvp.Key] += kvp.Value;
        }/*
        foreach (var kvp in _cargoToMoveOrders)
        {
            if(!newDict.ContainsKey(kvp.Key))
                newDict.Add(kvp.Key, kvp.Value);
            else
                newDict[kvp.Key] += kvp.Value;
        }*/
        foreach (var kvp in _cargoToMoveUI)
        {
            if(!newDict.ContainsKey(kvp.Key))
                newDict.Add(kvp.Key, kvp.Value);
            else
                newDict[kvp.Key] += kvp.Value;
        }
        _cargoToMove = newDict;
    }


    internal bool HasCargoInStore(ICargoable cargoItem)
    {
        if (_stores.ContainsKey(cargoItem.CargoTypeID))
        {
            return _stores[cargoItem.CargoTypeID].HasCargoInStore(cargoItem.ID);
        }
        return false;
    }

    public void Display()
    {

        ImGui.BeginChild(_entityState.Name, new Vector2(360, 200), true);
        ImGui.Text(_entityState.Name);
        ImGui.Text("Transfer Rate: " + _volStorageDB.TransferRate);
        ImGui.Text("At DeltaV < " + Stringify.Velocity(_volStorageDB.TransferRangeDv_mps));

        foreach (var typeStoreKVP in _stores)
        {
            TypeStore typeStore = typeStoreKVP.Value;
            CargoTypeBlueprint stype = _staticData.CargoTypes[typeStoreKVP.Key];
            var freeVolume = _volStorageDB.GetFreeVolume(typeStoreKVP.Key);
            var maxVolume = typeStore.MaxVolume;
            var storedVolume = maxVolume - freeVolume;
            ImGui.PushID(_entityState.Entity.Id.ToString()); //this helps the ui diferentiate between the left and right side
            //and the three ### below forces it to ignore everything before the ### wrt being an ID and the stuff after the ### is an id.
            //this stops the header closing whenever we change the headertext (ie in this case, change the volume)
            string headerText = stype.Name + " " + Stringify.VolumeLtr(freeVolume) + " / " + Stringify.VolumeLtr(maxVolume) + " free" + "###" + stype.UniqueID;
            if(ImGui.CollapsingHeader(headerText, ImGuiTreeNodeFlags.CollapsingHeader ))
            {

                ImGui.Columns(4);
                ImGui.SetColumnWidth(0, 90);
                ImGui.SetColumnWidth(1, 120);
                ImGui.SetColumnWidth(2, 60);
                ImGui.SetColumnWidth(3, 90);
                ImGui.Text("Item");
                ImGui.NextColumn();
                ImGui.Text("Count");
                ImGui.NextColumn();
                ImGui.Text("Mass");
                ImGui.NextColumn();
                ImGui.Text("Volume");
                ImGui.NextColumn();
                ImGui.Separator();

                var cargoables = _stores[typeStoreKVP.Key].GetCargoables();
                var storeInUnits = typeStoreKVP.Value.CurrentStoreInUnits;
                Dictionary<int, ICargoable> cargoToDisplay = new (cargoables); 
                foreach (var item in _cargoToMove)
                {
                    if (!cargoToDisplay.ContainsKey(item.Key.ID))
                        cargoToDisplay.Add(item.Key.ID, item.Key);
                }
                
                foreach (var cargoItem in cargoToDisplay.Values)
                {
                    var cname = cargoItem.Name;
                    long unitsStored = 0;
                    if(storeInUnits.ContainsKey(cargoItem.ID)) 
                        unitsStored = storeInUnits[cargoItem.ID];

                    var volumePerItem = cargoItem.VolumePerUnit;
                    var volumeStored = _volStorageDB.GetVolumeStored(cargoItem, true);
                    var massStored = _volStorageDB.GetMassStored(cargoItem, true);

                    bool isSelected = selectedCargo == cargoItem;
                    if (ImGui.Selectable(cname, isSelected))
                    {
                        selectedCargo = cargoItem;
                        CargoItemSelectedEvent?.Invoke(this);
                    }

                    ImGui.NextColumn();
                    ImGui.Text(Stringify.Quantity(unitsStored, "0.#######"));


                    if (_cargoToMove.ContainsKey(cargoItem))
                    {
                        var unitsMoving = _cargoToMove[cargoItem];
                        string text = Stringify.Quantity(unitsMoving, "0");
                        ImGui.SameLine();

                        float blue = 0f;
                        if (_cargoToMoveDatablob.ContainsKey(cargoItem))
                        {
                            if (_cargoToMoveDatablob[cargoItem] != 0)
                                blue = 0.25f;
                        }
                        if (_cargoToMoveOrders.ContainsKey(cargoItem))
                        {
                            if (_cargoToMoveOrders[cargoItem] != 0)
                                blue = 0.5f;
                        }
                        if (_cargoToMoveUI.ContainsKey(cargoItem))
                        {
                            if (_cargoToMoveUI[cargoItem] != 0)
                                blue = 0.75f;
                        }

                        if (unitsMoving > 0)
                            ImGui.TextColored(new Vector4(0.5f, 1, blue, 1), text);
                        else
                            ImGui.TextColored(new Vector4(1f, 0.5f, blue, 1), text);
                    }

                    ImGui.NextColumn();
                    ImGui.Text(Stringify.Mass(massStored));
                    ImGui.NextColumn();
                    ImGui.Text(Stringify.VolumeLtr(volumeStored));
                    ImGui.NextColumn();
                }

                ImGui.Columns(1);
            }
        }
        ImGui.EndChild();
    }

}