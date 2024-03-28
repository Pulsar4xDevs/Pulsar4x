using System;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace GameEngine.WeaponFireControl;

public class GenericFiringWeaponsProcessor : IHotloopProcessor
{
    public void Init(Game game)
    {
        //do nothing
    }

    public void ProcessEntity(Entity entity, int deltaSeconds)
    {
        if(entity.TryGetDatablob(out GenericFiringWeaponsDB db))
            UpdateWeapons(db);
    }

    public int ProcessManager(EntityManager manager, int deltaSeconds)
    {
        var list = manager.GetAllDataBlobsOfType<GenericFiringWeaponsDB>();
        foreach(GenericFiringWeaponsDB db in list)
            UpdateWeapons(db);
        return list.Count;
    }

    private void UpdateWeapons(GenericFiringWeaponsDB db)
    {
        //fire weapons that are able.
        for (int i = 0; i < db.WpnIDs.Length; i++)
        {
            int shots = (int)(db.InternalMagQty[i] / db.AmountPerShot[i]); 
            if (shots >= db.MinShotsPerfire[i])
            {
                db.ShotsFiredThisTick[i] = shots;
                var tgt = db.FireControlStates[i].Target;
                db.FireInstructions[i].FireWeapon(db.OwningEntity, tgt, shots);
                db.InternalMagQty[i] -= shots * db.AmountPerShot[i];
                db.WeaponStates[i].InternalMagCurAmount = db.InternalMagQty[i];
            }    
        }
        
        //reload all internal magazines.
        for (int i = 0; i < db.WpnIDs.Length ; i++)
        {
            var tickReloadAmount = db.ReloadAmountsPerSec[i];
            var magQty = Math.Max(db.InternalMagQty[i] + tickReloadAmount, db.InternalMagSizes[i]);
            db.InternalMagQty[i] = magQty;
            db.WeaponStates[i].InternalMagCurAmount = magQty;
        }
        

    }

    public TimeSpan RunFrequency { get; } = TimeSpan.FromSeconds(1);
    public TimeSpan FirstRunOffset { get; } = TimeSpan.Zero;
    public Type GetParameterType { get; } = typeof(GenericFiringWeaponsDB);
}