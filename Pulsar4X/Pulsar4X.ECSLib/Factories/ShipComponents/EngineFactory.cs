using System;
using System.Collections.Generic;
using System.Security.Policy;

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

        /// <summary>
        /// this needs to be an generic SD.
        /// </summary>
        /// <param name="factionTechDB"></param>
        /// <param name="staticData"></param>
        public static void DataForUI(TechDB factionTechDB, StaticDataStore staticData)
        {
            //gui selection from list.
            List<TechSD> engineTechSDs = new List<TechSD>();
            List<Guid> engineTechGuids = new List<Guid>()
            {
               new Guid("35608fe6-0d65-4a5f-b452-78a3e5e6ce2c"),
               new Guid("c827d369-3f16-43ef-b112-7d5bcafb74c7"),
               new Guid("db6818f3-99e9-46c1-b903-f3af978c38b2"),
               new Guid("f3f10e56-9345-40cc-af42-342e7240355d"),
               new Guid("58d047e6-c567-4db6-8c76-bfd4a201af94"),
               new Guid("bd75bf88-1dad-4022-b401-acdf05ab73f8"),
               new Guid("042ce9d4-5a2c-4d8e-9ae4-be059920839c"),
               new Guid("93611831-9183-484a-9920-13b39d64e272"),
               new Guid("32eda0ab-c117-4224-b148-6c9d0e474296"),
               new Guid("cbb1a7ce-3c26-4b5b-abd7-9a99c670d68d"),
               new Guid("6e34cc46-0693-4676-b0ca-f076fb36acaf"),
               new Guid("9bb4d1c4-680f-4c98-b927-337654073575"),
               new Guid("c9587310-f7dd-45d0-ac4c-b6f59a1e1897")
            };

            foreach (var techGuid in engineTechGuids)
            {
                if(factionTechDB.ResearchedTechs.ContainsKey(techGuid))
                    engineTechSDs.Add(staticData.Techs[techGuid]);
            }

            //gui select number between max and min.
            double componentSizeMin = 1;
            double componentSizeMax = 50;

            //gui select number between max and min (power vs efficency mod) slider?
            Guid maxpowertech = new Guid("b8ef73c7-2ef0-445e-8461-1e0508958a0e");
            TechSD maxpowerSD = staticData.Techs[maxpowertech];
            double maxEnginePowerMod = TechProcessor.ExpresionDataEval(factionTechDB, maxpowerSD);

            Guid minpowertech = new Guid("08fa4c4b-0ddb-4b3a-9190-724d715694de");
            TechSD minpowerSD = staticData.Techs[minpowertech];
            double minEnginePowerMod = TechProcessor.ExpresionDataEval(factionTechDB, minpowerSD);

            //gui max level. need not be editible. 
            Guid baseFuelConsumption = new Guid("8557acb9-c764-44e7-8ee4-db2c2cebf0bc");
            TechSD baseFuelConsumptionSD = staticData.Techs[baseFuelConsumption];
            double fuelConsumptionBase = TechProcessor.ExpresionDataEval(factionTechDB, baseFuelConsumptionSD);

            //gui select from 0 to max
            Guid thermalReduction = new Guid("8557acb9-c764-44e7-8ee4-db2c2cebf0bc");
            TechSD thermalReductionSD = staticData.Techs[thermalReduction];
            double thermalReductionMod = TechProcessor.ExpresionDataEval(factionTechDB, thermalReductionSD);

        }

        public static void EngineRules(TechDB factionTechDB)
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



        public static void engineasComponentSD()
        {
            ComponentSD2 component = new ComponentSD2();
            component.Name = "Engine";
            component.Description = "Moves a ship";
            component.ID = new Guid();

            component.SizeFormula = "[GUIMinMax] [Min]1 [Max]50";
            component.HTKFormula = "Min(1, [Size] / 25)";
            component.CrewSizeFormula = "[Size] * 8";
            component.CostFormula = new JDictionary<Guid, string>();
            component.CostFormula.Add(new Guid("2d4b2866-aa4a-4b9a-b8aa-755fe509c0b3"), "[Size] * 0.016");

            component.ComponentAbilitySDs = new List<ComponentAbilitySD2>();

            ComponentAbilitySD2 engineTypeAbility = new ComponentAbilitySD2();
            engineTypeAbility.Name = "Engine Type";
            engineTypeAbility.Description = "Type of engine Tech";
            engineTypeAbility.AbilityDataBlob = "";
            engineTypeAbility.AbilityFormula = "[GUIListSelection] " + 
                "Guid(35608fe6-0d65-4a5f-b452-78a3e5e6ce2c)," + 
                "Guid(c827d369-3f16-43ef-b112-7d5bcafb74c7)," + 
                "Guid(db6818f3-99e9-46c1-b903-f3af978c38b2)," + 
                "Guid(f3f10e56-9345-40cc-af42-342e7240355d)," + 
                "Guid(58d047e6-c567-4db6-8c76-bfd4a201af94)," + 
                "Guid(bd75bf88-1dad-4022-b401-acdf05ab73f8)," + 
                "Guid(042ce9d4-5a2c-4d8e-9ae4-be059920839c)," + 
                "Guid(93611831-9183-484a-9920-13b39d64e272)," + 
                "Guid(32eda0ab-c117-4224-b148-6c9d0e474296)," + 
                "Guid(cbb1a7ce-3c26-4b5b-abd7-9a99c670d68d)," + 
                "Guid(6e34cc46-0693-4676-b0ca-f076fb36acaf)," + 
                "Guid(9bb4d1c4-680f-4c98-b927-337654073575)," + 
                "Guid(c9587310-f7dd-45d0-ac4c-b6f59a1e1897)";

            ComponentAbilitySD2 enginePowerEfficency = new ComponentAbilitySD2();
            enginePowerEfficency.Name = "Engine Consumption vs Power";
            enginePowerEfficency.Description = "More Powerfull engines are less efficent for a given size";
            enginePowerEfficency.AbilityDataBlob = "";
            enginePowerEfficency.AbilityFormula = "[GUIMinMax] [Min][Tech]Guid(08fa4c4b-0ddb-4b3a-9190-724d715694de [Max][Tech]Guid(b8ef73c7-2ef0-445e-8461-1e0508958a0e) ";

            ComponentAbilitySD2 enginePowerAbility = new ComponentAbilitySD2();
            enginePowerAbility.Name = "Engine Power";
            enginePowerAbility.Description = "Move Power for ship";
            enginePowerAbility.AbilityDataBlob = "EnginePowerDB";
            enginePowerAbility.AbilityFormula = "[Ability]0.ExpressionData * [Size]";

            ComponentAbilitySD2 fuelConsumptionBase = new ComponentAbilitySD2();
            enginePowerAbility.Name = "Fuel Consumption";
            enginePowerAbility.Description = "From Tech";
            enginePowerAbility.AbilityDataBlob = "";
            enginePowerAbility.AbilityFormula = "[Tech]Guid(8557acb9-c764-44e7-8ee4-db2c2cebf0bc) * Pow([Ability]2, 2.25)";

            ComponentAbilitySD2 fuelConsumptionSizeMod = new ComponentAbilitySD2();
            enginePowerAbility.Name = "Fuel Consumption";
            enginePowerAbility.Description = "Size Mod";
            enginePowerAbility.AbilityDataBlob = "FuelUseDB";
            enginePowerAbility.AbilityFormula = "[Ability]2 - [Ability]2 * [Size] * 0.002";

        }
    }

}