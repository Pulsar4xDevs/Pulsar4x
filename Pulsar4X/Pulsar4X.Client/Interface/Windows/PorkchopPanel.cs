using System;
using System.Threading.Tasks;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Orbital;
using Vector2 = System.Numerics.Vector2;
using Vector3 = Pulsar4X.Orbital.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Pulsar4X.Client
{
    /// <summary>
    /// One porkchop. NavWindow and the Orders panel share it. The scan starts only when
    /// <see cref="Start"/> is called, never because a row was drawn.
    /// </summary>
    public sealed class PorkchopPanel
    {
        public static int ScansStarted { get; private set; }

        /// <summary>NavWindow selects the cheapest cell when the scan finishes. Orders does not.</summary>
        public bool SelectBestWhenReady { get; set; } = true;

        public TransferGrid? Grid { get; private set; }
        public string Error { get; private set; } = "";
        public bool Scanning => _task != null && !_task.IsCompleted;
        public int SelectedI { get; private set; } = -1;
        public int SelectedJ { get; private set; } = -1;
        public TransferWindow? BestWindow { get; private set; }
        public TransferWindow? DepartNow { get; private set; }

        int _gen;
        int _taskGen;
        Task<TransferGrid>? _task;

        public TransferWindow? Selected
        {
            get
            {
                if (Grid == null || SelectedI < 0 || SelectedJ < 0)
                    return null;
                if (SelectedI >= Grid.Departures.Length || SelectedJ >= Grid.TimeOfFlightSeconds.Length)
                    return null;
                TransferWindow cell = Grid.Cells[SelectedI, SelectedJ];
                return cell.IsFinite ? cell : null;
            }
        }

        public void Reset()
        {
            _gen++;
            _task = null;
            Grid = null;
            SelectedI = -1;
            SelectedJ = -1;
            BestWindow = null;
            DepartNow = null;
            Error = "";
        }

        public void Start(
            KeplerElements ship, KeplerElements target, DateTime start,
            double departSeconds, double tofMin, double tofMax, int steps, bool prograde,
            Func<Vector3, Vector3, double>? departCost, Func<Vector3, Vector3, double>? arriveCost)
        {
            ScansStarted++;
            Error = "";
            KeplerElements shipCopy = ship;
            KeplerElements targetCopy = target;
            double mu = shipCopy.StandardGravParameter;
            if (!(tofMax > tofMin))
                tofMax = tofMin + 86400.0;
            int clamped = Math.Clamp(steps, 8, 120);
            var depart = departCost ?? TransferWindows.MagnitudeCost;
            var arrive = arriveCost ?? TransferWindows.MagnitudeCost;
            _taskGen = _gen;
            _task = Task.Run(() => TransferWindows.Scan(
                mu,
                at => KeplerState(shipCopy, at),
                at => KeplerState(targetCopy, at),
                start, start.AddSeconds(Math.Max(1.0, departSeconds)), clamped,
                tofMin, tofMax, clamped,
                depart, arrive,
                prograde));
        }

        public void Take()
        {
            if (_task == null || !_task.IsCompleted)
                return;
            if (_task.Status == TaskStatus.RanToCompletion && _taskGen == _gen)
            {
                Grid = _task.Result;
                if (SelectBestWhenReady)
                    SelectAbsoluteBest();
            }
            else if (_task.IsFaulted)
                Error = _task.Exception?.GetBaseException().Message ?? "scan failed";
            _task = null;
        }

        /// <summary>Outline the Best-window cell and the Depart-now cell from this grid.</summary>
        public void MarkRule(double budget)
        {
            BestWindow = null;
            DepartNow = null;
            if (Grid == null || Grid.Departures.Length == 0 || Grid.TimeOfFlightSeconds.Length == 0)
                return;
            double span = (Grid.Departures[Grid.Departures.Length - 1] - Grid.Departures[0]).TotalSeconds;
            double tofMin = Grid.TimeOfFlightSeconds[0];
            double tofMax = Grid.TimeOfFlightSeconds[Grid.TimeOfFlightSeconds.Length - 1];
            double cap = TransferWindows.BestWindowWaitCap(tofMin, tofMax, span);
            if (Grid.TryBestWindow(budget, cap, TransferWindows.BestWindowDvTolerance, out TransferWindow best))
                BestWindow = best;
            if (Grid.TryCheapestInColumn(0, budget, out TransferWindow now))
                DepartNow = now;
        }

        public void Draw(float ceiling, bool tripCost)
        {
            TransferGrid? grid = Grid;
            if (grid == null)
                return;
            int nx = grid.Departures.Length;
            int ny = grid.TimeOfFlightSeconds.Length;
            if (nx == 0 || ny == 0)
                return;

            float plotW = Math.Max(80f, ImGui.GetContentRegionAvail().X);
            float plotH = 220f;
            Vector2 origin = ImGui.GetCursorScreenPos();
            ImGui.Dummy(new Vector2(plotW, plotH));

            double lowest = double.PositiveInfinity;
            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < ny; j++)
                {
                    double cost = grid.Cost[i, j];
                    if (double.IsFinite(cost) && cost <= ceiling && cost < lowest)
                        lowest = cost;
                }
            }
            if (!double.IsFinite(lowest))
                lowest = 0;
            double span = Math.Max(1.0, ceiling - lowest);

            var draw = ImGui.GetWindowDrawList();
            uint grey = ImGui.ColorConvertFloat4ToU32(new Vector4(0.35f, 0.35f, 0.38f, 1f));
            uint white = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));
            uint yellow = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.85f, 0.2f, 1f));
            uint blue = ImGui.ColorConvertFloat4ToU32(new Vector4(0.4f, 0.75f, 1f, 1f));
            float cellW = plotW / nx;
            float cellH = plotH / ny;
            double bestDv = grid.HasBest ? grid.Best.TotalDeltaV : double.NaN;

            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < ny; j++)
                {
                    float x0 = origin.X + i * cellW;
                    float y0 = origin.Y + (ny - 1 - j) * cellH;
                    var min = new Vector2(x0, y0);
                    var max = new Vector2(x0 + cellW - 1f, y0 + cellH - 1f);
                    double cost = grid.Cost[i, j];
                    uint colour = grey;
                    if (double.IsFinite(cost) && cost <= ceiling)
                    {
                        float t = (float)((cost - lowest) / span);
                        if (t < 0f)
                            t = 0f;
                        if (t > 1f)
                            t = 1f;
                        colour = ImGui.ColorConvertFloat4ToU32(new Vector4(0.15f + 0.75f * t, 0.72f * (1f - t), 0.12f, 1f));
                    }
                    draw.AddRectFilled(min, max, colour);
                    if (double.IsFinite(bestDv) && double.IsFinite(cost) && cost <= bestDv * 1.05 && cost <= ceiling)
                        draw.AddRect(min, max, white, 0f, ImDrawFlags.None, 1f);
                }
            }

            if (Find(grid, BestWindow, out int bestI, out int bestJ))
                Outline(draw, origin, cellW, cellH, ny, bestI, bestJ, white);
            if (Find(grid, DepartNow, out int nowI, out int nowJ))
                Outline(draw, origin, cellW, cellH, ny, nowI, nowJ, blue);
            if (SelectBestWhenReady && grid.HasBest && Find(grid, grid.Best, out int absI, out int absJ))
                Outline(draw, origin, cellW, cellH, ny, absI, absJ, white);
            Outline(draw, origin, cellW, cellH, ny, SelectedI, SelectedJ, yellow);

            ImGui.Text(grid.Departures[0].ToString("yyyy-MM-dd")
                + "  →  " + grid.Departures[nx - 1].ToString("yyyy-MM-dd")
                + "     TOF " + (grid.TimeOfFlightSeconds[0] / 86400.0).ToString("0.##")
                + "–" + (grid.TimeOfFlightSeconds[ny - 1] / 86400.0).ToString("0.##") + " days");

            Vector2 mouse = ImGui.GetMousePos();
            if (!TryCellAt(origin.X, origin.Y, mouse.X, mouse.Y, plotW, plotH, nx, ny, out int hoverI, out int hoverJ)
                || !ImGui.IsWindowHovered())
                return;

            TransferWindow cell = grid.Cells[hoverI, hoverJ];
            ImGui.BeginTooltip();
            if (!cell.IsFinite)
                ImGui.Text("No solution");
            else
            {
                ImGui.Text("Depart " + cell.Depart.ToString("yyyy-MM-dd HH:mm"));
                ImGui.Text("Arrive " + cell.Arrive.ToString("yyyy-MM-dd HH:mm"));
                ImGui.Text((tripCost ? "Trip " : "Δv ") + Stringify.Velocity(cell.TotalDeltaV));
                if (!tripCost)
                    ImGui.Text("depart " + Stringify.Velocity(cell.DepartDeltaV.Length())
                        + "   arrive " + Stringify.Velocity(cell.ArriveDeltaV.Length()));
                ImGui.Text("vInf " + Stringify.Velocity(cell.VInfDepart) + " / " + Stringify.Velocity(cell.VInfArrive));
            }
            ImGui.EndTooltip();
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                SelectedI = hoverI;
                SelectedJ = hoverJ;
            }
        }

        /// <summary>The cell under a mouse position. The top of the plot is the last time-of-flight row.</summary>
        public static bool TryCellAt(
            float originX, float originY, float mouseX, float mouseY,
            float plotW, float plotH, int nx, int ny, out int i, out int j)
        {
            i = 0;
            j = 0;
            if (nx <= 0 || ny <= 0 || !(plotW > 0) || !(plotH > 0))
                return false;
            if (mouseX < originX || mouseX >= originX + plotW || mouseY < originY || mouseY >= originY + plotH)
                return false;
            float cellW = plotW / nx;
            float cellH = plotH / ny;
            i = Math.Clamp((int)((mouseX - originX) / cellW), 0, nx - 1);
            j = ny - 1 - Math.Clamp((int)((mouseY - originY) / cellH), 0, ny - 1);
            return true;
        }

        /// <summary>Position and velocity from a copied Kepler set. Velocity keeps inclination.</summary>
        public static (Vector3 pos, Vector3 vel) KeplerState(KeplerElements ke, DateTime at)
        {
            var missing = new Vector3(double.NaN, double.NaN, double.NaN);
            try
            {
                double trueAnomaly = OrbitalMath.TrueAnomalyFromElements(ke, at);
                Vector3 pos = OrbitalMath.GetPosition(ke, at);
                Vector3 vel = OrbitalMath.ParentLocalVeclocityVector(
                    ke.StandardGravParameter, pos, ke.SemiMajorAxis, ke.Eccentricity,
                    trueAnomaly, ke.AoP, ke.Inclination, ke.LoAN);
                if (!double.IsFinite(pos.X) || !double.IsFinite(vel.X))
                    return (missing, missing);
                return (pos, vel);
            }
            catch
            {
                return (missing, missing);
            }
        }

        public static bool TrySuggest(KeplerElements ship, KeplerElements target, DateTime now,
            out double departSeconds, out double tofMin, out double tofMax, out bool prograde)
        {
            double synodic = TransferWindows.SynodicSeconds(ship.Period, target.Period);
            double days = double.IsFinite(synodic) && synodic > 0 ? synodic / 86400.0 : 365.25;
            if (days < 1)
                days = 1;
            if (days > 2000)
                days = 2000;
            departSeconds = days * 86400.0;
            (Vector3 pos, Vector3 vel) shipNow = KeplerState(ship, now);
            (Vector3 pos, Vector3 vel) targetNow = KeplerState(target, now);
            var band = TransferWindows.HohmannTofBand(ship.StandardGravParameter, shipNow.pos.Length(), targetNow.pos.Length());
            if (double.IsFinite(band.tofMin) && double.IsFinite(band.tofMax) && band.tofMax > band.tofMin)
            {
                tofMin = band.tofMin;
                tofMax = band.tofMax;
            }
            else
            {
                tofMin = 86400;
                tofMax = 30 * 86400;
            }
            prograde = shipNow.pos.X * shipNow.vel.Y - shipNow.pos.Y * shipNow.vel.X >= 0;
            return shipNow.pos.Length() > 1 && targetNow.pos.Length() > 1;
        }

        void SelectAbsoluteBest()
        {
            SelectedI = -1;
            SelectedJ = -1;
            if (Grid == null || !Grid.HasBest)
                return;
            if (Find(Grid, Grid.Best, out int i, out int j))
            {
                SelectedI = i;
                SelectedJ = j;
            }
        }

        static bool Find(TransferGrid grid, TransferWindow? window, out int i, out int j)
        {
            i = -1;
            j = -1;
            if (grid == null || window is not { } wanted || !wanted.IsFinite)
                return false;
            for (int x = 0; x < grid.Departures.Length; x++)
            {
                for (int y = 0; y < grid.TimeOfFlightSeconds.Length; y++)
                {
                    TransferWindow cell = grid.Cells[x, y];
                    if (cell.IsFinite && cell.Depart == wanted.Depart && cell.Arrive == wanted.Arrive)
                    {
                        i = x;
                        j = y;
                        return true;
                    }
                }
            }
            return false;
        }

        static void Outline(ImDrawListPtr draw, Vector2 origin, float cellW, float cellH, int ny, int i, int j, uint colour)
        {
            if (i < 0 || j < 0)
                return;
            float x0 = origin.X + i * cellW;
            float y0 = origin.Y + (ny - 1 - j) * cellH;
            draw.AddRect(new Vector2(x0, y0), new Vector2(x0 + cellW - 1f, y0 + cellH - 1f), colour, 0f, ImDrawFlags.None, 2f);
        }
    }
}
