using System.Linq;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;

namespace Pulsar4X.Damage;

public class SimpleDamage
{
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