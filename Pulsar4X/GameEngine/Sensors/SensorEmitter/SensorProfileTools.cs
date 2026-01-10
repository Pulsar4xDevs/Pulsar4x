using System;
using System.Collections.Generic;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Galaxy;
using Pulsar4X.Movement;

namespace Pulsar4X.Sensors;

public static class SensorProfileTools
{

    public static void SetProfileDB(Entity parentEntity)
    {
        
        if (!parentEntity.TryGetDataBlob<SensorProfileDB>(out var sensorProfileDB))
        {
            sensorProfileDB = new SensorProfileDB();
            parentEntity.SetDataBlob<SensorProfileDB>(sensorProfileDB);
        }
        ComponentInstancesDB components = parentEntity.GetDataBlob<ComponentInstancesDB>();
        sensorProfileDB.EmittedEMSpectra.Clear();
        
        if (components.TryGetComponentsByAttribute<SensorSignatureAtb>(out var componentInstances))
        {
            foreach (var instance in componentInstances)
            {
                var atts = instance.GetAttributes();
                var sensorAtb = (SensorSignatureAtb)atts[typeof(SensorSignatureAtb)];
                var partWaveForm = sensorAtb.PartWaveForm;
                var partWaveFormMag = sensorAtb.PartWaveFormMag;
                
                var emdata = new EMData()
                {
                    SourceEntity = parentEntity,
                    Instance = instance, 
                    WaveForm = partWaveForm,
                    Magnitude = partWaveFormMag,
                };

                sensorProfileDB.EmittedEMSpectra.Add(emdata);
            }
        }
    }

    public static void UpdateReflectionProfile(SensorProfileDB sensorProfileDB, DateTime atDateTime)
    {
        var entity = sensorProfileDB.OwningEntity;
        var position = sensorProfileDB.OwningEntity.GetDataBlob<PositionDB>();

        sensorProfileDB.LastPositionOfReflectionSet = position.AbsolutePosition;
        sensorProfileDB.LastDatetimeOfReflectionSet = atDateTime;
        sensorProfileDB.ReflectedEMSpectra.Clear();
        
        var profiles = entity.Manager.GetAllDataBlobsOfType<SensorProfileDB>();
        
        foreach (var profileDB in profiles)
        {
            var emittingEntity = profileDB.OwningEntity;

            // onlyl reflect valid entities and not ourself
            if(emittingEntity == Entity.InvalidEntity || emittingEntity == entity)
                continue;

            double distance = position.GetDistanceTo_m(emittingEntity.GetDataBlob<PositionDB>());
            if (distance < 1)
                distance = 1;
            profileDB.ReflectedEMSpectra.Clear();
            foreach (var emitedItem in profileDB.EmittedEMSpectra)
            {
                //TODO: we're ignoring anything under a petawatt(pre attenuated) for reflection.
                //we may have to balance this later, maybe add a flag in the emmissionDB or a seperate dictionary for stuff that should be reflected.
                //picking up ALL emmisions for reflection is probabily overkill/too much ui data/too much processing.
                if(emitedItem.Magnitude < 1e+12)
                    continue;
                
                var attenuated = SensorTools.AttenuationCalc(emitedItem.Magnitude, distance);//per meter^2
                var reflectedMagnatude = profileDB.ReflectionCoefficent * attenuated;
                
                //debug code:
                if (emitedItem.Magnitude < 0)
                    throw new Exception("Source should not be less than 0");
                if(attenuated > emitedItem.Magnitude)
                    throw new Exception("Attenuated value shoudl be less than source");
                if(reflectedMagnatude > emitedItem.Magnitude)
                {
                    // var source = Stringify.Power(emitedItem.Value);
                    // var reflec = Stringify.Power(reflectedMagnatude);
                    // var dist = Stringify.Distance(distance);
                    // var surface = Stringify.Distance(surfaceArea);
                    // var dif = Stringify.Power(emitedItem.Value - reflectedMagnatude);
                    //throw new Exception("final magnitude shoudl not be more than source");
                    //TODO: there's got to be a better way of calculating this. for now I'm just going to hack it.

                    reflectedMagnatude = emitedItem.Magnitude * sensorProfileDB.Reflectivity;

                }
                if(reflectedMagnatude < 0)
                    throw new Exception("Final magnitude should not be less than 0");

                if(reflectedMagnatude > 0.001) //ignore it if the signal is less than a watt
                {
                    var emdata = new EMData()
                    {
                        SourceEntity = emittingEntity,
                        Instance = emitedItem.Instance,
                        WaveForm = emitedItem.WaveForm,
                        Magnitude = reflectedMagnatude,
                    };
                    sensorProfileDB.ReflectedEMSpectra.Add(emdata);
                }
            }
        }
    }
}