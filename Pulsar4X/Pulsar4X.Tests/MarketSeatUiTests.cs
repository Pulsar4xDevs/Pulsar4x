using System;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;

namespace Pulsar4X.Tests;

/// <summary>Client logic for the market-head seat. No ImGui and no game.</summary>
[TestFixture]
public class MarketSeatUiTests
{
    static CommanderSnapshot Person(
        int id,
        string name,
        CommanderKind kind = CommanderKind.Civilian,
        bool assigned = false,
        string? assignment = null,
        int seed = 11,
        string role = PortraitRoles.Civilian,
        string species = "human")
        => new(id, name, kind, assigned, 0, 0, default)
        {
            AssignmentName = assignment,
            PortraitSeed = seed,
            PortraitRole = role,
            PortraitSpecies = species,
        };

    static ColonyView Colony(
        int administratorId = -1,
        int industryHeadId = -1,
        int marketHeadId = -1,
        string? marketHeadName = null)
        => new(0, null)
        {
            AdministratorId = administratorId,
            IndustryHeadId = industryHeadId,
            MarketHeadId = marketHeadId,
            MarketHeadName = marketHeadName,
        };

    [Test]
    public void HolderText_IsEmpty_WhenTheSeatHasNoHead()
    {
        Assert.That(MarketSeatUi.IsSeated(-1), Is.False);
        Assert.That(MarketSeatUi.HolderText(-1, null), Is.EqualTo(MarketSeatUi.EmptyLabel));
        Assert.That(MarketSeatUi.HolderText(-1, "Mara"), Is.EqualTo(MarketSeatUi.EmptyLabel));
        Assert.That(MarketSeatUi.IsSeated(12), Is.True);
        Assert.That(MarketSeatUi.HolderText(12, "Mara"), Is.EqualTo("Mara"));
    }

    [Test]
    public void Candidates_AreUnassignedCivilians_AndNeverIncludeAChairIndustryOrMarketHead()
    {
        var lee = Person(1, "Lee");
        var ada = Person(2, "Ada", assigned: true, assignment: "Earth HQ");
        var navy = Person(3, "Kim", CommanderKind.Navy);
        var scientist = Person(4, "Ravi", CommanderKind.Scientist);
        var later = Person(5, "Zara");
        var chair = Person(6, "Chair", assigned: false);
        var industry = Person(7, "Ivy", assigned: false);
        var otherMarket = Person(8, "Max", assigned: false);

        var picked = MarketSeatUi.Candidates(
            new[] { later, lee, ada, navy, scientist, chair, industry, otherMarket },
            blockedIds: MarketSeatUi.BlockedIds(new ColonyView?[]
            {
                Colony(administratorId: 6, industryHeadId: 7, marketHeadId: 8),
                Colony(administratorId: 2),
            }));

        Assert.That(picked.Select(p => p.Name), Is.EqualTo(new[] { "Lee", "Zara" }));
        Assert.That(picked.All(p => p.Kind == CommanderKind.Civilian), Is.True);
        Assert.That(picked.All(p => !p.IsAssigned), Is.True);
        Assert.That(picked.Select(p => p.Id), Does.Not.Contain(6).And.Not.Contain(7).And.Not.Contain(8).And.Not.Contain(2));
    }

    [Test]
    public void Candidates_FromANullRoster_AreEmpty()
    {
        Assert.That(MarketSeatUi.Candidates(null), Is.Empty);
        Assert.That(MarketSeatUi.BlockedIds(null), Is.Empty);
        Assert.That(MarketSeatUi.BlockedIds(Array.Empty<ColonyView?>()), Is.Empty);
    }

    [Test]
    public void BlockedIds_CollectFilledChairsAndHeads()
    {
        var ids = MarketSeatUi.BlockedIds(new ColonyView?[]
        {
            Colony(administratorId: 11),
            Colony(),
            null,
            Colony(industryHeadId: 33, marketHeadId: 44, marketHeadName: "Mara"),
        });
        Assert.That(ids, Is.EqualTo(new[] { 11, 33, 44 }));
    }

    [Test]
    public void Portrait_ComesFromTheRoster_AndIsEmptyWhenTheSeatIsVacant()
    {
        var mara = Person(44, "Mara", seed: 99, role: PortraitRoles.Civilian, species: "human");
        Assert.That(MarketSeatUi.Portrait(-1, new[] { mara }).IsEmpty, Is.True);
        Assert.That(MarketSeatUi.Portrait(44, null).IsEmpty, Is.True);
        Assert.That(MarketSeatUi.Portrait(99, new[] { mara }).IsEmpty, Is.True);

        var portrait = MarketSeatUi.Portrait(44, new[] { mara });
        Assert.That(portrait.IsEmpty, Is.False);
        Assert.That((portrait.Seed, portrait.Role, portrait.Species), Is.EqualTo((99, PortraitRoles.Civilian, "human")));
    }

    [Test]
    public void RefusalMessage_ShowsTheEngineReason_WhenTheCommandIsRejected()
    {
        Assert.That(MarketSeatUi.RefusalMessage(null), Is.Null);
        Assert.That(MarketSeatUi.RefusalMessage(CommandResult.Ok("ok")), Is.Null);
        Assert.That(
            MarketSeatUi.RefusalMessage(CommandResult.Reject("Lee is an industry head. One person holds one post: the city-hall chair, the industry seat or the market seat.")),
            Is.EqualTo("Lee is an industry head. One person holds one post: the city-hall chair, the industry seat or the market seat."));
        Assert.That(MarketSeatUi.RefusalMessage(new CommandResult(false)), Is.EqualTo("The command was refused."));
    }
}
