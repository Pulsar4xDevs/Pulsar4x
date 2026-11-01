using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Storage;

namespace Pulsar4X.Logistics;

/// <summary>
/// One good or crate inside a shipment. For a resource, <see cref="Open"/> is the library object and
/// <see cref="Held"/> is the <see cref="SealedLineCargo"/> that travels. For a crate both are the
/// same <see cref="ICargoable"/> instance, so a crate is never split or wrapped.
/// </summary>
public sealed class ShipmentLine
{
    /// <summary>The id the line is sealed under in the open store: library id, or the crate instance id.</summary>
    [JsonProperty] public int ItemId;

    [JsonProperty] public string CargoUniqueId = "";
    [JsonProperty] public long Units;

    /// <summary>Price per unit paid at the seal. 0 on the non-market path.</summary>
    [JsonProperty] public decimal Price;

    [JsonProperty] public ICargoable Open = null!;
    [JsonProperty] public ICargoable Held = null!;

    /// <summary>The line has left the hold. It sits in a store as itself.</summary>
    [JsonProperty] public bool Released;

    /// <summary>Units unpacked at the destination.</summary>
    [JsonProperty] public long Delivered;

    /// <summary>The ship carrying the released line, or -1.</summary>
    [JsonProperty] public int CarrierId = -1;

    [JsonIgnore] public bool IsCrate => Held is not SealedLineCargo;
    [JsonIgnore] public bool Arrived => Delivered >= Units;
    [JsonIgnore] public long Mass => Held.MassPerUnit * Units;
    [JsonIgnore] public double Volume => Held.VolumePerUnit * Units;
}

/// <summary>
/// A sealed shipment: a container with a source and a destination that goods and crates go into.
/// While any line is unreleased the object sits in the source warehouse as one unit of cargo, with the
/// mass and volume of the lines still in it, so nothing else can take them. Releasing a line moves it
/// out of the hold into the store as itself, ready for a ship to load.
/// All of one cargo type, so it fits the one hold the lines need.
/// </summary>
public sealed class ShipmentCargo : ICargoable
{
    [JsonProperty] public int ID { get; private set; }
    [JsonProperty] public string CargoTypeID { get; private set; } = "";

    [JsonProperty] public int SourceId { get; internal set; } = -1;
    [JsonProperty] public int DestId { get; internal set; } = -1;

    /// <summary>Market path only. -1 on the non-market path.</summary>
    [JsonProperty] public int BuyerFactionId { get; internal set; } = -1;

    [JsonProperty] public int SellerFactionId { get; internal set; } = -1;

    /// <summary>Market path only. The seal filled a bid at the buyer, not an ask at the seller.</summary>
    [JsonProperty] public bool FilledBid { get; internal set; }

    /// <summary>Freight fee for this shipment. 0 on the non-market path.</summary>
    [JsonProperty] public decimal Fee { get; internal set; }

    /// <summary>The fleet order this package is held for, or empty.</summary>
    [JsonProperty] public string FleetOrder { get; internal set; } = "";

    [JsonProperty] public List<ShipmentLine> Lines { get; private set; } = new();

    [JsonIgnore] public bool IsMarket => BuyerFactionId >= 0;

    public string UniqueID => "shipment-" + ID;
    public string Name => "Shipment " + ID;

    /// <summary>Mass of the lines still in the hold.</summary>
    public long MassPerUnit
    {
        get
        {
            long mass = 0;
            foreach (var line in Lines)
                if (!line.Released)
                    mass += line.Mass;
            return mass;
        }
    }

    /// <summary>Volume of the lines still in the hold.</summary>
    public double VolumePerUnit
    {
        get
        {
            double volume = 0;
            foreach (var line in Lines)
                if (!line.Released)
                    volume += line.Volume;
            return volume;
        }
    }

    [JsonIgnore] public bool AllArrived
    {
        get
        {
            foreach (var line in Lines)
                if (!line.Arrived)
                    return false;
            return Lines.Count > 0;
        }
    }

    [JsonConstructor]
    ShipmentCargo()
    {
    }

    internal ShipmentCargo(int id, string cargoTypeId)
    {
        ID = id;
        CargoTypeID = cargoTypeId;
    }

    /// <summary>Total units across lines. Used for the intel row name.</summary>
    [JsonIgnore] public int GoodsCount => Lines.Count;
}
