using System;
using System.Collections.Generic;


namespace Pulsar4X.ECSLib
{
    public static class EngineFactory
    {
        public static Entity Create(EntityManager systemEntityManager, int enginePower, double fuelPerHour, int thermalSig)
        {
            EnginePowerDB drivePower = new EnginePowerDB(enginePower);
            FuelUseDB fuelUse = new FuelUseDB(fuelPerHour);
            SensorSignatureDB sensorSig = new SensorSignatureDB(thermalSig, 0);

            Entity engine = new Entity(systemEntityManager);
            engine.SetDataBlob(drivePower);
            engine.SetDataBlob(fuelUse);
            engine.SetDataBlob(sensorSig);

            return engine;

        }
    }

}