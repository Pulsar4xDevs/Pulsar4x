using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Components;

namespace Pulsar4X.Ships
{
    /// <summary>
    /// Crew capacity and designer checks (crew plan phases 1-3). The designer and the projector call
    /// these. Berths come from parts with a CrewQuartersAtb: crew quarters, and a few on every bridge.
    /// </summary>
    public static class ShipCrew
    {
        /// <summary>Berths one copy of the part gives; 0 for parts without quarters.</summary>
        public static int BerthsPer(ComponentDesign design)
            => design != null && design.TryGetAttribute<CrewQuartersAtb>(out var q) ? q.Berths : 0;

        /// <summary>Berths the parts give: crew quarters plus bridge berths.</summary>
        public static int Berths(IEnumerable<(ComponentDesign design, int count)> components)
            => components?.Sum(c => BerthsPer(c.design) * c.count) ?? 0;

        /// <summary>Crew the parts need: Σ part crew × count, as ShipDesign.CrewReq.</summary>
        public static int CrewRequired(IEnumerable<(ComponentDesign design, int count)> components)
            => components?.Sum(c => (c.design?.CrewReq ?? 0) * c.count) ?? 0;

        /// <summary>Designer warnings about crew: fewer berths than the parts need.</summary>
        public static List<string> DesignCrewWarnings(IEnumerable<(ComponentDesign design, int count)> components)
        {
            var warnings = new List<string>();
            if (components == null) return warnings;
            var list = components.ToList();
            int crew = CrewRequired(list), berths = Berths(list);
            if (berths < crew)
                warnings.Add($"Crew {crew} but only {berths} berths: add crew quarters for {crew - berths} more.");
            return warnings;
        }
    }
}
