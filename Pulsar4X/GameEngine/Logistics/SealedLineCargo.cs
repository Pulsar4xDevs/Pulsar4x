using Newtonsoft.Json;
using Pulsar4X.Storage;

namespace Pulsar4X.Logistics;

/// <summary>
/// One resource line of a sealed shipment, once it is out of the hold and travelling as itself.
/// It copies the cargo type, mass and volume from the resource, so a ship's hold treats it like the
/// open goods. It has an id of its own, so it never merges with open stock of the same resource, and
/// it keeps the resource id and the shipment id so the destination can unpack it.
/// </summary>
public sealed class SealedLineCargo : ICargoable
{
    [JsonProperty] public int ID { get; private set; }

    /// <summary>The resource this line holds, by library id.</summary>
    [JsonProperty] public int ResourceId { get; private set; }

    [JsonProperty] public string ResourceUniqueId { get; private set; } = "";

    /// <summary>The shipment this line belongs to, by <see cref="ShipmentCargo.ID"/>.</summary>
    [JsonProperty] public int ShipmentId { get; private set; }

    [JsonProperty] public string CargoTypeID { get; private set; } = "";
    [JsonProperty] public string Name { get; private set; } = "";
    [JsonProperty] public long MassPerUnit { get; private set; }
    [JsonProperty] public double VolumePerUnit { get; private set; }

    /// <summary>The open resource the line unpacks to. The faction's own library object.</summary>
    [JsonProperty] public ICargoable Resource { get; private set; } = null!;

    public string UniqueID => "sealed-" + ID;

    [JsonConstructor]
    SealedLineCargo()
    {
    }

    internal SealedLineCargo(int id, int shipmentId, ICargoable resource)
    {
        ID = id;
        ShipmentId = shipmentId;
        Resource = resource;
        ResourceId = resource.ID;
        ResourceUniqueId = resource.UniqueID;
        CargoTypeID = resource.CargoTypeID;
        MassPerUnit = resource.MassPerUnit;
        VolumePerUnit = resource.VolumePerUnit;
        Name = "Sealed " + resource.Name;
    }
}
