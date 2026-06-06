using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Storage;

namespace Pulsar4X.Client;

public class CreateTransferWindow : PulsarGuiWindow
{
    public Entity? TransferLeft { get; private set; }
    public Entity? TransferRight { get; private set; }

    public Dictionary<ICargoable, (long, long)> TransferLeftGoods { get; private set; } = new ();
    public Dictionary<ICargoable, (long, long)> TransferRightGoods { get; private set; } = new ();

    internal static CreateTransferWindow GetInstance()
    {
        return _uiState.LoadedWindows.ContainsKey(typeof(CreateTransferWindow)) ? (CreateTransferWindow)_uiState.LoadedWindows[typeof(CreateTransferWindow)] : new CreateTransferWindow();
    }

    public void SetLeft(Entity entity)
    {
        TransferLeft = entity;
        TransferLeftGoods.Clear();
    }

    public void SetRight(Entity entity)
    {
        TransferRight = entity;
        TransferRightGoods.Clear();
    }

    internal override void Display()
    {
        if(!IsActive) return;

        if(Window.Begin("Create Transfer Order", ref IsActive))
        {
            Vector2 windowContentSize = ImGui.GetContentRegionAvail();
            var firstChildSize = new Vector2(Styles.LeftColumnWidthLg, windowContentSize.Y);
            var secondChildSize = new Vector2(windowContentSize.X - (Styles.LeftColumnWidthLg * 2) - (windowContentSize.X * 0.01f), windowContentSize.Y);
            var thirdChildSize = new Vector2(Styles.LeftColumnWidthLg - (windowContentSize.X * 0.01f), windowContentSize.Y);
            if(ImGui.BeginChild(GetLeftTitle() + "###left", firstChildSize, ImGuiChildFlags.Borders))
            {
                DisplayTransferTarget(TransferLeft, TransferRight);
            }
            ImGui.EndChild();
            ImGui.SameLine();

            if(ImGui.BeginChild("Transfer Details", secondChildSize, ImGuiChildFlags.Borders))
            {

                ImGui.Columns(2);

                ImGui.Text("Items to Transfer");
                ImGui.NextColumn();
                ImGui.Text("Items to Transfer");
                ImGui.Separator();
                ImGui.NextColumn();

                if(TransferLeft != null)
                    DisplayTradeList(TransferLeftGoods, TransferLeft);

                ImGui.NextColumn();

                if(TransferRight != null)
                    DisplayTradeList(TransferRightGoods, TransferRight);

                ImGui.Columns(1);

                if(TransferLeftGoods.Count > 0 || TransferRightGoods.Count > 0)
                {
                    ImGui.Separator();
                    if(ImGui.Button("Create"))
                    {
                        var tlNull = TransferLeft == null;
                        var trNull = TransferRight == null;
                        if(TransferLeft != null && TransferRight != null && TransferLeftGoods.Count > 0)
                        {
                            var itemsToTransfer = new List<(ICargoable, long)>();
                            foreach(var item in TransferLeftGoods)
                            {
                                itemsToTransfer.Add((item.Key, -item.Value.Item1));
                            }
                            CargoTransferOrder.CreateCommands(_uiState.Faction.Id, TransferLeft, TransferRight, itemsToTransfer);
                        }
                        if(TransferLeft != null && TransferRight != null && TransferRightGoods.Count > 0)
                        {
                            var itemsToTransfer = new List<(ICargoable, long)>();
                            foreach(var item in TransferRightGoods)
                            {
                                itemsToTransfer.Add((item.Key, -item.Value.Item1));
                            }
                            CargoTransferOrder.CreateCommands(_uiState.Faction.Id, TransferRight, TransferLeft, itemsToTransfer);
                        }
                    }
                }
            }
            ImGui.EndChild();
            ImGui.SameLine();

            if(ImGui.BeginChild(GetRightTitle() + "###right", thirdChildSize, ImGuiChildFlags.Borders))
            {
                DisplayTransferTarget(TransferRight, TransferLeft);
            }
            ImGui.EndChild();
            
        }
        Window.End();
    }

    private void DisplayTransferTarget(Entity? entity, Entity? other = null, bool readOnlySelector = false)
    {
        // At least one target needs to be set to allow selection of the other.
        // If we don't have other, lock the current selector.
        readOnlySelector |= other is null;

        ImGui.SetNextItemWidth(-1.0f);
        if (readOnlySelector)
            ImGui.BeginDisabled();

        if (ImGui.BeginCombo("###selector", entity?.GetName(_uiState.Faction.Id) ?? "Select transfer partner"))
        {
            // Find storages in range and populate list.
            if(other is not null && other.Manager is not null)
            {
                var systemState = _uiState.StarSystemStates[other.Manager.ManagerID];
                var allFriendlyStorageInSystem = systemState.GetFilteredEntities(DataStructures.EntityFilter.Friendly, _uiState.Faction.Id, typeof(CargoStorageDB));

                foreach (var potentialTarget in allFriendlyStorageInSystem)
                {
                    if (potentialTarget.Id == other.Id) continue;

                    // TODO: check the distance from other to potentialTarget
                    // make sure it is within the transfer range
                    if (ImGui.Selectable(potentialTarget.Name, entity is not null && potentialTarget.Id == entity.Id))
                    {
                        // Make the target the current entity.
                        if (entity == TransferLeft)
                        {
                            SetLeft(potentialTarget.Entity);
                        } 
                        else if (entity == TransferRight)
                        {
                            SetRight(potentialTarget.Entity);
                        }
                        entity = potentialTarget.Entity;
                    }
                }
            }

            ImGui.EndCombo();
        }

        if (readOnlySelector)
            ImGui.EndDisabled();

        ImGui.Separator();

        if (entity is null)
            return;

        DisplayStorageList(entity);
        return;
    }

    private void DisplayStorageList(Entity entity)
    {
        if(entity.TryGetDataBlob<CargoStorageDB>(out var leftVolumeStorageDB))
        {
            foreach(var (storageId, storageType) in leftVolumeStorageDB.TypeStores)
            {
                string header = entity.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoTypes[storageId].Name + " Storage";
                if(ImGui.CollapsingHeader(header + "###" + storageId, ImGuiTreeNodeFlags.DefaultOpen))
                {
                    var cargoables = storageType.GetCargoables();
                    // Sort the display by the cargoables name
                    var sortedUnitsByCargoablesName = storageType.CurrentStoreInUnits.OrderBy(e => cargoables[e.Key].Name);
                    var contentSize = ImGui.GetContentRegionAvail();

                    foreach(var (id, value) in sortedUnitsByCargoablesName)
                    {

                        if(ImGui.SmallButton("+###add" + cargoables[id].Name))
                        {
                            if(entity == TransferLeft && !TransferLeftGoods.ContainsKey(cargoables[id]))
                            {
                                TransferLeftGoods.Add(cargoables[id], (0, value));
                            }
                            else if(entity == TransferRight && !TransferRightGoods.ContainsKey(cargoables[id]))
                            {
                                TransferRightGoods.Add(cargoables[id], (0, value));
                            }
                        }
                        ImGui.SameLine();
                        ImGui.Text(cargoables[id].Name);
                        cargoables[id].ShowTooltip();
                        ImGui.SameLine();

                        string amount = Stringify.Quantity(value);
                        var amountSize = ImGui.CalcTextSize(amount);

                        ImGui.SetCursorPosX(contentSize.X - amountSize.X);
                        ImGui.Text(value.ToString());

                    }
                }
            }
        }
    }

    private void DisplayTradeList(Dictionary<ICargoable, (long, long)> list, Entity entity)
    {
        var contentSize = ImGui.GetContentRegionAvail();
        var currentX = ImGui.GetCursorPosX();
        var toRemove = new List<ICargoable>();
        foreach(var (cargoable, value) in list)
        {
            var amount = (int)value.Item1;
            if(ImGui.SmallButton("-###remove" + cargoable.Name))
            {
                toRemove.Add(cargoable);
            }
            ImGui.SameLine();
            ImGui.Text(cargoable.Name);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(96);
            ImGui.SetCursorPosX(currentX + contentSize.X - 96);
            ImGui.InputInt("###input" + cargoable.Name, ref amount);
            cargoable.ShowTooltip();

            if(amount > value.Item2)
                amount = (int)value.Item2;
            if(amount < 0)
                amount = 0;

            list[cargoable] = ((long)amount, value.Item2);
        }

        foreach(var item in toRemove)
        {
            list.Remove(item);
        }
    }

    private string GetLeftTitle() => TransferLeft?.GetFactionName() ?? "Select Entity";
    private string GetRightTitle() => TransferRight?.GetFactionName() ?? "Select Entity";
}