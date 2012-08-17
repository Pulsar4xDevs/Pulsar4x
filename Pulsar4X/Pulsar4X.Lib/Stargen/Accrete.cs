using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Pulsar4X.Entities;

namespace Pulsar4X.Stargen
{
    class Accrete
    {
        private readonly bool _generateMoons;
        private readonly double _minimumStellarAge;
        private readonly double _maximumStellarAge;
        private readonly StarFactory _starFactory;

        public Accrete(double minStellarAge, double maxStellarAge, bool genMoons)
        {
            _generateMoons = genMoons;
            _minimumStellarAge = minStellarAge;
            _maximumStellarAge = maxStellarAge;

            _starFactory = new StarFactory(_minimumStellarAge, _maximumStellarAge);
        }

        public StarSystem Create(string name)
        {
            //create the starsystem
            var starSystem = new StarSystem(name);
            _starFactory.Create(name).ForEach(star => starSystem.Stars.Add(star));

            for (var i = 0; i < starSystem.Stars.Count; i++)
            {
                var star = starSystem.Stars[i];
                var protoStar = new ProtoStar(star);

                while (protoStar.DustLeft)
                {
                    var protoPlanet =
                        new ProtoPlanet(MathUtilities.Random.NextDouble(protoStar.PlanetInnerBound, protoStar.PlanetOuterBound),
                                        AccreteUtilities.RandomEccentricity(), Constants.Stargen.PROTOPLANET_MASS);

                    if (AccreteUtilities.DustAvailable(protoStar, protoPlanet))
                    {
                        var criticalMass = AccreteUtilities.CriticalLimit(protoPlanet.SemiMajorAxis, protoPlanet.Eccentricity, protoStar.Luminosity);
                        AccreteDust(protoStar, protoPlanet, criticalMass);

                        if (protoPlanet.Mass > Constants.Stargen.PROTOPLANET_MASS)
                        {
                            CoalescePlanetesimals(protoStar, protoPlanet, criticalMass);
                        }
                    }
                }

                GeneratePlanets(protoStar);

                //populate the Star from the protoStar
                protoStar.Planets.ForEach(planet => star.Planets.Add(planet));
            }

            return starSystem;
        }

        private void GeneratePlanets(ProtoStar protoStar)
        {
            for (int i = 0; i < protoStar.Planets.Count; i++)
            {
                var planet = protoStar.Planets[i];
                planet.Id = i + 1;
                planet.Name = string.Format("{0} {1}", protoStar.Star.Name, planet.Id);
                GeneratePlanet(planet);
            }
        }

        private void GeneratePlanet(Planet planet)
        {
            planet.SurfaceTemperature = 0;
            planet.HighTemperature = 0;
            planet.LowTemperature = 0;
            planet.MaxTemperature = 0;
            planet.MinTemperature = 0;
            planet.RiseInTemperatureDueToGreenhouse = 0;
            planet.IsInResonantRotation = false;

            planet.OrbitZone = AccreteUtilities.OrbitalZone(planet.Primary.Luminosity, planet.SemiMajorAxis);
            planet.OrbitalPeriod = EnviroUtilities.Period(planet.SemiMajorAxis, planet.Mass, planet.Primary.Mass);
            planet.AxialTilt = EnviroUtilities.Inclination(planet.SemiMajorAxis);

            planet.ExoSphericTemperature = Constants.Sol.Earth.EXOSPHERE_TEMP / Math.Pow(planet.SemiMajorAxis / planet.Primary.EcoSphereRadius, 2.0);
            planet.RootMeanSquaredVelocity = EnviroUtilities.RootMeanSquareVelocity(Constants.Gasses.MolecularWeights.MOL_NITROGEN, planet.ExoSphericTemperature);
            planet.RadiusOfCore = EnviroUtilities.KothariRadius(planet.MassOfDust, false, planet.OrbitZone);

            // Calculate the radius as a gas giant, to verify it will retain gas.
            // Then if mass > Earth, it's at least 5% gas and retains He, it's
            // some flavor of gas giant.

            planet.Density = EnviroUtilities.EmpiricalDensity(planet.Mass, planet.SemiMajorAxis, planet.Primary.EcoSphereRadius, true);
            planet.Radius = EnviroUtilities.VolumeRadius(planet.Mass, planet.Density);

            planet.SurfaceAcceleration = EnviroUtilities.Acceleration(planet.Mass, planet.Radius);
            planet.SurfaceGravity = EnviroUtilities.Gravity(planet.SurfaceAcceleration);

            planet.MolecularWeightRetained = EnviroUtilities.MinMolecWeight(planet);

            if (((planet.Mass * Constants.Sol.Sun.MASS_IN_EARTH_MASSES) > 1.0)
              && ((planet.MassOfGas / planet.Mass) > 0.05)
              && (EnviroUtilities.MinMolecWeight(planet) <= 4.0))
            {
                if ((planet.MassOfGas / planet.Mass) < 0.20)
                    planet.PlanetType = PlanetTypes.GasDwarf;
                else if ((planet.Mass * Constants.Sol.Sun.MASS_IN_EARTH_MASSES) < 20.0)
                    planet.PlanetType = PlanetTypes.IceGiant;
                else
                    planet.PlanetType = PlanetTypes.GasGiant;
            }
            else // If not, it's rocky.
            {
                planet.Radius = EnviroUtilities.KothariRadius(planet.Mass, true, planet.OrbitZone);
                planet.Density = EnviroUtilities.VolumeDensity(planet.Mass, planet.Radius);

                if ((planet.MassOfGas / planet.Mass) > 0.000001)
                {
                    double h2Mass = planet.MassOfGas * 0.85;
                    double heMass = (planet.MassOfGas - h2Mass) * 0.999;

                    double h2Life = EnviroUtilities.GasLife(Constants.Gasses.MolecularWeights.MOL_HYDROGEN, planet);
                    double heLife = EnviroUtilities.GasLife(Constants.Gasses.MolecularWeights.HELIUM, planet);

                    if (h2Life < planet.Primary.Age)
                    {
                        var h2Loss = ((1.0 - (1.0 / Math.Exp(planet.Primary.Age / h2Life))) * h2Mass);
                        planet.MassOfGas -= h2Loss;
                        planet.Mass -= h2Loss;

                        //Mass of planet changed so recalculate
                        planet.SurfaceAcceleration = EnviroUtilities.Acceleration(planet.Mass, planet.Radius);
                        planet.SurfaceGravity = EnviroUtilities.Gravity(planet.SurfaceAcceleration);
                    }
                    if (heLife < planet.Primary.Age)
                    {
                        var heLoss = ((1.0 - (1.0 / Math.Exp(planet.Primary.Age / heLife))) * heMass);
                        planet.MassOfGas -= heLoss;
                        planet.Mass -= heLoss;

                        //Mass of planet changed so recalculate
                        planet.SurfaceAcceleration = EnviroUtilities.Acceleration(planet.Mass, planet.Radius);
                        planet.SurfaceGravity = EnviroUtilities.Gravity(planet.SurfaceAcceleration);
                    }
                }
            }

            planet.Day = EnviroUtilities.DayLength(planet);	/* Modifies planet->resonant_period */
            planet.EscapeVelocity = EnviroUtilities.EscapeVel(planet.Mass, planet.Radius);
            planet.MolecularWeightRetained = EnviroUtilities.MinMolecWeight(planet);

            if ((planet.PlanetType == PlanetTypes.GasGiant) || (planet.PlanetType == PlanetTypes.IceGiant) || (planet.PlanetType == PlanetTypes.GasDwarf))
            {
                planet.HaGreenhouseEffect = false;
                planet.VolatileGasInventory = Constants.Units.INCREDIBLY_LARGE_NUMBER;
                planet.SurfacePressure = Constants.Units.INCREDIBLY_LARGE_NUMBER;
                planet.BoilingPoint = Constants.Units.INCREDIBLY_LARGE_NUMBER;
                planet.SurfaceTemperature = Constants.Units.INCREDIBLY_LARGE_NUMBER;
                planet.RiseInTemperatureDueToGreenhouse = 0;
                planet.Albedo = MathUtilities.About(Constants.Units.GAS_GIANT_ALBEDO, 0.1);
                planet.HydrosphereCover = 1.0;
                planet.CloudCover = 1.0;
                planet.IceCover = 0.0;
                planet.SurfaceGravity = Constants.Units.INCREDIBLY_LARGE_NUMBER;
                planet.EstimatedTemperature = EnviroUtilities.EstTemp(planet.Primary.EcoSphereRadius, planet.SemiMajorAxis, planet.Albedo);
                planet.EstimatedTerrestrialTemperature = EnviroUtilities.EstTemp(planet.Primary.EcoSphereRadius, planet.SemiMajorAxis, Constants.Sol.Earth.ALBEDO);
            }
            else
            {
                planet.EstimatedTemperature = EnviroUtilities.EstTemp(planet.Primary.EcoSphereRadius, planet.SemiMajorAxis, Constants.Sol.Earth.ALBEDO);
                planet.SurfaceGravity = EnviroUtilities.Gravity(planet.SurfaceAcceleration);
                planet.HaGreenhouseEffect = EnviroUtilities.Greenhouse(planet.Primary.EcoSphereRadius, planet.SemiMajorAxis);
                planet.VolatileGasInventory = EnviroUtilities.VolInventory(planet.Mass, planet.EscapeVelocity, planet.RootMeanSquaredVelocity,
                                                                                    planet.Primary.Mass, planet.OrbitZone, planet.HaGreenhouseEffect,
                                                                                    (planet.MassOfGas / planet.Mass) > 0.000001);
                planet.SurfacePressure = EnviroUtilities.Pressure(planet.VolatileGasInventory, planet.Radius, planet.SurfaceGravity);

                if (planet.SurfacePressure == 0.0D)
                    planet.BoilingPoint = 0.0;
                else
                    planet.BoilingPoint = EnviroUtilities.BoilingPoint(planet.SurfacePressure);

                EnviroUtilities.IterateSurfaceTemp(planet);

                if ((planet.MaxTemperature >= Constants.Sol.Earth.FREEZING_POINT_OF_WATER) && (planet.MinTemperature <= planet.BoilingPoint))
                    CalculateGases(planet);

                /*
                 *	Next we assign a type to the planet.
                 */

                if (planet.SurfacePressure < 1.0)
                {
                    if (!planet.IsMoon && ((planet.Mass * Constants.Sol.Sun.MASS_IN_EARTH_MASSES) < Constants.Stargen.ASTEROID_MASS_LIMIT))
                        planet.PlanetType = PlanetTypes.Asteroid;
                    else
                        planet.PlanetType = PlanetTypes.Rock;
                }
                else if ((planet.SurfacePressure > 6000.0) && (planet.MolecularWeightRetained <= 2.0))	// Retains Hydrogen
                {
                    planet.PlanetType = PlanetTypes.GasDwarf;
                    planet.Gases.Clear(); //TODO: Do we really want to clear the atmosphere just because it is a gas dwarf?
                }
                else
                {	// Atmospheres:
                    //if (((int)planet.Day == (int)(planet.OrbitalPeriod * 24.0) || (planet.IsInResonantRotation)))
                    //    planet.PlanetType = PlanetTypes.Face;
                    //else 
                    if (planet.HydrosphereCover >= 0.95)
                    {
                        planet.PlanetType = PlanetTypes.Water; // >95% water
                    }
                    else if (planet.IceCover >= 0.95)
                    {
                        planet.PlanetType = PlanetTypes.Ice; // >95% ice
                    }
                    else if (planet.HydrosphereCover > 0.05)
                    {
                        planet.PlanetType = PlanetTypes.Terrestrial; // Terrestrial
                        // else <5% water
                    }
                    else if (planet.MaxTemperature > planet.BoilingPoint)
                    {
                        planet.PlanetType = PlanetTypes.Venusian; // Hot = Venusian
                    }
                    else if ((planet.MassOfGas / planet.Mass) > 0.0001)
                    {
                        // Accreted gas, But no Greenhouse, or liquid water, Make it an Ice World
                        planet.PlanetType = PlanetTypes.Ice;
                        planet.IceCover = 1.0;
                    }
                    else if (planet.SurfacePressure <= 250.0)
                    {
                        // Thin air = Martian
                        planet.PlanetType = PlanetTypes.Martian;
                    }
                    else if (planet.SurfaceTemperature < Constants.Sol.Earth.FREEZING_POINT_OF_WATER)
                    {
                        planet.PlanetType = PlanetTypes.Ice;
                    }
                    else
                    {
                        planet.PlanetType = PlanetTypes.Unknown;
                    }
                }
            }

            if (_generateMoons && !planet.IsMoon)
            {
                if (planet.Moons != null)
                {
                    for (int n = 0; n < planet.Moons.Count; n++)
                    {
                        var moon = planet.Moons[n];
                        if (moon.Mass * Constants.Sol.Sun.MASS_IN_EARTH_MASSES > .000001)
                        {
                            moon.SemiMajorAxis = planet.SemiMajorAxis;
                            moon.Eccentricity = planet.Eccentricity;
                            moon.Id = n + 1;
                            moon.Name = string.Format("{0}.{1}", planet.Name, moon.Id);
                            moon.Primary = planet.Primary;

                            GeneratePlanet(moon);

                            //TODO: Look at adding atmosphere call to this
                            var rocheLimit = 2.44 * planet.Radius * Math.Pow((planet.Density / moon.Density), (1.0 / 3.0));
                            var hillSphere = planet.SemiMajorAxis * Constants.Units.KM_PER_AU * Math.Pow((planet.Mass / (3.0 * planet.Primary.Mass)), (1.0 / 3.0));

                            if ((rocheLimit * 3.0) < hillSphere)
                            {
                                moon.SemiMajorAxis = MathUtilities.Random.NextDouble(rocheLimit * 1.5, hillSphere / 2.0) / Constants.Units.KM_PER_AU;
                                moon.Eccentricity = AccreteUtilities.RandomEccentricity();
                            }
                            else
                            {
                                //TODO: Figure out what to do if the planet is not large enough to hold the moon
                                moon.SemiMajorAxis = 0;
                                moon.Eccentricity = 0;
                            }
                        }
                    }
                }
            }
        }

        private void CalculateGases(Planet planet)
        {
            if (planet.SurfacePressure > 0)
            {
                //var amounts = new List<double>();
                double totamount = 0;
                var pressure = planet.SurfacePressure / Constants.Units.MILLIBARS_PER_BAR;
                //bool gasesExist = false;
                
                foreach(Molecule gas in Constants.Gasses.GasLookup.Values)
                //for (int i = 0; i < ElementalTable.Instance.Count; i++)
                {
                    double yp = gas.BoilingPoint /
                                     (373.0 * ((Math.Log((pressure) + 0.001) / -5050.5) + (1.0 / 373.0)));

                    if ((yp >= 0 && yp < planet.LowTemperature) && (gas.AtomicWeight >= planet.MolecularWeightRetained))
                    {
                        var vrms = EnviroUtilities.RootMeanSquareVelocity(gas.AtomicWeight, planet.ExoSphericTemperature);
                        var pvrms = Math.Pow(1 / (1 + vrms / planet.EscapeVelocity), planet.Primary.Age / 1e9);
                        var abund = gas.AbundanceS; 				/* gases[i].abunde */
                        var react = 1.0D;
                        var fract = 1.0D;
                        var pres2 = 1.0D;

                        if (gas.Symbol == "Ar")
                        {
                            react = .15 * planet.Primary.Age / 4e9;
                        }
                        else if (gas.Symbol == "He")
                        {
                            abund = abund * (0.001 + (planet.MassOfGas / planet.Mass));
                            pres2 = (0.75 + pressure);
                            react = Math.Pow(1 / (1 + gas.Reactivity), planet.Primary.Age / 2e9 * pres2);
                        }
                        else if (((gas.Symbol == "O") || (gas.Symbol == "O2")) && (planet.Primary.Age > 2e9) && (planet.SurfaceTemperature > 270 && planet.SurfaceTemperature < 400))
                        {
                            /*	pres2 = (0.65 + pressure/2);			Breathable - M: .55-1.4 	*/
                            pres2 = (0.89 + pressure / 4);		/*	Breathable - M: .6 -1.8 	*/
                            react = Math.Pow(1 / (1 + gas.Reactivity), Math.Pow(planet.Primary.Age / 2e9, 0.25) * pres2);
                        }
                        else if ((gas.Symbol == "CO2") && (planet.Primary.Age > 2e9) && (planet.SurfaceTemperature > 270 && planet.SurfaceTemperature < 400))
                        {
                            pres2 = (0.75 + pressure);
                            react = Math.Pow(1 / (1 + gas.Reactivity), Math.Pow(planet.Primary.Age / 2e9, 0.5) * pres2);
                            react *= 1.5;
                        }
                        else
                        {
                            pres2 = (0.75 + pressure);
                            react = Math.Pow(1 / (1 + gas.Reactivity), planet.Primary.Age / 2e9 * pres2);
                        }

                        fract = (1 - (planet.MolecularWeightRetained / gas.AtomicWeight));
                        double amount = abund * pvrms * react * fract;

                        totamount += amount;
                        if (amount > 0.0)
                        {

                            if (planet.Gases == null)
                                planet.Gases = new ObservableCollection<Gas>();
                            planet.Gases.Add(new Gas()
                            {
                                ElementId = gas.Id,
                                SurfacePressure = planet.SurfacePressure * amount
                            });
                        }
                    }

                }

                if (planet.Gases != null)
                {
                    foreach (Gas gas in planet.Gases)
                    {
                        gas.SurfacePressure /= totamount;
                    }
                }


            }
        }

        /// <summary>
        /// Checks to see if the current planetoid will collide with another planetoid.
        /// If it does collide, change orbits of both, and either combine the planetoids or create a moon
        /// If it does not collide, create a new planet.
        /// </summary>
        /// <param name="protoPlanet"> </param>
        /// <param name="criticalMass"></param>
        /// <param name="protoStar"> </param>
        private void CoalescePlanetesimals(ProtoStar protoStar, ProtoPlanet protoPlanet, double criticalMass)
        {
            var finished = false;
            var reducedMassOfProtoPlanet = Math.Pow(protoPlanet.Mass / (1.0 + protoPlanet.Mass), (1.0 / 4.0));

            // First we try to find an existing planet with an over-lapping orbit.
            for (int i = 0; i < protoStar.Planets.Count; i++)
            {
                var thePlanet = protoStar.Planets[i];
                double diff = thePlanet.SemiMajorAxis - protoPlanet.SemiMajorAxis;
                double reducedMassOfPlanet;
                double dist1;
                double dist2;
                if (diff > 0.0)
                {
                    dist1 = (protoPlanet.SemiMajorAxis * (1.0 + protoPlanet.Eccentricity) * (1.0 + reducedMassOfProtoPlanet)) - protoPlanet.SemiMajorAxis;
                    /* x aphelion	 */
                    reducedMassOfPlanet = Math.Pow(thePlanet.Mass / (1.0 + thePlanet.Mass), (1.0 / 4.0));
                    dist2 = thePlanet.SemiMajorAxis
                            - (thePlanet.SemiMajorAxis * (1.0 - thePlanet.Eccentricity) * (1.0 - reducedMassOfPlanet));
                }
                else
                {
                    dist1 = protoPlanet.SemiMajorAxis - (protoPlanet.SemiMajorAxis * (1.0 - protoPlanet.Eccentricity) * (1.0 - reducedMassOfProtoPlanet));
                    /* x perihelion */
                    reducedMassOfPlanet = Math.Pow((thePlanet.Mass / (1.0 + thePlanet.Mass)), (1.0 / 4.0));
                    dist2 = (thePlanet.SemiMajorAxis * (1.0 + thePlanet.Eccentricity) * (1.0 + reducedMassOfPlanet))
                        - thePlanet.SemiMajorAxis;
                }

                //protoPlanet passes near a planet
                if ((Math.Abs(diff) <= Math.Abs(dist1)) || (Math.Abs(diff) <= Math.Abs(dist2)))
                {
                    double newSemiMajorAxis = (thePlanet.Mass + protoPlanet.Mass)
                                              / ((thePlanet.Mass / thePlanet.SemiMajorAxis) + (protoPlanet.Mass / protoPlanet.SemiMajorAxis));

                    double temp = thePlanet.Mass * Math.Sqrt(thePlanet.SemiMajorAxis) * Math.Sqrt(1.0 - Math.Pow(thePlanet.Eccentricity, 2.0));
                    temp = temp + (protoPlanet.Mass * Math.Sqrt(protoPlanet.SemiMajorAxis) * Math.Sqrt(Math.Sqrt(1.0 - Math.Pow(protoPlanet.Eccentricity, 2.0))));
                    temp = temp / ((thePlanet.Mass + protoPlanet.Mass) * Math.Sqrt(newSemiMajorAxis));
                    temp = 1.0 - Math.Pow(temp, 2.0);
                    if (((temp < 0.0) || (temp >= 1.0)))
                        temp = 0.0;
                    protoPlanet.Eccentricity = Math.Sqrt(temp);

                    if (_generateMoons)
                    {
                        if (thePlanet.Moons == null)
                            thePlanet.Moons = new ObservableCollection<Planet>();

                        var existingMoonMass = thePlanet.Moons.Sum(x => x.Mass);

                        if (protoPlanet.Mass < criticalMass) //TODO: remove this check and see if this allows moons w/ atmosphere
                        {
                            if ((protoPlanet.Mass * Constants.Sol.Sun.MASS_IN_EARTH_MASSES) < 2.5
                                && (protoPlanet.Mass * Constants.Sol.Sun.MASS_IN_EARTH_MASSES) > .0001
                                && existingMoonMass < (thePlanet.Mass * .05))
                            {
                                var theMoon = new Planet();
                                theMoon.IsMoon = true;
                                theMoon.PlanetType = PlanetTypes.Unknown;
                                //the_moon.SemiMajorAxis = a; 
                                //the_moon.Eccentricity= e;
                                theMoon.Mass = protoPlanet.Mass;
                                theMoon.MassOfDust = protoPlanet.DustMass;
                                theMoon.MassOfGas = protoPlanet.GasMass;
                                theMoon.IsGasGiant = false;
                                theMoon.Albedo = 0;
                                theMoon.SurfaceTemperature = 0;
                                theMoon.HighTemperature = 0;
                                theMoon.LowTemperature = 0;
                                theMoon.MaxTemperature = 0;
                                theMoon.MinTemperature = 0;
                                theMoon.RiseInTemperatureDueToGreenhouse = 0;

                                //if the moon masses more than the planet, then swap them
                                if ((theMoon.MassOfDust + theMoon.MassOfGas) > (thePlanet.MassOfDust + thePlanet.MassOfGas))
                                {
                                    var tempDust = thePlanet.MassOfDust;
                                    var tempGas = thePlanet.MassOfGas;
                                    var tempMass = thePlanet.Mass;

                                    thePlanet.MassOfDust = theMoon.MassOfDust;
                                    thePlanet.MassOfGas = theMoon.MassOfGas;
                                    thePlanet.MassOfGas = theMoon.Mass;

                                    theMoon.MassOfDust = tempDust;
                                    theMoon.MassOfGas = tempGas;
                                    theMoon.Mass = tempMass;
                                }

                                thePlanet.Moons.Add(theMoon);
                                finished = true;
                            }
                        }
                    }

                    if (!finished)
                    {
                        protoPlanet.Mass = thePlanet.Mass + protoPlanet.Mass;

                        AccreteDust(protoStar, protoPlanet, criticalMass);

                        thePlanet.SemiMajorAxis = newSemiMajorAxis;
                        thePlanet.Eccentricity = protoPlanet.Eccentricity;
                        thePlanet.Mass = protoPlanet.Mass;
                        thePlanet.MassOfDust += protoPlanet.DustMass;
                        thePlanet.MassOfGas += protoPlanet.GasMass;
                        if (thePlanet.Mass >= criticalMass)
                            thePlanet.IsGasGiant = true;

                        //move this planet to a new position in planet order based on SemiMajorAxis
                        //protoStar.Planets.Add(thePlanet);
                        //protoStar.Planets.Sort((x, y) =>
                        //    {
                        //        if (x.SemiMajorAxis < y.SemiMajorAxis)
                        //            return -1;
                        //        if (x.SemiMajorAxis > y.SemiMajorAxis)
                        //            return 1;
                        //        return 0;
                        //    });
                        int index = protoStar.Planets.FindIndex(x => x.SemiMajorAxis > thePlanet.SemiMajorAxis);
                        if (index != -1 && i != index - 1)
                        {
                            protoStar.Planets.RemoveAt(i);
                            protoStar.Planets.Insert(index - 1, thePlanet);
                        }
                    }
                    finished = true;
                    break;
                }
            }

            if (!finished)			// Planetesimals didn't collide. Make it a planet.
            {
                var thePlanet = new Planet();

                thePlanet.PlanetType = PlanetTypes.Unknown;
                thePlanet.SemiMajorAxis = protoPlanet.SemiMajorAxis;
                thePlanet.Eccentricity = protoPlanet.Eccentricity;
                thePlanet.Mass = protoPlanet.Mass;
                thePlanet.MassOfDust = protoPlanet.DustMass;
                thePlanet.MassOfGas = protoPlanet.GasMass;
                thePlanet.Albedo = 0;
                thePlanet.SurfaceTemperature = 0;
                thePlanet.HighTemperature = 0;
                thePlanet.LowTemperature = 0;
                thePlanet.MaxTemperature = 0;
                thePlanet.MinTemperature = 0;
                thePlanet.RiseInTemperatureDueToGreenhouse = 0;

                thePlanet.Primary = protoStar.Star;
                thePlanet.IsGasGiant = protoPlanet.Mass >= criticalMass;

                if (protoStar.Planets == null)
                    protoStar.Planets = new List<Planet>();

                int index = protoStar.Planets.FindIndex(x => x.SemiMajorAxis > thePlanet.SemiMajorAxis);
                if (index == -1)
                    protoStar.Planets.Add(thePlanet);
                else
                    protoStar.Planets.Insert(index, thePlanet);
            }
        }

        /// <summary>
        /// Accrete dust for a planetoid based on the given parameters.
        /// </summary>
        private void AccreteDust(ProtoStar protoStar, ProtoPlanet protoPlanet, double criticalMass)
        {
            double previousMass;
            double newMass = protoPlanet.Mass;
            double newDust = 0.0;
            double newGas = 0.0;
            do
            {
                previousMass = newMass;
                newMass = CollectDust(protoStar, protoPlanet, newMass, ref newDust, ref newGas, criticalMass, 0);
            }
            while (!((newMass - previousMass) < (0.0001 * previousMass)));

            protoPlanet.Mass += newMass;
            protoPlanet.DustMass = newDust;
            protoPlanet.GasMass = newGas;
            AccreteUtilities.UpdateDustLanes(protoStar, protoPlanet, previousMass, criticalMass);
        }

        /// <summary>
        /// Method to aggregate dust onto a planetoid based on the disk it is sweeping through.
        /// </summary>
        /// <param name="protoStar"> </param>
        /// <param name="protoPlanet"> </param>
        /// <param name="newGas"> </param>
        /// <param name="critMass"> </param>
        /// <param name="lastMass"> </param>
        /// <param name="newDust"> </param>
        /// <param name="bandIndex"> </param>
        private double CollectDust(ProtoStar protoStar, ProtoPlanet protoPlanet, double lastMass, ref double newDust, ref double newGas, double critMass, int bandIndex)
        {
            //TODO: Needs to be refactored, has more parameters than it needs
            double gasDensity = 0.0;
            double nextDust = 0;
            double nextGas = 0;

            double dustDensity = AccreteUtilities.DustDensity(protoStar, protoPlanet);

            var reducedMass = Math.Pow(lastMass / (1.0 + lastMass), (1.0 / 4.0));
            var rInner = AccreteUtilities.InnerEffectLimit(protoPlanet.SemiMajorAxis, protoPlanet.Eccentricity, reducedMass, protoStar.CloudEccentricity);
            var rOuter = AccreteUtilities.OuterEffectLimit(protoPlanet.SemiMajorAxis, protoPlanet.Eccentricity, reducedMass, protoStar.CloudEccentricity);

            if ((rInner < 0.0))
                rInner = 0.0;

            if (bandIndex >= protoStar.Bands.Count)
                return 0.0;

            var band = protoStar.Bands[bandIndex];
            double tempDensity;
            if ((band.DustPresent == false))
                tempDensity = 0.0;
            else
                tempDensity = dustDensity;

            double massDensity;
            if ((lastMass < critMass) || (band.GasPresent == false))
                massDensity = tempDensity;
            else
            {
                massDensity = Constants.Stargen.K * tempDensity / (1.0 + Math.Sqrt(critMass / lastMass)
                                                           * (Constants.Stargen.K - 1.0));
                gasDensity = massDensity - tempDensity;
            }

            if ((band.OuterEdge <= rInner) || (band.InnerEdge >= rOuter))
            {
                return CollectDust(protoStar, protoPlanet, lastMass, ref  newDust, ref  newGas, critMass, bandIndex + 1);
            }

            double bandwidth = (rOuter - rInner);
            double outerOffset = rOuter - band.OuterEdge;
            if (outerOffset < 0.0)
                outerOffset = 0.0;
            double width = bandwidth - outerOffset;

            double innerOffset = band.InnerEdge - rInner;
            if (innerOffset < 0.0)
                innerOffset = 0.0;
            width = width - innerOffset;

            double bandHeight = 4.0 * Math.PI * Math.Pow(protoPlanet.SemiMajorAxis, 2.0) * reducedMass
                                * (1.0 - protoPlanet.Eccentricity * (outerOffset - innerOffset) / bandwidth);
            double volume = bandHeight * width;

            double newMass = volume * massDensity;
            newGas = volume * gasDensity;
            newDust = newMass - newGas;

            double nextMass = CollectDust(protoStar, protoPlanet, lastMass, ref  nextDust, ref  nextGas, critMass, bandIndex + 1);

            newGas = newGas + nextGas;
            newDust = newDust + nextDust;

            return (newMass + nextMass);
        }
    }
}
