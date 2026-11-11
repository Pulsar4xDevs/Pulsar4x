using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Blueprints;

namespace Pulsar4X.Modding
{
    /// <summary>
    /// Checks the loaded blueprints for references to ids that are not loaded: the component and ship
    /// design lists on a colony or faction start, and the component and armor ids on a ship design.
    /// </summary>
    public static class ModDataReferences
    {
        /// <summary>Every missing reference in the store. Empty when the data is sound.</summary>
        public static List<ModLoadError> FindMissing(ModDataStore data)
        {
            var errors = new List<ModLoadError>();

            foreach (var colony in data.Colonies.Values)
            {
                AddMissing(errors, colony, nameof(ColonyBlueprint), colony.ComponentDesigns,
                    nameof(ColonyBlueprint.ComponentDesigns), id => data.ComponentDesigns.ContainsKey(id));
                AddMissing(errors, colony, nameof(ColonyBlueprint), colony.ShipDesigns,
                    nameof(ColonyBlueprint.ShipDesigns), id => data.ShipDesigns.ContainsKey(id));
            }

            foreach (var faction in data.Factions.Values)
            {
                AddMissing(errors, faction, nameof(FactionBlueprint), faction.ComponentDesigns,
                    nameof(FactionBlueprint.ComponentDesigns), id => data.ComponentDesigns.ContainsKey(id));
                AddMissing(errors, faction, nameof(FactionBlueprint), faction.ShipDesigns,
                    nameof(FactionBlueprint.ShipDesigns), id => data.ShipDesigns.ContainsKey(id));
            }

            foreach (var shipDesign in data.ShipDesigns.Values)
            {
                foreach (var component in shipDesign.Components ?? new List<ShipDesignBlueprint.ShipComponentBlueprint>())
                {
                    if (!data.ComponentDesigns.ContainsKey(component.Id))
                    {
                        errors.Add(new ModLoadError(
                            shipDesign.ModNamespace,
                            shipDesign.JsonFileName,
                            nameof(ShipDesignBlueprint),
                            shipDesign.UniqueID,
                            nameof(ShipDesignBlueprint.Components),
                            component.Id));
                    }
                }

                string armorId = shipDesign.Armor.Id;
                if (!string.IsNullOrEmpty(armorId) && !data.Armor.ContainsKey(armorId))
                {
                    errors.Add(new ModLoadError(
                        shipDesign.ModNamespace,
                        shipDesign.JsonFileName,
                        nameof(ShipDesignBlueprint),
                        shipDesign.UniqueID,
                        "Armor.Id",
                        armorId));
                }
            }

            return errors;
        }

        private static void AddMissing(
            List<ModLoadError> errors,
            Blueprint blueprint,
            string objectType,
            List<string>? ids,
            string field,
            Func<string, bool> isLoaded)
        {
            foreach (var id in ids ?? new List<string>())
            {
                if (!isLoaded(id))
                    errors.Add(new ModLoadError(blueprint.ModNamespace, blueprint.JsonFileName, objectType, blueprint.UniqueID, field, id));
            }
        }
    }
}
