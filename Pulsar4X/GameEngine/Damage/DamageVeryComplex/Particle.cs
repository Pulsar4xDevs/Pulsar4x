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
    float Density { get; } //as a solid
    public float ThermalCapacity;
    public float ThermalConductivity;
    public float MeltingZeroPoint;
    public (float bar, float kelvin) TriplePoint;
    public (float bar, float kelvin) CriticalPoint;

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


public enum PhaseState
{
    Solid,
    Liquid,
    Gas,
    Plasma
}

public class Particle
{
    public ParticleMaterial MatType;
    public PhaseState StateOfPhase = PhaseState.Solid;
    public Vector2 Position;
    public Vector2 Velocity;
    public float Temperature;
    public int Life;
    public float Mass;
    public DamageMap _pMap;
    
    public Particle(ParticleMaterial matType, Vector2 position, Vector2 velocity = default)
    {
        MatType = matType;
        Position = position;
        Velocity = velocity;
        Temperature = 293.15f; // Room temperature in Kelvin
        Life = 100; // Arbitrary starting life
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
        int index = 0;
        foreach (var resource in resources)
        {
            if (modData.Minerals.ContainsKey(resource.Key))
            {
                var item = modData.Minerals[resource.Key];
                if (item.PartMatID.IsNotNullOrEmpty() && partMatBPs.ContainsKey(item.PartMatID))
                {
                    var partMat = new ParticleMaterial(partMatBPs[item.PartMatID], item);
                    partMats[index] = (partMat, (int)resource.Value);
                }
            }
            if (modData.ProcessedMaterials.ContainsKey(resource.Key))
            {
                var item = modData.ProcessedMaterials[resource.Key];
                if (item.PartMatID.IsNotNullOrEmpty() && partMatBPs.ContainsKey(item.PartMatID))
                {
                    var partMat = new ParticleMaterial(partMatBPs[item.PartMatID], item);
                    partMats[index] = (partMat, (int)resource.Value);
                }
            }
            
        }
        
        return partMats;

    }
}
