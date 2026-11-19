using System;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;

namespace Pulsar4X.Tests;

/// <summary>Client logic for the industry-head seat. No ImGui and no game.</summary>
[TestFixture]
public class IndustrySeatUiTests
{
    static CommanderSnapshot Person(
        int id,
        string name,
        CommanderKind kind = CommanderKind.Civilian,
        bool assigned = false,
        string? assignment = null)
        => new(id, name, kind, assigned, 0, 0, default) { AssignmentName = assignment };

    static ColonyView Colony(int administratorId, int industryHeadId = -1, string? industryHeadName = null)
        => new(0, null)
        {
            AdministratorId = administratorId,
            IndustryHeadId = industryHeadId,
            IndustryHeadName = industryHeadName,
        };

    [Test]
    public void HolderText_IsEmpty_WhenTheSeatHasNoHead()
    {
        Assert.That(IndustrySeatUi.IsSeated(-1), Is.False);
        Assert.That(IndustrySeatUi.HolderText(-1, null), Is.EqualTo("Empty"));
        Assert.That(IndustrySeatUi.HolderText(-1, "Lee"), Is.EqualTo(IndustrySeatUi.EmptyLabel));
        Assert.That(IndustrySeatUi.IsSeated(12), Is.True);
        Assert.That(IndustrySeatUi.HolderText(12, "Lee"), Is.EqualTo("Lee"));
    }

    [Test]
    public void Candidates_AreUnassignedCivilians_AndNeverIncludeACityHallChairHolder()
    {
        var lee = Person(1, "Lee");
        var ada = Person(2, "Ada", assigned: true, assignment: "Earth HQ");
        var navy = Person(3, "Kim", CommanderKind.Navy);
        var scientist = Person(4, "Ravi", CommanderKind.Scientist);
        var later = Person(5, "Zara");
        var chair = Person(6, "Chair", assigned: false); // excluded by city-hall id even if the flag slipped

        var picked = IndustrySeatUi.Candidates(
            new[] { later, lee, ada, navy, scientist, chair },
            cityHallIds: new[] { 6, 2 });

        Assert.That(picked.Select(p => p.Name), Is.EqualTo(new[] { "Lee", "Zara" }));
        Assert.That(picked.All(p => p.Kind == CommanderKind.Civilian), Is.True);
        Assert.That(picked.All(p => !p.IsAssigned), Is.True);
        Assert.That(picked.Select(p => p.Id), Does.Not.Contain(6).And.Not.Contain(2));
    }

    [Test]
    public void Candidates_FromANullRoster_AreEmpty()
    {
        Assert.That(IndustrySeatUi.Candidates(null), Is.Empty);
        Assert.That(IndustrySeatUi.CityHallIds(null), Is.Empty);
        Assert.That(IndustrySeatUi.CityHallIds(Array.Empty<ColonyView?>()), Is.Empty);
    }

    [Test]
    public void CityHallIds_CollectFilledAdministratorChairs()
    {
        var ids = IndustrySeatUi.CityHallIds(new ColonyView?[]
        {
            Colony(11),
            Colony(-1),
            null,
            Colony(22, industryHeadId: 33, industryHeadName: "Lee"),
        });
        Assert.That(ids, Is.EqualTo(new[] { 11, 22 }));
        Assert.That(ids, Does.Not.Contain(33), "the industry head is not the city-hall chair");
    }

    [Test]
    public void RefusalMessage_ShowsTheEngineReason_WhenTheCommandIsRejected()
    {
        Assert.That(IndustrySeatUi.RefusalMessage(null), Is.Null);
        Assert.That(IndustrySeatUi.RefusalMessage(CommandResult.Ok("ok")), Is.Null);
        Assert.That(
            IndustrySeatUi.RefusalMessage(CommandResult.Reject("Ada holds a city-hall chair. One person cannot hold both the city-hall chair and the industry seat.")),
            Is.EqualTo("Ada holds a city-hall chair. One person cannot hold both the city-hall chair and the industry seat."));
        Assert.That(IndustrySeatUi.RefusalMessage(new CommandResult(false)), Is.EqualTo("The command was refused."));
    }
}
