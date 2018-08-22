using System;
using System.Diagnostics;

namespace Pulsar4X.ECSLib
{
    internal class SetReflectedEMProfile// : IHotloopProcessor
    {
        /*
        public TimeSpan RunFrequency => TimeSpan.FromMinutes(60);

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            SetEntityProfile(entity);
        }

        public void ProcessManager(EntityManager manager, int deltaSeconds)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            var entites = manager.GetAllEntitiesWithDataBlob<SensorProfileDB>();
            foreach (var entity in entites)
            {
                ProcessEntity(entity, deltaSeconds);
            }
            var ms = timer.ElapsedMilliseconds;
            var numEntites = entites.Count;
        }
        */

        internal static void SetEntityProfile(Entity entity, DateTime atDate)
        {
            var position = entity.GetDataBlob<PositionDB>();
            var sensorSig = entity.GetDataBlob<SensorProfileDB>();

            sensorSig.LastPositionOfReflectionSet = position.AbsolutePosition_AU;
            sensorSig.LastDatetimeOfReflectionSet = atDate;

            var emmiters = entity.Manager.GetAllEntitiesWithDataBlob<SensorProfileDB>();
            int numberOfEmmitters = emmiters.Count;
            sensorSig.ReflectedEMSpectra.Clear();

            PercentValue reflectionPercent = 0.1f; //TODO: this should be calculated from crossSection(size), distance, and a reflectivity value(stealth armor?/ other design factors?). 

            foreach (var emittingEntity in emmiters)
            {
                if (emittingEntity != entity) // don't reflect our own emmision. 
                {
                    double distance = PositionDB.GetDistanceBetween(position, emittingEntity.GetDataBlob<PositionDB>());
                    var emmissionDB = emittingEntity.GetDataBlob<SensorProfileDB>();

                    foreach (var emitedItem in emmissionDB.EmittedEMSpectra)
                    {

                        var reflectedMagnatude = SensorProcessorTools.AttenuationCalc(emitedItem.Value, distance) * reflectionPercent;
                        
                        sensorSig.ReflectedEMSpectra.Add(emitedItem.Key, emitedItem.Value);
                    }
                }
            }
        }
    }
}
