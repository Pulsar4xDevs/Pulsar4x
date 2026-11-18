namespace Pulsar4X.Api;

/// <summary>
/// An estimate of what a good costs to make, from its recipe at current market prices on a colony's body.
/// Null cost fields mean that part could not be priced. <see cref="Reason"/> is empty when the estimate is usable.
/// </summary>
public sealed record GoodAppraisal(
    string CargoId,
    string Name,
    long Quantity,
    decimal? UnitCost,
    decimal? TotalCost,
    decimal? InputCost,
    decimal? EnergyCost,
    decimal? LabourCost,
    IReadOnlyList<AppraisalLine> Inputs,
    IReadOnlyList<string> MissingPrices,
    IReadOnlyList<string> Cycles,
    string? Reason)
{
    public static GoodAppraisal None(string cargoId, string name, long quantity, string reason) =>
        new(cargoId, name, quantity, null, null, null, null, null,
            Array.Empty<AppraisalLine>(), Array.Empty<string>(), Array.Empty<string>(), reason);
}

/// <summary>One recipe input. <see cref="FromMarket"/> is true when the unit price came from a listing on the body;
/// <see cref="Recursed"/> is true when that input was itself estimated from a recipe.</summary>
public sealed record AppraisalLine(
    string CargoId,
    string Name,
    double Quantity,
    decimal? UnitPrice,
    decimal? LineCost,
    bool FromMarket,
    bool Recursed);
