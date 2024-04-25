using System;
using System.Linq;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;
using Pulsar4X.SDL2UI;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.ImGuiNetUI.EntityManagement
{
    public class OrdersListUI : NonUniquePulsarGuiWindow
    {
        private Entity _orderEntity;
        private OrderableDB _orderableDB;

        private OrdersListUI(EntityState entity, GlobalUIState state)
        {
            _uiState = state;
            SetName("OrdersList|" + entity.Entity.Id.ToString());
            _flags = ImGuiWindowFlags.None;
            onEntityChange(entity);
            _orderEntity = entity.Entity;
            _orderableDB = entity.Entity.GetDataBlob<OrderableDB>();
            OnSystemTickChange(entity.Entity.StarSysDateTime);
        }

        internal static OrdersListUI GetInstance(EntityState entity, GlobalUIState state)
        {
            string name = "OrdersList|" + entity.Entity.Id.ToString();
            OrdersListUI thisItem;
            if (!_uiState.LoadedNonUniqueWindows.ContainsKey(name))
            {
                thisItem = new OrdersListUI(entity, state);
                thisItem.StartDisplay();
            }
            else
            {
                thisItem = (OrdersListUI)_uiState.LoadedNonUniqueWindows[name];
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
            var orders = _orderableDB.ActionList;
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(550, 325), ImGuiCond.Once);
            if (IsActive == true && ImGui.Begin("Orders: " + _orderEntity.GetOwnersName(), ref IsActive, _flags))
            {
                ImGui.Columns(6);
                ImGui.SetColumnWidth(0, 124);
                ImGui.Text("Name");
                ImGui.NextColumn();
                ImGui.SetColumnWidth(1, 280);
                ImGui.Text("Details");
                ImGui.NextColumn();
                ImGui.SetColumnWidth(2, 32);
                ImGui.Text("Mov");
                ImGui.NextColumn();
                ImGui.SetColumnWidth(3, 32);
                ImGui.Text("IE");
                ImGui.NextColumn();
                ImGui.SetColumnWidth(4, 32);
                ImGui.Text("IS");
                ImGui.NextColumn();
                ImGui.SetColumnWidth(5, 44);
                ImGui.Text("Pause");
                ImGui.NextColumn();
                ImGui.Separator();

                if (orders.Any())
                {
                    foreach (EntityCommand order in orders)
                    {
                        if (ImGui.Selectable(order.Name))
                        {
                        }

                        ImGui.NextColumn();
                        ImGui.Text(order.Details);
                        ImGui.NextColumn();
                        if(order.ActionLanes.HasFlag(EntityCommand.ActionLaneTypes.Movement))
                        {
                            if(order.IsBlocking)
                                ImGui.TextColored(new System.Numerics.Vector4(1,0,0,1), "--");
                            else
                                ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "|");
                        }
                        ImGui.NextColumn();
                        if (order.ActionLanes.HasFlag(EntityCommand.ActionLaneTypes.InteractWithExternalEntity))
                        {
                            if (order.IsBlocking)
                                ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "--");
                            else
                                ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "|");
                        }
                        ImGui.NextColumn();
                        if (order.ActionLanes.HasFlag(EntityCommand.ActionLaneTypes.IneteractWithSelf))
                        {
                            if (order.IsBlocking)
                                ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "--");
                            else
                                ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "|");
                        }
                        ImGui.NextColumn();
                        if (ImGui.Checkbox("##"+order.CmdID, ref order.PauseOnAction))
                        {
                        }

                        ImGui.NextColumn();

                    }
                }
                else
                {
                    ImGui.Text("No Orders");

                    ImGui.NextColumn();
                    if (ImGui.Selectable("* Double Click to add some now *"))
                    {
                    }
                    ImGui.NextColumn();
                }

                ImGui.Columns(1);
            }

        }
    }
}