using Pulsar4X.Engine.Designs;
using Pulsar4X.Engine;
using System.Diagnostics.CodeAnalysis;

namespace Pulsar4X.Interfaces
{
    public interface IFireWeaponInstr
    {
        public bool CanLoadOrdnance(OrdnanceDesign ordnanceDesign);
        public bool AssignOrdnance(OrdnanceDesign ordnanceDesign);

        public bool TryGetOrdnance([NotNullWhen(true)] out OrdnanceDesign? ordnanceDesign);
        public void FireWeapon(Entity launchingEntity, Entity tgtEntity, int count);

        public float ToHitChance(Entity launchingEntity, Entity tgtEntity);

    }
}