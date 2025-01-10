using System;
using System.Collections.Generic;
using System.Numerics;
using Pulsar4X.Sensors;


namespace GameEngine.Damage;

public static class PhotonMath
{
    public static void ResolveCollision(PhotonParticle laser, PhysicalParticle target, DamageMap map)
    {
        var material = target.MatType;
        // 1. Calculate photon's energy transfer to the target
        var photonInteraction = CalculatePhotonInteraction(laser.WaveLength, material);    
        
        // 2. Add absorbed energy to the target's temperature
        // Energy absorbed converted to temperature using the material's thermal capacity
        //* 10e6 because laser power is in MW.
        float tempIncrease = (float)(laser.Power * 10e6 * photonInteraction.absorbed) / (target.Mass * material.ThermalCapacity);
        target.Temperature += tempIncrease;
        
        
        // 3. Handle the photon based on interaction results
        if (photonInteraction.absorbed == 1.0f) // Fully absorbed
        {
            // Absorb photon completely (remove it from the map)
            laser.IsDeleted = true;
            map.PhMap[laser.mapIndex] = null; // Remove photon from PhMap
            laser.IsDeleted = true;
        }
        else
        {
            // Partial absorption; handle reflection and transmission
            if(photonInteraction.transmitted > 0.0f)
            {
                laser.Power *= photonInteraction.transmitted;
                if (photonInteraction.reflected > 0.0f)
                {
                    var newVector = Vector2.Reflect(laser.Velocity, target.Position - laser.Position);
                    PhotonParticle.CloneWithNewVelocityAndPower(laser, newVector, laser.Power * photonInteraction.reflected);
                }
            }
            else
            {
                laser.Power *= photonInteraction.reflected;
                laser.Velocity = Vector2.Reflect(laser.Velocity, target.Position - laser.Position);
            }
        }
    }

    private static (float reflected, float transmitted, float absorbed) CalculatePhotonInteraction(
        float wavelength,
        ParticleMaterial material
    )
    {
        var reflectivityWaveform = material.PhotonReflectivity;
        var reflectivityPeak = material.PhotonReflectivityPeak;
        var transparencyWaveform = material.PhotonTransparency;
        var transparencyPeak = material.PhotonTransperencyPeak;
        // Calculate reflectivity and transparency based on the wavelength
        float reflected = CalculateWaveformResponse(reflectivityWaveform, wavelength, reflectivityPeak);
        float transmitted = CalculateWaveformResponse(transparencyWaveform, wavelength, transparencyPeak);

        // Ensure the total sums to 1.0
        float absorbed = Math.Clamp(1f - (reflected + transmitted), 0f, 1f);

        return (reflected, transmitted, absorbed);
    }
    
    private static float CalculateWaveformResponse(EMWaveForm waveform, float wavelength, float peakReflectivity)
    {
        // Check if the wavelength is outside the waveform range
        if (wavelength < waveform.WavelengthMin_nm || wavelength > waveform.WavelengthMax_nm)
            return 0f;

        // Normalize the wavelength to the range [0, π]
        double normalizedWavelength = Math.PI * (wavelength - waveform.WavelengthMin_nm) 
                                      / (waveform.WavelengthMax_nm - waveform.WavelengthMin_nm);

        // Calculate sine-squared scaling
        double sineValue = Math.Sin(normalizedWavelength);
        float reflectivity = (float)(sineValue * sineValue) * peakReflectivity;

        return reflectivity;
    }

    public static void HandleSpawners(List<PhotonParticle> spawners, DamageMap damageMap, ref List<IDamageParticle> movingParticles, float timesstep)
    {
        foreach (var spawner in spawners)
        {
            // Clone the spawner
            PhotonParticle clone = PhotonParticle.SpawnNew(spawner);

            // Assign a new index for the clone (in the Photon map)
            clone.mapIndex = damageMap.GetIndex(clone); // (1)

            // Add the new photon particle to the damage map
            damageMap.PhMap[clone.mapIndex] = clone;

            movingParticles.Add(clone); 
            
            spawner.SpawnerLifetime -= timesstep;
            if (spawner.SpawnerLifetime <= 0)
            {
                damageMap.PhMap[clone.mapIndex] = null;
                spawner.IsDeleted = true;
            }
            
        }
    }
}