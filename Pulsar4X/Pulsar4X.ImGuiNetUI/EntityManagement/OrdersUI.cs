using System;
using ImGuiNET;
using Pulsar4X.ECSLib;
using Pulsar4X.SDL2UI;

namespace Pulsar4X.ImGuiNetUI.EntityManagement
{
    public class OrdersUI : PulsarGuiWindow
    {
        private Entity _orderEntity;
        private OrderableDB _orderableDB;
        
        
        private OrdersUI(Entity orderEntity)
        {
            _flags = ImGuiWindowFlags.None;
            _orderEntity = orderEntity;
            _orderableDB = orderEntity.GetDataBlob<OrderableDB>();
        }


        public static OrdersUI GetInstance(EntityState orderEntity)
        {
            OrdersUI thisitem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(OrdersUI)))
            {
                thisitem = new OrdersUI(orderEntity.Entity);
            }
            else
            {
                thisitem = (OrdersUI)_uiState.LoadedWindows[typeof(OrdersUI)];
                if (thisitem._orderEntity != orderEntity.Entity)
                {
                    thisitem._orderEntity = orderEntity.Entity;
                }
            }

            return thisitem;
        }
        
        
        
        
        internal override void Display()
        {
            var orders = _orderableDB.GetActionList();



            if (ImGui.Begin("Orders"))
            {
                foreach (EntityCommand order in orders)
                {
                    ImGui.Text(order.Name);
                    ImGui.Text(order.Details);
                }
            }
            
        }
    }


    public static class OrdersUIs
    {
        
        
        
    }

}