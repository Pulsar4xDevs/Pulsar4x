using System;
using GameEngine.People;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Colonies
{
    /// <summary>
    /// Processor that updates colony hex maps based on administration building sizes
    /// </summary>
    public class ColonyHexMapProcessor : IInstanceProcessor
    {
        internal override void ProcessEntity(Entity entity, DateTime atDateTime)
        {
            // Only process entities that are colonies with admin space
            if (!entity.HasDataBlob<ColonyInfoDB>() || !entity.HasDataBlob<AdminSpaceDB>())
                return;

            UpdateColonyHexMap(entity);
        }

        /// <summary>
        /// Update the hex map size for a colony based on its administration buildings
        /// </summary>
        public static void UpdateColonyHexMap(Entity colonyEntity)
        {
            if (!colonyEntity.HasDataBlob<ColonyInfoDB>())
                return;

            // Get or create hex map DataBlob
            if (!colonyEntity.TryGetDataBlob<ColonyHexMapDB>(out var hexMapDB))
            {
                hexMapDB = new ColonyHexMapDB();
                colonyEntity.SetDataBlob(hexMapDB);
            }

            // Calculate total office space from all admin buildings
            int totalOfficeSpace = CalculateTotalOfficeSpace(colonyEntity);

            // Update hex map size based on office space
            if (totalOfficeSpace > 0)
            {
                hexMapDB.UpdateMaxRadius(totalOfficeSpace);
            }
        }

        /// <summary>
        /// Calculate total office space from all administration buildings in the colony
        /// </summary>
        private static int CalculateTotalOfficeSpace(Entity colonyEntity)
        {
            int totalOfficeSpace = 0;

            if (colonyEntity.TryGetDataBlob<ComponentInstancesDB>(out var instancesDB))
            {
                // Look for all admin buildings (components with AdminSpaceAtb)
                if (instancesDB.TryGetComponentsByAttribute<AdminSpaceAtb>(out var adminComponents))
                {
                    foreach (var component in adminComponents)
                    {
                        // Get from attribute directly
                        var attributes = component.GetAttributes();
                        if (attributes.TryGetValue(typeof(AdminSpaceAtb), out var adminSpaceAtb))
                        {
                            var atb = (AdminSpaceAtb)adminSpaceAtb;
                            totalOfficeSpace += atb.ConsoleSpace;
                        }
                    }
                }
            }

            return totalOfficeSpace;
        }

        /// <summary>
        /// Force update hex map for a specific colony (useful when admin buildings change)
        /// </summary>
        public static void ForceUpdateColonyHexMap(Entity colonyEntity)
        {
            UpdateColonyHexMap(colonyEntity);
        }
    }
}