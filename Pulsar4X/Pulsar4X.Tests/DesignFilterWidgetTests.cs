using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Tests;

/// <summary>Filtering logic for the reusable design picker widget (QoL Fix 5 prep). No ImGui.</summary>
[TestFixture]
public class DesignFilterWidgetTests
{
    static readonly DesignFilterEntry[] Items =
    {
        new("Civilian Reactor", "Ship", "Power"),
        new("Mining Installation", "Colony", "Industry"),
        new("Research Lab", "Colony", "Science", "University"),
        new("Military Reactor", "Ship", "Power"),
        new("Cargo Hold", "Ship", "Storage"),
    };

    static DesignFilterEntry Describe(DesignFilterEntry entry) => entry;

    DesignFilterWidget Widget() => new();

    List<string> Names(DesignFilterWidget widget, IReadOnlyList<DesignFilterEntry>? items = null)
        => widget.Filter(items ?? Items, Describe).Select(e => e.Name).ToList();

    [Test]
    public void Filter_Default_ReturnsEveryItem()
    {
        Assert.That(Names(Widget()), Is.EqualTo(Items.Select(e => e.Name).ToList()));
    }

    [Test]
    public void Filter_Category_KeepsMatchingItems()
    {
        var widget = Widget();
        widget.SelectedCategory = "Ship";
        Assert.That(Names(widget), Is.EqualTo(new[] { "Civilian Reactor", "Military Reactor", "Cargo Hold" }));

        widget.SelectedCategory = "colony"; // case-insensitive
        Assert.That(Names(widget), Is.EqualTo(new[] { "Mining Installation", "Research Lab" }));
    }

    [Test]
    public void Filter_Type_KeepsMatchingItems()
    {
        var widget = Widget();
        widget.SelectedType = "Power";
        Assert.That(Names(widget), Is.EqualTo(new[] { "Civilian Reactor", "Military Reactor" }));
    }

    [Test]
    public void Filter_Search_IsCaseInsensitiveSubstringAcrossFields()
    {
        var widget = Widget();
        widget.SearchText = "reac";
        Assert.That(Names(widget), Is.EqualTo(new[] { "Civilian Reactor", "Military Reactor" }));

        widget.SearchText = "SCIENCE";
        Assert.That(Names(widget), Is.EqualTo(new[] { "Research Lab" }));

        widget.SearchText = "university"; // Extra/subtitle field
        Assert.That(Names(widget), Is.EqualTo(new[] { "Research Lab" }));
    }

    [Test]
    public void Filter_CombinesCategoryTypeAndSearch()
    {
        var widget = Widget();
        widget.SelectedCategory = "Ship";
        widget.SelectedType = "Power";
        widget.SearchText = "military";
        Assert.That(Names(widget), Is.EqualTo(new[] { "Military Reactor" }));
    }

    [Test]
    public void Sanitize_ResetsStaleCategoryAndTypeToAll()
    {
        var widget = Widget();
        widget.SelectedCategory = "Orbital";
        widget.SelectedType = "Weapon";
        Assert.That(widget.Sanitize(Items, Describe), Is.True);
        Assert.That(widget.SelectedCategory, Is.EqualTo(DesignFilterWidget.All));
        Assert.That(widget.SelectedType, Is.EqualTo(DesignFilterWidget.All));
        Assert.That(widget.Sanitize(Items, Describe), Is.False);
    }

    [Test]
    public void Filter_SanitizesStaleSelectionSoItemsReappear()
    {
        var widget = Widget();
        widget.SelectedCategory = "Gone";
        Assert.That(Names(widget), Is.EqualTo(Items.Select(e => e.Name).ToList()));
        Assert.That(widget.SelectedCategory, Is.EqualTo(DesignFilterWidget.All));
    }

    [Test]
    public void Reset_ClearsAllSelections()
    {
        var widget = Widget();
        widget.SelectedCategory = "Ship";
        widget.SelectedType = "Power";
        widget.SearchText = "reac";
        widget.Reset();
        Assert.That(widget.SelectedCategory, Is.EqualTo(DesignFilterWidget.All));
        Assert.That(widget.SelectedType, Is.EqualTo(DesignFilterWidget.All));
        Assert.That(widget.SearchText, Is.EqualTo(""));
        Assert.That(Names(widget), Has.Count.EqualTo(Items.Length));
    }

    [Test]
    public void Filter_EmptyList_ReturnsEmptyAndKeepsAll()
    {
        var widget = Widget();
        widget.SelectedCategory = "Ship";
        var empty = Array.Empty<DesignFilterEntry>();
        Assert.That(widget.Filter(empty, Describe), Is.Empty);
        Assert.That(widget.SelectedCategory, Is.EqualTo(DesignFilterWidget.All));
    }
}
