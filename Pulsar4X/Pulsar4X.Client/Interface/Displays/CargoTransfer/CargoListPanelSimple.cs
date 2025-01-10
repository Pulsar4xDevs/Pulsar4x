using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Blueprints;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Storage;

namespace Pulsar4X.SDL2UI;

public class CargoListPanelSimple : UpdateWindowState
{
    FactionDataStore _staticData;
    EntityState _entityState;
    CargoStorageDB _volStorageDB;
    SafeDictionary<string, TypeStore> _stores = new();

    public CargoListPanelSimple(FactionDataStore staticData, EntityState entity)
    {
        _staticData = staticData;
        _entityState = entity;

        _volStorageDB = entity.Entity.GetDataBlob<CargoStorageDB>();
        if(_entityState.Entity.Manager != null)
            _entityState.Entity.Manager.ManagerSubpulses.SystemDateChangedEvent += ManagerSubpulsesOnSystemDateChangedEvent;

        Update();
    }

    private void ManagerSubpulsesOnSystemDateChangedEvent(DateTime newdate)
    {
        Update();
    }


    public void Update()
    {
        if (_volStorageDB == null) //if this colony does not have any storage.
            return;
        _stores = _volStorageDB.TypeStores;

    }



    public void Display()
    {
        var width = ImGui.GetWindowWidth() * 0.5f;

        ImGui.BeginChild(_entityState.Name, new Vector2(240, 200), true, ImGuiWindowFlags.AlwaysAutoResize);
        foreach (var typeStore in _stores)
        {
            CargoTypeBlueprint stype = _staticData.CargoTypes[typeStore.Key];
            var freeVolume = _volStorageDB.GetFreeVolume(typeStore.Key);
            var maxVolume = typeStore.Value.MaxVolume;
            var storedVolume = maxVolume - freeVolume;
            var cargoables = typeStore.Value.GetCargoables();
            var unitsInStore = typeStore.Value.CurrentStoreInUnits;


            ImGui.PushID(_entityState.Entity.Id.ToString());//this helps the ui diferentiate between the left and right side
            //and the three ### below forces it to ignore everything before the ### wrt being an ID and the stuff after the ### is an id.
            //this stops the header closing whenever we change the headertext (ie in this case, change the volume)
            string headerText = stype.Name + " " + Stringify.VolumeLtr(freeVolume) + " / " + Stringify.Volume(maxVolume) + " free" + "###" + stype.UniqueID;
            if(ImGui.CollapsingHeader(headerText, ImGuiTreeNodeFlags.CollapsingHeader ))
            {
                ImGui.Columns(4);
                ImGui.Text("Item");
                ImGui.NextColumn();
                ImGui.Text("Count");
                ImGui.NextColumn();
                ImGui.Text("Mass");
                ImGui.NextColumn();
                ImGui.Text("Volume");
                ImGui.Separator();
                foreach (var cargoType in unitsInStore)
                {
                    ICargoable ctype = cargoables[cargoType.Key];
                    var cname = ctype.Name;
                    var volumeStored = cargoType.Value;
                    var volumePerItem = ctype.VolumePerUnit;
                    var massStored = cargoType.Value * ctype.MassPerUnit;
                    var itemsStored = unitsInStore[ctype.ID];
                    if (ImGui.Selectable(cname))
                    {
                    }

                    ImGui.NextColumn();
                    ImGui.Text(Stringify.Quantity(itemsStored));
                    ImGui.NextColumn();
                    ImGui.Text(Stringify.Mass(massStored));
                    ImGui.NextColumn();
                    ImGui.Text(Stringify.VolumeLtr(volumeStored));
                    //ImGui.SetTooltip(ctype.ToDescription);
                    ImGui.NextColumn();
                }

                ImGui.Columns(1);
            }
        }

        ImGui.EndChild();
    }

    public override bool GetActive()
    {
        return true;
    }

    public override void OnGameTickChange(DateTime newDate)
    {
    }

    public override void OnSystemTickChange(DateTime newDate)
    {
        Update();
    }

}