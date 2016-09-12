using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public static class AtmosphereProcessor
    {
        /// <summary>
        /// Processes and updates the atmosphers of all system bodies in the game.
        /// Should run with the other econ updates.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="systems"></param>
        /// <param name="deltaSeconds"></param>
        public static void Process(Game game, List<StarSystem> systems, int deltaSeconds)
        {
            ///< @todo Write full atmo processor.
        }

        public static void UpdateAtmosphere(AtmosphereDB atmoDB, SystemBodyInfoDB bodyDB)
        {
            if (atmoDB.Exists)
            {
                // clear old values.
                atmoDB.Pressure = 0;
                atmoDB.GreenhousePressure = 0;

                foreach (var gas in atmoDB.Composition)
                {
                    atmoDB.Pressure += gas.Value;

                    // only add a greenhouse gas if it is not frozen or liquid:
                    if (atmoDB.SurfaceTemperature >= gas.Key.BoilingPoint)
                    {
                        // actual greenhouse pressure adjusted by gas GreenhouseEffect.
                        // note that this produces the same affect as in aurora if all GreenhouseEffect bvalue are -1, 0 or 1.
                        atmoDB.GreenhousePressure += (float)gas.Key.GreenhouseEffect * gas.Value;
                    }
                }

                if (bodyDB.BodyType == BodyType.GasDwarf
                    || bodyDB.BodyType == BodyType.GasGiant
                    || bodyDB.BodyType == BodyType.IceGiant)
                {
                    // special gas giant stuff, needed because we do not apply greenhouse factor to them:
                    atmoDB.SurfaceTemperature = bodyDB.BaseTemperature * (1 - atmoDB.Albedo);
                    atmoDB.Pressure = 1;       // because thats the definition of the surface of these planets, when 
                    // atmosphereic pressure = the pressure of earths atmosphere at its surface (what we call 1 atm).
                }
                else
                {
                    // From Aurora: Greenhouse Factor = 1 + (Atmospheric Pressure /10) + Greenhouse Pressure   (Maximum = 3.0)
                    atmoDB.GreenhouseFactor = (atmoDB.Pressure * 0.035F) + atmoDB.GreenhousePressure;  // note that we do without the extra +1 as it seems to give us better temps.
                    atmoDB.GreenhouseFactor = (float)GMath.Clamp(atmoDB.GreenhouseFactor, -3.0, 3.0);

                    // From Aurora: Surface Temperature in Kelvin = Base Temperature in Kelvin x Greenhouse Factor x Albedo
                    atmoDB.SurfaceTemperature = Temperature.ToKelvin(bodyDB.BaseTemperature);
                    atmoDB.SurfaceTemperature += atmoDB.SurfaceTemperature * atmoDB.GreenhouseFactor * (float)Math.Pow(1 - atmoDB.Albedo, 0.25);   // We need to raise albedo to the power of 1/4, see: http://en.wikipedia.org/wiki/Stefan%E2%80%93Boltzmann_law
                    atmoDB.SurfaceTemperature = Temperature.ToCelsius(atmoDB.SurfaceTemperature);
                }
            }
            else
            {
                // simply apply albedo, see here: http://en.wikipedia.org/wiki/Stefan%E2%80%93Boltzmann_law
                atmoDB.Pressure = 0;
                atmoDB.SurfaceTemperature = Temperature.ToKelvin(bodyDB.BaseTemperature);
                atmoDB.SurfaceTemperature = atmoDB.SurfaceTemperature * (float)Math.Pow(1 - atmoDB.Albedo, 0.25);   // We need to raise albedo to the power of 1/4
            }

            // update the descriptions:
            atmoDB.GenerateDescriptions();
        }
    }
}
