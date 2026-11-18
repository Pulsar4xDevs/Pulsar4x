using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Energy;
using Pulsar4X.Extensions;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Industry;
using Pulsar4X.Storage;

namespace Pulsar4X.Logistics
{
    /// <summary>
    /// Multi-faction play note: estimate what a good costs to make from its recipe, at current market
    /// prices on the colony's body. Recurses into intermediate goods; energy is the production step;
    /// labour is staff wages for the industry points at one fully staffed 8,000-IP/day site.
    /// Cycles and missing prices are reported, not thrown.
    /// </summary>
    public static class MarketAppraisal
    {
        public const long ReferenceIndustryPointsPerDay = 8000;

        public static GoodAppraisal Appraise(Game game, Entity colony, string cargoId, long quantity)
        {
            if (quantity <= 0)
                return GoodAppraisal.None(cargoId, cargoId, quantity, "quantity must be positive");
            if (game == null || colony == null || string.IsNullOrEmpty(cargoId))
                return GoodAppraisal.None(cargoId ?? "", cargoId ?? "", quantity, "colony or cargo is missing");
            if (!game.Factions.TryGetValue(colony.FactionOwnerID, out var faction)
                || !faction.TryGetDataBlob<FactionInfoDB>(out var info))
                return GoodAppraisal.None(cargoId, cargoId, quantity, "faction not found");

            var data = info.Data;
            var cargo = data.CargoGoods.Contains(cargoId) ? data.CargoGoods.GetAny(cargoId) : null;
            string name = cargo?.Name ?? data.GetName(cargoId);
            if (string.IsNullOrEmpty(name))
                name = cargoId;

            var missing = new List<string>();
            var cycles = new List<string>();
            var inputs = new List<AppraisalLine>();
            var visiting = new HashSet<string>(StringComparer.Ordinal);

            var (unit, input, energy, labour) = CostOf(game, colony, data, cargoId, 1, visiting, missing, cycles, inputs, top: true);
            if (unit is null && energy is null && labour is null && input is null)
            {
                string reason = cycles.Count > 0 ? "recipe cycle" : missing.Count > 0 ? "missing prices" : "no recipe or market price";
                return new GoodAppraisal(cargoId, name, quantity, null, null, null, null, null, inputs, missing, cycles, reason);
            }

            decimal? Scale(decimal? v) => v is decimal d ? d * quantity : null;
            var total = (input ?? 0m) + (energy ?? 0m) + (labour ?? 0m);
            bool any = input != null || energy != null || labour != null;
            return new GoodAppraisal(
                cargoId, name, quantity,
                any ? total : unit,
                any ? total * quantity : Scale(unit),
                Scale(input), Scale(energy), Scale(labour),
                inputs, missing, cycles,
                "");
        }

        static (decimal? unit, decimal? input, decimal? energy, decimal? labour) CostOf(
            Game game, Entity colony, FactionDataStore data, string cargoId, long needed,
            HashSet<string> visiting, List<string> missing, List<string> cycles, List<AppraisalLine> inputs,
            bool top)
        {
            if (!visiting.Add(cargoId))
            {
                if (!cycles.Contains(cargoId))
                    cycles.Add(cargoId);
                return (null, null, null, null);
            }

            try
            {
                var material = TryMaterial(data, cargoId);
                if (material != null && material.ResourceCosts != null && material.ResourceCosts.Count > 0)
                {
                    decimal input = 0;
                    bool anyInput = false;
                    long output = Math.Max(1, (long)material.OutputAmount);
                    foreach (var (inId, inQty) in material.ResourceCosts.OrderBy(k => k.Key, StringComparer.Ordinal))
                    {
                        if (inQty <= 0)
                            continue;
                        // Quantity of this input for one output unit of the parent.
                        double perOut = inQty / (double)output;
                        bool hasRecipe = TryMaterial(data, inId) is ProcessedMaterial m && m.ResourceCosts != null && m.ResourceCosts.Count > 0;
                        var (childUnit, _, _, _) = CostOf(game, colony, data, inId, 1, visiting, missing, cycles, inputs, top: false);
                        var market = MarketUnitPrice(game, colony, data, inId, out bool fromMarket);
                        decimal? price = hasRecipe ? childUnit : market ?? childUnit;
                        if (!hasRecipe)
                            fromMarket = market != null && fromMarket;
                        var lineQty = (long)Math.Ceiling(perOut * needed);
                        string inName = data.GetName(inId);
                        if (string.IsNullOrEmpty(inName)) inName = inId;
                        inputs.Add(new AppraisalLine(inId, inName, lineQty, price, price is decimal p ? p * lineQty : null, fromMarket, hasRecipe));
                        if (price is decimal pay)
                        {
                            input += pay * (decimal)perOut;
                            anyInput = true;
                        }
                        else if (!missing.Contains(inId))
                            missing.Add(inId);
                    }

                    decimal? energy = EnergyCostOf(game, material, output);
                    decimal? labour = LabourCostOf(game, material, output);
                    decimal? unit = (anyInput ? input : 0m) + (energy ?? 0m) + (labour ?? 0m);
                    if (!anyInput && energy is null && labour is null)
                        unit = MarketUnitPrice(game, colony, data, cargoId, out _);
                    return (unit, anyInput ? input : null, energy, labour);
                }

                var marketPrice = MarketUnitPrice(game, colony, data, cargoId, out _);
                if (marketPrice is null && top && !missing.Contains(cargoId))
                    missing.Add(cargoId);
                return (marketPrice, null, null, null);
            }
            finally
            {
                visiting.Remove(cargoId);
            }
        }

        static ProcessedMaterial? TryMaterial(FactionDataStore data, string cargoId)
        {
            if (data.CargoGoods.IsMaterial(cargoId))
                return data.CargoGoods.GetMaterial(cargoId);
            if (data.LockedCargoGoods.IsMaterial(cargoId))
                return data.LockedCargoGoods.GetMaterial(cargoId);
            return null;
        }

        /// <summary>Lowest ask of a seller on this body with a positive sell quantity; otherwise the seed ask.</summary>
        public static decimal? MarketUnitPrice(Game game, Entity colony, FactionDataStore data, string cargoId, out bool fromMarket)
        {
            fromMarket = false;
            decimal? best = null;
            if (colony.TryGetDataBlob<ColonyInfoDB>(out var info))
            {
                foreach (var other in info.PlanetEntity.ColoniesOnBody())
                {
                    if (!MarketBook.TryGet(other, cargoId, out var listing) || listing.SellQuantity <= 0 || listing.Ask <= 0)
                        continue;
                    if (best is null || listing.Ask < best)
                    {
                        best = listing.Ask;
                        fromMarket = true;
                    }
                }
            }
            if (best != null)
                return best;
            if (!data.CargoGoods.Contains(cargoId) && !data.LockedCargoGoods.Contains(cargoId))
                return null;
            decimal seed = EnergyPricing.SeedAsk(game, data, cargoId);
            return seed > 0 ? seed : null;
        }

        static decimal? EnergyCostOf(Game game, ProcessedMaterial material, long output)
        {
            double kWh = material.EnergyCost ?? 0;
            if (kWh <= 0)
                return null;
            return EnergyPricing.Round(EnergyPricing.CreditsPerKWh(game) * kWh / output);
        }

        static decimal? LabourCostOf(Game game, ProcessedMaterial material, long output)
        {
            long ip = material.IndustryPointCosts;
            if (ip <= 0)
                return null;
            decimal wage = game?.Settings?.StaffWagePerDay ?? GameSettings.DefaultStaffWagePerDay;
            return Math.Round(wage * ip / ReferenceIndustryPointsPerDay / output, 4, MidpointRounding.AwayFromZero);
        }
    }
}
