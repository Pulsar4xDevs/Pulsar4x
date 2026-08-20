using System.Linq;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client
{
    internal static class OrderWindowExtensions
    {
        private static int _idCounter = 0;

        /// <summary>
        /// Activates a window displaying the orders for a given entity.
        /// </summary>
        /// <param name="manager">The window manager instance.</param>
        /// <param name="entity">The entity requested for display.</param>
        public static OrdersListWindow ActivateOrderListWindow(this WindowManager manager, EntityState entity)
        {
            if(manager.NamedWindowsByType.TryGetValue(typeof(OrdersListWindow), out var windowList))
            {
                // Reuse an existing inactive window if available
                foreach (var window in windowList.Cast<OrdersListWindow>().Where(w => !w.GetActive()))
                {
                    window.SetEntity(entity);
                    window.SetActive(true);
                    return window;
                }
            }

            string name = MakeName();
            var orderList = new OrdersListWindow(name, entity.Id, entity.StarSystemId!);
            manager.AddNamedWindow(name, orderList);
            orderList.SetActive(true);

            return orderList;
        }

        /// <summary>
        /// Checks if there is an active orders window for a given entity.
        /// </summary>
        /// <param name="manager">The window manager instance.</param>
        /// <param name="entity">The entity to check for an active orders window.</param>
        /// <returns>True if there is an active orders window for the given entity; otherwise, false.</returns>
        public static bool HasActiveOrdersWindow(this WindowManager manager, EntityState entity)
        {
            if(manager.NamedWindowsByType.TryGetValue(typeof(OrdersListWindow), out var windowList))
            {
                foreach (var window in windowList.Cast<OrdersListWindow>())
                {
                    if (window.GetActive() && window.EntityId == entity.Id && window.SystemId == entity.StarSystemId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static string MakeName()
        {
            return "OrdersList|" + _idCounter++;
        }
    }

    public class OrdersListWindow : NamedPulsarGuiWindow
    {
        private int _entityId;
        private string _systemId;

        internal int EntityId => _entityId;
        internal string SystemId => _systemId;

        internal OrdersListWindow(string windowName, int entityId, string systemId) : base(windowName)
        {
            _flags = ImGuiWindowFlags.None;
            _entityId = entityId;
            _systemId = systemId;
        }

        internal void SetEntity(EntityState entity)
        {
            _entityId = entity.Id;
            _systemId = entity.StarSystemId!;
        }

        internal static OrdersListWindow GetInstance(EntityState entity)
        {
            var winManager = _uiState.WindowManager;
            string name = "OrdersList|" + entity.Id.ToString();
            if (!winManager.TryGetNamedWindow(name, out OrdersListWindow? orderList))
            {
                orderList = new OrdersListWindow(name, entity.Id, entity.StarSystemId!);
                winManager.AddNamedWindow(name, orderList);
            }

            return orderList;
        }

        internal override void Display()
        {
            if (!IsActive) return;

            var entity = _uiState.GameClient?.Galaxy.GetSystem(_systemId)?.GetEntity(_entityId);
            if (entity == null)
            {
                IsActive = false;
                return;
            }

            var orders = entity.GetView<OrdersView>()?.Orders ?? System.Array.Empty<OrderSnapshot>();
            string entityName = entity.GetView<NameView>()?.Name ?? "Unknown";

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(550, 325), ImGuiCond.Once);
            if (Window.Begin("Orders: " + entityName + "###" + UniqueName, ref IsActiveRef, _flags))
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
                        foreach (var order in orders)
                        {
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            if (ImGui.Selectable(order.Name + "###" + order.OrderId, false, ImGuiSelectableFlags.SpanAllColumns))
                            {
                            }

                            ImGui.TableNextColumn();
                            ImGui.Text(order.Details);

                            ImGui.TableNextColumn();
                            if (order.UsesMovementLane)
                            {
                                ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), order.IsBlocking ? "--" : "|");
                            }

                            ImGui.TableNextColumn();
                            if (order.UsesExternalLane)
                            {
                                ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), order.IsBlocking ? "--" : "|");
                            }

                            ImGui.TableNextColumn();
                            if (order.UsesSelfLane)
                            {
                                ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), order.IsBlocking ? "--" : "|");
                            }

                            ImGui.TableNextColumn();
                            bool pause = order.PauseOnAction;
                            if (ImGui.Checkbox("##" + order.OrderId, ref pause))
                            {
                                _uiState.GameClient?.SubmitCommandAsync(
                                    new SetOrderPauseCommand(entity.Id, order.OrderId, pause));
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
