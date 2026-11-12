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

        /// <summary>One faction's colonies in a system, in snapshot order. The player's group has a null faction.</summary>
        public sealed record ColonyGroup(int? FactionId, string? FactionName, IReadOnlyList<EntitySnapshot> Colonies);

        /// <summary>
        /// One system's colonies for the Colony Management list: the player's owned colonies first (FactionId null,
        /// FactionName null), then one group per other faction ordered by FactionName (StringComparer.Ordinal) then FactionId.
        /// Only Kind == BodyKind.Colony. Colonies keep snapshot order inside a group. Groups with no colonies are not returned
        /// (so a system with no colonies returns an empty list, and a system with only foreign colonies returns only foreign groups).
        /// </summary>
        public static IReadOnlyList<ColonyGroup> ColonyGroups(IEnumerable<EntitySnapshot> entities, IReadOnlyList<FactionStanceRow> stances)
        {
            var owned = new List<EntitySnapshot>();
            var byFaction = new Dictionary<int, List<EntitySnapshot>>();
            if (entities != null)
            {
                foreach (var entity in entities)
                {
                    if (entity == null || entity.Kind != BodyKind.Colony)
                        continue;
                    if (entity.Relation == OwnerRelation.Owned)
                    {
                        owned.Add(entity);
                        continue;
                    }

                    if (!byFaction.TryGetValue(entity.FactionId, out var list))
                    {
                        list = new List<EntitySnapshot>();
                        byFaction.Add(entity.FactionId, list);
                    }
                    list.Add(entity);
                }
            }

            var groups = new List<ColonyGroup>();
            if (owned.Count > 0)
                groups.Add(new ColonyGroup(null, null, owned));

            var factionIds = new List<int>(byFaction.Keys);
            factionIds.Sort((a, b) =>
            {
                int byName = StringComparer.Ordinal.Compare(FactionName(stances, a), FactionName(stances, b));
                if (byName != 0)
                    return byName;
                return a.CompareTo(b);
            });
            foreach (int factionId in factionIds)
                groups.Add(new ColonyGroup(factionId, FactionName(stances, factionId), byFaction[factionId]));
            return groups;
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
        }

        /// <summary>
        /// Owned: Summary|Production|Construction|Market|Shipping|Log, plus Mining when ColonyMiningView is present,
        /// NavalAcademy when NavalAcademyView is present, Recruitment when RecruitmentView is present (exactly today's conditions).
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
