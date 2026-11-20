using System;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;

namespace Pulsar4X.Tests;

/// <summary>Size clamping and candidate filtering for the shared assignment chooser. No ImGui.</summary>
[TestFixture]
public class PersonAssignmentUiTests
{
    static CommanderSnapshot Person(int id, string name, CommanderKind kind = CommanderKind.Civilian, bool assigned = false)
        => new(id, name, kind, assigned, 0, 0, default);

    [Test]
    public void MaxHeight_IsSeventyPercentOfTheViewport()
    {
        Assert.That(PersonAssignmentUi.MaxHeight(1000f), Is.EqualTo(700f).Within(0.001f));
        Assert.That(PersonAssignmentUi.MaxHeightFraction, Is.EqualTo(0.7f).Within(0.0001f));
        var max = PersonAssignmentUi.MaxSize(new Vector2(1280, 800));
        Assert.That(max.X, Is.EqualTo(1280f).Within(0.001f));
        Assert.That(max.Y, Is.EqualTo(560f).Within(0.001f));
    }

    [Test]
    public void Constrain_ClampsToMinAndToSeventyPercentHeight()
    {
        var viewport = new Vector2(1000, 1000);
        var tooSmall = PersonAssignmentUi.Constrain(new Vector2(100, 100), viewport);
        Assert.That(tooSmall, Is.EqualTo(PersonAssignmentUi.MinSize));

        var tooTall = PersonAssignmentUi.Constrain(new Vector2(800, 900), viewport);
        Assert.That(tooTall.X, Is.EqualTo(800f).Within(0.001f));
        Assert.That(tooTall.Y, Is.EqualTo(700f).Within(0.001f), "max height is 70% of the viewport");

        var inRange = PersonAssignmentUi.Constrain(PersonAssignmentUi.DefaultSize, viewport);
        Assert.That(inRange, Is.EqualTo(PersonAssignmentUi.DefaultSize));
    }

    [Test]
    public void ExcludeCurrent_DropsThePersonAlreadyInThePost()
    {
        var people = new[] { Person(1, "Ada"), Person(2, "Lee"), Person(3, "Mo") };
        Assert.That(PersonAssignmentUi.ExcludeCurrent(people, 2).Select(p => p.Id), Is.EqualTo(new[] { 1, 3 }));
        Assert.That(PersonAssignmentUi.ExcludeCurrent(people, -1).Select(p => p.Id), Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(PersonAssignmentUi.ExcludeCurrent(null, 1), Is.Empty);
    }

    [Test]
    public void FilterByName_IsACaseInsensitiveSubstring_AndBlankKeepsEveryone()
    {
        var people = new[] { Person(1, "Ada Lovelace"), Person(2, "Lee"), Person(3, "Adalyn") };
        Assert.That(PersonAssignmentUi.FilterByName(people, "ada").Select(p => p.Id), Is.EqualTo(new[] { 1, 3 }));
        Assert.That(PersonAssignmentUi.FilterByName(people, "LEE").Select(p => p.Name), Is.EqualTo(new[] { "Lee" }));
        Assert.That(PersonAssignmentUi.FilterByName(people, "  ").Select(p => p.Id), Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(PersonAssignmentUi.FilterByName(people, "nobody"), Is.Empty);
    }

    [Test]
    public void OfKind_KeepsOnlyThatKind_OrderedByName()
    {
        var people = new[]
        {
            Person(2, "Zed", CommanderKind.Navy),
            Person(1, "Ann", CommanderKind.Navy),
            Person(3, "Bo", CommanderKind.Civilian),
        };
        Assert.That(PersonAssignmentUi.OfKind(people, CommanderKind.Navy).Select(p => p.Name), Is.EqualTo(new[] { "Ann", "Zed" }));
        Assert.That(PersonAssignmentUi.OfKind(people, CommanderKind.Scientist), Is.Empty);
    }
}
