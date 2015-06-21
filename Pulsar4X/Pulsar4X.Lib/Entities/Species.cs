using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Pulsar4X.Entities
{
    public class Species : GameEntity
    {
        public enum Respiration
        {
            OxygenBreather,
            MethaneBreather,
            Count
        }
        public double BaseGravity { get; set; }
        public double MinimumGravityConstraint { get; set; }
        public double MaximumGravityConstraint { get; set; }
        public double BasePressure { get; set; }
        public double MinimumPressureConstraint { get; set; }
        public double MaximumPressureConstraint { get; set; }
        public double BaseTemperature { get; set; }
        public double MinimumTemperatureConstraint { get; set; }
        public double MaximumTemperatureConstraint { get; set; }
        public double MinimumBreatheableConstraint { get; set; }
        public double MaximumBreatheableConstraint { get; set; }

        /// <summary>
        /// What gas does this species breathe?
        /// </summary>
        public Respiration GasToBreathe { get; set; }

        public Species()
            : base()
        {
            // set default values:
            Name = "Human";
            BaseGravity = 1.0;
            MinimumGravityConstraint = 0.1;
            MaximumGravityConstraint = 1.9;
            BasePressure = 1.0;
            MinimumPressureConstraint = 0.4;
            MaximumPressureConstraint = 4.0;
            BaseTemperature = 14.0;
            MinimumTemperatureConstraint = -15.0;
            MaximumTemperatureConstraint = 45.0;
            MinimumBreatheableConstraint = 0.1;
            MaximumBreatheableConstraint = 0.3;

            GasToBreathe = Respiration.OxygenBreather;

        }

        /// <summary>
        /// GetTNHabRating is an attempt to faithfully reproduce the TN aurora habitability system. Planets must have breathable gas in the right amount and right percentage of the atmosphere
        /// as well as no hazardous gases. TN Aurora has two levels of hazard for gases, those that raise habitability to an automatic 2, and those that raise it to an automatic 3. Finally temperature
        /// and gravity have to be acceptable. Gravity can outright disallow habitation, and temperature can raise the cost extremely highly.
        /// </summary>
        /// <param name="planet">Planet to test</param>
        /// <returns>habitability rating for this species</returns>
        public float GetTNHabRating(SystemBody planet)
        {
#warning implement orbhabs and specialized infrastructure in TN hab rating, also do hazard stuff in addGas, likewise implement this and infrastructure in civilian population growth.
#warning Chase down all the appropriate hazard ratings and set them here for the atmosphere when gas is added or removed.

            /// <summary>
            /// This planet is not habitable at all without orbhabs or specialized infrastructure(neither implemented as yet).
            /// </summary>
            float Gravity = planet.SurfaceGravity / 9.8f;

            if (Gravity < MinimumGravityConstraint || Gravity > MaximumGravityConstraint)
                return -1.0f;

            float HazardRating = 0.0f;

            /// <summary>
            /// Oxygen breathers see Methane as HazardOne, while Methane breathers may or may not see Oxygen as HazardOne
            /// <summary>
            if (Constants.GameSettings.TNTerraformingRules == true)
            {
                if (planet.Atmosphere.HazardOne == true)
                    HazardRating = 2.0f;

                if (planet.Atmosphere.HazardTwo == true)
                    HazardRating = 3.0f;
            }
            else
            {
                /// <summary>
                /// This should use the IsToxic rating for the gas. if an isToxic gas is added, the hazard rating is 2.0, not the more variable rating.
                /// </summary>
                if (planet.Atmosphere.HazardOne == true)
                    HazardRating = 2.0f;
            }

            /// <summary>
            /// Temperature hab rating is Temperature difference / 24.
            /// </summary>
            if (planet.Atmosphere.SurfaceTemperature < MinimumTemperatureConstraint)
            {
                float TempDiff = (float)(MinimumTemperatureConstraint - planet.Atmosphere.SurfaceTemperature) / 24.0f;

                if (HazardRating < TempDiff)
                    HazardRating = TempDiff;
            }
            if(planet.Atmosphere.SurfaceTemperature > MaximumTemperatureConstraint)
            {
                float TempDiff = (float)(planet.Atmosphere.SurfaceTemperature - MaximumTemperatureConstraint) / 24.0f;

                if (HazardRating < TempDiff)
                    HazardRating = TempDiff;
            }

            AtmosphericGas GasRespired = null;
            switch(GasToBreathe)
            {
                case Respiration.OxygenBreather:
                    foreach (Pulsar4X.Helpers.GameMath.WeightedValue<AtmosphericGas> Gas in AtmosphericGas.AtmosphericGases)
                    {
#warning there should maybe be a better way of doing this?
                        if(Gas.Value.Name == "Oxygen")
                        {
                            GasRespired = Gas.Value;
                            break;
                        }
                    }
                    break;
                case Respiration.MethaneBreather:
                    foreach (Pulsar4X.Helpers.GameMath.WeightedValue<AtmosphericGas> Gas in AtmosphericGas.AtmosphericGases)
                    {
                        if (Gas.Value.Name == "Methane")
                        {
                            GasRespired = Gas.Value;
                            break;
                        }
                    }
                    break;
            }

            /// <summary>
            /// Does this gas exist in the atmosphere? is there enough of it to breathe? and is the partial pressure of the breatheable gas not too high?
            bool isBreathable = false;
            if (planet.Atmosphere.Composition.ContainsKey(GasRespired) == true)
            {
                if (planet.Atmosphere.Composition[GasRespired] >= MinimumBreatheableConstraint && planet.Atmosphere.Composition[GasRespired] <= MaximumBreatheableConstraint)
                {
                   float breathePressure = planet.Atmosphere.Composition[GasRespired] / planet.Atmosphere.Pressure;
                   if (breathePressure <= 0.3f)
                       isBreathable = true;
                }
            }

            /// <summary>
            /// Don't run this if the atmosphere is breatheable or if the hazard rating is already higher than 2.0f.
            /// </summary>
            if (isBreathable == false && HazardRating < 2.0f)
            {
                HazardRating = 2.0f;
            }

            return HazardRating;
        }

        public double ColonyCost(SystemBody planet)
        {
            double cost = 1.0;
            cost *= ColonyGravityCost(planet);
            cost *= ColonyPressureCost(planet);
            cost *= ColonyTemperatureCost(planet);
            cost *= ColonyGasCost(planet);

            return cost;
        }

        public double ColonyGravityCost(SystemBody planet)
        {
            return 1;
        }

        public double ColonyPressureCost(SystemBody planet)
        {
            return 1;
        }

        public double ColonyTemperatureCost(SystemBody planet)
        {
            double cost = 1;
            return cost;
        }

        public double ColonyGasCost(SystemBody planet)
        {
            double cost = 1;
            return cost;
        }
    }
}
