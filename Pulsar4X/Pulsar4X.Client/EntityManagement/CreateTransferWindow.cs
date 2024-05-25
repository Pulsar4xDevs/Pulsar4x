using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Extensions;
using Pulsar4X.Interfaces;

namespace Pulsar4X.SDL2UI;

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
    }

    public void SetRight(Entity entity)
    {
        TransferRight = entity;
    }

    internal override void Display()
    {
        if(!IsActive) return;

        if(ImGui.Begin("Create Transfer Order", ref IsActive))
        {
            Vector2 windowContentSize = ImGui.GetContentRegionAvail();
            var firstChildSize = new Vector2(Styles.LeftColumnWidthLg, windowContentSize.Y);
            var secondChildSize = new Vector2(windowContentSize.X - (Styles.LeftColumnWidthLg * 2) - (windowContentSize.X * 0.01f), windowContentSize.Y);
            var thirdChildSize = new Vector2(Styles.LeftColumnWidthLg - (windowContentSize.X * 0.01f), windowContentSize.Y);
            if(ImGui.BeginChild(GetLeftTitle() + "###left", firstChildSize, true))
            {
                if(TransferLeft != null)
                    DisplayStorageList(TransferLeft);

                ImGui.EndChild();
            }
            ImGui.SameLine();

            if(ImGui.BeginChild("Transfer Details", secondChildSize, true))
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
                        if(TransferLeft != null && TransferRight != null && TransferLeftGoods.Count > 0)
                        {
                            var itemsToTransfer = new List<(ICargoable, long)>();
                            foreach(var item in TransferLeftGoods)
                            {
                                itemsToTransfer.Add((item.Key, item.Value.Item1));
                            }
                            CargoUnloadToOrder.CreateCommand(_uiState.Faction.Id, TransferLeft, TransferRight, itemsToTransfer);
                        }
                    }
                }

                ImGui.EndChild();
            }
            ImGui.SameLine();

            if(ImGui.BeginChild(GetRightTitle() + "###right", thirdChildSize, true))
            {
                if(TransferRight != null)
                    DisplayStorageList(TransferRight);
                else
                    DisplayTransferSelection();

                ImGui.EndChild();
            }
            ImGui.End();
        }
    }

    private void DisplayStorageList(Entity entity)
    {
        if(entity.TryGetDatablob<VolumeStorageDB>(out var leftVolumeStorageDB))
        {
            ImGui.Text(entity.GetName(_uiState.Faction.Id));
            ImGui.Separator();
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

                        string amount = Stringify.Number(value);
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

    private void DisplayTransferSelection()
    {
        // We get the system from the TransferLeft so it needs to be set
        if(TransferLeft == null || TransferLeft.Manager == null) return;

        // Setup the target list
        var systemState = _uiState.StarSystemStates[TransferLeft.Manager.ManagerGuid];
        var allFriendlyStorageInSystem = systemState.GetFilteredEntities(DataStructures.EntityFilter.Friendly, _uiState.Faction.Id, typeof(VolumeStorageDB));

        ImGui.Text("Select a Transfer Partner");
        ImGui.Separator();

        foreach(var potentialTarget in allFriendlyStorageInSystem)
        {
            if(potentialTarget.Entity.Id == TransferLeft.Id)  continue;

            // TODO: check the distance from TransferLeft to potentialTarget
            // make sure it is within the transfer range
            if(ImGui.Button(potentialTarget.Name))
            {
                SetRight(potentialTarget.Entity);
            }
        }
    }

    private string GetLeftTitle() => TransferLeft?.GetFactionName() ?? "Select Entity";
    private string GetRightTitle() => TransferRight?.GetFactionName() ?? "Select Entity";
}