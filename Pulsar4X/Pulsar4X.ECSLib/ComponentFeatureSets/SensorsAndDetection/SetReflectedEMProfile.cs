using System;
using System.Diagnostics;

namespace Pulsar4X.ECSLib
{
    public class SetReflectedEMProfile// : IHotloopProcessor
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

        public static void SetEntityProfile(Entity entity, DateTime atDate)
        {
            var position = entity.GetDataBlob<PositionDB>();
            var sensorSig = entity.GetDataBlob<SensorProfileDB>();

            sensorSig.LastPositionOfReflectionSet = position.AbsolutePosition_m;
            sensorSig.LastDatetimeOfReflectionSet = atDate;

            var emmiters = entity.Manager.GetAllDataBlobsOfType<SensorProfileDB>();
            int numberOfEmmitters = emmiters.Count;
            sensorSig.ReflectedEMSpectra.Clear();

            //PercentValue reflectionPercent = 0.1f; //TODO: this should be calculated from crossSection(size), and a reflectivity value(stealth armor?/ other design factors?). 
            //var surfaceArea = sensorSig.TargetCrossSection_msq;

            double tRad = 500;
            if (entity.HasDataBlob<MassVolumeDB>())
                tRad = entity.GetDataBlob<MassVolumeDB>().RadiusInM;
            
            
            
            
            
            
            foreach (var emmissionDB in emmiters)
            {
                var emittingEntity = emmissionDB.OwningEntity;
                if (emittingEntity != entity) // don't reflect our own emmision. 
                {
                    double distance = position.GetDistanceTo_m(emittingEntity.GetDataBlob<PositionDB>());
                    if (distance < 1)
                        distance = 1;
                    
                    var drad = Math.Sin(tRad / distance);
                    var srad = Math.Sin(drad) * tRad;
                    var surfaceArea = Math.PI * srad * srad;
                    double reflectionCoefficent = surfaceArea * sensorSig.Reflectivity;
                    
                    
                    //var emmissionDB = emittingEntity.GetDataBlob<SensorProfileDB>();

                    foreach (var emitedItem in emmissionDB.EmittedEMSpectra)
                    {

                        var attenuated = SensorProcessorTools.AttenuationCalc(emitedItem.Value, distance);//per meter^2
                        var reflectedMagnatude = attenuated * reflectionCoefficent;
                        
                        
                        //debug code:
                        if (emitedItem.Value < 0)
                            throw new Exception("Source should not be less than 0");                        
                        if(attenuated > emitedItem.Value)
                            throw new Exception("Attenuated value shoudl be less than source");                       
                        if(reflectedMagnatude > emitedItem.Value)
                        {
                            var source = Stringify.Power(emitedItem.Value);
                            var reflec = Stringify.Power(reflectedMagnatude);
                            var dist = Stringify.Distance(distance);
                            var surface = Stringify.Distance(surfaceArea);
                            var dif = Stringify.Power(emitedItem.Value - reflectedMagnatude);
                            //throw new Exception("final magnitude shoudl not be more than source");
                            //TODO: there's got to be a better way of calculating this. for now I'm just going to hack it.

                            reflectedMagnatude = emitedItem.Value * sensorSig.Reflectivity;

                        }
                        if(reflectedMagnatude < 0)
                            throw new Exception("Final magnitude should not be less than 0");                          
                        
                        
                        
                        
                        
                        
                        if(reflectedMagnatude > 0.001) //ignore it if the signal is less than a watt 
                        {
                            if (sensorSig.ReflectedEMSpectra.ContainsKey(emitedItem.Key))
                            {
                                sensorSig.ReflectedEMSpectra[emitedItem.Key] = sensorSig.ReflectedEMSpectra[emitedItem.Key] + reflectedMagnatude;
                            }
                            else
                                sensorSig.ReflectedEMSpectra.Add(emitedItem.Key, reflectedMagnatude);
                        }
                            



                     
                        
                    }
                }
            }
        }
    }
}
