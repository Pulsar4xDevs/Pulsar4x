using System;

namespace Pulsar4X.ECSLib
{
    public static class EngineFactory
    {
        //this should be more generic, and take a json file I think. 
        //idealy we want to define a component compleatly from mod/json files.
        public static Entity CreateEngineComponent(EntityManager systemEntityManager, int size, int htk, JDictionary<Guid,int> costs, Guid techreq, int crew, int enginePower, double fuelPerHour, int thermalSig)
        {
            ComponentInfoDB genericInfo = new ComponentInfoDB(size, htk, costs, techreq, crew);
            
            EnginePowerDB drivePower = new EnginePowerDB(enginePower);
            FuelUseDB fuelUse = new FuelUseDB(fuelPerHour);
            SensorSignatureDB sensorSig = new SensorSignatureDB(thermalSig, 0);

            genericInfo.StatRecalcDelegate = new StatRecalc(EnginePowerProcessor.CalcMaxSpeed);
            
            Entity engine = new Entity(systemEntityManager);
            engine.SetDataBlob(genericInfo);
            engine.SetDataBlob(drivePower);
            engine.SetDataBlob(fuelUse);
            engine.SetDataBlob(sensorSig);

            return engine;

        }



        #region need to find a way to jsonise this:
        public static void EngineRules()
        {
            int engineSizeinTons = 1; //from Engine Design UI (player input)
            double powerMultiplier = 1;  //from Engine Design UI (player input)
            int baseEnginePowerVsSize = 5; //from research/tech - maybe techSD should have an int Level; 
            int basefuelConsumption = 1; //from research/tech - maybe techSD should have an int Level; 
            int thermalMod = 1; //from research/tech & Engine Design UI (player input) - maybe techSD should have an int Level; 
            double consumptionPerHour = basefuelConsumption * EnginePowerEfficencyFunc(powerMultiplier); 
            consumptionPerHour -= consumptionPerHour * SizeConsumptionModFunc(engineSizeinTons);

            int totalPower = baseEnginePowerVsSize * engineSizeinTons;

            int thermalSig = ThermalSigFunc(totalPower, thermalMod);

            int hitTokill = HitToKillFunc(engineSizeinTons);

            JDictionary<Guid, int> costs = Cost(engineSizeinTons);

            int crew = CrewReq(engineSizeinTons);
            
            Guid tech = new Guid();

            CreateEngineComponent(null,engineSizeinTons, hitTokill, costs, tech, crew ,totalPower, consumptionPerHour, thermalSig);
        }

        //how do we jsonise math, especialy when we've got variables like power, size, etc etc.

        //I think this changes with thermal mod as well...
        private static JDictionary<Guid, int> Cost(int size)
        {
            JDictionary<Guid,int> costs = new JDictionary<Guid, int>();
            Guid gallicite = new Guid("2d4b2866-aa4a-4b9a-b8aa-755fe509c0b3"); //Gallicite.
            costs.Add(gallicite, (int)(size * 0.016));
            return costs;
        }
        private static int CrewReq(int size)
        {
            return 8 * size;
        }

        private static double EnginePowerEfficencyFunc(double power)
        {
            double consumption = Math.Pow(power, 2.25);
            return consumption;
        }

        private static double SizeConsumptionModFunc(int size)
        {
            double consumption = size * 0.002;
            return consumption;
        }

        private static int ThermalSigFunc(int power, int thermalModifier)
        {
            return power * thermalModifier;
        }

        private static int HitToKillFunc(int size)
        {
            return Math.Min(1, size / 25);
        }

        #endregion
    }

}