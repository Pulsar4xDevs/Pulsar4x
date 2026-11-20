using System;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;

namespace Pulsar4X.Tests;

/// <summary>Maps engine portrait fields and the empty-post rule. No ImGui and no game.</summary>
[TestFixture]
public class PortraitIdentityTests
{
    static CommanderSnapshot Person(int id, CommanderKind kind, int seed, string role)
        => new(id, "Lee", kind, false, 0, 0, default)
        {
            PortraitSeed = seed,
            PortraitRole = role,
        };

    static CandidateView Candidate(int id, int seed, string role)
        => new(id, "Ada", 30, 1, "Earth", 100, "Physics", 2)
        {
            PortraitSeed = seed,
            PortraitRole = role,
        };

    [Test]
    public void OfAPerson_UsesTheEngineSeedAndRole_NotIdOrKind()
    {
        var seated = Person(id: 9, CommanderKind.Civilian, seed: 404, PortraitRoles.IndustryHead);
        var mapped = PortraitIdentity.Of(seated);
        Assert.That(mapped.Seed, Is.EqualTo(404));
        Assert.That(mapped.Seed, Is.Not.EqualTo(seated.Id));
        Assert.That(mapped.Role, Is.EqualTo(PortraitRoles.IndustryHead));
        Assert.That(mapped.IsEmpty, Is.False);

        var navy = Person(id: 2, CommanderKind.Navy, seed: 88, PortraitRoles.Captain);
        Assert.That(PortraitIdentity.Of(navy).Role, Is.EqualTo(PortraitRoles.Captain));

        var tech = Person(id: 3, CommanderKind.Scientist, seed: 12, PortraitRoles.LabTechnician);
        Assert.That(PortraitIdentity.Of(tech).Role, Is.EqualTo(PortraitRoles.LabTechnician),
            "a lab technician keeps the engine role; Kind is not used");
    }

    [Test]
    public void OfACandidate_UsesTheCandidateFields()
    {
        var candidate = Candidate(id: 15, seed: 77, PortraitRoles.Candidate);
        var mapped = PortraitIdentity.Of(candidate);
        Assert.That(mapped.Seed, Is.EqualTo(77));
        Assert.That(mapped.Role, Is.EqualTo(PortraitRoles.Candidate));
        Assert.That(mapped.IsEmpty, Is.False);
    }

    [Test]
    public void EmptyPost_WhenTheSeedIsZero_OrTheRoleIsBlank()
    {
        Assert.That(PortraitIdentity.IsEmpty(0, "captain"), Is.True);
        Assert.That(PortraitIdentity.IsEmpty(4, ""), Is.True);
        Assert.That(PortraitIdentity.IsEmpty(4, null), Is.True);
        Assert.That(PortraitIdentity.IsEmpty(4, "captain"), Is.False);
        Assert.That(new PortraitRef(0, "administrator").IsEmpty, Is.True);
        Assert.That(new PortraitRef(9, "").IsEmpty, Is.True);
        Assert.That(new PortraitRef(9, "civilian").IsEmpty, Is.False);

        var unseated = Person(id: 9, CommanderKind.Civilian, seed: 0, "");
        Assert.That(PortraitIdentity.Of(unseated).IsEmpty, Is.True, "a missing person portrait is treated as an empty post");
    }

    [Test]
    public void SeatAndChairViews_MapThePostFields_AndAreEmptyWhenTheEngineSaysSo()
    {
        var emptyColony = new ColonyView(0, null);
        Assert.That(PortraitIdentity.Administrator(emptyColony).IsEmpty, Is.True);
        Assert.That(PortraitIdentity.IndustryHead(emptyColony).IsEmpty, Is.True);
        Assert.That(PortraitIdentity.Administrator(emptyColony), Is.EqualTo(new PortraitRef(0, "")));

        var colony = new ColonyView(1, 2)
        {
            AdministratorPortraitSeed = 11,
            AdministratorPortraitRole = PortraitRoles.Administrator,
            IndustryHeadPortraitSeed = 22,
            IndustryHeadPortraitRole = PortraitRoles.IndustryHead,
        };
        Assert.That(PortraitIdentity.Administrator(colony), Is.EqualTo(new PortraitRef(11, PortraitRoles.Administrator)));
        Assert.That(PortraitIdentity.IndustryHead(colony), Is.EqualTo(new PortraitRef(22, PortraitRoles.IndustryHead)));

        var industry = new IndustryView(Array.Empty<ProductionLineView>())
        {
            IndustryHeadPortraitSeed = 22,
            IndustryHeadPortraitRole = PortraitRoles.IndustryHead,
        };
        Assert.That(PortraitIdentity.IndustryHead(industry), Is.EqualTo(new PortraitRef(22, PortraitRoles.IndustryHead)));
        Assert.That(PortraitIdentity.IndustryHead(new IndustryView(Array.Empty<ProductionLineView>())).IsEmpty, Is.True);

        var shipView = new ShipView("Hull") { CommanderPortraitSeed = 33, CommanderPortraitRole = PortraitRoles.Captain };
        Assert.That(PortraitIdentity.Commander(shipView), Is.EqualTo(new PortraitRef(33, PortraitRoles.Captain)));
        Assert.That(PortraitIdentity.Commander(new ShipView("Hull")).IsEmpty, Is.True);

        var ship = new ShipSnapshot(1, "Flag", "sol") { CommanderPortraitSeed = 33, CommanderPortraitRole = PortraitRoles.Captain };
        Assert.That(PortraitIdentity.Commander(ship), Is.EqualTo(new PortraitRef(33, PortraitRoles.Captain)));

        var fleet = new FleetSnapshot { CommanderPortraitSeed = 33, CommanderPortraitRole = PortraitRoles.Captain };
        Assert.That(PortraitIdentity.Commander(fleet), Is.EqualTo(new PortraitRef(33, PortraitRoles.Captain)));
        Assert.That(PortraitIdentity.Commander(new FleetSnapshot()).IsEmpty, Is.True);

        var vacant = new ModifiedValue(0, 0, Array.Empty<ValueModifier>());
        var lab = new ResearcherView("Lab", "Lab", "", 1, "Earth", null, null, vacant, vacant, 0, Array.Empty<string>())
        {
            ScientistPortraitSeed = 44,
            ScientistPortraitRole = PortraitRoles.Scientist,
        };
        Assert.That(PortraitIdentity.Scientist(lab), Is.EqualTo(new PortraitRef(44, PortraitRoles.Scientist)));
        Assert.That(PortraitIdentity.Scientist(new ResearcherView("Lab", "Lab", "", 1, "Earth", null, null, vacant, vacant, 0, Array.Empty<string>())).IsEmpty, Is.True);
    }

    [Test]
    public void SpeciesFromView_DefaultsToHuman_UntilEngineFieldsLand()
    {
        Assert.That(PortraitIdentity.SpeciesFromView(), Is.EqualTo("human"));
        Assert.That(PortraitIdentity.ResolveSpecies(null), Is.EqualTo("human"));
        Assert.That(PortraitIdentity.ResolveSpecies(""), Is.EqualTo("human"));
        Assert.That(PortraitIdentity.ResolveSpecies("  "), Is.EqualTo("human"));
        Assert.That(PortraitIdentity.ResolveSpecies("zephyrian"), Is.EqualTo("zephyrian"));

        var person = new CommanderSnapshot(9, "Lee", CommanderKind.Civilian, false, 0, 0, default)
        {
            PortraitSeed = 404,
            PortraitRole = PortraitRoles.IndustryHead,
        };
        Assert.That(PortraitIdentity.Of(person).Species, Is.EqualTo("human"));
        Assert.That(PortraitIdentity.Fallback(3, CommanderKind.Navy).Species, Is.EqualTo("human"));
        Assert.That(new PortraitRef(4, "captain").Species, Is.EqualTo("human"));
    }

    [Test]
    public void Fallback_IsOnlyTheIdAndKind_ForAViewWithNoPortraitField()
    {
        Assert.That(PortraitIdentity.Fallback(9, CommanderKind.Navy), Is.EqualTo(new PortraitRef(9, PortraitRoles.Captain)));
        Assert.That(PortraitIdentity.Fallback(8, CommanderKind.Ground), Is.EqualTo(new PortraitRef(8, PortraitRoles.Captain)));
        Assert.That(PortraitIdentity.Fallback(7, CommanderKind.Scientist), Is.EqualTo(new PortraitRef(7, PortraitRoles.Scientist)));
        Assert.That(PortraitIdentity.Fallback(6, CommanderKind.Civilian), Is.EqualTo(new PortraitRef(6, PortraitRoles.Civilian)));
    }
}
