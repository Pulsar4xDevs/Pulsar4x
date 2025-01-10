using Pulsar4X.Engine;
using System.Diagnostics.CodeAnalysis;

namespace Pulsar4X.Weapons
{
    public interface IFireWeaponInstr
    {
        public string WeaponType { get; }
        
        public void SetWeaponState(WeaponState state);
        
        public bool CanLoadOrdnance(OrdnanceDesign ordnanceDesign);
        public bool AssignOrdnance(OrdnanceDesign ordnanceDesign);

        public bool TryGetOrdnance([NotNullWhen(true)] out OrdnanceDesign? ordnanceDesign);
        public void FireWeapon(Entity launchingEntity, Entity tgtEntity, int count);

        public float ToHitChance(Entity launchingEntity, Entity tgtEntity);

    }
}