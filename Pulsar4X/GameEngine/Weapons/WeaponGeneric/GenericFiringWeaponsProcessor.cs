using System;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Damage;
using Pulsar4X.Engine.Orders;
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
        if(entity.TryGetDatablob<GenericFiringWeaponsDB>(out var db))
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
            if (shots >= db.MinShotsPerfire[i] && db.OwningEntity != null)
            {
                db.ShotsFiredThisTick[i] = shots;
                var tgt = db.FireControlStates[i].Target;
                if(tgt.IsValid)
                {
                    db.FireInstructions[i].FireWeapon(db.OwningEntity, tgt, shots);
                    db.InternalMagQty[i] -= shots * db.AmountPerShot[i];
                    db.WeaponStates[i].InternalMagCurAmount = db.InternalMagQty[i];
                }
                else
                {
                    // If we encounter an invalid target check to see if any valid targets exist
                    ValidateTargetExists(db, db.FireControlStates);
                }
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

    /// <summary>
    /// Check if each of the FireControlAbilityStates have a valid target,
    /// if not issue the cease firing command for that fire control.
    /// </summary>
    ///<param name="genericFiringWeaponsDB"></param>
    /// <param name="fireControlAbilityStates"></param>
    private void ValidateTargetExists(GenericFiringWeaponsDB genericFiringWeaponsDB, FireControlAbilityState[] fireControlAbilityStates)
    {
        for(int i  = 0; i < fireControlAbilityStates.Length; i++)
        {
            if(!fireControlAbilityStates[i].Target.IsValid)
            {
                SetOpenFireControlOrder.CreateCmd(
                    genericFiringWeaponsDB.OwningEntity.Manager.Game,
                    genericFiringWeaponsDB.OwningEntity.FactionOwnerID,
                    genericFiringWeaponsDB.OwningEntity.Id,
                    fireControlAbilityStates[i].ID,
                    SetOpenFireControlOrder.FireModes.CeaseFire);
            }
        }
    }

    public TimeSpan RunFrequency { get; } = TimeSpan.FromSeconds(1);
    public TimeSpan FirstRunOffset { get; } = TimeSpan.Zero;
    public Type GetParameterType { get; } = typeof(GenericFiringWeaponsDB);
}