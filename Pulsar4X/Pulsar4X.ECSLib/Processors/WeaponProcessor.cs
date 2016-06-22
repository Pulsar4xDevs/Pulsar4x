using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    static class WeaponProcessor
    {


        static void FireBeamWeapons(StarSystem starSys, Entity beamWeapon)
        {

            WeaponStateDB stateInfo = beamWeapon.GetDataBlob<WeaponStateDB>();
            if (stateInfo.CoolDown <= TimeSpan.FromSeconds(0) && stateInfo.Target != null)
            {
                //TODO chance to hit
                ShipDamageProcessor.OnTakingDamage(stateInfo.Target);
                stateInfo.CoolDown = TimeSpan.FromSeconds(beamWeapon.GetDataBlob<BeamWeaponAtbDB>().PowerRechargeRate);
                //starSys.SystemSubpulses.AddEntityInterupt(starSys.SystemSubpulses.SystemLocalDateTime + stateInfo.CoolDown, PulseActionEnum.SomeOtherProcessor, highLevelEntity, beamWeapon);
            }            
        }
    }



    static class TargetingProcessor
    {

        static void SetWeaponToSensor(Entity ship, ComponentInstanceInfoDB weapon, ComponentInstanceInfoDB fireControlSensor)
        {

        }

    }
}
