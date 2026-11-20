using System;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;

namespace Pulsar4X.Tests;

/// <summary>Client logic for warehouse Min/Max sliders and the auto-pricing banner. No ImGui and no game.</summary>
[TestFixture]
public class WarehouseStockUiTests
{
    static CargoItemView Item(int id, string name, string cargoId, long units)
        => new(id, name, "material", "", units, 0, 0, 0, 0, 0, 0, false) { CargoId = cargoId };

    static CargoStorageView Storage(
        bool industry = false,
        bool market = false,
        StockRuleView[]? rules = null,
        CargoItemView[]? items = null)
        => new(0, 0, 0)
        {
            IndustrySeatFilled = industry,
            MarketSeatFilled = market,
            StockRules = rules ?? Array.Empty<StockRuleView>(),
            Stores = items == null || items.Length == 0
                ? Array.Empty<CargoTypeStoreView>()
                : new[] { new CargoTypeStoreView("general", "General", 0, 0) { Items = items } },
        };

    [Test]
    public void MinAndMax_AreShownOnlyWhenTheMatchingSeatIsFilled()
    {
        Assert.That(WarehouseStockUi.ShowMin(false), Is.False);
        Assert.That(WarehouseStockUi.ShowMin(true), Is.True);
        Assert.That(WarehouseStockUi.ShowMax(false), Is.False);
        Assert.That(WarehouseStockUi.ShowMax(true), Is.True);
        Assert.That(WarehouseStockUi.MinHint(true), Is.Null);
        Assert.That(WarehouseStockUi.MaxHint(true), Is.Null);
        Assert.That(WarehouseStockUi.MinHint(false), Is.EqualTo(WarehouseStockUi.MinHiddenHint));
        Assert.That(WarehouseStockUi.MaxHint(false), Is.EqualTo(WarehouseStockUi.MaxHiddenHint));
    }

    [Test]
    public void AutoPricing_IsOffWithoutAMarketHead()
    {
        Assert.That(WarehouseStockUi.AutoPricingOn(false), Is.False);
        Assert.That(WarehouseStockUi.AutoPricingOn(true), Is.True);
        Assert.That(WarehouseStockUi.AutoPricingHint(true), Is.Null);
        Assert.That(WarehouseStockUi.AutoPricingHint(false), Is.EqualTo(WarehouseStockUi.AutoPricingOffHint));
    }

    [Test]
    public void Rows_UnionRulesAndCargo_AndExtras_OrderedByName()
    {
        var ironRule = new StockRuleView("iron", "Iron", 10, 20) { Stock = 5, Queued = 3 };
        var storage = Storage(
            rules: new[] { ironRule },
            items: new[]
            {
                Item(1, "Iron", "iron", 99),
                Item(2, "Water", "water", 7),
                Item(3, "Nameless", "", 1),
            });

        var rows = WarehouseStockUi.Rows(
            storage,
            new[] { new MarketGoodChoice("copper", "Copper"), new MarketGoodChoice("iron", "Iron listed") });

        Assert.That(rows.Select(r => r.CargoId), Is.EqualTo(new[] { "copper", "iron", "water" }));
        var iron = rows.Single(r => r.CargoId == "iron");
        Assert.That((iron.Min, iron.Max, iron.Stock, iron.Queued, iron.HasRule), Is.EqualTo(((long?)10, (long?)20, 5L, 3L, true)));
        var water = rows.Single(r => r.CargoId == "water");
        Assert.That((water.Min, water.Max, water.Stock, water.Queued, water.HasRule), Is.EqualTo(((long?)null, (long?)null, 7L, 0L, false)));
        var copper = rows.Single(r => r.CargoId == "copper");
        Assert.That((copper.Stock, copper.HasRule), Is.EqualTo((0L, false)));
        Assert.That(WarehouseStockUi.Rows(null), Is.Empty);
    }

    [Test]
    public void SliderCeiling_HasHeadroom_AndGrowsToANiceStep()
    {
        Assert.That(WarehouseStockUi.SliderCeiling(0, 0, null, null), Is.EqualTo(100));
        Assert.That(WarehouseStockUi.SliderCeiling(40, 0, null, null), Is.EqualTo(100));
        Assert.That(WarehouseStockUi.SliderCeiling(80, 0, null, null), Is.EqualTo(200));
        Assert.That(WarehouseStockUi.SliderCeiling(0, 0, 300, null), Is.EqualTo(1000));
        Assert.That(WarehouseStockUi.SliderCeiling(0, 0, null, 600), Is.EqualTo(2000));
        Assert.That(WarehouseStockUi.StockQueuedText(1200, 400), Is.EqualTo("Stock 1,200   Queued 400"));
    }

    [Test]
    public void WithMinOrMax_ClampsSoMaxIsNeverBelowMin()
    {
        Assert.That(WarehouseStockUi.WithMin(10, 20, 30), Is.EqualTo(((long?)30, (long?)30)));
        Assert.That(WarehouseStockUi.WithMax(10, 20, 5), Is.EqualTo(((long?)5, (long?)5)));
        Assert.That(WarehouseStockUi.WithMin(10, 20, null), Is.EqualTo(((long?)null, (long?)20)));
        Assert.That(WarehouseStockUi.WithMax(10, 20, null), Is.EqualTo(((long?)10, (long?)null)));
    }

    [Test]
    public void Command_ClearsWhenBothBoundsAreUnset_OtherwiseSets()
    {
        var clear = WarehouseStockUi.Command(9, "iron", null, null);
        Assert.That(clear, Is.TypeOf<ClearStockRuleCommand>());
        Assert.That(((ClearStockRuleCommand)clear).TargetEntityId, Is.EqualTo(9));
        Assert.That(((ClearStockRuleCommand)clear).CargoId, Is.EqualTo("iron"));

        var set = WarehouseStockUi.Command(9, "water", null, 50);
        Assert.That(set, Is.TypeOf<SetStockRuleCommand>());
        var typed = (SetStockRuleCommand)set;
        Assert.That((typed.TargetEntityId, typed.CargoId, typed.Min, typed.Max), Is.EqualTo((9, "water", (long?)null, (long?)50)));
    }

    [Test]
    public void RefusalMessage_ShowsTheEngineReason_WhenTheCommandIsRejected()
    {
        Assert.That(WarehouseStockUi.RefusalMessage(null), Is.Null);
        Assert.That(WarehouseStockUi.RefusalMessage(CommandResult.Ok("ok")), Is.Null);
        Assert.That(WarehouseStockUi.RefusalMessage(CommandResult.Reject("Max cannot be below min.")), Is.EqualTo("Max cannot be below min."));
        Assert.That(WarehouseStockUi.RefusalMessage(new CommandResult(false)), Is.EqualTo("The command was refused."));
    }
}
