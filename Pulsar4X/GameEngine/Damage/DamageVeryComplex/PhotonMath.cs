using System;
using System.Collections.Generic;
using System.Numerics;
using Pulsar4X.Sensors;


namespace GameEngine.Damage;

public static class PhotonMath
{
    internal const float minPower = 1e-6f;
    public static List<BeamPoint> BeamProcessing(DamageMap map, float ticklen)
    {
        List<BeamPoint> beamPoints = new List<BeamPoint>();

        for (int index = 0; index < map.BeamStarts.Count; index++)
        {
            BeamPoint? beamStart = map.BeamStarts[index];
            if(beamStart == null)
                continue;
            if (beamStart.LifeTime >= 0)
            {
                ProcessBeam(beamStart, map, beamPoints);
                beamStart.LifeTime -= ticklen;
            }
            else
            {
                map.BeamStarts.RemoveAt(index);
                index--;
            }
        }

        return beamPoints;
    }

    private static void ProcessBeam(BeamPoint current, DamageMap map, List<BeamPoint> beamPoints)
    {
        
        beamPoints.Add(current); // Add the current point to the list

        // Process reflection
        if (current.ReflectPercentage > 0)
        {
            if (BeamCollision(current, current.ReflectDirection, map, out PhysicalParticle collisionParticle, out Vector2 position))
            {
                BeamPoint reflectedPoint = new BeamPoint(current, position, current.ReflectDirection, current.Power * current.ReflectPercentage, collisionParticle);
                ProcessBeam(reflectedPoint, map, beamPoints); // Recursively process the reflection
            }
        }

        // Process transmission
        if (current.TransmitPercentage > 0)
        {
            if (BeamCollision(current, current.TransmitDirection, map, out PhysicalParticle collisionParticle, out Vector2 position))
            {
                BeamPoint transmittedPoint = new BeamPoint(current, position, current.TransmitDirection, current.Power * current.TransmitPercentage, collisionParticle);
                ProcessBeam(transmittedPoint, map, beamPoints); // Recursively process the transmission
            }
        }
    }

    
    public static bool BeamCollision(BeamPoint beamPoint, Vector2 direction, DamageMap map, out PhysicalParticle collisionParticle, out Vector2 position)
    {
        collisionParticle = null; // Default to no intersection
        position = Vector2.Zero;
        Vector2 start = beamPoint.Position;
        
        // Initial setup
        int x0 = (int)Math.Floor(start.X);
        int y0 = (int)Math.Floor(start.Y);
        int x1 = x0;
        int y1 = y0;
    
        // Determine step direction
        int stepX = (direction.X >= 0) ? 1 : -1;
        int stepY = (direction.Y >= 0) ? 1 : -1;

        // Calculate initial error terms
        float tMaxX = (x0 + (stepX > 0 ? 1 : 0) - start.X) / direction.X;
        float tMaxY = (y0 + (stepY > 0 ? 1 : 0) - start.Y) / direction.Y;
        float tDeltaX = 1.0f / Math.Abs(direction.X);
        float tDeltaY = 1.0f / Math.Abs(direction.Y);

        while (true)
        {
            // Check if the current position is within bounds
            if (x1 < 0 || x1 >= map.Width || y1 < 0 || y1 >= map.Height)
            {
                return false; // Ray is out of bounds
            }

            // Calculate index in 1D array
            int currentIndex = map.GetIndex(x1, y1);
            collisionParticle = map.PMap[currentIndex];
            // Check for intersection
            if (collisionParticle != null)
            {
                position = new Vector2(x1, y1);
                return true; // Intersection detected
            }

            // Move to the next cell
            if (tMaxX < tMaxY)
            {
                x1 += stepX;
                tMaxX += tDeltaX;
            }
            else
            {
                y1 += stepY;
                tMaxY += tDeltaY;
            }

        }

        return false;
    }

    public static void ApplyTemperatureChanges(DamageMap map, float tickLength)
    {
        if (map.BeamPoints == null) return; // Ensure beam points exist

        foreach (var point in map.BeamPoints)
        {
            if (point.AbsorbPercentage > 0)
            {
                int particleIndex = map.GetIndex(point.Position);
                PhysicalParticle particle = map.PMap[particleIndex];
                if (particle != null)
                {
                    float absorbedPower =  point.Power * point.AbsorbPercentage * 10e6f;//* 10e6 because laser power is in MW.
                    particle.Temperature += (absorbedPower / particle.Mass) * particle.MatType.ThermalCapacity * tickLength;
                }
            }
        }
    }
    

    public static (float reflected, float transmitted, float absorbed) CalculatePhotonInteraction(
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

    /*
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
    }*/
}