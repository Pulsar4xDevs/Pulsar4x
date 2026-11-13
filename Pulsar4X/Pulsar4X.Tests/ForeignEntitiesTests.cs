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
        public void OwnedBySystem_OwnedColoniesByKnownSystemsOrder_SkippingForeignAndNonColonies()
        {
            var mars = Colony(2, OwnerRelation.Owned, 1, 4, "Mars Base");
            var earth = Colony(1, OwnerRelation.Owned, 1, 9, "Earth");
            var ceres = Colony(11, OwnerRelation.Friendly, 7, 500, "Ceres One");
            var camp = Colony(100, OwnerRelation.Hostile, 3, 500, "Zeus Outpost");
            var systems = new (string SystemId, string SystemName, IEnumerable<EntitySnapshot>? Entities)[]
            {
                NamedNoEntities("gap", "Gap"),
                NamedSys("outer", "Outer", Planet(500, "Ceres"), camp, Ship(600, OwnerRelation.Friendly, 7, "Hauler")),
                NamedSys("alpha", "Alpha", mars, Planet(9, "Mars")),
                NamedSys("sol", "Sol", ceres, earth, Ship(601, OwnerRelation.Owned, 1, "Surveyor")),
            };

            var owned = ForeignEntities.OwnedBySystem(systems);
            Assert.AreEqual(2, owned.Count, "only systems with at least one owned colony");
            Assert.AreEqual("alpha", owned[0].SystemId, "KnownSystems order, not name order");
            Assert.AreEqual("Alpha", owned[0].SystemName);
            Assert.AreEqual(1, owned[0].Colonies.Count);
            Assert.AreEqual(2, owned[0].Colonies[0].Id);

            Assert.AreEqual("sol", owned[1].SystemId);
            Assert.AreEqual("Sol", owned[1].SystemName);
            Assert.AreEqual(1, owned[1].Colonies.Count, "foreign colonies and non-colonies are omitted");
            Assert.AreEqual(1, owned[1].Colonies[0].Id);

            var twoOwned = ForeignEntities.OwnedBySystem(new[]
            {
                NamedSys("sol", "Sol", mars, Planet(4, "Mars"), earth),
            });
            Assert.AreEqual(2, twoOwned[0].Colonies.Count, "owned colonies stay in snapshot order");
            Assert.AreEqual(2, twoOwned[0].Colonies[0].Id);
            Assert.AreEqual(1, twoOwned[0].Colonies[1].Id);

            var onlyForeign = ForeignEntities.OwnedBySystem(new[]
            {
                NamedSys("sol", "Sol", ceres, camp, Planet(500, "Ceres")),
            });
            Assert.AreEqual(0, onlyForeign.Count, "a system with only foreign colonies is hidden from Your colonies");

            var none = ForeignEntities.OwnedBySystem(new[]
            {
                NamedNoEntities("gap", "Gap"),
                NamedSys("empty", "Empty", Planet(500, "Ceres"), Ship(600, OwnerRelation.Friendly, 7, "Hauler")),
            });
            Assert.AreEqual(0, none.Count);
            Assert.AreEqual(0, ForeignEntities.OwnedBySystem(Array.Empty<(string, string, IEnumerable<EntitySnapshot>?)>()).Count);
        }

        [Test]
        public void ForeignByFaction_NameThenId_SingleSystemHasNoSystemNode_MultiSystemDoes()
        {
            var entitiesSol = new[]
            {
                Planet(500, "Ceres"),
                Colony(2, OwnerRelation.Owned, 1, 4, "Mars Base"),
                Colony(31, OwnerRelation.Friendly, 30, 1, "Twin B"),
                Ship(600, OwnerRelation.Friendly, 7, "Hauler"),
                Colony(11, OwnerRelation.Friendly, 7, 500, "Ceres One"),
                Colony(10, OwnerRelation.Friendly, 7, 500, "Ceres Two"),
                Colony(32, OwnerRelation.Hostile, 4, 3, "Twin A"),
                Colony(1, OwnerRelation.Owned, 1, 9, "Earth"),
                Colony(80, OwnerRelation.Neutral, 8, 6, "Luna Concord"),
            };
            var entitiesAlpha = new[]
            {
                Colony(40, OwnerRelation.Neutral, 20, 2, "Alpha Post"),
                Colony(81, OwnerRelation.Neutral, 8, 6, "Luna Outpost"),
            };
            var systems = new (string SystemId, string SystemName, IEnumerable<EntitySnapshot>? Entities)[]
            {
                NamedNoEntities("gap", "Gap"),
                NamedSys("outer", "Outer", Colony(100, OwnerRelation.Hostile, 3, 500, "Zeus Outpost")),
                NamedSys("alpha", "Alpha", entitiesAlpha),
                NamedSys("sol", "Sol", entitiesSol),
                NamedSys("empty", "Empty", Planet(9, "Rock"), Ship(7, OwnerRelation.Owned, 1, "Surveyor")),
            };

            var foreign = ForeignEntities.ForeignByFaction(systems, Stances);
            Assert.AreEqual(6, foreign.Count);

            Assert.AreEqual(20, foreign[0].FactionId);
            Assert.AreEqual("Alpha Yard", foreign[0].FactionName);
            Assert.AreEqual(1, foreign[0].Systems.Count, "a faction in only one system has no system node");
            Assert.AreEqual("alpha", foreign[0].Systems[0].SystemId);
            Assert.AreEqual("Alpha", foreign[0].Systems[0].SystemName);
            Assert.AreEqual(40, foreign[0].Systems[0].Colonies[0].Id);

            Assert.AreEqual(7, foreign[1].FactionId);
            Assert.AreEqual("Ceres Depot Authority", foreign[1].FactionName);
            Assert.AreEqual(1, foreign[1].Systems.Count);
            Assert.AreEqual("sol", foreign[1].Systems[0].SystemId);
            Assert.AreEqual(2, foreign[1].Systems[0].Colonies.Count, "colonies in a system stay in snapshot order");
            Assert.AreEqual(11, foreign[1].Systems[0].Colonies[0].Id);
            Assert.AreEqual(10, foreign[1].Systems[0].Colonies[1].Id);
            Assert.AreEqual(OwnerRelation.Friendly, foreign[1].Systems[0].Colonies[0].Relation);

            Assert.AreEqual(8, foreign[2].FactionId);
            Assert.AreEqual("Luna Concord", foreign[2].FactionName);
            Assert.AreEqual(2, foreign[2].Systems.Count, "a faction in more than one system gets a system node each");
            Assert.AreEqual("alpha", foreign[2].Systems[0].SystemId, "system nodes follow KnownSystems order");
            Assert.AreEqual("Alpha", foreign[2].Systems[0].SystemName);
            Assert.AreEqual(81, foreign[2].Systems[0].Colonies[0].Id);
            Assert.AreEqual("sol", foreign[2].Systems[1].SystemId);
            Assert.AreEqual("Sol", foreign[2].Systems[1].SystemName);
            Assert.AreEqual(80, foreign[2].Systems[1].Colonies[0].Id);
            Assert.AreEqual(OwnerRelation.Neutral, foreign[2].Systems[1].Colonies[0].Relation);

            Assert.AreEqual(4, foreign[3].FactionId, "equal faction names break ties by FactionId");
            Assert.AreEqual("Twin", foreign[3].FactionName);
            Assert.AreEqual(1, foreign[3].Systems.Count);
            Assert.AreEqual(32, foreign[3].Systems[0].Colonies[0].Id);
            Assert.AreEqual(30, foreign[4].FactionId);
            Assert.AreEqual("Twin", foreign[4].FactionName);
            Assert.AreEqual(31, foreign[4].Systems[0].Colonies[0].Id);

            Assert.AreEqual(3, foreign[5].FactionId);
            Assert.AreEqual("Zeus Combine", foreign[5].FactionName);
            Assert.AreEqual(1, foreign[5].Systems.Count);
            Assert.AreEqual("outer", foreign[5].Systems[0].SystemId);
            Assert.AreEqual(OwnerRelation.Hostile, foreign[5].Systems[0].Colonies[0].Relation);

            foreach (var group in foreign)
                Assert.IsFalse(group.FactionName == "No Colonies Here");

            var onlyForeign = ForeignEntities.ForeignByFaction(new[]
            {
                NamedSys("sol", "Sol",
                    Colony(100, OwnerRelation.Hostile, 3, 500, "Zeus Outpost"),
                    Colony(11, OwnerRelation.Friendly, 7, 500, "Ceres One"),
                    Colony(40, OwnerRelation.Neutral, 20, 2, "Alpha Post")),
            }, Stances);
            Assert.AreEqual(3, onlyForeign.Count, "a galaxy with only foreign colonies still fills Other factions");
            Assert.AreEqual("Alpha Yard", onlyForeign[0].FactionName);
            Assert.AreEqual("Ceres Depot Authority", onlyForeign[1].FactionName);
            Assert.AreEqual("Zeus Combine", onlyForeign[2].FactionName);
            Assert.AreEqual(1, onlyForeign[0].Systems.Count);
            Assert.AreEqual(1, onlyForeign[1].Systems.Count);
            Assert.AreEqual(1, onlyForeign[2].Systems.Count);

            var none = ForeignEntities.ForeignByFaction(new[]
            {
                NamedNoEntities("gap", "Gap"),
                NamedSys("empty", "Empty", Planet(500, "Ceres"), Ship(600, OwnerRelation.Friendly, 7, "Hauler"),
                    Colony(1, OwnerRelation.Owned, 1, 9, "Earth")),
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
                new RecruitmentView(Array.Empty<CandidateView>(), null, Array.Empty<CandidateView>()),
                EmptyPower());
            Assert.AreEqual(
                ownedBase
                | ForeignEntities.ColonyTabs.Mining
                | ForeignEntities.ColonyTabs.NavalAcademy
                | ForeignEntities.ColonyTabs.Recruitment
                | ForeignEntities.ColonyTabs.Power,
                ForeignEntities.ColonyTabsFor(ownedFull));

            var foreignFull = With(
                Colony(5, OwnerRelation.Friendly, 7, 8, "Ceres Depot"),
                new ColonyMiningView(2),
                new NavalAcademyView(Array.Empty<NavalAcademyClassView>(), 1000, 10000),
                new RecruitmentView(Array.Empty<CandidateView>(), null, Array.Empty<CandidateView>()),
                new IndustryView(Array.Empty<ProductionLineView>()),
                new ConstructionView(0),
                EmptyPower());
            Assert.AreEqual(foreignTabs, ForeignEntities.ColonyTabsFor(foreignFull));
            Assert.IsFalse(ForeignEntities.ColonyTabsFor(foreignFull).HasFlag(ForeignEntities.ColonyTabs.Production));
            Assert.IsFalse(ForeignEntities.ColonyTabsFor(foreignFull).HasFlag(ForeignEntities.ColonyTabs.Construction));
            Assert.IsFalse(ForeignEntities.ColonyTabsFor(foreignFull).HasFlag(ForeignEntities.ColonyTabs.Mining));
            Assert.IsFalse(ForeignEntities.ColonyTabsFor(foreignFull).HasFlag(ForeignEntities.ColonyTabs.NavalAcademy));
            Assert.IsFalse(ForeignEntities.ColonyTabsFor(foreignFull).HasFlag(ForeignEntities.ColonyTabs.Recruitment));
            Assert.IsFalse(ForeignEntities.ColonyTabsFor(foreignFull).HasFlag(ForeignEntities.ColonyTabs.Log));
            Assert.IsFalse(ForeignEntities.ColonyTabsFor(foreignFull).HasFlag(ForeignEntities.ColonyTabs.Power));

            Assert.AreEqual(foreignTabs, ForeignEntities.ColonyTabsFor(Colony(8, OwnerRelation.Neutral, 8, 6, "Luna Concord")));
            Assert.AreEqual(foreignTabs, ForeignEntities.ColonyTabsFor(Colony(9, OwnerRelation.Hostile, 3, 3, "Raid Camp")));
        }

        static ColonyPowerView EmptyPower() => new ColonyPowerView("electricity", 0, 0, 0, 0.92, 0, 0, 0,
            Array.Empty<ColonyPowerConsumerRow>(), Array.Empty<ColonyPowerPlantRow>(),
            Array.Empty<ColonyPowerTierRow>(), Array.Empty<ColonyPowerFuelRow>());

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

        static (string SystemId, string SystemName, IEnumerable<EntitySnapshot>? Entities) NamedSys(
            string systemId, string name, params EntitySnapshot[] entities)
            => (systemId, name, entities);

        static (string SystemId, string SystemName, IEnumerable<EntitySnapshot>? Entities) NamedNoEntities(string systemId, string name)
            => (systemId, name, null);
    }
}
