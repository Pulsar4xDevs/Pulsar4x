using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Contains info on the ships engines and fuel reserves.
    /// </summary>
    public class PropulsionAbilityDB : BaseDataBlob, ICreateViewmodel
    {
        //Non Newtonion:
        public int MaximumSpeed_MS { get; set; }
        public Vector4 CurrentVectorMS { get; set; }
        public int TotalEnginePower { get; set; }
        public Dictionary<Guid, double> FuelUsePerKM { get; internal set; } = new Dictionary<Guid, double>();


        //Newtonion
        public float TotalDV_MS { get; set; } = 10000;
        public float RemainingDV_MS { get; set; } = 10000;

        
        public PropulsionAbilityDB()
        {
        }

        public PropulsionAbilityDB(PropulsionAbilityDB propulsionDB)
        {
            MaximumSpeed_MS = propulsionDB.MaximumSpeed_MS;
            CurrentVectorMS = propulsionDB.CurrentVectorMS;
            TotalEnginePower = propulsionDB.TotalEnginePower;
            FuelUsePerKM = new Dictionary<Guid, double>(propulsionDB.FuelUsePerKM);
        }

        public override object Clone()
        {
            return new PropulsionAbilityDB(this);
        }

        public IDBViewmodel CreateVM(Game game, CommandReferences cmdRef)
        {
            return new TranslationMoveVM(game, cmdRef, this.OwningEntity);
        }
    }
}