using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Client;

namespace Pulsar4X.Tests;

/// <summary>Column assignment, ordering, cycle breaking, and warnings for the tech-tree visualiser. No ImGui.</summary>
[TestFixture]
public class TechTreeGraphTests
{
    static TechTreeUnlock U(string id, int level = 1) => new(level, id);

    static TechTreeSpec Tech(
        string id,
        string? name = null,
        string category = "cat-a",
        string categoryName = "Alpha",
        string cost = "1000",
        int maxLevel = 1,
        params TechTreeUnlock[] unlocks)
        => new(id, name ?? id, "", category, categoryName, cost, maxLevel, unlocks);

    static TechTreeUnlockable Comp(string id, string? name = null)
        => new(id, name ?? id, "component");

    static TechTreeGraph Build(IEnumerable<TechTreeSpec> techs, params TechTreeUnlockable[] unlockables)
        => TechTreeGraph.Build(new TechTreeInput(techs.ToList(), unlockables));

    [Test]
    public void Column_IsTheLongestPrerequisiteChain_AndRootsAreZero()
    {
        // A → B → C, and D also unlocks C, and E stands alone.
        var graph = Build(new[]
        {
            Tech("A", unlocks: U("B")),
            Tech("B", unlocks: U("C")),
            Tech("C"),
            Tech("D", unlocks: U("C")),
            Tech("E"),
        });

        Assert.That(graph.Find("A")!.Column, Is.EqualTo(0));
        Assert.That(graph.Find("D")!.Column, Is.EqualTo(0));
        Assert.That(graph.Find("E")!.Column, Is.EqualTo(0));
        Assert.That(graph.Find("B")!.Column, Is.EqualTo(1));
        Assert.That(graph.Find("C")!.Column, Is.EqualTo(2), "longest chain is A-B-C, not D-C");
        Assert.That(graph.Find("C")!.Prerequisites, Is.EquivalentTo(new[] { "A", "B", "D" }).Or.EquivalentTo(new[] { "B", "D" }));
        Assert.That(graph.Find("C")!.Prerequisites, Does.Contain("B").And.Contain("D"));
        Assert.That(graph.Find("C")!.X, Is.EqualTo(2 * (TechTreeGraph.NodeWidth + TechTreeGraph.ColumnGap)).Within(0.01f));
    }

    [Test]
    public void Barycentre_PullsChildrenTowardTheirParents()
    {
        // Column 0 is A, B, C in name order. X only needs A; Y only needs C.
        // After the barycentre pass X should sit above Y, next to its parent.
        var graph = Build(new[]
        {
            Tech("A", unlocks: U("X")),
            Tech("B"),
            Tech("C", unlocks: U("Y")),
            Tech("X"),
            Tech("Y"),
        });

        Assert.That(graph.Find("A")!.Column, Is.EqualTo(0));
        Assert.That(graph.Find("X")!.Column, Is.EqualTo(1));
        Assert.That(graph.Find("Y")!.Column, Is.EqualTo(1));
        Assert.That(graph.Find("X")!.Row, Is.LessThan(graph.Find("Y")!.Row));
        Assert.That(graph.Find("A")!.Row, Is.LessThan(graph.Find("C")!.Row));
    }

    [Test]
    public void Cycles_AreBrokenWithoutHang_AndStillGetColumns()
    {
        var graph = Build(new[]
        {
            Tech("A", unlocks: U("B")),
            Tech("B", unlocks: U("C")),
            Tech("C", unlocks: U("A")),
        });

        Assert.That(graph.Warnings.Cycles, Is.Not.Empty);
        var cycle = graph.Warnings.Cycles[0].Ids;
        Assert.That(cycle, Does.Contain("A").And.Contain("B").And.Contain("C"));
        Assert.That(graph.Nodes.Select(n => n.Column).Max(), Is.GreaterThanOrEqualTo(0));
        Assert.That(graph.Nodes.All(n => n.Column >= 0), Is.True);
        // A walk around the original cycle still returns every member and does not hang.
        Assert.That(graph.Downstream("A"), Is.EquivalentTo(new[] { "A", "B", "C" }));
        Assert.That(graph.Upstream("A"), Is.EquivalentTo(new[] { "A", "B", "C" }));
    }

    [Test]
    public void MissingPrereqIds_AreListed_AndDoNotBecomeNodes()
    {
        var graph = Build(new[]
        {
            Tech("A", unlocks: new[] { U("tech-missing"), U("ghost-component"), U("B") }),
            Tech("B"),
        }, Comp("real-comp"));

        Assert.That(graph.Warnings.MissingIds, Is.EquivalentTo(new[] { "ghost-component", "tech-missing" }));
        Assert.That(graph.Find("tech-missing"), Is.Null);
        Assert.That(graph.Find("A")!.Unlocks.Select(u => u.Id), Does.Contain("B").And.Contain("tech-missing"));
        Assert.That(graph.Find("A")!.Unlocks.First(u => u.Id == "tech-missing").Kind, Is.EqualTo(TechTreeUnlockKind.Missing));
        Assert.That(graph.Find("B")!.Column, Is.EqualTo(1));
    }

    [Test]
    public void Orphans_HaveNoTechPrereqsAndUnlockNoTechs()
    {
        var graph = Build(new[]
        {
            Tech("linked-a", unlocks: U("linked-b")),
            Tech("linked-b"),
            Tech("orphan", unlocks: U("some-comp")),
            Tech("lonely"),
        }, Comp("some-comp"));

        Assert.That(graph.Warnings.Orphans, Is.EquivalentTo(new[] { "lonely", "orphan" }));
        Assert.That(graph.Warnings.Orphans, Does.Not.Contain("linked-a").And.Not.Contain("linked-b"));
    }

    [Test]
    public void NoComponentTechs_AreThoseWhoseUnlocksListNoComponent()
    {
        var graph = Build(new[]
        {
            Tech("opens-comp", unlocks: U("comp-a")),
            Tech("opens-tech", unlocks: U("child")),
            Tech("child"),
            Tech("opens-nothing"),
        }, Comp("comp-a"));

        Assert.That(graph.Warnings.NoComponentTechs, Is.EquivalentTo(new[] { "child", "opens-nothing", "opens-tech" }));
        Assert.That(graph.Find("opens-comp")!.Unlocks.Single().Kind, Is.EqualTo(TechTreeUnlockKind.Component));
    }

    [Test]
    public void UpstreamAndDownstream_WalkTheFullChains()
    {
        // A → B → D
        // A → C → D
        // D → E
        var graph = Build(new[]
        {
            Tech("A", unlocks: new[] { U("B"), U("C") }),
            Tech("B", unlocks: U("D")),
            Tech("C", unlocks: U("D")),
            Tech("D", unlocks: U("E")),
            Tech("E"),
            Tech("Z"),
        });

        Assert.That(graph.Upstream("D"), Is.EquivalentTo(new[] { "A", "B", "C", "D" }));
        Assert.That(graph.Downstream("B"), Is.EquivalentTo(new[] { "B", "D", "E" }));
        Assert.That(graph.Upstream("Z"), Is.EquivalentTo(new[] { "Z" }));
        Assert.That(graph.Downstream("Z"), Is.EquivalentTo(new[] { "Z" }));
        Assert.That(graph.Upstream("nope"), Is.Empty);
    }

    [Test]
    public void Search_MatchesNameOrCategory_AndIgnoresCase()
    {
        var graph = Build(new[]
        {
            Tech("t1", name: "Ion Drive", category: "tech-category-power", categoryName: "Power & Propulsion"),
            Tech("t2", name: "Mining Laser", category: "tech-category-construction", categoryName: "Construction"),
        });

        Assert.That(graph.Search("ion drive").Select(n => n.Id), Is.EqualTo(new[] { "t1" }));
        Assert.That(graph.Search("PROPULSION").Select(n => n.Id), Is.EqualTo(new[] { "t1" }));
        Assert.That(graph.Search("mining").Select(n => n.Id), Is.EqualTo(new[] { "t2" }));
        Assert.That(graph.Search("construction").Select(n => n.Id), Is.EqualTo(new[] { "t2" }));
        Assert.That(graph.Search("  "), Is.Empty);
        Assert.That(graph.Search(null), Is.Empty);
    }

    // --- RP graph ---

    static long? Cost(string formula, int level)
    {
        if (formula == "flat")
            return 1000;
        if (formula == "plus")
            return 100L * (level + 1); // level 1 = 100, level 2 = 200, level 3 = 300
        if (formula == "pow")
            return 50_000L * (level + 1) * (level + 1) * (level + 1);
        if (formula == "bad")
            return null;
        if (long.TryParse(formula, out long value))
            return value;
        return null;
    }

    static TechTreeGraph BuildRp(IEnumerable<TechTreeSpec> techs)
        => TechTreeGraph.Build(new TechTreeInput(techs.ToList(), Array.Empty<TechTreeUnlockable>()), Cost);

    [Test]
    public void CumulativeRp_AddsLevelCosts_AndChildStartsAtTheUnlockingParentLevel()
    {
        // A has two levels: 1000 + 1000. It unlocks B at level 2, so B starts at 2000.
        var graph = BuildRp(new[]
        {
            Tech("A", cost: "flat", maxLevel: 2, unlocks: U("B", 2)),
            Tech("B", cost: "flat", maxLevel: 1),
        });

        var a = graph.Find("A")!;
        Assert.That(a.UnlockRp, Is.EqualTo(0));
        Assert.That(a.Levels.Select(l => (l.Level, l.Cost, l.CumulativeRp)), Is.EqualTo(new[]
        {
            (1, 1000L, 1000L),
            (2, 1000L, 2000L),
        }));
        Assert.That(a.CumulativeRpAt(2), Is.EqualTo(2000));

        var b = graph.Find("B")!;
        Assert.That(b.UnlockRp, Is.EqualTo(2000), "B starts when A has paid for the unlocking level");
        Assert.That(b.Levels.Single().CumulativeRp, Is.EqualTo(3000));
        Assert.That(graph.MaxCumulativeRp, Is.EqualTo(3000));
    }

    [Test]
    public void CumulativeRp_UsesTheCheapestParentUnlock()
    {
        // P1 unlocks C at level 1 (100). P2 unlocks C at level 3 (100+200+300=600). C starts at 100.
        var graph = BuildRp(new[]
        {
            Tech("P1", cost: "plus", maxLevel: 1, unlocks: U("C", 1)),
            Tech("P2", cost: "plus", maxLevel: 3, unlocks: U("C", 3)),
            Tech("C", cost: "flat", maxLevel: 1),
        });

        Assert.That(graph.Find("P1")!.CumulativeRpAt(1), Is.EqualTo(100));
        Assert.That(graph.Find("P2")!.CumulativeRpAt(3), Is.EqualTo(600));
        Assert.That(graph.Find("C")!.UnlockRp, Is.EqualTo(100));
        Assert.That(graph.Find("C")!.Levels.Single().CumulativeRp, Is.EqualTo(1100));
    }

    [Test]
    public void LevelPositions_MoveRightWithCumulativeRp_AndEdgeStartsAtTheUnlockingLevel()
    {
        var graph = BuildRp(new[]
        {
            Tech("A", cost: "plus", maxLevel: 3, unlocks: U("B", 2)),
            Tech("B", cost: "plus", maxLevel: 1),
        });

        var a = graph.Find("A")!;
        float x0 = TechTreeGraph.RpToX(0);
        float xL1 = TechTreeGraph.RpToX(a.CumulativeRpAt(1));
        float xL2 = TechTreeGraph.RpToX(a.CumulativeRpAt(2));
        float xL3 = TechTreeGraph.RpToX(a.CumulativeRpAt(3));
        Assert.That(a.RpX, Is.EqualTo(x0).Within(0.01f));
        Assert.That(xL1, Is.GreaterThan(x0));
        Assert.That(xL2, Is.GreaterThan(xL1));
        Assert.That(xL3, Is.GreaterThan(xL2));
        Assert.That(a.RpX + a.RpWidth, Is.EqualTo(xL3).Within(0.01f));

        var b = graph.Find("B")!;
        Assert.That(b.RpX, Is.EqualTo(xL2).Within(0.01f), "the edge to B leaves the end of A's level 2");
        Assert.That(a.CumulativeRpAt(2), Is.EqualTo(300)); // 100+200
        Assert.That(b.UnlockRp, Is.EqualTo(300));
    }

    [Test]
    public void LogMapping_PutsZeroAtTheOrigin_AndIsMonotonic_WithStandardTicks()
    {
        Assert.That(TechTreeGraph.RpToX(0), Is.EqualTo(0).Within(0.001f));
        Assert.That(TechTreeGraph.RpToX(250), Is.GreaterThan(TechTreeGraph.RpToX(100)));
        Assert.That(TechTreeGraph.RpToX(1000), Is.GreaterThan(TechTreeGraph.RpToX(500)));
        Assert.That(TechTreeGraph.RpToX(10_000), Is.GreaterThan(TechTreeGraph.RpToX(1000)));
        Assert.That(TechTreeGraph.XToRp(TechTreeGraph.RpToX(5000)), Is.EqualTo(5000).Within(0.5));

        var ticks = TechTreeGraph.RulerTicks(12_000);
        Assert.That(ticks, Is.EqualTo(new long[] { 500, 1000, 2000, 5000, 10_000, 20_000 }));
        Assert.That(TechTreeGraph.RulerTicks(100).First(), Is.EqualTo(500), "the ruler always starts at 500");
    }

    [Test]
    public void RpPacking_DoesNotOverlap_AndKeepsAFamilyTogether()
    {
        // A unlocks B at the end of A, so they can share a row. C is a second root at the same
        // RP range, so it must sit on a lower family block, not tucked between A and B.
        var graph = BuildRp(new[]
        {
            Tech("A", name: "A", cost: "flat", maxLevel: 1, unlocks: U("B", 1)),
            Tech("B", name: "B", cost: "flat", maxLevel: 1),
            Tech("C", name: "C", cost: "flat", maxLevel: 1),
        });

        var a = graph.Find("A")!;
        var b = graph.Find("B")!;
        var c = graph.Find("C")!;
        Assert.That(b.UnlockRp, Is.EqualTo(a.CumulativeRpAt(1)));
        Assert.That(a.RpRow, Is.EqualTo(b.RpRow), "A and B form a chain and do not overlap on X");
        Assert.That(c.RpRow, Is.GreaterThan(a.RpRow), "the second root is packed on rows below the first family");

        // Two children that both start at the same RP as each other must not share a row.
        var fork = BuildRp(new[]
        {
            Tech("Root", cost: "flat", maxLevel: 1, unlocks: new[] { U("X", 1), U("Y", 1) }),
            Tech("X", cost: "flat", maxLevel: 1),
            Tech("Y", cost: "flat", maxLevel: 1),
        });
        Assert.That(fork.Find("X")!.UnlockRp, Is.EqualTo(fork.Find("Y")!.UnlockRp));
        Assert.That(fork.Find("X")!.RpRow, Is.Not.EqualTo(fork.Find("Y")!.RpRow));
        Assert.That(fork.Find("Root")!.RpRow, Is.EqualTo(0));
    }

    [Test]
    public void FailedCostFormulas_AreListed_AndPowStyleCostsEvaluateThroughTheInjector()
    {
        var graph = BuildRp(new[]
        {
            Tech("ok", cost: "pow", maxLevel: 2),
            Tech("broken", cost: "bad", maxLevel: 1),
        });

        Assert.That(graph.Find("ok")!.Levels[0].Cost, Is.EqualTo(50_000)); // 50000*(0+1)^3
        Assert.That(graph.Find("ok")!.Levels[1].Cost, Is.EqualTo(400_000)); // 50000*(2)^3
        Assert.That(graph.Warnings.FailedCostFormulas, Is.EqualTo(new[] { "broken" }));
        Assert.That(graph.Find("broken")!.Levels.Single().CostFailed, Is.True);
        Assert.That(graph.Find("broken")!.Levels.Single().Cost, Is.EqualTo(0));
    }
}
