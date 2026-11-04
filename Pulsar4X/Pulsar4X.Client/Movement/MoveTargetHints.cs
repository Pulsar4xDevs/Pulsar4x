using System.Collections.Generic;
using Pulsar4X.Api;

namespace Pulsar4X.Client
{
    /// <summary>
    /// Row tags for Move to. Arithmetic over snapshots the client already holds.
    /// No orbit propagation and no <c>PreviewMove</c>.
    /// </summary>
    public static class MoveTargetHints
    {
        public const string Here = "Here";
        public const string SameSphere = "Same sphere";
        public const string OtherSphere = "Other sphere";
        public const string OtherSystem = "Other system";
        public const string NoRoute = "No route";

        public static int? ParentId(EntitySnapshot body)
        {
            if (body == null)
                return null;
            if (body.GetView<OrbitView>()?.ParentId is int orbitParent && orbitParent != body.Id)
                return orbitParent;
            if (body.GetView<PositionView>()?.ParentId is int positionParent && positionParent != body.Id)
                return positionParent;
            if (body.GetView<ColonyView>()?.PlanetEntityId is int planetId && planetId != body.Id)
                return planetId;
            return null;
        }

        /// <summary>
        /// One short tag. <paramref name="links"/> maps a system id to the systems its discovered
        /// jump points lead to. A missing map is no route out of the ship's system.
        /// </summary>
        public static string? Of(
            EntitySnapshot target,
            int? shipParentId,
            string shipSystemId,
            string targetSystemId,
            IReadOnlyDictionary<string, IReadOnlyList<string>> links)
        {
            if (target == null || target.Kind == BodyKind.Star)
                return null;
            if (shipParentId is int parent && target.Id == parent)
                return Here;

            bool sameSystem = !string.IsNullOrEmpty(shipSystemId)
                && string.Equals(shipSystemId, targetSystemId);
            if (!sameSystem)
                return Reachable(shipSystemId, targetSystemId, links) ? OtherSystem : NoRoute;

            int? targetParent = ParentId(target);
            if (targetParent != null && shipParentId != null && targetParent == shipParentId)
                return SameSphere;
            return OtherSphere;
        }

        public static bool Reachable(
            string fromSystemId, string toSystemId, IReadOnlyDictionary<string, IReadOnlyList<string>> links)
        {
            if (string.IsNullOrEmpty(fromSystemId) || string.IsNullOrEmpty(toSystemId) || links == null)
                return false;
            if (fromSystemId == toSystemId)
                return true;
            var seen = new HashSet<string> { fromSystemId };
            var queue = new Queue<string>();
            queue.Enqueue(fromSystemId);
            while (queue.Count > 0)
            {
                string system = queue.Dequeue();
                if (!links.TryGetValue(system, out var next) || next == null)
                    continue;
                foreach (string dest in next)
                {
                    if (string.IsNullOrEmpty(dest))
                        continue;
                    if (dest == toSystemId)
                        return true;
                    if (seen.Add(dest))
                        queue.Enqueue(dest);
                }
            }

            return false;
        }
    }
}
