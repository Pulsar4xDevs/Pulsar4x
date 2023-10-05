using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.SDL2UI;
using static Pulsar4X.SDL2UI.UserOrbitSettings;

namespace Pulsar4X.ImGuiNetUI.EntityManagement
{
    public class OrderCreationUI : PulsarGuiWindow
    {
        private Entity _orderEntity = null;

        private OrderableDB _orderableDB = null;

        private int _movementTargetGuid = -1;

        private enum OrderCreationSubWindow
        {
            Movement,
            Transfers,
            Scanning
        }

        private enum MovementAction
        {
            MoveTo,
            DockWith,
            Follow
        }

        private OrderCreationSubWindow _selectedSubWindow = OrderCreationSubWindow.Movement;
        private MovementAction _movementAction = MovementAction.MoveTo;

        private OrderCreationUI()
        {
            _flags = ImGuiWindowFlags.None;
        }

        public static OrderCreationUI GetInstance()
        {
            OrderCreationUI thisItem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(OrderCreationUI)))
            {
                thisItem = new OrderCreationUI();
            }
            else
            {
                thisItem = (OrderCreationUI)_uiState.LoadedWindows[typeof(OrderCreationUI)];
            }

            return thisItem;
        }

        internal override void Display()
        {
            if (_uiState.LastClickedEntity != null && _uiState.StarSystemStates.ContainsKey(_uiState.SelectedStarSysGuid))
            {
                EntityState _SelectedEntityState = _uiState.LastClickedEntity;
                if (_orderEntity == null || _orderEntity != _SelectedEntityState.Entity)
                {
                    _orderEntity = _SelectedEntityState.Entity;
                    _orderableDB = null;
                    if (_orderEntity != null && _orderEntity.HasDataBlob<OrderableDB>())
                    {
                        _orderableDB = _orderEntity.GetDataBlob<OrderableDB>();
                    }
                }
            }
            else
            {
                _orderEntity = null;
                _orderableDB = null;
            }

            if (IsActive == true && ImGui.Begin("Order Creation", ref IsActive, _flags))
            {
                RenderTabOptions();
                if (_orderableDB != null) {
                    ImGui.TextColored(new System.Numerics.Vector4(0, 123, 0, 255), "Selected Entity: " + _orderEntity.GetName(_uiState.Faction.Id));
                }

                ImGui.BeginChild("order_creation_tabs");
                switch (_selectedSubWindow)
                {
                    case OrderCreationSubWindow.Movement:
                        RenderMovement();
                        break;
                    case OrderCreationSubWindow.Scanning:
                        RenderScanning();
                        break;
                    case OrderCreationSubWindow.Transfers:
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
            ImGui.Columns(3);
            // List of target Entities
            if (ImGui.TreeNode("Target"))
            {
                //var moveCandidates = _uiState.Faction
                if (_uiState.StarSystemStates.ContainsKey(_uiState.SelectedStarSysGuid))
                {
                    SystemState _StarSystemState = _uiState.StarSystemStates[_uiState.SelectedStarSysGuid];
                    var _NamedEntityStatesByBodyType = _StarSystemState.EntityStatesWithPosition.Values.GroupBy(x => x.BodyType).ToDictionary(k => k.Key, v => v.Select(x => x).ToList());

                    foreach (OrbitBodyType orbitBodyType in _NamedEntityStatesByBodyType.Keys.OrderBy(x => (int)x))
                    {
                        if (ImGui.TreeNode(orbitBodyType.ToDescription()))
                        {
                            var _NamedEntityStates = _NamedEntityStatesByBodyType[orbitBodyType];
                            foreach (var body in _NamedEntityStates)
                            {
                                if (_orderEntity == null || body.Entity.Id != _orderEntity.Id)
                                {
                                    if (ImGui.Selectable(_NamedEntityStates.First(x => x == body).Name, _movementTargetGuid == body.Entity.Id))
                                    {
                                        _movementTargetGuid = body.Entity.Id;
                                    }
                                }
                            }
                            ImGui.TreePop();
                        }
                    }
                    ImGui.TreePop();
                }
            }

            ImGui.NextColumn();
            if (ImGui.TreeNode("Action"))
            {
                if (ImGui.Selectable("Move To Target", _movementAction == MovementAction.MoveTo))
                {
                    _movementAction = MovementAction.MoveTo;
                }
                if (ImGui.Selectable("Dock With Target", _movementAction == MovementAction.DockWith))
                {
                    _movementAction = MovementAction.DockWith;
                }
                if (ImGui.Selectable("Follow Target", _movementAction == MovementAction.DockWith))
                {
                    _movementAction = MovementAction.Follow;
                }
            }


            ImGui.NextColumn();

            if (_orderableDB != null)
            {
                if (_movementTargetGuid != -1)
                {
                    ImGui.Button("Add Order to\r\nSelected Entity");
                }
                else
                {
                    ImGui.TextColored(new System.Numerics.Vector4(123, 0, 0, 255), "Order is\r\nIncomplete");
                }
            } 
            else
            {
                ImGui.TextColored(new System.Numerics.Vector4(123, 0, 0, 255), "Selected\r\nEntity\r\nCannot\r\nAccept\r\nOrders");
            }
            

            ImGui.Columns(1);
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
                _selectedSubWindow = OrderCreationSubWindow.Movement;
            }

            ImGui.SameLine();
            if (ImGui.SmallButton("Scanning"))
            {
                _selectedSubWindow = OrderCreationSubWindow.Scanning;
            }

            ImGui.SameLine();
            if (ImGui.SmallButton("Transfers"))
            {
                _selectedSubWindow = OrderCreationSubWindow.Transfers;
            }
        }
    }
}