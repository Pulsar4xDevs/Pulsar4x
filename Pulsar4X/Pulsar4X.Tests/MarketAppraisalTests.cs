using System;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Energy;
using Pulsar4X.Factions;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Names;

namespace Pulsar4X.Tests;

/// <summary>Market appraisal: a good's production cost from its recipe at current market prices.</summary>
[TestFixture]
[NonParallelizable]
public class MarketAppraisalTests
{
    const string WarpTechStart = "Pulsar4x.Scenario.WarpTechStart";

    Game _game = null!;
    Entity _player = null!;
    Entity _company = null!;
    EngineGameServer _server = null!;
    PlayerSession _session;

    [SetUp]
    public void SetUp()
    {
        ModsState.RefreshModsList("Data");
        var paths = ModsState.AvailableMods.Where(m => m.Mod.ModName == "Pulsar4x").Select(m => m.Path).ToList();
        var store = GameLifecycle.LoadModStore(paths, ModsState.AvailableMods.Select(m => m.Path), WarpTechStart);
        var result = GameLifecycle.CreateGameCore(store, GameLifecycle.BuildQuickstartRequest(paths, GameLifecycle.BuildCatalog(store), 12345678));
        Assert.That(result, Is.Not.Null);
        (_game, _player) = (result!.Value.game, result.Value.faction);
        _game.Settings.EnforceSingleThread = true;
        _company = _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            .Single(c => c.FactionOwnerID == _player.Id && c.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<NameDB>().DefaultName == "Earth");
        _server = new EngineGameServer(_game);
        _session = new PlayerSession(Guid.NewGuid(), _player.Id);
    }

    [TearDown]
    public void TearDown() => _server.Dispose();

    [Test]
    public void AppraiseGood_UsesMarketPricesForRecipeInputs_AndAddsEnergyAndLabour()
    {
        // Plastic is 1 kg hydrocarbons → 2 kg plastic, 12 kWh and 5 industry points per batch.
        const decimal hydroAsk = 0.25m;
        MarketBook.SetListing(_company, new MarketListing { CargoId = "hydrocarbons", SellQuantity = 1_000, Ask = hydroAsk });

        var view = _server.AppraiseGood(_session, _company.Id, "plastic", 2);
        Assert.That(view.Reason, Is.Empty.Or.Null);
        Assert.That(view.CargoId, Is.EqualTo("plastic"));
        Assert.That(view.Quantity, Is.EqualTo(2));
        Assert.That(view.Cycles, Is.Empty);
        Assert.That(view.Inputs, Has.Some.Matches<AppraisalLine>(l => l.CargoId == "hydrocarbons" && l.FromMarket && l.UnitPrice == hydroAsk && l.Quantity == 1 && l.LineCost == hydroAsk * 1));

        // One batch makes 2 kg, so 2 kg is one batch: 1 kg hydrocarbons at the market ask, plus energy 12 kWh × CreditsPerKWh, plus labour.
        decimal energy = EnergyPricing.Round(EnergyPricing.CreditsPerKWh(_game) * 12);
        decimal labour = Math.Round(2m * 5 / MarketAppraisal.ReferenceIndustryPointsPerDay / 2, 4, MidpointRounding.AwayFromZero) * 2;
        Assert.That(view.InputCost, Is.EqualTo(hydroAsk * 1));
        Assert.That(view.EnergyCost, Is.EqualTo(energy));
        Assert.That(view.LabourCost, Is.EqualTo(labour));
        Assert.That(view.TotalCost, Is.EqualTo(hydroAsk * 1 + energy + labour));
        Assert.That(view.UnitCost, Is.EqualTo(view.TotalCost / 2));
    }

    [Test]
    public void AppraiseGood_QuantityOne_WithOutputAmountTwo_LinesUseFractionalQuantitiesThatSumToInputCost()
    {
        // Plastic is 1 kg hydrocarbons → 2 kg plastic. One kilogram should show 0.5 kg hydrocarbons, not a ceiled 1.
        const decimal hydroAsk = 1.5m;
        MarketBook.SetListing(_company, new MarketListing { CargoId = "hydrocarbons", SellQuantity = 1_000, Ask = hydroAsk });

        var view = _server.AppraiseGood(_session, _company.Id, "plastic", 1);
        Assert.That(view.Reason, Is.Empty.Or.Null);
        Assert.That(view.InputCost, Is.EqualTo(0.75m));
        var hydro = view.Inputs.Single(l => l.CargoId == "hydrocarbons");
        Assert.That(hydro.Quantity, Is.EqualTo(0.5).Within(1e-9));
        Assert.That(hydro.UnitPrice, Is.EqualTo(hydroAsk));
        Assert.That(hydro.LineCost, Is.EqualTo(0.75m));
        Assert.That(view.Inputs.Where(l => l.LineCost != null).Sum(l => l.LineCost!.Value), Is.EqualTo(view.InputCost));
    }

    [Test]
    public void AppraiseGood_ReportsACycleAndMissingPricesWithoutThrowing()
    {
        var info = _player.GetDataBlob<FactionInfoDB>();
        var steel = info.Data.CargoGoods.GetMaterial("stainless-steel");
        // Point steel at itself so the recipe is a cycle, then restore.
        var original = steel.ResourceCosts.ToDictionary(k => k.Key, k => k.Value);
        steel.ResourceCosts.Clear();
        steel.ResourceCosts["stainless-steel"] = 1;
        try
        {
            var cyclic = MarketAppraisal.Appraise(_game, _company, "stainless-steel", 1);
            Assert.That(cyclic.Cycles, Does.Contain("stainless-steel"));
            Assert.That(cyclic.UnitCost, Is.Null.Or.Not.Null); // must not throw; cost may still include energy
        }
        finally
        {
            steel.ResourceCosts.Clear();
            foreach (var (k, v) in original)
                steel.ResourceCosts[k] = v;
        }

        var missing = MarketAppraisal.Appraise(_game, _company, "this-good-does-not-exist", 1);
        Assert.That(missing.Reason, Is.Not.Empty);
        Assert.That(missing.TotalCost, Is.Null);
    }

    [Test]
    public void AppraiseGood_ComponentDesign_SumsResourceCostsAtMarketPrices_AndAddsEnergyAndLabour()
    {
        var info = _player.GetDataBlob<FactionInfoDB>();
        bool IsLeaf(string id)
        {
            if (info.ComponentDesigns.ContainsKey(id))
                return false;
            if (!info.Data.CargoGoods.IsMaterial(id) && !info.Data.LockedCargoGoods.IsMaterial(id))
                return true;
            var material = info.Data.CargoGoods.IsMaterial(id)
                ? info.Data.CargoGoods.GetMaterial(id)
                : info.Data.LockedCargoGoods.GetMaterial(id);
            return material.ResourceCosts == null || material.ResourceCosts.Count == 0;
        }
        var design = info.ComponentDesigns.Values.First(d =>
            d.ResourceCosts != null && d.ResourceCosts.Count > 0 && d.ResourceCosts.Keys.All(IsLeaf));
        const decimal ask = 0.05m;
        foreach (var cargoId in design.ResourceCosts.Keys)
            MarketBook.SetListing(_company, new MarketListing { CargoId = cargoId, SellQuantity = 1_000_000, Ask = ask });

        var view = _server.AppraiseGood(_session, _company.Id, design.UniqueID, 2);
        Assert.That(view.Reason, Is.Empty.Or.Null, view.Reason);
        Assert.That(view.CargoId, Is.EqualTo(design.UniqueID));
        Assert.That(view.Name, Is.EqualTo(design.Name));
        Assert.That(view.Quantity, Is.EqualTo(2));
        Assert.That(view.Cycles, Is.Empty);

        decimal input = 0;
        foreach (var (cargoId, qty) in design.ResourceCosts)
        {
            if (qty <= 0) continue;
            var line = view.Inputs.Single(l => l.CargoId == cargoId);
            Assert.That(line.Quantity, Is.EqualTo(qty * 2).Within(1e-9));
            Assert.That(line.FromMarket, Is.True);
            Assert.That(line.UnitPrice, Is.EqualTo(ask));
            Assert.That(line.LineCost, Is.EqualTo(ask * (decimal)(qty * 2)));
            input += line.LineCost!.Value;
        }
        Assert.That(view.InputCost, Is.EqualTo(input));

        long output = Math.Max(1, (long)design.OutputAmount);
        decimal? energyPer = design.EnergyCost > 0
            ? EnergyPricing.Round(EnergyPricing.CreditsPerKWh(_game) * design.EnergyCost / output)
            : null;
        decimal? labourPer = design.IndustryPointCosts > 0
            ? Math.Round(2m * design.IndustryPointCosts / MarketAppraisal.ReferenceIndustryPointsPerDay / output, 4, MidpointRounding.AwayFromZero)
            : null;
        Assert.That(view.EnergyCost, Is.EqualTo(energyPer is decimal e ? e * 2 : (decimal?)null));
        Assert.That(view.LabourCost, Is.EqualTo(labourPer is decimal l ? l * 2 : (decimal?)null));
        Assert.That(view.TotalCost, Is.EqualTo(view.InputCost + (view.EnergyCost ?? 0) + (view.LabourCost ?? 0)));
        Assert.That(view.UnitCost, Is.EqualTo(view.TotalCost / 2));
    }
}
