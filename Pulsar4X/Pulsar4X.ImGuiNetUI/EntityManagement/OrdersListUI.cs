using System;
using System.Linq;
using ImGuiNET;
using Pulsar4X.ECSLib;
using Pulsar4X.SDL2UI;

namespace Pulsar4X.ImGuiNetUI.EntityManagement
{
    public class OrdersListUI : NonUniquePulsarGuiWindow
    {
        private Entity _orderEntity;
        private OrderableDB _orderableDB;        
        
        private OrdersListUI(EntityState entity, GlobalUIState state)
        {
            _state = state;
            SetName("OrdersList|" + entity.Entity.Guid.ToString());
            _flags = ImGuiWindowFlags.None;
            onEntityChange(entity);
            _orderEntity = entity.Entity;
            _orderableDB = entity.Entity.GetDataBlob<OrderableDB>();
        }

        internal static OrdersListUI GetInstance(EntityState entity, GlobalUIState state)
        {
            string name = "OrdersList|" + entity.Entity.Guid.ToString();
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

        internal override void Display()
        {
            var orders = _orderableDB.GetActionList();

            if (IsActive == true && ImGui.Begin("Orders: " + _orderEntity.GetOwnersName(), ref IsActive, _flags))
            {
                ImGui.Columns(2);
                ImGui.Text("Name");
                ImGui.NextColumn();
                ImGui.Text("Details");
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