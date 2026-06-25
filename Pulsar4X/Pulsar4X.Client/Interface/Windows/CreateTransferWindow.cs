using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client;

public class CreateTransferWindow : UniquePulsarGuiWindow<CreateTransferWindow>
{
    private int? _leftId;
    private int? _rightId;
    private string? _systemId;

    // Units selected to move, keyed by cargo-item id; clamped against the live snapshot each frame.
    private readonly Dictionary<int, long> _leftSelected = new();
    private readonly Dictionary<int, long> _rightSelected = new();

    internal static CreateTransferWindow GetInstance()
    {
        return _uiState.LoadedWindows.ContainsKey(typeof(CreateTransferWindow)) ? (CreateTransferWindow)_uiState.LoadedWindows[typeof(CreateTransferWindow)] : new CreateTransferWindow();
    }

    public void SetLeft(int entityId, string systemId)
    {
        _leftId = entityId;
        _systemId = systemId;
        _leftSelected.Clear();
    }

    public void SetRight(int entityId)
    {
        _rightId = entityId;
        _rightSelected.Clear();
    }

    internal override void Display()
    {
        if(!IsActive) return;

        if(Window.Begin("Create Transfer Order", ref IsActive))
        {
            var system = _systemId != null ? _uiState.GameClient?.Galaxy.GetSystem(_systemId) : null;
            var left = _leftId is int leftId ? system?.GetEntity(leftId) : null;
            var right = _rightId is int rightId ? system?.GetEntity(rightId) : null;

            Vector2 windowContentSize = ImGui.GetContentRegionAvail();
            var firstChildSize = new Vector2(Styles.LeftColumnWidthLg, windowContentSize.Y);
            var secondChildSize = new Vector2(windowContentSize.X - (Styles.LeftColumnWidthLg * 2) - (windowContentSize.X * 0.01f), windowContentSize.Y);
            var thirdChildSize = new Vector2(Styles.LeftColumnWidthLg - (windowContentSize.X * 0.01f), windowContentSize.Y);
            if(ImGui.BeginChild(GetTitle(left) + "###left", firstChildSize, ImGuiChildFlags.Borders))
            {
                DisplayTransferTarget(system, left, right, isLeft: true);
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

                if(left != null)
                    DisplayTradeList(_leftSelected, left);

                ImGui.NextColumn();

                if(right != null)
                    DisplayTradeList(_rightSelected, right);

                ImGui.Columns(1);

                if(_leftSelected.Count > 0 || _rightSelected.Count > 0)
                {
                    ImGui.Separator();
                    if(ImGui.Button("Create") && left != null && right != null)
                    {
                        SubmitTransfer(left.Id, right.Id, _leftSelected);
                        SubmitTransfer(right.Id, left.Id, _rightSelected);
                        _leftSelected.Clear();
                        _rightSelected.Clear();
                    }
                }
            }
            ImGui.EndChild();
            ImGui.SameLine();

            if(ImGui.BeginChild(GetTitle(right) + "###right", thirdChildSize, ImGuiChildFlags.Borders))
            {
                DisplayTransferTarget(system, right, left, isLeft: false);
            }
            ImGui.EndChild();

        }
        Window.End();
    }

    private void SubmitTransfer(int fromId, int toId, Dictionary<int, long> selected)
    {
        var items = selected
            .Where(kvp => kvp.Value > 0)
            .Select(kvp => new CargoTransferItem(kvp.Key, kvp.Value))
            .ToList();

        if(items.Count > 0)
            _uiState.GameClient?.SubmitCommandAsync(new TransferCargoCommand(fromId, toId, items));
    }

    private void DisplayTransferTarget(IClientSystem? system, EntitySnapshot? entity, EntitySnapshot? other, bool isLeft)
    {
        // At least one target needs to be set to allow selection of the other.
        // If we don't have other, lock the current selector.
        bool readOnlySelector = other is null;

        ImGui.SetNextItemWidth(-1.0f);
        if (readOnlySelector)
            ImGui.BeginDisabled();

        if (ImGui.BeginCombo("###selector", GetName(entity) ?? "Select transfer partner"))
        {
            // Find storages in range and populate list.
            if(other is not null && system is not null)
            {
                foreach (var potentialTarget in system.Entities)
                {
                    if (potentialTarget.Id == other.Id) continue;
                    if (!potentialTarget.HasView<CargoStorageView>()) continue;

                    // TODO: check the distance from other to potentialTarget
                    // make sure it is within the transfer range
                    if (ImGui.Selectable(GetName(potentialTarget), entity is not null && potentialTarget.Id == entity.Id))
                    {
                        if (isLeft)
                            SetLeft(potentialTarget.Id, system.SystemId);
                        else
                            SetRight(potentialTarget.Id);
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

        DisplayStorageList(entity, isLeft ? _leftSelected : _rightSelected);
    }

    private void DisplayStorageList(EntitySnapshot entity, Dictionary<int, long> selected)
    {
        if(entity.GetView<CargoStorageView>() is not { } storage)
            return;

        foreach(var store in storage.Stores)
        {
            string header = store.TypeName + " Storage";
            if(ImGui.CollapsingHeader(header + "###" + store.TypeId, ImGuiTreeNodeFlags.DefaultOpen))
            {
                var contentSize = ImGui.GetContentRegionAvail();

                foreach(var item in store.Items)
                {
                    if(ImGui.SmallButton("+###add" + item.Name))
                    {
                        if(!selected.ContainsKey(item.Id))
                            selected.Add(item.Id, 0);
                    }
                    ImGui.SameLine();
                    ImGui.Text(item.Name);
                    if(ImGui.IsItemHovered() && item.Description.Length > 0)
                        DisplayHelpers.DescriptiveTooltip(item.Name, item.ItemKind, item.Description);
                    ImGui.SameLine();

                    string amount = Stringify.Quantity(item.Units);
                    var amountSize = ImGui.CalcTextSize(amount);

                    ImGui.SetCursorPosX(contentSize.X - amountSize.X);
                    ImGui.Text(item.Units.ToString());

                }
            }
        }
    }

    private void DisplayTradeList(Dictionary<int, long> selected, EntitySnapshot entity)
    {
        var itemsById = ItemsById(entity);

        var contentSize = ImGui.GetContentRegionAvail();
        var currentX = ImGui.GetCursorPosX();
        var toRemove = new List<int>();
        foreach(var (itemId, units) in selected)
        {
            // The item may have left storage since it was selected (transferred away, consumed).
            if(!itemsById.TryGetValue(itemId, out var item))
            {
                toRemove.Add(itemId);
                continue;
            }

            var amount = (int)units;
            if(ImGui.SmallButton("-###remove" + item.Name))
            {
                toRemove.Add(itemId);
            }
            ImGui.SameLine();
            ImGui.Text(item.Name);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(96);
            ImGui.SetCursorPosX(currentX + contentSize.X - 96);
            ImGui.InputInt("###input" + item.Name, ref amount);
            if(ImGui.IsItemHovered() && item.Description.Length > 0)
                DisplayHelpers.DescriptiveTooltip(item.Name, item.ItemKind, item.Description);

            if(amount > item.Units)
                amount = (int)item.Units;
            if(amount < 0)
                amount = 0;

            selected[itemId] = amount;
        }

        foreach(var itemId in toRemove)
        {
            selected.Remove(itemId);
        }
    }

    private static Dictionary<int, CargoItemView> ItemsById(EntitySnapshot entity)
    {
        var items = new Dictionary<int, CargoItemView>();
        if(entity.GetView<CargoStorageView>() is { } storage)
            foreach(var store in storage.Stores)
                foreach(var item in store.Items)
                    items[item.Id] = item;
        return items;
    }

    private static string? GetName(EntitySnapshot? entity) => entity?.GetView<NameView>()?.Name;

    private string GetTitle(EntitySnapshot? entity) => GetName(entity) ?? "Select Entity";
}
