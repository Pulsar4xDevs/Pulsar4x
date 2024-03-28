using System;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Sensors;

namespace Pulsar4X.Extensions;

public static class SensorProfileDBExtensions
{
    public static void SetReflectionProfile(this SensorProfileDB sensorProfileDB, DateTime atDateTime)
    {
        var entity = sensorProfileDB.OwningEntity;
        var position = sensorProfileDB.OwningEntity.GetDataBlob<PositionDB>();

        sensorProfileDB.LastPositionOfReflectionSet = position.AbsolutePosition;
        sensorProfileDB.LastDatetimeOfReflectionSet = atDateTime;
        sensorProfileDB.ReflectedEMSpectra.Clear();

        //PercentValue reflectionPercent = 0.1f; //TODO: this should be calculated from crossSection(size), and a reflectivity value(stealth armor?/ other design factors?).
        //var surfaceArea = sensorSig.TargetCrossSection_msq;

        double tRad = 500;
        if (entity.HasDataBlob<MassVolumeDB>())
            tRad = entity.GetDataBlob<MassVolumeDB>().RadiusInM;

        var emmiters = entity.Manager.GetAllDataBlobsOfType<SensorProfileDB>();
        int numberOfEmmitters = emmiters.Count;
        foreach (var emmissionDB in emmiters)
        {
            var emittingEntity = emmissionDB.OwningEntity;

            // onlyl reflect valid entities and not ourself
            if(emittingEntity == Entity.InvalidEntity || emittingEntity == entity)
                continue;

            double distance = position.GetDistanceTo_m(emittingEntity.GetDataBlob<PositionDB>());
            if (distance < 1)
                distance = 1;

            var drad = Math.Sin(tRad / distance);
            var srad = Math.Sin(drad) * tRad;
            var surfaceArea = Math.PI * srad * srad;
            double reflectionCoefficent = surfaceArea * sensorProfileDB.Reflectivity;

            foreach (var emitedItem in emmissionDB.EmittedEMSpectra)
            {

                var attenuated = SensorTools.AttenuationCalc(emitedItem.Value, distance);//per meter^2
                var reflectedMagnatude = attenuated * reflectionCoefficent;


                //debug code:
                if (emitedItem.Value < 0)
                    throw new Exception("Source should not be less than 0");
                if(attenuated > emitedItem.Value)
                    throw new Exception("Attenuated value shoudl be less than source");
                if(reflectedMagnatude > emitedItem.Value)
                {
                    // var source = Stringify.Power(emitedItem.Value);
                    // var reflec = Stringify.Power(reflectedMagnatude);
                    // var dist = Stringify.Distance(distance);
                    // var surface = Stringify.Distance(surfaceArea);
                    // var dif = Stringify.Power(emitedItem.Value - reflectedMagnatude);
                    //throw new Exception("final magnitude shoudl not be more than source");
                    //TODO: there's got to be a better way of calculating this. for now I'm just going to hack it.

                    reflectedMagnatude = emitedItem.Value * sensorProfileDB.Reflectivity;

                }
                if(reflectedMagnatude < 0)
                    throw new Exception("Final magnitude should not be less than 0");

                if(reflectedMagnatude > 0.001) //ignore it if the signal is less than a watt
                {
                    if (sensorProfileDB.ReflectedEMSpectra.ContainsKey(emitedItem.Key))
                    {
                        sensorProfileDB.ReflectedEMSpectra[emitedItem.Key] = sensorProfileDB.ReflectedEMSpectra[emitedItem.Key] + reflectedMagnatude;
                    }
                    else
                        sensorProfileDB.ReflectedEMSpectra.Add(emitedItem.Key, reflectedMagnatude);
                }
            }
        }
    }
}