using System;
using System.Collections.Generic;
using Pulsar4X.Api;

namespace Pulsar4X.Client
{
    /// <summary>
    /// Grouping and selection for colonies and ships this faction does not own.
    /// No drawing and no commands.
    /// </summary>
    public static class ForeignEntities
    {
        /// <summary>
        /// Owner of a non-owned entity, for display. Falls back to "Unknown faction" when the stance list has no row.
        /// </summary>
        public static string FactionName(IReadOnlyList<FactionStanceRow> stances, int factionId)
        {
            if (stances != null)
            {
                foreach (var row in stances)
                {
                    if (row != null && row.FactionId == factionId)
                        return row.Name;
                }
            }
            return "Unknown faction";
        }

        /// <summary>Colonies in one system, in snapshot order.</summary>
        public sealed record SystemColonies(string SystemId, string SystemName, IReadOnlyList<EntitySnapshot> Colonies);

        /// <summary>
        /// One other faction's colonies across known systems. When Systems has one entry, the leaves sit directly under
        /// the faction; when it has more than one, the UI nests a system node per entry (KnownSystems order).
        /// </summary>
        public sealed record ForeignFactionGroup(int FactionId, string FactionName, IReadOnlyList<SystemColonies> Systems);

        /// <summary>
        /// Systems that have at least one OwnerRelation.Owned colony, in the given (KnownSystems) order. Only
        /// Kind == BodyKind.Colony. Owned colonies keep snapshot order. Systems with none, and foreign colonies, are omitted.
        /// </summary>
        public static IReadOnlyList<SystemColonies> OwnedBySystem(
            IEnumerable<(string SystemId, string SystemName, IEnumerable<EntitySnapshot>? Entities)> systems)
        {
            var result = new List<SystemColonies>();
            if (systems == null)
                return result;

            foreach (var system in systems)
            {
                var owned = new List<EntitySnapshot>();
                if (system.Entities != null)
                {
                    foreach (var entity in system.Entities)
                    {
                        if (entity == null || entity.Kind != BodyKind.Colony)
                            continue;
                        if (entity.Relation == OwnerRelation.Owned)
                            owned.Add(entity);
                    }
                }
                if (owned.Count > 0)
                    result.Add(new SystemColonies(system.SystemId, system.SystemName, owned));
            }
            return result;
        }

        /// <summary>
        /// Visible non-owned colonies (Kind == Colony, Relation != Owned) grouped by faction, ordered by FactionName
        /// (StringComparer.Ordinal) then FactionId. Each faction's Systems are the known systems that actually have
        /// that faction's colonies, in the given (KnownSystems) order; colonies keep snapshot order inside a system.
        /// Factions with no colonies are not returned.
        /// </summary>
        public static IReadOnlyList<ForeignFactionGroup> ForeignByFaction(
            IEnumerable<(string SystemId, string SystemName, IEnumerable<EntitySnapshot>? Entities)> systems,
            IReadOnlyList<FactionStanceRow> stances)
        {
            var result = new List<ForeignFactionGroup>();
            if (systems == null)
                return result;

            var systemOrder = new List<(string SystemId, string SystemName)>();
            var byFaction = new Dictionary<int, Dictionary<string, List<EntitySnapshot>>>();
            foreach (var system in systems)
            {
                systemOrder.Add((system.SystemId, system.SystemName));
                if (system.Entities == null)
                    continue;
                foreach (var entity in system.Entities)
                {
                    if (entity == null || entity.Kind != BodyKind.Colony || entity.Relation == OwnerRelation.Owned)
                        continue;
                    if (!byFaction.TryGetValue(entity.FactionId, out var bySystem))
                    {
                        bySystem = new Dictionary<string, List<EntitySnapshot>>();
                        byFaction.Add(entity.FactionId, bySystem);
                    }
                    if (!bySystem.TryGetValue(system.SystemId, out var list))
                    {
                        list = new List<EntitySnapshot>();
                        bySystem.Add(system.SystemId, list);
                    }
                    list.Add(entity);
                }
            }

            var factionIds = new List<int>(byFaction.Keys);
            factionIds.Sort((a, b) =>
            {
                int byName = StringComparer.Ordinal.Compare(FactionName(stances, a), FactionName(stances, b));
                if (byName != 0)
                    return byName;
                return a.CompareTo(b);
            });
            foreach (int factionId in factionIds)
            {
                var factionSystems = new List<SystemColonies>();
                var bySystem = byFaction[factionId];
                foreach (var system in systemOrder)
                {
                    if (bySystem.TryGetValue(system.SystemId, out var colonies) && colonies.Count > 0)
                        factionSystems.Add(new SystemColonies(system.SystemId, system.SystemName, colonies));
                }
                result.Add(new ForeignFactionGroup(factionId, FactionName(stances, factionId), factionSystems));
            }
            return result;
        }

        /// <summary>
        /// The colony the window should show. A stored selection that still resolves to a Kind == Colony entity in its
        /// system is kept whatever its Relation (so SelectColony of a foreign colony is never replaced by auto-select).
        /// Otherwise the first OwnerRelation.Owned colony in system order. Null when there is none.
        /// </summary>
        public static (int ColonyId, string SystemId)? ResolveColonySelection(
            int? selectedId, string? selectedSystemId,
            IEnumerable<(string SystemId, IEnumerable<EntitySnapshot>? Entities)> systems)
        {
            (int ColonyId, string SystemId)? firstOwned = null;
            if (systems == null)
                return null;

            foreach (var system in systems)
            {
                if (system.Entities == null)
                    continue;

                bool selectedHere = selectedId != null && selectedSystemId != null && system.SystemId == selectedSystemId;
                foreach (var entity in system.Entities)
                {
                    if (entity == null || entity.Kind != BodyKind.Colony)
                        continue;
                    if (selectedHere && entity.Id == selectedId)
                        return (entity.Id, system.SystemId);
                    if (firstOwned == null && entity.Relation == OwnerRelation.Owned)
                        firstOwned = (entity.Id, system.SystemId);
                }
            }
            return firstOwned;
        }

        /// <summary>Which Colony Management tabs a colony may show.</summary>
        [Flags]
        public enum ColonyTabs
        {
            None = 0,
            Summary = 1,
            Production = 2,
            Construction = 4,
            Market = 8,
            Shipping = 16,
            Mining = 32,
            NavalAcademy = 64,
            Recruitment = 128,
            Log = 256,
            Power = 512,
        }

        /// <summary>
        /// Owned: Summary|Production|Construction|Market|Shipping|Log, plus Mining when ColonyMiningView is present,
        /// NavalAcademy when NavalAcademyView is present, Recruitment when RecruitmentView is present, Power when ColonyPowerView is present.
        /// Not owned: Summary|Market|Shipping only.
        /// </summary>
        public static ColonyTabs ColonyTabsFor(EntitySnapshot colony)
        {
            if (colony.Relation != OwnerRelation.Owned)
                return ColonyTabs.Summary | ColonyTabs.Market | ColonyTabs.Shipping;

            var tabs = ColonyTabs.Summary | ColonyTabs.Production | ColonyTabs.Construction
                | ColonyTabs.Market | ColonyTabs.Shipping | ColonyTabs.Log;
            if (colony.HasView<ColonyMiningView>())
                tabs |= ColonyTabs.Mining;
            if (colony.HasView<NavalAcademyView>())
                tabs |= ColonyTabs.NavalAcademy;
            if (colony.HasView<RecruitmentView>())
                tabs |= ColonyTabs.Recruitment;
            if (colony.HasView<ColonyPowerView>())
                tabs |= ColonyTabs.Power;
            return tabs;
        }

        /// <summary>True for anything the player does not own: the windows hide command chrome for it.</summary>
        public static bool IsReadOnly(EntitySnapshot entity) => entity.Relation != OwnerRelation.Owned;

        /// <summary>
        /// Non-owned colonies on a body: Kind == Colony, Relation != Owned, ColonyView.PlanetEntityId == bodyId, snapshot order.
        /// </summary>
        public static IReadOnlyList<EntitySnapshot> ForeignColoniesOn(IEnumerable<EntitySnapshot> entities, int bodyId)
        {
            var list = new List<EntitySnapshot>();
            if (entities == null)
                return list;
            foreach (var entity in entities)
            {
                if (entity == null || entity.Kind != BodyKind.Colony || entity.Relation == OwnerRelation.Owned)
                    continue;
                if (entity.GetView<ColonyView>()?.PlanetEntityId == bodyId)
                    list.Add(entity);
            }
            return list;
        }

        /// <summary>
        /// A visible non-owned ship to inspect: Id == shipId, Kind == BodyKind.Ship, Relation != Owned.
        /// Looks in the hinted system first, then the others in order. Null when not visible anywhere (or owned).
        /// </summary>
        public static (EntitySnapshot Ship, string SystemId)? ResolveForeignShip(
            int shipId, string? systemIdHint,
            IEnumerable<(string SystemId, IEnumerable<EntitySnapshot>? Entities)> systems)
        {
            if (systems == null)
                return null;

            (EntitySnapshot Ship, string SystemId)? firstOther = null;
            foreach (var system in systems)
            {
                if (system.Entities == null)
                    continue;

                bool hinted = systemIdHint != null && system.SystemId == systemIdHint;
                foreach (var entity in system.Entities)
                {
                    if (entity == null || entity.Id != shipId || entity.Kind != BodyKind.Ship)
                        continue;
                    if (entity.Relation == OwnerRelation.Owned)
                        continue;

                    var hit = (Ship: entity, SystemId: system.SystemId);
                    if (hinted)
                        return hit;
                    if (firstOther == null)
                        firstOther = hit;
                }
            }
            return firstOther;
        }
    }
}
