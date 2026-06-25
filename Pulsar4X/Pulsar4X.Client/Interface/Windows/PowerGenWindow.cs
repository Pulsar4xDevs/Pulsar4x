using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;
using Vector2 = System.Numerics.Vector2;

namespace Pulsar4X.Client
{
    public class PowerGenWindow : UniquePulsarGuiWindow<PowerGenWindow>
    {
        private int _entityId = -1;
        private string? _systemId;
        Vector2 _plotSize = new Vector2(512, 64);

        internal static PowerGenWindow GetInstance()
        {
            PowerGenWindow instance;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(PowerGenWindow)))
            {
                instance = new PowerGenWindow();
            }
            else
            {
                instance = (PowerGenWindow)_uiState.LoadedWindows[typeof(PowerGenWindow)];
            }

            if (_uiState.LastClickedEntity is { } clicked && clicked.StarSystemId != null)
                instance.SetEntity(clicked.Id, clicked.StarSystemId);

            return instance;
        }

        private PowerGenWindow()
        {
        }

        public void SetEntity(int entityId, string systemId)
        {
            _entityId = entityId;
            _systemId = systemId;
        }

        internal override void Display()
        {
            if (!IsActive || _systemId == null)
                return;

            var entity = _uiState.GameClient?.Galaxy.GetSystem(_systemId)?.GetEntity(_entityId);
            var energy = entity?.GetView<EnergyView>();
            if (entity == null || energy == null)
            {
                IsActive = false;
                return;
            }

            string entityName = entity.GetView<NameView>()?.Name ?? "Unknown";
            if (Window.Begin("Power Display " + entityName, ref IsActive, _flags))
            {
                ImGui.Text("Current Load: ");
                ImGui.SameLine();
                ImGui.Text(energy.Load.ToString());

                ImGui.Text("Current Output: ");
                ImGui.SameLine();
                ImGui.Text(energy.Output.ToString() + " / " + energy.MaxOutput);

                ImGui.Text("Current Demand: ");
                ImGui.SameLine();
                ImGui.Text(energy.Demand.ToString());

                ImGui.Text("Stored: ");
                ImGui.SameLine();
                ImGui.Text(energy.Stored + " / " + energy.StoreMax);

                var histogram = energy.Histogram;
                if (histogram.Count > 1 && energy.StoreMax > 0)
                {
                    var colour1 = ImGui.GetColorU32(ImGuiCol.Text);
                    var colour2 = ImGui.GetColorU32(ImGuiCol.PlotLines);
                    var colour3 = ImGui.GetColorU32(ImGuiCol.Button);
                    ImDrawListPtr draw_list = ImGui.GetWindowDrawList();

                    var plotPos = ImGui.GetCursorScreenPos();
                    ImGui.InvisibleButton("PowerPlot", _plotSize);

                    float xstep = _plotSize.X / histogram[histogram.Count - 1].Seconds;
                    float ystep = (float)(_plotSize.Y / energy.StoreMax);
                    float posYBase = plotPos.Y + _plotSize.Y;

                    var first = histogram[0];
                    float posX = 0;
                    float posYO = ystep * (float)first.Output;
                    float posYD = ystep * (float)first.Demand;
                    float posYS = ystep * (float)first.Stored;

                    for (int i = 1; i < histogram.Count; i++)
                    {
                        var sample = histogram[i];
                        float nextX = xstep * sample.Seconds;
                        float nextYO = ystep * (float)sample.Output;
                        float nextYD = ystep * (float)sample.Demand;
                        float nextYS = ystep * (float)sample.Stored;
                        draw_list.AddLine(new Vector2(plotPos.X + posX, posYBase - posYO), new Vector2(plotPos.X + nextX, posYBase - nextYO), colour1);
                        draw_list.AddLine(new Vector2(plotPos.X + posX, posYBase - posYD), new Vector2(plotPos.X + nextX, posYBase - nextYD), colour2);
                        draw_list.AddLine(new Vector2(plotPos.X + posX, posYBase - posYS), new Vector2(plotPos.X + nextX, posYBase - nextYS), colour3);
                        posX = nextX;
                        posYO = nextYO;
                        posYD = nextYD;
                        posYS = nextYS;
                    }
                }
                Window.End();
            }

        }
    }
}
