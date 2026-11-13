using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Tests;

/// <summary>Filtering logic for the reusable design picker widget (QoL Fix 5). No ImGui.</summary>
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

    [Test]
    public void MountCategories_MapsFlagsAndAllowsMultipleCategories()
    {
        Assert.That(DesignFilterWidget.MountCategories(ComponentMountFlags.ShipComponent),
            Is.EqualTo(new[] { "Ship" }));
        Assert.That(DesignFilterWidget.MountCategories(ComponentMountFlags.Fighter),
            Is.EqualTo(new[] { "Ship" }));
        Assert.That(DesignFilterWidget.MountCategories(ComponentMountFlags.PlanetInstallation),
            Is.EqualTo(new[] { "Colony" }));
        Assert.That(DesignFilterWidget.MountCategories(ComponentMountFlags.Missile),
            Is.EqualTo(new[] { "Missile" }));
        Assert.That(DesignFilterWidget.MountCategories(ComponentMountFlags.ShipCargo),
            Is.EqualTo(new[] { "Cargo" }));

        // Cargo is only used when none of Ship / Colony / Missile matched.
        Assert.That(
            DesignFilterWidget.MountCategories(ComponentMountFlags.ShipComponent | ComponentMountFlags.ShipCargo),
            Is.EqualTo(new[] { "Ship" }));

        Assert.That(
            DesignFilterWidget.MountCategories(
                ComponentMountFlags.ShipComponent | ComponentMountFlags.PlanetInstallation),
            Is.EqualTo(new[] { "Ship", "Colony" }));

        Assert.That(DesignFilterWidget.MountCategories(ComponentMountFlags.None), Is.Empty);
        Assert.That(DesignFilterWidget.MountCategories(ComponentMountFlags.Pdc), Is.Empty);
    }

    [Test]
    public void ConstructibleCategories_UsesShipDesignMaterialOrMountFlags()
    {
        var ship = new ConstructibleItemView("s", "Scout", 1, 1, false)
        {
            IsShipDesign = true,
            MountFlags = ComponentMountFlags.ShipComponent,
            ComponentType = "Ship",
        };
        Assert.That(DesignFilterWidget.ConstructibleCategories(ship), Is.EqualTo(new[] { "Ship Design" }));

        var material = new ConstructibleItemView("m", "Steel", 1, 1, false)
        {
            IsMaterial = true,
            ComponentType = "Material",
        };
        Assert.That(DesignFilterWidget.ConstructibleCategories(material), Is.EqualTo(new[] { "Material" }));

        var component = new ConstructibleItemView("c", "Passive Sensor", 1, 1, false)
        {
            MountFlags = ComponentMountFlags.ShipComponent | ComponentMountFlags.PlanetInstallation,
            ComponentType = "Sensors",
        };
        Assert.That(DesignFilterWidget.ConstructibleCategories(component), Is.EqualTo(new[] { "Ship", "Colony" }));
    }

    [Test]
    public void Filter_MultiCategoryItem_MatchesAnySelectedCategoryAndSearch()
    {
        var items = new[]
        {
            new DesignFilterEntry("Passive Sensor", new[] { "Ship", "Colony" }, "Sensors"),
            new DesignFilterEntry("Fuel Cargo Hold", new[] { "Ship", "Colony" }, "Storage", "hold"),
            new DesignFilterEntry("Military Reactor", "Ship", "Power"),
        };

        var widget = Widget();
        widget.SelectedCategory = "Colony";
        Assert.That(Names(widget, items), Is.EqualTo(new[] { "Passive Sensor", "Fuel Cargo Hold" }));

        widget.SelectedCategory = "ship";
        Assert.That(Names(widget, items), Is.EqualTo(new[] { "Passive Sensor", "Fuel Cargo Hold", "Military Reactor" }));

        widget.SelectedCategory = "Colony";
        widget.SearchText = "hold";
        Assert.That(Names(widget, items), Is.EqualTo(new[] { "Fuel Cargo Hold" }));
    }

    [Test]
    public void Sanitize_KeepsCategoryPresentOnAMultiCategoryItem()
    {
        var items = new[]
        {
            new DesignFilterEntry("Passive Sensor", new[] { "Ship", "Colony" }, "Sensors"),
        };
        var widget = Widget();
        widget.SelectedCategory = "Colony";
        Assert.That(widget.Sanitize(items, Describe), Is.False);
        Assert.That(widget.SelectedCategory, Is.EqualTo("Colony"));

        widget.SelectedCategory = "Missile";
        Assert.That(widget.Sanitize(items, Describe), Is.True);
        Assert.That(widget.SelectedCategory, Is.EqualTo(DesignFilterWidget.All));
    }

    [Test]
    public void DescribeHelpers_PopulateNameTypeAndCategories()
    {
        var template = new ComponentTemplateSummary("passive-sensor", "Passive Sensor", "Sensors", "A quiet eye")
        {
            MountFlags = ComponentMountFlags.ShipComponent | ComponentMountFlags.PlanetInstallation,
        };
        var described = DesignFilterWidget.DescribeTemplate(template);
        Assert.That(described.Name, Is.EqualTo("Passive Sensor"));
        Assert.That(described.Type, Is.EqualTo("Sensors"));
        Assert.That(described.Categories, Is.EqualTo(new[] { "Ship", "Colony" }));
        Assert.That(described.MatchesSearch("quiet"), Is.True);

        var item = new ConstructibleItemView("scout", "Scout", 10, 1, false)
        {
            IsShipDesign = true,
            ComponentType = "Ship",
            CrewWarning = "10 berths for 28 crew",
        };
        var constructible = DesignFilterWidget.DescribeConstructible(item);
        Assert.That(constructible.Categories, Is.EqualTo(new[] { "Ship Design" }));
        Assert.That(constructible.Type, Is.EqualTo("Ship"));
        Assert.That(constructible.MatchesSearch("berths"), Is.True);
    }

    [Test]
    public void SingleCategoryConstructor_TreatsWhitespaceAsEmptyList()
    {
        var entry = new DesignFilterEntry("Nameless", "  ", "Type");
        Assert.That(entry.Categories, Is.Empty);
        Assert.That(entry.Category, Is.EqualTo(""));
        Assert.That(entry.HasCategory("Ship"), Is.False);
    }
}
