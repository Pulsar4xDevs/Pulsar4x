using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Blueprints;
using Pulsar4X.Components;
using Pulsar4X.Extensions;
using Pulsar4X.Industry;
using Pulsar4X.Modding;
using Pulsar4X.Orbital;
using Pulsar4X.Storage;

namespace GameEngine.Damage;


public struct ParticleMaterial
{
    public uint PartMatID;
    public float Density; //as a solid
    //Gpa
    public float Elasticity = 0.5f;
    //Mpa
    public float TensileStrength = 110;
    public float ThermalCapacity;
    public float ThermalConductivity;
    public float MeltingZeroPoint;
    public PhasePoint TriplePoint;
    public PhasePoint CriticalPoint;
    public ParticleMaterial(ParticleMaterialBlueprint materialBP, ICargoable minOrMat)
    {
        PartMatID = materialBP.PartMatID;
        ThermalCapacity = materialBP.ThermalCapacity;
        ThermalConductivity = materialBP.ThermalConductivity;
        MeltingZeroPoint = materialBP.MeltingZeroPoint;
        TriplePoint = materialBP.TriplePoint;
        CriticalPoint = materialBP.CriticalPoint;
        Density = (float)(minOrMat.MassPerUnit / minOrMat.VolumePerUnit);
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

public class Particle
{
    public static int NextID = 0;
    public int ID = NextID++;
    public ParticleMaterial MatType;
    public PhaseState StateOfPhase = PhaseState.Solid;
    public int mapIndex;
    public Vector2 Position;
    public Vector2 Velocity;
    public int Life;
    public float Mass;
    public DamageMap _pMap;
    public bool IsDeleted = false;
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

    public Particle(ParticleMaterial matType, Vector2 position, Vector2 velocity, int scale)
    {
        MatType = matType;
        Position = position;
        Velocity = velocity;
        Temperature = 293.15f; // Room temperature in Kelvin
        Life = 100; // Arbitrary starting life
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
