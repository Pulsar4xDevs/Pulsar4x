using System.Collections.Generic;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Industry;
using Pulsar4X.Storage;

namespace Pulsar4X.Interfaces
{
    public interface IConstructableDesign
    {
        ConstructableGuiHints GuiHints { get; }

        string UniqueID { get;  }
        string Name { get;  } //player defined name. ie "5t 2kn Thruster".

        bool IsValid { get; }

        Dictionary<string, long> ResourceCosts { get; }

        long IndustryPointCosts { get; }

        /// <summary>
        /// Build energy of one job item (one batch for a material), kWh. Spent on the colony grid
        /// in step with the industry points, never stockpiled or refunded. 0 builds free.
        /// </summary>
        double EnergyCost { get; }
        string IndustryTypeID { get; }
        ushort OutputAmount { get; }
        void OnConstructionComplete(Entity industryEntity, CargoStorageDB storage, string productionLine, IndustryJob batchJob, IConstructableDesign designInfo);

    }
}