using System;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Movement;
using Pulsar4X.Orbital;

namespace Pulsar4X.Tests
{
    /// <summary>
    /// The departure rule on a hand-built porkchop. Best window waits for a near-cheapest
    /// cell inside the cap. Depart now is the first column.
    /// </summary>
    public class DepartureWindowTests
    {
        static readonly DateTime T0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [Test]
        public void BestWindow_WaitsForTheCheaperWindowInsideTheCap()
        {
            var grid = Grid(
                (0, 150, 0),
                (1, 100, 1),
                (2, 50, 10));
            var answer = TransferPlanner.Choose(grid, 200, TimeSpan.FromDays(2).TotalSeconds, DepartureTiming.BestWindow, null);

            Assert.IsTrue(answer.Feasible, answer.Note);
            Assert.AreEqual(100, answer.Window.TotalDeltaV, 0.001);
            Assert.AreEqual(T0.AddDays(1), answer.Window.Depart);
        }

        [Test]
        public void BestWindow_TakesTheEarlierWindowInsideTheTolerance()
        {
            var grid = Grid(
                (0, 106, 0),
                (1, 100, 1));
            var answer = TransferPlanner.Choose(grid, 200, TimeSpan.FromDays(10).TotalSeconds, DepartureTiming.BestWindow, null);

            Assert.IsTrue(answer.Feasible, answer.Note);
            Assert.AreEqual(106, answer.Window.TotalDeltaV, 0.001, "6% apart is inside the 10% band, so the earlier window flies");
            Assert.AreEqual(T0, answer.Window.Depart);
        }

        [Test]
        public void BestWindow_NothingInsideTheCap_WidensAndSaysSo()
        {
            var grid = Grid(
                (0, 500, 0),
                (1, 100, 10));
            var answer = TransferPlanner.Choose(grid, 200, 3600, DepartureTiming.BestWindow, null);

            Assert.IsTrue(answer.Feasible, answer.Note);
            Assert.That(answer.Note, Does.Contain("No window inside the wait cap"));
            Assert.AreEqual(100, answer.Window.TotalDeltaV, 0.001);
        }

        [Test]
        public void DepartNow_TakesTheFirstColumn()
        {
            var t0 = T0;
            var departures = new[] { t0, t0.AddDays(1) };
            var tof = new[] { 3600.0, 7200.0 };
            var cells = new TransferWindow[2, 2];
            cells[0, 0] = Cell(departures[0], 150, tof[0]);
            cells[0, 1] = Cell(departures[0], 80, tof[1]);
            cells[1, 0] = Cell(departures[1], 40, tof[0]);
            cells[1, 1] = Miss(departures[1]);
            var grid = TransferGrid.FromCells(departures, tof, cells);

            var answer = TransferPlanner.Choose(grid, 200, TimeSpan.FromDays(10).TotalSeconds, DepartureTiming.DepartNow, null);

            Assert.IsTrue(answer.Feasible, answer.Note);
            Assert.AreEqual(80, answer.Window.TotalDeltaV, 0.001);
            Assert.AreEqual(t0, answer.Window.Depart);
        }

        [Test]
        public void DepartNow_NoWindowNow_SaysEarliestIs()
        {
            var grid = Grid(
                (0, 500, 0),
                (1, 100, 3));
            var answer = TransferPlanner.Choose(grid, 200, TimeSpan.FromDays(10).TotalSeconds, DepartureTiming.DepartNow, null);

            Assert.IsTrue(answer.Feasible, answer.Note);
            Assert.That(answer.Note, Does.Contain("Cannot leave now"));
            Assert.AreEqual(100, answer.Window.TotalDeltaV, 0.001);
        }

        [Test]
        public void Preview_PickOnPorkchop_UsesTheChosenWindow()
        {
            var grid = Grid(
                (0, 150, 0),
                (1, 100, 1));
            double cap = TimeSpan.FromDays(10).TotalSeconds;
            TransferWindow picked = grid.Cells[1, 0];

            var flown = TransferPlanner.Choose(grid, 200, cap, DepartureTiming.PickOnPorkchop, picked);
            Assert.IsTrue(flown.Feasible, flown.Note);
            Assert.AreEqual(picked.Depart, flown.Window.Depart);
            Assert.AreEqual(100, flown.Window.TotalDeltaV, 0.001);
            Assert.AreEqual(DepartureTiming.BestWindow, flown.AltTiming);

            var none = TransferPlanner.Choose(grid, 200, cap, DepartureTiming.PickOnPorkchop, null);
            Assert.IsFalse(none.Feasible);
            Assert.That(none.Note, Does.Contain("Pick a cell on the porkchop."));

            TransferWindow expensive = Cell(T0.AddDays(1), 900, 3600);
            var refused = TransferPlanner.Choose(grid, 200, cap, DepartureTiming.PickOnPorkchop, expensive);
            Assert.IsFalse(refused.Feasible);
            Assert.That(refused.Note, Does.Contain("needs"));
            Assert.That(refused.Note, Does.Contain("900"));
            Assert.That(refused.Note, Does.Contain("200"));
        }

        static TransferGrid Grid(params (int column, double dv, double waitDays)[] filled)
        {
            int columns = 0;
            foreach (var cell in filled)
                if (cell.column + 1 > columns)
                    columns = cell.column + 1;
            var departures = new DateTime[columns];
            for (int i = 0; i < columns; i++)
                departures[i] = T0.AddDays(i);
            var tof = new[] { 3600.0 };
            var cells = new TransferWindow[columns, 1];
            for (int i = 0; i < columns; i++)
                cells[i, 0] = Miss(departures[i]);
            foreach (var cell in filled)
            {
                var depart = T0.AddDays(cell.waitDays);
                departures[cell.column] = depart;
                cells[cell.column, 0] = Cell(depart, cell.dv, tof[0]);
            }
            return TransferGrid.FromCells(departures, tof, cells);
        }

        static TransferWindow Cell(DateTime depart, double dv, double tofSeconds)
            => new TransferWindow(depart, depart.AddSeconds(tofSeconds), new Vector3(dv, 0, 0), new Vector3(0, 0, 0), dv, 0, 0);

        static TransferWindow Miss(DateTime depart)
            => new TransferWindow(depart, depart, default, default, double.NaN, double.NaN, double.NaN);
    }
}
