using System.Linq;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;

namespace Pulsar4X.Damage;

public class SimpleDamage
{
    /// <summary>
    /// Deals damage to the specified entity
    /// </summary>
    /// <param name="entityToDamage">The entity to damage</param>
    /// <param name="damageMin">Inclusive minimum</param>
    /// <param name="damageMax">Exclusive maximum</param>
    /// <returns>Returns true if the entity was destroyed.</returns>
    public static bool OnTakingDamage(Entity entityToDamage, int damageMin, int damageMax)
    {
        if(entityToDamage.TryGetDatablob<ComponentInstancesDB>(out var componentInstancesDB)
            && componentInstancesDB.AllComponents.Count > 0)
        {
            var rng = entityToDamage.Manager.Game.RNG;
            var components = componentInstancesDB.AllComponents.Values.ToList();
            var damagedIndex = rng.Next(components.Count);
            var damage = rng.Next(damageMin, damageMax);

            components[damagedIndex].HTKRemaining -= damage;

            if(components[damagedIndex].HTKRemaining <= 0)
            {
                componentInstancesDB.RemoveComponentInstance(components[damagedIndex]);
            }

            // Check if the entity should be removed
            if(componentInstancesDB.AllComponents.Count <= 0)
            {
                entityToDamage.Destroy();
                return true;
            }
        }

        return false;
    }
}