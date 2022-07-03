using Pulsar4X.Orbital;
using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public static class AtmosphereDBExtensions
    {
        public static bool WouldBeFrozenAtGivenTemperature(this AtmosphericGasSD gas, float temperatureInC)
        {
            return (gas.MeltingPoint > temperatureInC);
        }

        public static float GetAtmosphericPressure(this AtmosphereDB atmosphere)
        {
            double totalPressure = 0;
            Dictionary<AtmosphericGasSD, float> atmosphereComp = atmosphere.Composition;

            var baseTemp = atmosphere.GetParentBaseTemperature();
            foreach (KeyValuePair<AtmosphericGasSD, float> kvp in atmosphereComp)
            {
                if (!kvp.Key.WouldBeFrozenAtGivenTemperature(baseTemp))
                {
                    var pressureContribution = kvp.Value;
                    totalPressure += pressureContribution;
                }
            }

            return Convert.ToSingle(totalPressure);
        }

        public static float GetGreenhousePressure(this AtmosphereDB atmosphere)
        {
            float totalPressure = 0;
            Dictionary<AtmosphericGasSD, float> atmosphereComp = atmosphere.Composition;

            var baseTemp = atmosphere.GetParentBaseTemperature();
            foreach (KeyValuePair<AtmosphericGasSD, float> kvp in atmosphereComp)
            {
                if (kvp.Key.GreenhouseEffect > 0 && !kvp.Key.WouldBeFrozenAtGivenTemperature(baseTemp)) {
                    totalPressure += kvp.Value;
                }
            }

            return Convert.ToSingle(Math.Round(totalPressure, 3));
        }

        public static float GetAntiGreenhousePressure(this AtmosphereDB atmosphere)
        {
            float totalPressure = 0;
            Dictionary<AtmosphericGasSD, float> atmosphereComp = atmosphere.Composition;

            var baseTemp = atmosphere.GetParentBaseTemperature();
            foreach (KeyValuePair<AtmosphericGasSD, float> kvp in atmosphereComp)
            {
                if (kvp.Key.GreenhouseEffect < 0 && !kvp.Key.WouldBeFrozenAtGivenTemperature(baseTemp))
                {
                    totalPressure += kvp.Value;
                }
            }

            return Convert.ToSingle(Math.Round(totalPressure, 3));
        }

        public static float CalculatedGreenhouseFactor(this AtmosphereDB atmosphere)
        {
            double calculated = 1.0 + (atmosphere.GetAtmosphericPressure() / 10.0f) + atmosphere.GetGreenhousePressure();
            double capped = Math.Min(calculated, 3.0);          // Max of 3.0
            double rounded = Math.Round(capped, 3);             // Round to 3 decimal places
            return Convert.ToSingle(rounded);
        }

        public static float CalulatedSurfaceTemperature(this AtmosphereDB atmosphere, float albedoFactor = 1.0f)
        {
            var ghFactor = atmosphere.CalculatedGreenhouseFactor();
            var calculatedTemperatureK = Temperature.ToKelvin(atmosphere.GetParentBaseTemperature()) * ghFactor * albedoFactor;
            
            return Temperature.ToCelsius(calculatedTemperatureK);
        }

        public static float GetParentBaseTemperature(this AtmosphereDB atmosphere)
        {
            if (!atmosphere.OwningEntity.HasDataBlob<SystemBodyInfoDB>())
            {
                throw new ArgumentException("Parent Entity isn't a System Body");
            }

            var parentBody = atmosphere.OwningEntity.GetDataBlob<SystemBodyInfoDB>();
            return parentBody.BaseTemperature;
        }
    }
}
