using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;

namespace Pulsar4X.Tests
{
    /// <summary>
    /// Selection and grouping for colonies and ships this faction does not own.
    /// Snapshots are built here. Nothing touches a game or a static id generator.
    /// </summary>
    public class ForeignEntitiesTests
    {
        static readonly FactionStanceRow[] Stances =
        {
            new FactionStanceRow(7, "Ceres Depot Authority", FactionStance.Friendly),
            new FactionStanceRow(8, "Luna Concord", FactionStance.Neutral),
            new FactionStanceRow(3, "Zeus Combine", FactionStance.Hostile),
            new FactionStanceRow(4, "Twin", FactionStance.Hostile),
            new FactionStanceRow(30, "Twin", FactionStance.Friendly),
            new FactionStanceRow(20, "Alpha Yard", FactionStance.Neutral),
            new FactionStanceRow(99, "No Colonies Here", FactionStance.Neutral),
        };

        [Test]
        public void FactionName_UsesTheStanceRow_OrUnknownFaction()
        {
            Assert.AreEqual("Ceres Depot Authority", ForeignEntities.FactionName(Stances, 7));
            Assert.AreEqual("Luna Concord", ForeignEntities.FactionName(Stances, 8));
            Assert.AreEqual("Unknown faction", ForeignEntities.FactionName(Stances, 12345));
            Assert.AreEqual("Unknown faction", ForeignEntities.FactionName(Array.Empty<FactionStanceRow>(), 7));
        }

        [Test]
        public void ColonyGroups_OwnedFirst_ThenForeignByName_SkippingNonColonies()
        {
            var entities = new[]
            {
                Planet(500, "Ceres"),
                Colony(100, OwnerRelation.Hostile, 3, 500, "Zeus Outpost"),
                Colony(2, OwnerRelation.Owned, 1, 4, "Mars Base"),
                Colony(31, OwnerRelation.Friendly, 30, 1, "Twin B"),
                Ship(600, OwnerRelation.Friendly, 7, "Hauler"),
                Colony(11, OwnerRelation.Friendly, 7, 500, "Ceres One"),
                Colony(10, OwnerRelation.Friendly, 7, 500, "Ceres Two"),
                Colony(40, OwnerRelation.Neutral, 20, 2, "Alpha Post"),
                Colony(32, OwnerRelation.Hostile, 4, 3, "Twin A"),
                Colony(1, OwnerRelation.Owned, 1, 9, "Earth"),
                Colony(80, OwnerRelation.Neutral, 8, 6, "Luna Concord"),
            };

            var groups = ForeignEntities.ColonyGroups(entities, Stances);
            Assert.AreEqual(7, groups.Count);

            Assert.IsNull(groups[0].FactionId);
            Assert.IsNull(groups[0].FactionName);
            Assert.AreEqual(2, groups[0].Colonies[0].Id, "owned colonies stay in snapshot order");
            Assert.AreEqual(1, groups[0].Colonies[1].Id);

            Assert.AreEqual(20, groups[1].FactionId);
            Assert.AreEqual("Alpha Yard", groups[1].FactionName);
            Assert.AreEqual(40, groups[1].Colonies[0].Id);

            Assert.AreEqual(7, groups[2].FactionId);
            Assert.AreEqual("Ceres Depot Authority", groups[2].FactionName);
            Assert.AreEqual(11, groups[2].Colonies[0].Id, "colonies in a faction stay in snapshot order");
            Assert.AreEqual(10, groups[2].Colonies[1].Id);
            Assert.AreEqual(OwnerRelation.Friendly, groups[2].Colonies[0].Relation);

            Assert.AreEqual(8, groups[3].FactionId);
            Assert.AreEqual("Luna Concord", groups[3].FactionName);
            Assert.AreEqual(OwnerRelation.Neutral, groups[3].Colonies[0].Relation);

            Assert.AreEqual(4, groups[4].FactionId, "equal faction names break ties by FactionId");
            Assert.AreEqual("Twin", groups[4].FactionName);
            Assert.AreEqual(32, groups[4].Colonies[0].Id);
            Assert.AreEqual(30, groups[5].FactionId);
            Assert.AreEqual("Twin", groups[5].FactionName);
            Assert.AreEqual(31, groups[5].Colonies[0].Id);

            Assert.AreEqual(3, groups[6].FactionId);
            Assert.AreEqual("Zeus Combine", groups[6].FactionName);
            Assert.AreEqual(OwnerRelation.Hostile, groups[6].Colonies[0].Relation);

            foreach (var group in groups)
                Assert.IsFalse(group.FactionName == "No Colonies Here");

            var onlyForeign = ForeignEntities.ColonyGroups(new[]
            {
                Colony(100, OwnerRelation.Hostile, 3, 500, "Zeus Outpost"),
                Colony(11, OwnerRelation.Friendly, 7, 500, "Ceres One"),
                Colony(40, OwnerRelation.Neutral, 20, 2, "Alpha Post"),
            }, Stances);
            Assert.AreEqual(3, onlyForeign.Count);
            Assert.IsNotNull(onlyForeign[0].FactionId);
            Assert.AreEqual("Alpha Yard", onlyForeign[0].FactionName);
            Assert.AreEqual("Ceres Depot Authority", onlyForeign[1].FactionName);
            Assert.AreEqual("Zeus Combine", onlyForeign[2].FactionName);

            var none = ForeignEntities.ColonyGroups(new[]
            {
                Planet(500, "Ceres"),
                Ship(600, OwnerRelation.Friendly, 7, "Hauler"),
            }, Stances);
            Assert.AreEqual(0, none.Count);
        }

        [Test]
        public void ResolveColonySelection_KeepsAResolvedColony_OtherwiseFirstOwned()
        {
            var earth = Colony(1, OwnerRelation.Owned, 1, 9, "Earth");
            var ceres = Colony(5, OwnerRelation.Friendly, 7, 8, "Ceres Depot");
            var alphaBase = Colony(2, OwnerRelation.Owned, 1, 4, "Alpha Base");
            var camp = Colony(9, OwnerRelation.Hostile, 3, 3, "Raid Camp");
            var systems = new (string SystemId, IEnumerable<EntitySnapshot>? Entities)[]
            {
                NoEntities("gap"),
                Sys("outer", camp),
                Sys("alpha", alphaBase),
                Sys("sol", earth, ceres),
            };

            ExpectColony(ForeignEntities.ResolveColonySelection(5, "sol", systems), 5, "sol",
                "a resolved foreign colony stays selected");
            ExpectColony(ForeignEntities.ResolveColonySelection(1, "sol", systems), 1, "sol",
                "an owned colony that is not the first one stays selected");
            ExpectColony(ForeignEntities.ResolveColonySelection(null, null, systems), 2, "alpha",
                "no selection opens the first owned colony");
            ExpectColony(ForeignEntities.ResolveColonySelection(404, "sol", systems), 2, "alpha");
            ExpectColony(ForeignEntities.ResolveColonySelection(5, "missing", systems), 2, "alpha");

            var onlyForeign = new (string SystemId, IEnumerable<EntitySnapshot>? Entities)[]
            {
                NoEntities("gap"),
                Sys("sol", ceres),
                Sys("outer", camp),
            };
            Assert.IsFalse(ForeignEntities.ResolveColonySelection(404, "sol", onlyForeign).HasValue);
            Assert.IsFalse(ForeignEntities.ResolveColonySelection(null, null, onlyForeign).HasValue);

            var sameIdElsewhere = new (string SystemId, IEnumerable<EntitySnapshot>? Entities)[]
            {
                NoEntities("gap"),
                Sys("sol", Planet(5, "Ceres"), earth),
                Sys("alpha", Colony(5, OwnerRelation.Friendly, 7, 5, "Same Id")),
            };
            ExpectColony(ForeignEntities.ResolveColonySelection(5, "sol", sameIdElsewhere), 1, "sol",
                "a stored id that is not a colony in its system falls back");
        }

        [Test]
        public void ColonyTabsFor_OwnedManagementTabs_ForeignSummaryMarketShipping()
        {
            const ForeignEntities.ColonyTabs ownedBase =
                ForeignEntities.ColonyTabs.Summary
                | ForeignEntities.ColonyTabs.Production
                | ForeignEntities.ColonyTabs.Construction
                | ForeignEntities.ColonyTabs.Market
                | ForeignEntities.ColonyTabs.Shipping
                | ForeignEntities.ColonyTabs.Log;
            const ForeignEntities.ColonyTabs foreignTabs =
                ForeignEntities.ColonyTabs.Summary
                | ForeignEntities.ColonyTabs.Market
                | ForeignEntities.ColonyTabs.Shipping;

            Assert.AreEqual(ownedBase, ForeignEntities.ColonyTabsFor(Colony(1, OwnerRelation.Owned, 1, 9, "Earth")));

            var ownedFull = With(
                Colony(1, OwnerRelation.Owned, 1, 9, "Earth"),
                new ColonyMiningView(2),
                new NavalAcademyView(Array.Empty<NavalAcademyClassView>(), 1000, 10000),
                new RecruitmentView(Array.Empty<CandidateView>(), null, Array.Empty<CandidateView>()));
            Assert.AreEqual(
                ownedBase
                | ForeignEntities.ColonyTabs.Mining
                | ForeignEntities.ColonyTabs.NavalAcademy
                | ForeignEntities.ColonyTabs.Recruitment,
                ForeignEntities.ColonyTabsFor(ownedFull));

            var foreignFull = With(
                Colony(5, OwnerRelation.Friendly, 7, 8, "Ceres Depot"),
                new ColonyMiningView(2),
                new NavalAcademyView(Array.Empty<NavalAcademyClassView>(), 1000, 10000),
                new RecruitmentView(Array.Empty<CandidateView>(), null, Array.Empty<CandidateView>()),
                new IndustryView(Array.Empty<ProductionLineView>()),
                new ConstructionView(0));
            Assert.AreEqual(foreignTabs, ForeignEntities.ColonyTabsFor(foreignFull));
            Assert.IsFalse(ForeignEntities.ColonyTabsFor(foreignFull).HasFlag(ForeignEntities.ColonyTabs.Production));
            Assert.IsFalse(ForeignEntities.ColonyTabsFor(foreignFull).HasFlag(ForeignEntities.ColonyTabs.Construction));
            Assert.IsFalse(ForeignEntities.ColonyTabsFor(foreignFull).HasFlag(ForeignEntities.ColonyTabs.Mining));
            Assert.IsFalse(ForeignEntities.ColonyTabsFor(foreignFull).HasFlag(ForeignEntities.ColonyTabs.NavalAcademy));
            Assert.IsFalse(ForeignEntities.ColonyTabsFor(foreignFull).HasFlag(ForeignEntities.ColonyTabs.Recruitment));
            Assert.IsFalse(ForeignEntities.ColonyTabsFor(foreignFull).HasFlag(ForeignEntities.ColonyTabs.Log));

            Assert.AreEqual(foreignTabs, ForeignEntities.ColonyTabsFor(Colony(8, OwnerRelation.Neutral, 8, 6, "Luna Concord")));
            Assert.AreEqual(foreignTabs, ForeignEntities.ColonyTabsFor(Colony(9, OwnerRelation.Hostile, 3, 3, "Raid Camp")));
        }

        [Test]
        public void IsReadOnly_IsTrueWhenTheFactionDoesNotOwnIt()
        {
            Assert.IsFalse(ForeignEntities.IsReadOnly(Colony(1, OwnerRelation.Owned, 1, 9, "Earth")));
            Assert.IsTrue(ForeignEntities.IsReadOnly(Colony(5, OwnerRelation.Friendly, 7, 8, "Ceres Depot")));
            Assert.IsTrue(ForeignEntities.IsReadOnly(Ship(6, OwnerRelation.Neutral, 8, "Luna Runner")));
            Assert.IsTrue(ForeignEntities.IsReadOnly(Ship(7, OwnerRelation.Hostile, 3, "Raider")));
        }

        [Test]
        public void ForeignColoniesOn_ListsNonOwnedColoniesOnThatBody()
        {
            var entities = new[]
            {
                Ship(600, OwnerRelation.Friendly, 7, "Hauler"),
                Colony(2, OwnerRelation.Friendly, 7, 10, "Ceres Depot"),
                Colony(1, OwnerRelation.Owned, 1, 10, "Earth"),
                Colony(3, OwnerRelation.Neutral, 8, 11, "Luna Concord"),
                Planet(10, "Ceres"),
                Colony(4, OwnerRelation.Hostile, 3, 10, "Outpost"),
            };

            var onCeres = ForeignEntities.ForeignColoniesOn(entities, 10);
            Assert.AreEqual(2, onCeres.Count);
            Assert.AreEqual(2, onCeres[0].Id);
            Assert.AreEqual(4, onCeres[1].Id);

            var onLuna = ForeignEntities.ForeignColoniesOn(entities, 11);
            Assert.AreEqual(1, onLuna.Count);
            Assert.AreEqual(3, onLuna[0].Id);
            Assert.AreEqual(0, ForeignEntities.ForeignColoniesOn(entities, 99).Count);
        }

        [Test]
        public void ResolveForeignShip_FindsAVisibleForeignShip_OrReturnsNull()
        {
            var inAlpha = Ship(42, OwnerRelation.Neutral, 8, "Luna Runner");
            var inSol = Ship(42, OwnerRelation.Friendly, 7, "Ceres Hauler");
            var raider = Ship(99, OwnerRelation.Hostile, 3, "Raider");
            var owned = Ship(7, OwnerRelation.Owned, 1, "Surveyor");
            var rock = Planet(8, "Pallas");
            var systems = new (string SystemId, IEnumerable<EntitySnapshot>? Entities)[]
            {
                NoEntities("gap"),
                Sys("alpha", inAlpha, raider),
                Sys("sol", rock, inSol, owned),
            };

            ExpectShip(ForeignEntities.ResolveForeignShip(42, "sol", systems), 42, "sol", "Ceres Hauler");
            ExpectShip(ForeignEntities.ResolveForeignShip(99, "sol", systems), 99, "alpha", "Raider");
            ExpectShip(ForeignEntities.ResolveForeignShip(42, null, systems), 42, "alpha", "Luna Runner",
                "with no hint, the first system in order wins");
            ExpectShip(ForeignEntities.ResolveForeignShip(42, "missing", systems), 42, "alpha", "Luna Runner");

            Assert.IsFalse(ForeignEntities.ResolveForeignShip(7, "sol", systems).HasValue, "an owned ship is not inspected");
            Assert.IsFalse(ForeignEntities.ResolveForeignShip(8, "sol", systems).HasValue, "a non-ship is not a ship");
            Assert.IsFalse(ForeignEntities.ResolveForeignShip(1000, null, systems).HasValue);
        }

        static EntitySnapshot Colony(int id, OwnerRelation rel, int faction, int? planet, string name) => new EntitySnapshot
        {
            Id = id,
            FactionId = faction,
            Relation = rel,
            Kind = BodyKind.Colony,
            Views = new IComponentView[] { new NameView(name), new ColonyView(1000, planet) },
        };

        static EntitySnapshot Ship(int id, OwnerRelation rel, int faction, string name) => new EntitySnapshot
        {
            Id = id,
            FactionId = faction,
            Relation = rel,
            Kind = BodyKind.Ship,
            Views = new IComponentView[] { new NameView(name), new ShipView(name) },
        };

        static EntitySnapshot Planet(int id, string name) => new EntitySnapshot
        {
            Id = id,
            Kind = BodyKind.Planet,
            Views = new IComponentView[] { new NameView(name) },
        };

        static EntitySnapshot With(EntitySnapshot entity, params IComponentView[] extra)
        {
            var views = new IComponentView[entity.Views.Count + extra.Length];
            for (int i = 0; i < entity.Views.Count; i++)
                views[i] = entity.Views[i];
            for (int i = 0; i < extra.Length; i++)
                views[entity.Views.Count + i] = extra[i];
            return new EntitySnapshot
            {
                Id = entity.Id,
                FactionId = entity.FactionId,
                Relation = entity.Relation,
                Kind = entity.Kind,
                Views = views,
            };
        }

        static void ExpectColony((int ColonyId, string SystemId)? actual, int id, string systemId, string message = "")
        {
            if (actual is not { } value)
            {
                Assert.Fail(message.Length == 0 ? "expected a colony" : message);
                return;
            }
            Assert.AreEqual(id, value.ColonyId, message);
            Assert.AreEqual(systemId, value.SystemId, message);
        }

        static void ExpectShip(
            (EntitySnapshot Ship, string SystemId)? actual, int id, string systemId, string name, string message = "")
        {
            if (actual is not { } value)
            {
                Assert.Fail(message.Length == 0 ? "expected a ship" : message);
                return;
            }
            Assert.AreEqual(id, value.Ship.Id, message);
            Assert.AreEqual(systemId, value.SystemId, message);
            Assert.AreEqual(name, value.Ship.GetView<NameView>()?.Name, message);
        }

        static (string SystemId, IEnumerable<EntitySnapshot>? Entities) Sys(string systemId, params EntitySnapshot[] entities)
            => (systemId, entities);

        static (string SystemId, IEnumerable<EntitySnapshot>? Entities) NoEntities(string systemId)
            => (systemId, null);
    }
}
