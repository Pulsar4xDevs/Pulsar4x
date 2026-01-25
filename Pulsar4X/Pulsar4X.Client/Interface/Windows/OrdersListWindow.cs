using System;
using System.Linq;
using ImGuiNET;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.Client
{
    public class OrdersListWindow : NonUniquePulsarGuiWindow
    {
        private Entity _orderEntity;
        private OrderableDB _orderableDB;

        private OrdersListWindow(EntityState entity, GlobalUIState state)
        {
            _uiState = state;
            SetName("OrdersList|" + entity.Entity.Id.ToString());
            _flags = ImGuiWindowFlags.None;
            onEntityChange(entity);
            _orderEntity = entity.Entity;
            _orderableDB = entity.Entity.GetDataBlob<OrderableDB>();
            OnSystemTickChange(entity.Entity.StarSysDateTime);
        }

        internal static OrdersListWindow GetInstance(EntityState entity, GlobalUIState state)
        {
            string name = "OrdersList|" + entity.Entity.Id.ToString();
            OrdersListWindow thisItem;
            if (!_uiState.LoadedNonUniqueWindows.ContainsKey(name))
            {
                thisItem = new OrdersListWindow(entity, state);
                thisItem.StartDisplay();
            }
            else
            {
                thisItem = (OrdersListWindow)_uiState.LoadedNonUniqueWindows[name];
                thisItem.onEntityChange(entity);
            }

            return thisItem;
        }

        internal void onEntityChange(EntityState entity)
        {
            _lookedAtEntity = entity;
        }

        public override void OnSystemTickChange(DateTime newDate)
        {
            foreach (var item in _orderableDB.ActionList)
            {
                item.UpdateDetailString();
            }
        }

        internal override void Display()
        {
            if (!IsActive) return;

            var orders = _orderableDB.ActionList;
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(550, 325), ImGuiCond.Once);
            if (Window.Begin("Orders: " + _orderEntity.GetOwnersName(), ref IsActive, _flags))
            {
                var tableFlags = ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingFixedFit;
                if (ImGui.BeginTable("OrdersTable", 6, tableFlags))
                {
                    ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 124);
                    ImGui.TableSetupColumn("Details", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Mov", ImGuiTableColumnFlags.WidthFixed, 32);
                    ImGui.TableSetupColumn("IE", ImGuiTableColumnFlags.WidthFixed, 32);
                    ImGui.TableSetupColumn("IS", ImGuiTableColumnFlags.WidthFixed, 32);
                    ImGui.TableSetupColumn("Pause", ImGuiTableColumnFlags.WidthFixed, 44);
                    ImGui.TableHeadersRow();

                    if (orders.Any())
                    {
                        foreach (EntityCommand order in orders)
                        {
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            if (ImGui.Selectable(order.Name, false, ImGuiSelectableFlags.SpanAllColumns))
                            {
                            }

                            ImGui.TableNextColumn();
                            ImGui.Text(order.Details);

                            ImGui.TableNextColumn();
                            if (order.ActionLanes.HasFlag(EntityCommand.ActionLaneTypes.Movement))
                            {
                                if (order.IsBlocking)
                                    ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "--");
                                else
                                    ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "|");
                            }

                            ImGui.TableNextColumn();
                            if (order.ActionLanes.HasFlag(EntityCommand.ActionLaneTypes.InteractWithExternalEntity))
                            {
                                if (order.IsBlocking)
                                    ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "--");
                                else
                                    ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "|");
                            }

                            ImGui.TableNextColumn();
                            if (order.ActionLanes.HasFlag(EntityCommand.ActionLaneTypes.IneteractWithSelf))
                            {
                                if (order.IsBlocking)
                                    ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "--");
                                else
                                    ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "|");
                            }

                            ImGui.TableNextColumn();
                            if (ImGui.Checkbox("##" + order.CmdID, ref order.PauseOnAction))
                            {
                            }
                        }
                    }
                    else
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.Text("No Orders");

                        ImGui.TableNextColumn();
                        if (ImGui.Selectable("* Double Click to add some now *"))
                        {
                        }
                    }

                    ImGui.EndTable();
                }
            }
            Window.End();
        }
    }
}