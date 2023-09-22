using System;
using System.Collections.Generic;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Industry;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Interfaces
{
    public interface IConstrucableDesign
    {
        ConstructableGuiHints GuiHints { get; }

        string UniqueID { get;  }
        string Name { get;  } //player defined name. ie "5t 2kn Thruster".

        bool IsValid { get; }

        Dictionary<string, long> ResourceCosts { get; }

        long IndustryPointCosts { get; }
        string IndustryTypeID { get; }
        ushort OutputAmount { get; }
        void OnConstructionComplete(Entity industryEntity, VolumeStorageDB storage, Guid productionLine, IndustryJob batchJob, IConstrucableDesign designInfo);

    }
}