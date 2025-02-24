using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using Pulsar4X.Blueprints;
using Pulsar4X.Components;
using Pulsar4X.Extensions;
using Pulsar4X.Helpers;
using Pulsar4X.Modding;
using Pulsar4X.Sensors;
using Pulsar4X.Storage;
using Pulsar4X.Weapons;

namespace GameEngine.Damage;


public struct ParticleMaterial
{
    public uint PartMatID;
    public float Density; //as a solid
    //0-1, this is kinetic elasticity not material elasticity
    public float Elasticity = 0.5f;
    //Mpa
    public float TensileStrength = 110;
    public float ThermalCapacity;
    public float ThermalConductivity;
    public float MeltingZeroPoint;
    public PhasePoint TriplePoint;
    public PhasePoint CriticalPoint;
    public EMWaveForm PhotonReflectivity;
    public float PhotonReflectivityPeak;
    public EMWaveForm PhotonTransparency;
    public float PhotonTransperencyPeak;

    
    public ParticleMaterial(ParticleMaterialBlueprint materialBP, ICargoable minOrMat)
    {
        PartMatID = materialBP.PartMatID;
        ThermalCapacity = materialBP.ThermalCapacity;
        ThermalConductivity = materialBP.ThermalConductivity;
        MeltingZeroPoint = materialBP.MeltingZeroPoint;
        TriplePoint = materialBP.TriplePoint;
        CriticalPoint = materialBP.CriticalPoint;
        Density = (float)(minOrMat.MassPerUnit / minOrMat.VolumePerUnit);
        PhotonReflectivity = materialBP.PhotonReflectivity;
        PhotonReflectivityPeak = materialBP.PhotonReflectivityPeak;
        PhotonTransparency = materialBP.PhotonTransparency;
        PhotonTransperencyPeak = materialBP.PhotonTransparencyPeak;
    }
    
}

public struct PhasePoint
{
    public float Bar;
    public float Kelvin;

    public PhasePoint(float bar, float kelvin)
    {
        Bar = bar;
        Kelvin = kelvin;
    }
}

public enum PhaseState
{
    Solid,
    Liquid,
    Gas,
    Plasma
}

public class BeamPoint
{
    public int BeamID { get; set; }
    public Vector2 Position { get; set; }
    
    public float Wavelength { get; set; }
    public float Power { get; set; }
    public float AbsorbPercentage { get; set; } = 1.0f;
    
    public Vector2 ReflectDirection { get; set; }
    public float ReflectPercentage { get; set; } = 0.0f;
    public int ReflectChildIndex { get; set; } = -1;
    
    public Vector2 TransmitDirection { get; set; }
    public float TransmitPercentage { get; set; } = 0.0f;
    public int TransmitChildIndex { get; set; } = -1;
    public float LifeTime { get; set; }
    public BeamPoint(int beamID, Vector2 position, Vector2 transmitDirection, float wavelength, float power)
    {
        BeamID = beamID;
        Position = position;
        TransmitDirection = transmitDirection;
        Wavelength = wavelength;
        Power = power;
        AbsorbPercentage = ReflectPercentage = TransmitPercentage = 0;
        
    }

    public BeamPoint(BeamInfoDB beamInfo, Vector2 particlePosition, float lifetime)
    {
        Position = particlePosition;
        TransmitDirection = Vector2.Normalize(beamInfo.VelocityVector.ToNumericsVector2());
        Wavelength = (float)beamInfo.Frequency;
        Power = (float)beamInfo.Energy;
        TransmitPercentage = 1.0f;
        AbsorbPercentage = 0.0f;
        ReflectPercentage = 0.0f;
        LifeTime = lifetime;
    }

    public BeamPoint(BeamPoint parent, Vector2 position, Vector2 direction, float power, PhysicalParticle collisionParticle)
    {
        
        var reflectVector = Vector2.Reflect(direction, collisionParticle.Position - position);
        BeamID = parent.BeamID;
        Position = position;
        TransmitDirection = parent.Position - position;
        Wavelength = parent.Wavelength;

        Power = power;
        ReflectDirection = reflectVector;
        
        if( power > PhotonMath.minPower)
        {
            (float reflected, float transmitted, float absorbed) = PhotonMath.CalculatePhotonInteraction(parent.Wavelength, collisionParticle.MatType);
            AbsorbPercentage = absorbed;
            ReflectPercentage = reflected;
            TransmitPercentage = transmitted;
        }
    }
}

public class PhysicalParticle
{
    public int compID { get; set; }
    public int mapIndex{ get; set; }
    public Vector2 Position{ get; set; }
    public Vector2 Velocity{ get; set; }
    public static int NextID = 0;
    public int ID = NextID++;
    
    public ParticleMaterial MatType;
    public PhaseState StateOfPhase = PhaseState.Solid;
    public bool IsComponentPartDestroyed = false;
    public float Mass;
    public DamageMap DMap;
    public bool IsDeleted  { get; set; } = false;
    public float Temperature
    {
        get => _temperature;
        set
        {
            if(float.IsNaN(value))
                throw new Exception("tempIsNaN");
            if(float.IsInfinity(value))
                throw new Exception("tempIsInfinit");
            if(value < 0)
                throw new Exception("tempIsNegative");
            _temperature = value;
        }
    }
    private float _temperature;

    public PhysicalParticle(int id, ParticleMaterial matType, Vector2 position, Vector2 velocity, int scale)
    {
        compID = id;
        MatType = matType;
        Position = position;
        Velocity = velocity;
        Temperature = 293.15f; // Room temperature in Kelvin
        Mass = matType.Density * 1 / scale;
        if(Mass <= 0)
            throw new Exception("mass canot be zero or negative");
    }
}

public static class ParticleHelpers
{
    /// <summary>
    /// this is a placeholder untill componentBlueprints can define what mats and mins go where. 
    /// </summary>
    /// <param name="matsList"></param>
    /// <param name="counter"></param>
    /// <returns></returns>
    public static ParticleMaterial GetRandomMat(List<(ParticleMaterial partMat, int amount)> matsList, Random random)
    {
        int totalWeight = matsList.Sum(item => item.amount);

        // Generate a random number between 0 and the total weight
        int randomWeight = random.Next(0, totalWeight);

        // Iterate through the list until we find the item where the cumulative sum exceeds the random number
        int cumulativeWeight = 0;
        foreach (var (item, amount) in matsList)
        {
            cumulativeWeight += amount;
            if (randomWeight < cumulativeWeight)
            {
                return item;
            }
        }
        return matsList[0].partMat;
    }
    
    public static List<(ParticleMaterial partMat, int amount)> GetMaterialsList(ModDataStore modData, ComponentDesign componentDesign)
    {
        var partMatBPs = modData.ParticleMaterials;
        
        var resources = componentDesign.ResourceCosts;
        List<(ParticleMaterial partMat, int amount)> partMats = new();
        foreach (var resource in resources)
        {
            if (modData.Minerals.ContainsKey(resource.Key))
            {
                var item = modData.Minerals[resource.Key];
                if (item.PartMatUniqueID.IsNotNullOrEmpty() && partMatBPs.ContainsKey(item.PartMatUniqueID))
                {
                    var partMat = new ParticleMaterial(partMatBPs[item.PartMatUniqueID], item);
                    partMats.Add((partMat, (int)resource.Value));
                }
            }
            if (modData.ProcessedMaterials.ContainsKey(resource.Key))
            {
                var item = modData.ProcessedMaterials[resource.Key];
                if (item.PartMatUniqueID.IsNotNullOrEmpty() && partMatBPs.ContainsKey(item.PartMatUniqueID))
                {
                    var partMat = new ParticleMaterial(partMatBPs[item.PartMatUniqueID], item);
                    partMats.Add((partMat, (int)resource.Value));
                }
            }
            
        }
        return partMats;
    }
}
