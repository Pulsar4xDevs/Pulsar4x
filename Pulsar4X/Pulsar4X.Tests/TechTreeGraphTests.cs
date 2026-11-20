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
}
