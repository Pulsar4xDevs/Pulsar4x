using System;
using System.Linq;
using ImGuiNET;
using Pulsar4X.ECSLib;
using Pulsar4X.SDL2UI;

namespace Pulsar4X.ImGuiNetUI.EntityManagement
{
    public class OrderCreationUI : PulsarGuiWindow
    {
        private Entity _orderEntity;
        private OrderableDB _orderableDB;


        private enum OrderCreationSubWindow
        {
            movement,
            transfers,
            scanning
        }

        private OrderCreationSubWindow _selectedSubWindow;

        private OrderCreationUI(Entity orderEntity)
        {
            _flags = ImGuiWindowFlags.None;
            _orderEntity = orderEntity;
            _orderableDB = orderEntity.GetDataBlob<OrderableDB>();
            _selectedSubWindow = OrderCreationSubWindow.movement;
        }

        public static OrderCreationUI GetInstance(EntityState orderEntity)
        {
            OrderCreationUI thisitem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(OrderCreationUI)))
            {
                thisitem = new OrderCreationUI(orderEntity.Entity);
            }
            else
            {
                thisitem = (OrderCreationUI)_uiState.LoadedWindows[typeof(OrderCreationUI)];
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

            if (IsActive == true && ImGui.Begin("Order Creation", ref IsActive, _flags))
            {
                RenderTabOptions();

                ImGui.BeginChild("order_creation_tabs");
                switch (_selectedSubWindow)
                {
                    case OrderCreationSubWindow.movement:
                        RenderMovement();
                        break;
                    case OrderCreationSubWindow.scanning:
                        RenderScanning();
                        break;
                    case OrderCreationSubWindow.transfers:
                        RenderTransfers();
                        break;
                    default:
                        break;
                }
                ImGui.EndChild();
                ImGui.End();
            }
            
        }

        private void RenderMovement()
        {
            ImGui.TextUnformatted("Movement Orders UI to go here:");

            ImGui.TextUnformatted("* Move Into Orbit of Body");
            ImGui.TextUnformatted("* Leave Orbit of Body");
            ImGui.TextUnformatted("* Dock to Body");
        }

        private void RenderScanning()
        {
            ImGui.TextUnformatted("Scanning Orders UI to go here:");

            ImGui.TextUnformatted("* Disable Active Scanners");
            ImGui.TextUnformatted("* Enable Active Scanners");
            ImGui.TextUnformatted("* Perform Geo Survey");
            ImGui.TextUnformatted("* Perform Grav Survey");
        }

        private void RenderTransfers()
        {
            ImGui.TextUnformatted("Transfer Orders UI to go here:");

            ImGui.TextUnformatted("* Load Cargo");
            ImGui.TextUnformatted("* Unload Cargo");
            ImGui.TextUnformatted("* Refuel");
            ImGui.TextUnformatted("* Empty Fuel to 10%");
        }

        private void RenderTabOptions()
        {
            if (ImGui.SmallButton("Movement"))
            {
                _selectedSubWindow = OrderCreationSubWindow.movement;
            }

            ImGui.SameLine();
            if (ImGui.SmallButton("Scanning"))
            {
                _selectedSubWindow = OrderCreationSubWindow.scanning;
            }

            ImGui.SameLine();
            if (ImGui.SmallButton("Transfers"))
            {
                _selectedSubWindow = OrderCreationSubWindow.transfers;
            }
        }
    }
}