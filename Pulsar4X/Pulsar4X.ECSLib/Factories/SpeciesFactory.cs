using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.ECSLib.Factories
{
    internal static class SpeciesFactory
    {

        private static SpeciesDB CreateSpecies_Human()
        {
            string Name = "Human";
            double BaseGravity = 1.0;
            double MinimumGravityConstraint = 0.1;
            double MaximumGravityConstraint = 1.9;
            double BasePressure = 1.0;
            double MinimumPressureConstraint = 0.4;
            double MaximumPressureConstraint = 4.0;
            double BaseTemperature = 14.0;
            double MinimumTemperatureConstraint = -15.0;
            double MaximumTemperatureConstraint = 45.0;
            SpeciesDB species = new SpeciesDB(Name, BaseGravity, 
                MinimumGravityConstraint, MaximumGravityConstraint, 
                BasePressure, MinimumPressureConstraint,
                MaximumPressureConstraint, BaseTemperature, 
                MinimumTemperatureConstraint, MaximumTemperatureConstraint);

            return species;
        }

        private static SpeciesDB CreateSpecies(SystemBodyDB planetInfo, int? seed = null)
        {
            Random rnd;
            if (seed != null) 
                rnd = new Random((int)seed);
            else 
                rnd = new Random();

            string Name = "somerace"; //create name from planet name or something. 

            throw new NotImplementedException();
            //double BaseGravity = planetInfo.SurfaceGravity;
            double MinimumGravityConstraint = 0.1;//rnd.Next(planetInfo.SurfaceGravity, 0.1);
            double MaximumGravityConstraint = 1.9;
            //double BasePressure = planetInfo.Atmosphere.Pressure;
            double MinimumPressureConstraint = 0.4;
            double MaximumPressureConstraint = 4.0;
            double BaseTemperature = planetInfo.BaseTemperature;
            double MinimumTemperatureConstraint = -15.0;
            double MaximumTemperatureConstraint = 45.0;
            //SpeciesDB species = new SpeciesDB(Name, BaseGravity,
            //    MinimumGravityConstraint, MaximumGravityConstraint,
            //    BasePressure, MinimumPressureConstraint,
            //    MaximumPressureConstraint, BaseTemperature,
            //    MinimumTemperatureConstraint, MaximumTemperatureConstraint);

            //return species;
        }
    }
}
