using System.Linq;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client
{
    public class OrdersListWindow : NonUniquePulsarGuiWindow
    {
        private readonly int _entityId;
        private readonly string _systemId;

        private OrdersListWindow(int entityId, string systemId, GlobalUIState state)
        {
            _uiState = state;
            SetName("OrdersList|" + entityId);
            _flags = ImGuiWindowFlags.None;
            _entityId = entityId;
            _systemId = systemId;
        }

        internal static OrdersListWindow GetInstance(EntityState entity, GlobalUIState state)
        {
            string name = "OrdersList|" + entity.Entity.Id.ToString();
            OrdersListWindow thisItem;
            if (!_uiState.LoadedNonUniqueWindows.ContainsKey(name))
            {
                thisItem = new OrdersListWindow(entity.Entity.Id, entity.StarSystemId!, state);
                thisItem.StartDisplay();
            }
            else
            {
                thisItem = (OrdersListWindow)_uiState.LoadedNonUniqueWindows[name];
            }

            return thisItem;
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
            if (Window.Begin("Orders: " + entityName + "###" + UniqueName, ref IsActive, _flags))
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
