using System;
using System.Collections.Generic;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Factions;

namespace Pulsar4X.Storage;

public static class TransferEntityFactory
{
    public static Entity CreateTransferEntity(EntityManager globalManager, int factionID, CargoTransferDataDB trasferDataDB)
    {
        List<BaseDataBlob> dblist = new();
        dblist.Add(trasferDataDB);
        var protoEntity = new ProtoEntity(dblist);
        Entity entity = globalManager.CreateAndAddEntity(protoEntity);
        entity.FactionOwnerID = factionID;
        return entity;
    }
}