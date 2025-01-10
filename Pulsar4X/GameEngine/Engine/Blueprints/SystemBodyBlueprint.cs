using System;
using System.Collections.Generic;

namespace Pulsar4X.Blueprints;
public class SystemBodyBlueprint : Blueprint
{
    public struct SystemBodyInfoBlueprint
    {
        public double? Gravity { get; set; }
        public string? Type { get; set; }
        public string? Tectonics { get; set; }
        public float? Albedo { get; set; }
        public float? AxialTilt { get; set; }
        public float? MagneticField { get; set; }
        public float? BaseTemperature { get; set; }
        public float? RadiationLevel { get; set; }
        public float? AtmosphericDust { get; set; }
        public TimeSpan? LengthOfDay { get; set; }
        public double? Mass { get; set; }
        public double? Radius { get; set; }
    }

    public struct OrbitBlueprint
    {
        // Game will prefer no suffix, then try for values in this order: _m > _km > _au
        public double? SemiMajorAxis { get; set; }
        public double? SemiMajorAxis_m { get; set; }
        public double? SemiMajorAxis_km { get; set; }
        public double? SemiMajorAxis_au { get; set; }

        public double? Eccentricity { get; set; }

        // Game prefers _r (radians), then will try _d or no suffix (assumes any no suffix value is in degrees)
        public double? EclipticInclination { get; set; }
        public double? EclipticInclination_r { get; set; }
        public double? EclipticInclination_d { get; set; }

        // Game prefer _r (radians), then will try _o or no suffix (assumes no suffix value is in degrees)
        public double? LoAN { get; set; }
        public double? LoAN_r { get; set; }
        public double? LoAN_d { get; set; }

        public double? AoP { get; set; }
        public double? AoP_d { get; set; }
        public double? AoP_r { get; set; }

        public double? MeanAnomaly { get; set; }
        public double? MeanAnomaly_r { get; set; }
        public double? MeanAnomaly_d { get; set; }

    }

    public struct AtmosphericGasValue
    {
        public string Symbol { get; set; }
        public float Percent { get; set; }
    }

    public struct AtmosphereBlueprint
    {
        public float? Pressure { get; set; }
        public List<AtmosphericGasValue>? Gases { get; set; }
        public bool? Hydrosphere { get; set; }
        public decimal? HydroExtent { get; set; }
        public float? GreenhouseFactor { get; set; }
        public float? GreenhousePressure { get; set; }
        public float? SurfaceTemperature { get; set; }
    }

    public struct StartingMineralBlueprint
    {
        public string Id { get; set; }
        public double Abundance { get; set; }
        public double Accessibility { get; set; }
    }

    public string Name { get; set; }

    // Can this body be selected as a starting location
    public bool CanStartHere { get; set; } = false;
    public string? Parent { get; set; }
    public bool? Colonizable { get; set; }
    public uint? GeoSurveyPointsRequired { get; set; }

    public SystemBodyInfoBlueprint Info { get; set; }
    public OrbitBlueprint Orbit { get; set; }
    public AtmosphereBlueprint? Atmosphere { get; set; }
    public List<StartingMineralBlueprint>? Minerals { get; set; }
    public string? GenerateMinerals { get; set; }

}