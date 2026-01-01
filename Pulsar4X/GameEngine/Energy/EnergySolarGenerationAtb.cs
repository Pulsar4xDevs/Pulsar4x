using System;
using Newtonsoft.Json;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Interfaces;
using Pulsar4X.Sensors;
using Pulsar4X.Storage;

namespace Pulsar4X.Energy;

public class EnergySolarGenerationAtb : IComponentDesignAttribute
{
    [JsonProperty]
    public string EnergyTypeID;

    [JsonProperty]
    public EMWaveForm AbsorptionWaveformCapability { get; internal set; }

    /// <summary>
    /// Sensitivity at the ideal wavelength, lower is better, 0 is (imposible) best. should not be negitive.
    /// </summary>
    [JsonProperty]
    public double BestEfficiency { get; internal set; }//sensitivity at ideal wavelength

    /// <summary>
    /// The sensitivity at worst detectable wavelengths, lower is better, should be higher than BestSensitivity_kW
    /// </summary>
    [JsonProperty]
    public double WorstEfficiency { get; internal set; } // sensitivity at worst detectable wavelengths
        

    public double Area_m2 { get; set; } // Panel surface area

    public EnergySolarGenerationAtb(double peakWaveLength, double bandwidth, double bestEfficiency, double worstEfficiency, double area)
    {
        EnergyTypeID = "electricity";
        Area_m2 = area;
        AbsorptionWaveformCapability = new EMWaveForm(peakWaveLength - bandwidth * 0.5,peakWaveLength, peakWaveLength + bandwidth * 0.5);
        BestEfficiency = bestEfficiency;
        WorstEfficiency = worstEfficiency;
    }

    public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
    {
        string resourceID = EnergyTypeID;
        ICargoable? energyCargoable = parentEntity.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny(resourceID);
        EnergyGenAbilityDB genDB;
        if (!parentEntity.HasDataBlob<EnergyGenAbilityDB>())
        {
            genDB = new EnergyGenAbilityDB(parentEntity.StarSysDateTime);
            genDB.EnergyType = energyCargoable;
            parentEntity.SetDataBlob(genDB);
        }
        else
        {
            genDB = parentEntity.GetDataBlob<EnergyGenAbilityDB>();


            if (genDB.EnergyType == null)
                genDB.EnergyType = energyCargoable;
            else if(genDB.EnergyType != energyCargoable)//this is just to reduce complexity. we can add this ability later.
                throw new Exception("PrimeEntity cannot use two different energy types");

        }
        
        genDB.SolarPanels.Add(this); 
        
    }

    public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
    {
        var genDB = parentEntity.GetDataBlob<EnergyGenAbilityDB>();
        genDB.SolarPanels.Remove(this);
    }

    public string AtbName()
    {
        return "Energy Generation";
    }

    public string AtbDescription()
    {
        //string fuelName = StaticRefLib.StaticData.CargoGoods.GetAny(FuelType).Name;
        //return "Generates " + PowerOutputMax + " Mw, using: " + FuelUsedAtMax + "kg/s of ";// + fuelName;
        return "Generates energy from the sun";
    }
}