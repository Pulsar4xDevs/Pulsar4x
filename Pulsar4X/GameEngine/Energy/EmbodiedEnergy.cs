using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Pulsar4X.Factions;
using Pulsar4X.Industry;
using Pulsar4X.Interfaces;
using Pulsar4X.Storage;

namespace Pulsar4X.Energy
{
    /// <summary>
    /// Embodied energy (§5 of the energy plan): what a good or design took to make, kWh. Computed,
    /// never hand-set: E = direct energy + mining energy + Σ inputs' E.
    /// <list type="bullet">
    /// <item>A mineral is extraction only: <see cref="Mineral.ExtractionEnergy"/> kWh/kg, set per
    /// mineral in minerals.json (default <see cref="DefaultExtraction_kWhPerKg"/>, the mine's own draw). Ore-to-metal energy belongs to
    /// the refinery recipe (Jon's decision 1).</item>
    /// <item>A material: (batch EnergyCost, or IP x EnergyPerPoint, + Σ inputs) / OutputAmount, per unit.</item>
    /// <item>A component, ordnance or installation design: its build EnergyCost + Σ its resources, per item.</item>
    /// <item>A ship: assembly EnergyCost + Σ components x count (its ResourceCosts are its components).</item>
    /// </list>
    /// Direct energy is the list figure (tech level 0): Industrial Energy Efficiency lowers what a
    /// colony draws, not what a good is worth. Goods are cached per <see cref="FactionDataStore"/>
    /// (mod data, which does not change in play); designs are summed fresh each call over the cached
    /// goods, so a new or replaced design needs no invalidation. A cycle in the recipes counts the
    /// repeated input as 0 and is listed in <see cref="Cycles"/>; nothing computed across a cycle is cached.
    /// </summary>
    public sealed class EmbodiedEnergy
    {
        /// <summary>Extraction energy of a mineral without its own figure in the data, kWh/kg. Matches the mine's draw.</summary>
        public const double DefaultExtraction_kWhPerKg = 0.05;

        /// <summary>The energy type cargo. One unit is one kJ.</summary>
        public const string ElectricityId = "electricity";

        public const double KJPerKWh = 3600;

        static readonly ConditionalWeakTable<FactionDataStore, EmbodiedEnergy> Caches = new();

        readonly FactionDataStore _data;
        readonly Dictionary<string, double> _goods = new(StringComparer.Ordinal);
        readonly HashSet<string> _visiting = new(StringComparer.Ordinal);
        readonly HashSet<string> _cycles = new(StringComparer.Ordinal);
        readonly HashSet<string> _unknown = new(StringComparer.Ordinal);
        int _cyclesInFlight; // counts up; a good is cached only if it did not move while summing it

        EmbodiedEnergy(FactionDataStore data) => _data = data;

        /// <summary>The calculator for this faction data, cached with it.</summary>
        public static EmbodiedEnergy For(FactionDataStore data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            return Caches.GetValue(data, d => new EmbodiedEnergy(d));
        }

        /// <summary>Drops the cached goods, e.g. after a mod edits a recipe in place.</summary>
        public static void Invalidate(FactionDataStore data)
        {
            if (data != null)
                Caches.Remove(data);
        }

        /// <summary>Ids reached again while still being summed (recipe cycles). Counted as 0 there.</summary>
        public IReadOnlyCollection<string> Cycles => _cycles;

        /// <summary>Ids with no known definition. Counted as 0.</summary>
        public IReadOnlyCollection<string> Unknown => _unknown;

        /// <summary>
        /// kWh per unit of a cargo id: a mineral, a material, or (with <paramref name="designs"/>) a
        /// component, ordnance or ship design. 0 for an unknown id.
        /// </summary>
        public double PerUnit(string id, FactionInfoDB? designs = null)
        {
            if (string.IsNullOrEmpty(id))
                return 0;
            if (_goods.TryGetValue(id, out double cached))
                return cached;

            if (TryGood(id, out var good))
            {
                if (!_visiting.Add(id))
                {
                    _cycles.Add(id);
                    _cyclesInFlight++;
                    return 0;
                }
                int before = _cyclesInFlight;
                double e;
                try { e = OfGood(good, designs); }
                finally { _visiting.Remove(id); }
                if (_cyclesInFlight == before)
                    _goods[id] = e;
                return e;
            }

            if (designs != null && TryDesign(designs, id, out var design))
                return OfDesign(design, designs);

            _unknown.Add(id);
            return 0;
        }

        /// <summary>kWh per kg of a cargo id (PerUnit / MassPerUnit).</summary>
        public double PerKg(string id, FactionInfoDB? designs = null)
        {
            double perUnit = PerUnit(id, designs);
            double mass = MassPerUnit(id, designs);
            return mass > 0 ? perUnit / mass : 0;
        }

        /// <summary>
        /// kWh of one item of a design (one unit for a material): its build energy plus its inputs.
        /// A ship is its assembly plus its components.
        /// </summary>
        public double OfDesign(IConstructableDesign design, FactionInfoDB? designs = null)
        {
            if (design == null)
                return 0;
            if (design is ProcessedMaterial material)
                return PerUnit(material.UniqueID, designs);

            string key = "design:" + design.UniqueID;
            if (!_visiting.Add(key))
            {
                _cycles.Add(design.UniqueID);
                _cyclesInFlight++;
                return 0;
            }
            try
            {
                return Sane(design.EnergyCost) + Inputs(design.ResourceCosts, designs);
            }
            finally
            {
                _visiting.Remove(key);
            }
        }

        double OfGood(ICargoable good, FactionInfoDB? designs)
        {
            switch (good)
            {
                case Mineral mineral:
                    return mineral.MassPerUnit * ExtractionOf(mineral);
                case ProcessedMaterial material:
                    if (material.UniqueID == ElectricityId)
                        return 1 / KJPerKWh;
                    if (material.OutputAmount <= 0)
                        return 0; // no recipe
                    return (Sane(material.BatchEnergy) + Inputs(material.ResourceCosts, designs)) / material.OutputAmount;
                default:
                    return 0;
            }
        }

        /// <summary>Extraction energy of a mineral, kWh/kg.</summary>
        public static double ExtractionOf(Mineral mineral)
            => mineral.ExtractionEnergy is double e && double.IsFinite(e) ? Math.Max(0, e) : DefaultExtraction_kWhPerKg;

        double Inputs(IReadOnlyDictionary<string, long>? costs, FactionInfoDB? designs)
        {
            double sum = 0;
            if (costs == null)
                return sum;
            foreach (var (id, amount) in costs)
                if (amount > 0)
                    sum += amount * PerUnit(id, designs);
            return sum;
        }

        bool TryGood(string id, out ICargoable good)
        {
            var found = _data.CargoGoods.GetAny(id) ?? _data.LockedCargoGoods.GetAny(id);
            if (found is Mineral || found is ProcessedMaterial)
            {
                good = found;
                return true;
            }
            good = null!;
            return false;
        }

        static bool TryDesign(FactionInfoDB designs, string id, out IConstructableDesign design)
        {
            if (designs.IndustryDesigns.TryGetValue(id, out var industry) && industry != null)
            {
                design = industry;
                return true;
            }
            if (designs.ComponentDesigns.TryGetValue(id, out var component) && component != null)
            {
                design = component;
                return true;
            }
            if (designs.ShipDesigns.TryGetValue(id, out var ship) && ship != null)
            {
                design = ship;
                return true;
            }
            design = null!;
            return false;
        }

        double MassPerUnit(string id, FactionInfoDB? designs)
        {
            if (TryGood(id, out var good))
                return good.MassPerUnit;
            if (designs != null && TryDesign(designs, id, out var design) && design is ICargoable cargo)
                return cargo.MassPerUnit;
            return 0;
        }

        static double Sane(double v) => double.IsFinite(v) ? Math.Max(0, v) : 0;
    }
}
