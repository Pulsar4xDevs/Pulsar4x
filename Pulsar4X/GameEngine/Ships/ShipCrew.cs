using System.Collections.Generic;
using Pulsar4X.Components;

namespace Pulsar4X.Ships
{
    /// <summary>
    /// Crew capacity and designer checks (crew plan phases 1-3). The designer and the projector call
    /// these, so the UI can bind to them before the engine fills them in. Phase 1 adds crew quarters
    /// and bridge berths; until then a design has no berths and no crew warnings.
    /// </summary>
    public static class ShipCrew
    {
        /// <summary>Berths the parts give: crew quarters plus bridge console space. 0 until phase 1.</summary>
        public static int Berths(IEnumerable<(ComponentDesign design, int count)> components) => 0;

        /// <summary>Designer warnings about crew, e.g. fewer berths than required crew. Empty until phase 1.</summary>
        public static List<string> DesignCrewWarnings(IEnumerable<(ComponentDesign design, int count)> components) => new();
    }
}
