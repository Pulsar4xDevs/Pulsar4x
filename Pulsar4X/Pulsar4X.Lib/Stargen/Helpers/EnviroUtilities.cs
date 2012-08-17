using System;
using Pulsar4X.Entities;

namespace Pulsar4X.Stargen
{
    public static class EnviroUtilities
    {
        /// <summary>
        ///	The separation is in units of AU, and both masses are in units of solar 
        ///	masses.	 The period returned is in terms of Earth days.
        /// </summary>
        /// <param name="separation"></param>
        /// <param name="smallMass"></param>
        /// <param name="largeMass"></param>
        /// <returns></returns>
        public static double Period(double separation, double smallMass, double largeMass)
        {
            double periodInYears = Math.Sqrt(Math.Pow(separation, 3.0) / (smallMass + largeMass));
            return (periodInYears * Constants.Sol.Earth.DAYS_IN_A_YEAR);
        }

        /// <summary>
        ///  The mass is in units of solar masses, and the density is in units
        ///	 of grams/cc.  The radius returned is in units of km.
        /// </summary>
        /// <param name="mass"></param>
        /// <param name="density"></param>
        /// <returns></returns>
        public static double VolumeRadius(double mass, double density)
        {
            mass = mass * Constants.Units.SOLAR_MASS_IN_GRAMS;
            double volume = mass / density;
            return Math.Pow((3.0 * volume) / (4.0 * Math.PI), (1.0 / 3.0)) / Constants.Units.CM_PER_KM;
        }
        
        /// <summary>
        ///	 Returns the radius of the planet in kilometers.						
        ///	 The mass passed in is in units of solar masses.						
        ///	 This formula is listed as eq.9 in Fogg's article, although some typos	
        ///	 crop up in that eq.  See "The Internal Constitution of Planets", by	
        ///	 Dr. D. S. Kothari, Mon. Not. of the Royal Astronomical Society, vol 96 
        ///	 pp.833-843, 1936 for the derivation.  Specifically, this is Kothari's	
        ///	 eq.23, which appears on page 840.										
        /// </summary>
        /// <param name="mass"></param>
        /// <param name="isGiant"></param>
        /// <param name="zone"></param>
        /// <returns></returns>
        public static double KothariRadius(double mass, bool isGiant, int zone)
        {
            double atomicWeight;
            double atomicNum;

            switch (zone)
            {
                case 1:
                    if (isGiant)
                    {
                        atomicWeight = 9.5;
                        atomicNum = 4.5;
                    }
                    else
                    {
                        atomicWeight = 15.0;
                        atomicNum = 8.0;
                    }
                    break;
                case 2:
                    if (isGiant)
                    {
                        atomicWeight = 2.47;
                        atomicNum = 2.0;
                    }
                    else
                    {
                        atomicWeight = 10.0;
                        atomicNum = 5.0;
                    }
                    break;
                default:
                    if (isGiant)
                    {
                        atomicWeight = 7.0;
                        atomicNum = 4.0;
                    }
                    else
                    {
                        atomicWeight = 10.0;
                        atomicNum = 5.0;
                    }
                    break;
            }

            double temp1 = atomicWeight * atomicNum;

            double temp = (2.0 * Constants.Stargen.BETA_20 * Math.Pow(Constants.Sol.Sun.MASS_IN_GRAMS, (1.0 / 3.0))) / (Constants.Stargen.A1_20 * Math.Pow(temp1, (1.0 / 3.0)));

            double temp2 = Constants.Stargen.A2_20 * Math.Pow(atomicWeight, (4.0 / 3.0)) * Math.Pow(Constants.Units.SOLAR_MASS_IN_GRAMS, (2.0 / 3.0));
            temp2 = temp2 * Math.Pow(mass, (2.0 / 3.0));
            temp2 = temp2 / (Constants.Stargen.A1_20 * Math.Pow(atomicNum, 2.0));
            temp2 = 1.0 + temp2;
            temp = temp / temp2;
            temp = (temp * Math.Pow(mass, (1.0 / 3.0))) / Constants.Units.CM_PER_KM;

            temp /= Constants.Stargen.JIMS_FUDGE;			/* Make Earth = actual earth */

            return (temp);
        }

        /// <summary>
        ///  The mass passed in is in units of solar masses, and the orbital radius	
        ///  is in units of AU.	The density is returned in units of grams/cc.		 
        /// </summary>
        /// <param name="mass"></param>
        /// <param name="orbRadius"></param>
        /// <param name="rEcosphere"></param>
        /// <param name="isGasGiant"></param>
        /// <returns></returns>
        public static double EmpiricalDensity(double mass, double orbRadius, double rEcosphere, bool isGasGiant)
        {
            double temp = Math.Pow(mass * Constants.Units.SUN_MASS_IN_EARTH_MASSES, (1.0 / 8.0));
            temp = temp * Math.Sqrt(Math.Sqrt(rEcosphere / orbRadius));
            if (isGasGiant)
                return (temp * 1.2);
            return (temp * 5.5);
        }
        
        /// <summary>
        ///	The mass passed in is in units of solar masses, and the equatorial		
        ///	radius is in km.  The density is returned in units of grams/cc.			
        /// </summary>
        /// <param name="mass"></param>
        /// <param name="equatRadius"></param>
        /// <returns></returns>
        public static double VolumeDensity(double mass, double equatRadius)
        {
            mass = mass * Constants.Units.SOLAR_MASS_IN_GRAMS;
            equatRadius = equatRadius * Constants.Units.CM_PER_KM;
            double volume = (4.0 * Math.PI * Math.Pow(equatRadius, 3.0)) / 3.0;
            return (mass / volume);
        }
        
        /// <summary>
        /// Fogg's information for this routine came from Dole "Habitable Planets	
        /// for Man", Blaisdell Publishing Company, NY, 1964.  From this, he came	
        /// up with his eq.12, which is the equation for the 'base_angular_velocity'
        /// below.  He then used an equation for the change in angular velocity per	
        /// time (dw/dt) from P. Goldreich and S. Soter's paper "Q in the Solar		
        /// System" in Icarus, vol 5, pp.375-389 (1966).	 Using as a comparison the	
        /// change in angular velocity for the Earth, Fogg has come up with an		
        /// approximation for our new planet (his eq.13) and take that into account.
        /// This is used to find 'change_in_angular_velocity' below.				
        ///																			
        ///	Input parameters are mass (in solar masses), radius (in Km), orbital	
        /// period (in days), orbital radius (in AU), density (in g/cc),			
        /// eccentricity, and whether it is a gas giant or not.						
        ///	The length of the day is returned in units of hours.					
        /// </summary>
        /// <param name="planet"></param>
        /// <returns></returns>
        public static double DayLength(Planet planet)
        {
            double planetaryMassInGrams = planet.Mass * Constants.Units.SOLAR_MASS_IN_GRAMS;
            double equatorialRadiusInCm = planet.Radius * Constants.Units.CM_PER_KM;
            double yearInHours = planet.OrbitalPeriod * 24.0;
            bool isGiant = (planet.PlanetType == PlanetTypes.GasGiant || planet.PlanetType == PlanetTypes.IceGiant || planet.PlanetType == PlanetTypes.GasDwarf);

            double dayInHours;

            bool stopped = false;

            planet.IsInResonantRotation = false;	// Warning: Modify the planet 

            double k2 = isGiant ? 0.24 : 0.33;

            double baseAngularVelocity = Math.Sqrt(2.0 * Constants.Stargen.J * (planetaryMassInGrams) /
                                                     (k2 * Math.Pow(equatorialRadiusInCm, 2.0)));

            //	This next calculation determines how much the planet's rotation is	 
            //	slowed by the presence of the star.								 

            double changeInAngularVelocity = Constants.Sol.Earth.CHANGE_IN_ANG_VEL *
                                                (planet.Density / Constants.Sol.Earth.DENSITY) *
                                                (equatorialRadiusInCm / Constants.Sol.Earth.RADIUS) *
                                                (Constants.Units.EARTH_MASS_IN_GRAMS / planetaryMassInGrams) *
                                                Math.Pow(planet.Primary.Mass, 2.0) *
                                                (1.0 / Math.Pow(planet.SemiMajorAxis, 6.0));
            double angVelocity = baseAngularVelocity + (changeInAngularVelocity *
                                                         planet.Primary.Age);

            // Now we change from rad/sec to hours/rotation.

            if (angVelocity <= 0.0)
            {
                stopped = true;
                dayInHours = Constants.Units.INCREDIBLY_LARGE_NUMBER;
            }
            else
                dayInHours = Constants.Units.RADIANS_PER_ROTATION / (Constants.Units.SECONDS_PER_HOUR * angVelocity);

            if ((dayInHours >= yearInHours) || stopped)
            {
                if (planet.Eccentricity > 0.1)
                {
                    double spinResonanceFactor = (1.0 - planet.Eccentricity) / (1.0 + planet.Eccentricity);
                    planet.IsInResonantRotation = true;
                    return (spinResonanceFactor * yearInHours);
                }
                return yearInHours;
            }

            return (dayInHours);
        }
        
        /// <summary>
        ///	 The orbital radius is expected in units of Astronomical Units (AU).	
        ///	 Inclination is returned in units of degrees.
        /// </summary>
        /// <param name="orbRadius"></param>
        /// <returns></returns>
        public static int Inclination(double orbRadius)
        {
            var temp = (int)(Math.Pow(orbRadius, 0.2) * MathUtilities.About(Constants.Sol.Earth.AXIAL_TILT, 0.4));
            return (temp % 360);
        }
        
        /// <summary>
        ///	 This function implements the escape velocity calculation.	Note that	
        ///	it appears that Fogg's eq.15 is incorrect.								
        ///	The mass is in units of solar mass, the radius in kilometers, and the	
        ///	velocity returned is in cm/sec.											
        /// </summary>
        /// <param name="mass"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static double EscapeVel(double mass, double radius)
        {
            double massInGrams = mass * Constants.Units.SOLAR_MASS_IN_GRAMS;
            double radiusInCm = radius * Constants.Units.CM_PER_KM;
            return Math.Sqrt(2.0 * Constants.Units.GRAV_CONSTANT * massInGrams / radiusInCm);
        }
        
        /// <summary>
        ///	This is Fogg's eq.16.  The molecular weight (usually assumed to be N2)	
        ///	is used as the basis of the Root Mean Square (RMS) velocity of the		
        ///	molecule or atom.  The velocity returned is in cm/sec.					
        ///	Orbital radius is in A.U.(ie: in units of the earth's orbital radius).	
        /// </summary>
        /// <param name="molecularWeight"></param>
        /// <param name="exosphericTemp"></param>
        /// <returns></returns>
        public static double RootMeanSquareVelocity(double molecularWeight, double exosphericTemp)
        {
            return Math.Sqrt((3.0 * Constants.Units.MOLAR_GAS_CONST * exosphericTemp) / molecularWeight) * Constants.Units.CM_PER_METER;
        }

        /// <summary>
        ///	 This function returns the smallest molecular weight retained by the	
        ///	body, which is useful for determining the atmosphere composition.		
        ///	Mass is in units of solar masses, and equatorial radius is in units of	
        ///	kilometers.																
        /// </summary>
        /// <param name="mass"></param>
        /// <param name="equatRadius"></param>
        /// <param name="exosphericTemp"></param>
        /// <returns></returns>
        public static double MoleculeLimit(double mass, double equatRadius, double exosphericTemp)
        {
            double escVelocity = EscapeVel(mass, equatRadius);

            return ((3.0 * Constants.Units.MOLAR_GAS_CONST * exosphericTemp) /
                    (Math.Pow((escVelocity / Constants.Units.GAS_RETENTION_THRESHOLD) / Constants.Units.CM_PER_METER, 2.0)));
        }

        /// <summary>
        ///	 This function calculates the surface acceleration of a planet.	 The	
        ///	mass is in units of solar masses, the radius in terms of km, and the	
        ///	acceleration is returned in units of cm/sec2.							
        /// </summary>
        /// <param name="mass"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static double Acceleration(double mass, double radius)
        {
            return (Constants.Units.GRAV_CONSTANT * (mass * Constants.Units.SOLAR_MASS_IN_GRAMS) /
                               Math.Pow(radius * Constants.Units.CM_PER_KM, 2.0));
        }
        
        /// <summary>
        ///	 This function calculates the surface gravity of a planet.	The			
        ///	acceleration is in units of cm/sec2, and the gravity is returned in		
        ///	units of Earth gravities.												
        /// </summary>
        /// <param name="acceleration"></param>
        /// <returns></returns>
        public static double Gravity(double acceleration)
        {
            return (acceleration / Constants.Sol.Earth.ACCELERATION);
        }

        /// <summary>
        /// This implements Fogg's eq.17.  The 'inventory' returned is unitless.
        /// </summary>
        /// <param name="mass"></param>
        /// <param name="escapeVel"></param>
        /// <param name="rootMeanSquareVelocity"></param>
        /// <param name="stellarMass"></param>
        /// <param name="zone"></param>
        /// <param name="greenhouseEffect"></param>
        /// <param name="accretedGas"></param>
        /// <returns></returns>
        public static double VolInventory(double mass, double escapeVel, double rootMeanSquareVelocity, double stellarMass, int zone,
                             bool greenhouseEffect, bool accretedGas)
        {
            double velocityRatio = escapeVel / rootMeanSquareVelocity;
            if (velocityRatio >= Constants.Units.GAS_RETENTION_THRESHOLD)
            {
                double proportionConst;
                switch (zone)
                {
                    case 1:
                        proportionConst = 140000.0;	/* 100 -> 140 JLB */
                        break;
                    case 2:
                        proportionConst = 75000.0;
                        break;
                    case 3:
                        proportionConst = 250.0;
                        break;
                    default:
                        throw new ArgumentException("Error: orbital zone not initialized correctly!", "zone");
                }
                double earthUnits = mass * Constants.Units.SUN_MASS_IN_EARTH_MASSES;
                double temp1 = (proportionConst * earthUnits) / stellarMass;

                if (greenhouseEffect || accretedGas)
                    return temp1;
                return (temp1 / 140.0);	/* 100 -> 140 JLB */
            }
            return 0.0;
        }
        
        /// <summary>
        ///	This implements Fogg's eq.18.  The pressure returned is in units of		
        ///	millibars (mb).	 The gravity is in units of Earth gravities, the radius 
        ///	in units of kilometers.													
        ///																			
        ///  JLB: Aparently this assumed that earth pressure = 1000mb. I've added a	
        ///	fudge factor (EARTH_SURF_PRES_IN_MILLIBARS / 1000.) to correct for that
        /// </summary>
        /// <param name="volatileGasInventory"></param>
        /// <param name="equatRadius"></param>
        /// <param name="gravity"></param>
        /// <returns></returns>
        public static double Pressure(double volatileGasInventory, double equatRadius, double gravity)
        {
            equatRadius = Constants.Sol.Earth.RADIUS_IN_KM / equatRadius;
            return (volatileGasInventory * gravity * (Constants.Sol.Earth.SURF_PRES_IN_MILLIBARS / 1000.0) / Math.Pow(equatRadius, 2.0));
        }

        /// <summary>
        ///	 This function returns the boiling point of water in an atmosphere of	
        ///	 pressure 'surf_pressure', given in millibars.	The boiling point is	
        ///	 returned in units of Kelvin.  This is Fogg's eq.21.
        /// </summary>
        /// <param name="surfPressure"></param>
        /// <returns></returns>
        public static double BoilingPoint(double surfPressure)
        {
            double surfacePressureInBars = surfPressure / Constants.Units.MILLIBARS_PER_BAR;
            return (1.0 / ((Math.Log(surfacePressureInBars) / -5050.5) + (1.0 / 373.0)));
        }
        
        /// <summary>
        ///	 This function is Fogg's eq.22.	 Given the volatile gas inventory and	
        ///	 planetary radius of a planet (in Km), this function returns the		
        ///	 fraction of the planet covered with water.								
        ///	 I have changed the function very slightly:	 the fraction of Earth's	
        ///	 surface covered by water is 71%, not 75% as Fogg used.
        /// </summary>
        /// <param name="volatileGasInventory"></param>
        /// <param name="planetRadius"></param>
        /// <returns></returns>
        public static double HydroFraction(double volatileGasInventory, double planetRadius)
        {
            double temp = (0.71 * volatileGasInventory / 1000.0) * Math.Pow(Constants.Sol.Earth.RADIUS_IN_KM / planetRadius, 2.0);
            if (temp >= 1.0)
                return (1.0);

            return (temp);
        }
        
        /// <summary>
        ///	 Given the surface temperature of a planet (in Kelvin), this function	
        ///	 returns the fraction of cloud cover available.	 This is Fogg's eq.23.	
        ///	 See Hart in "Icarus" (vol 33, pp23 - 39, 1978) for an explanation.		
        ///	 This equation is Hart's eq.3.											
        ///	 I have modified it slightly using constants and relationships from		
        ///	 Glass's book "Introduction to Planetary Geology", p.46.				
        ///	 The 'CLOUD_COVERAGE_FACTOR' is the amount of surface area on Earth		
        ///	 covered by one Kg. of cloud.
        /// </summary>
        /// <param name="surfTemp"></param>
        /// <param name="smallestMwRetained"></param>
        /// <param name="equatRadius"></param>
        /// <param name="hydroFraction"></param>
        /// <returns></returns>
        public static double CloudFraction(double surfTemp, double smallestMwRetained, double equatRadius, double hydroFraction)
        {
            if (smallestMwRetained > Constants.Gasses.H2O.AtomicWeight)
                return 0.0;

            double surfArea = 4.0 * Math.PI * Math.Pow(equatRadius, 2.0);
            double hydroMass = hydroFraction * surfArea * Constants.Sol.Earth.WATER_MASS_PER_AREA;
            double waterVaporInKg = (0.00000001 * hydroMass) * Math.Exp(Constants.Stargen.Q2_36 * (surfTemp - Constants.Sol.Earth.AVERAGE_KELVIN));
            double fraction = Constants.Units.CLOUD_COVERAGE_FACTOR * waterVaporInKg / surfArea;
            if (fraction >= 1.0)
                return (1.0);

            return (fraction);
        }

        /// <summary>
        ///	 Given the surface temperature of a planet (in Kelvin), this function	
        ///	 returns the fraction of the planet's surface covered by ice.  This is	
        ///	 Fogg's eq.24.	See Hart[24] in Icarus vol.33, p.28 for an explanation. 
        ///	 I have changed a constant from 70 to 90 in order to bring it more in	
        ///	 line with the fraction of the Earth's surface covered with ice, which	
        ///	 is approximatly .016 (=1.6%).
        /// </summary>
        /// <param name="hydroFraction"></param>
        /// <param name="surfTemp"></param>
        /// <returns></returns>
        public static double IceFraction(double hydroFraction, double surfTemp)
        {
            if (surfTemp > 328.0)
                surfTemp = 328.0;
            double temp = Math.Pow(((328.0 - surfTemp) / 90.0), 5.0);
            if (temp > (1.5 * hydroFraction))
                temp = (1.5 * hydroFraction);

            if (temp >= 1.0)
                return 1.0;

            return temp;
        }

        /// <summary>
        ///	This is Fogg's eq.19.  The ecosphere radius is given in AU, the orbital 
        ///	radius in AU, and the temperature returned is in Kelvin.				
        /// </summary>
        /// <param name="ecosphereRadius"></param>
        /// <param name="orbRadius"></param>
        /// <param name="albedo"></param>
        /// <returns></returns>
        public static double EffTemp(double ecosphereRadius, double orbRadius, double albedo)
        {
            return (Math.Sqrt(ecosphereRadius / orbRadius)
                   * Math.Sqrt(Math.Sqrt((1.0 - albedo) / (1.0 - Constants.Sol.Earth.ALBEDO)))
                   * Constants.Sol.Earth.EFFECTIVE_TEMP);
        }
        
        public static double EstTemp(double ecosphereRadius, double orbRadius, double albedo)
        {
            return (Math.Sqrt(ecosphereRadius / orbRadius)
                   * Math.Sqrt(Math.Sqrt((1.0 - albedo) / (1.0 - Constants.Sol.Earth.ALBEDO)))
                   * Constants.Sol.Earth.AVERAGE_KELVIN);
        }
        
        /// <summary>
        /// Old grnhouse:                                                           
        ///	Note that if the orbital radius of the planet is greater than or equal	
        ///	to R_inner, 99% of it's volatiles are assumed to have been deposited in 
        ///	surface reservoirs (otherwise, it suffers from the greenhouse effect).	
        /// 
        ///	if ((orb_radius &lt; r_greenhouse) && (zone == 1)) 
        /// 
        ///	The new definition is based on the inital surface temperature and what	
        ///	state water is in. If it's too hot, the water will never condense out	
        ///	of the atmosphere, rain down and form an ocean. The albedo used here	
        ///	was chosen so that the boundary is about the same as the old method		
        ///	Neither zone, nor r_greenhouse are used in this version				JLB	
        /// </summary>
        /// <param name="rEcosphere"></param>
        /// <param name="orbitalRadius"></param>
        /// <returns></returns>
        public static bool Greenhouse(double rEcosphere, double orbitalRadius)
        {
            double temp = EffTemp(rEcosphere, orbitalRadius, Constants.Units.GREENHOUSE_TRIGGER_ALBEDO);

            if (temp > Constants.Sol.Earth.FREEZING_POINT_OF_WATER)
                return true;

            return false;
        }

        /// <summary>
        ///	This is Fogg's eq.20, and is also Hart's eq.20 in his "Evolution of		
        ///	Earth's Atmosphere" article.  The effective temperature given is in		
        ///	units of Kelvin, as is the rise in temperature produced by the			
        ///	greenhouse effect, which is returned.									
        ///	I tuned this by changing a pow(x,.25) to pow(x,.4) to match Venus - JLB	
        /// </summary>
        /// <param name="opticalDepth"></param>
        /// <param name="effectiveTemp"></param>
        /// <param name="surfPressure"></param>
        /// <returns></returns>
        public static double GreenRise(double opticalDepth, double effectiveTemp, double surfPressure)
        {
            double convectionFactor = Constants.Sol.Earth.CONVECTION_FACTOR * Math.Pow(surfPressure / Constants.Sol.Earth.SURF_PRES_IN_MILLIBARS, 0.4);
            double rise = (Math.Sqrt(Math.Sqrt(1.0 + 0.75 * opticalDepth)) - 1.0) * effectiveTemp * convectionFactor;

            if (rise < 0.0) rise = 0.0;

            return rise;
        }
        
        /// <summary>
        ///	 The surface temperature passed in is in units of Kelvin.				
        ///	 The cloud adjustment is the fraction of cloud cover obscuring each		
        ///	 of the three major components of albedo that lie below the clouds.		
        /// </summary>
        /// <param name="waterFraction"></param>
        /// <param name="cloudFraction"></param>
        /// <param name="iceFraction"></param>
        /// <param name="surfPressure"></param>
        /// <returns></returns>
        public static double PlanetAlbedo(double waterFraction, double cloudFraction, double iceFraction, double surfPressure)
        {
            double rockPart, waterPart, icePart;

            double rockFraction = 1.0 - waterFraction - iceFraction;
            double components = 0.0;
            if (waterFraction > 0.0)
                components = components + 1.0;
            if (iceFraction > 0.0)
                components = components + 1.0;
            if (rockFraction > 0.0)
                components = components + 1.0;

            double cloudAdjustment = cloudFraction / components;

            if (rockFraction >= cloudAdjustment)
                rockFraction = rockFraction - cloudAdjustment;
            else
                rockFraction = 0.0;

            if (waterFraction > cloudAdjustment)
                waterFraction = waterFraction - cloudAdjustment;
            else
                waterFraction = 0.0;

            if (iceFraction > cloudAdjustment)
                iceFraction = iceFraction - cloudAdjustment;
            else
                iceFraction = 0.0;

            double cloudPart = cloudFraction * Constants.Units.CLOUD_ALBEDO;

            if (surfPressure == 0.0D)
            {
                rockPart = rockFraction * Constants.Units.ROCKY_AIRLESS_ALBEDO;	/* about(...,0.3); */
                icePart = iceFraction * Constants.Units.AIRLESS_ICE_ALBEDO;		/* about(...,0.4); */
                waterPart = 0;
            }
            else
            {
                rockPart = rockFraction * Constants.Units.ROCKY_ALBEDO;	/* about(...,0.1); */
                waterPart = waterFraction * Constants.Units.WATER_ALBEDO;	/* about(...,0.2); */
                icePart = iceFraction * Constants.Units.ICE_ALBEDO;		/* about(...,0.1); */
            }

            return (cloudPart + rockPart + waterPart + icePart);
        }
        
        /// <summary>
        ///	 This function returns the dimensionless quantity of optical depth,		
        ///	 which is useful in determining the amount of greenhouse effect on a	
        ///	 planet.																
        /// </summary>
        /// <param name="molecularWeight"></param>
        /// <param name="surfPressure"></param>
        /// <returns></returns>
        public static double Opacity(double molecularWeight, double surfPressure)
        {
            double opticalDepth = 0.0;
            if ((molecularWeight >= 0.0) && (molecularWeight < 10.0))
                opticalDepth = opticalDepth + 3.0;
            if ((molecularWeight >= 10.0) && (molecularWeight < 20.0))
                opticalDepth = opticalDepth + 2.34;
            if ((molecularWeight >= 20.0) && (molecularWeight < 30.0))
                opticalDepth = opticalDepth + 1.0;
            if ((molecularWeight >= 30.0) && (molecularWeight < 45.0))
                opticalDepth = opticalDepth + 0.15;
            if ((molecularWeight >= 45.0) && (molecularWeight < 100.0))
                opticalDepth = opticalDepth + 0.05;

            if (surfPressure >= (70.0 * Constants.Sol.Earth.SURF_PRES_IN_MILLIBARS))
                opticalDepth = opticalDepth * 8.333;
            else
                if (surfPressure >= (50.0 * Constants.Sol.Earth.SURF_PRES_IN_MILLIBARS))
                    opticalDepth = opticalDepth * 6.666;
                else
                    if (surfPressure >= (30.0 * Constants.Sol.Earth.SURF_PRES_IN_MILLIBARS))
                        opticalDepth = opticalDepth * 3.333;
                    else
                        if (surfPressure >= (10.0 * Constants.Sol.Earth.SURF_PRES_IN_MILLIBARS))
                            opticalDepth = opticalDepth * 2.0;
                        else
                            if (surfPressure >= (5.0 * Constants.Sol.Earth.SURF_PRES_IN_MILLIBARS))
                                opticalDepth = opticalDepth * 1.5;

            return opticalDepth;
        }
        
        /// <summary>
        /// calculates the number of years it takes for 1/e of a gas to escape
        ///	from a planet's atmosphere. 
        ///	Taken from Dole p. 34. He cites Jeans (1916) & Jones (1923)
        /// </summary>
        /// <param name="molecularWeight"></param>
        /// <param name="planet"></param>
        /// <returns></returns>
        public static double GasLife(double molecularWeight, Planet planet)
        {
            var v = RootMeanSquareVelocity(molecularWeight, planet.ExoSphericTemperature);
            var g = planet.SurfaceGravity * Constants.Sol.Earth.ACCELERATION;
            var r = (planet.Radius * Constants.Units.CM_PER_KM);
            var t = (Math.Pow(v, 3.0) / (2.0 * Math.Pow(g, 2.0) * r)) * Math.Exp((3.0 * g * r) / Math.Pow(v, 2.0));
            var years = t / (Constants.Units.SECONDS_PER_HOUR * 24.0 * Constants.Sol.Earth.DAYS_IN_A_YEAR);

            if (years > 2.0E10)
                years = Constants.Units.INCREDIBLY_LARGE_NUMBER;

            return years;
        }

        public static double MinMolecWeight(Planet planet)
        {
            var mass = planet.Mass;
            var radius = planet.Radius;
            var temp = planet.ExoSphericTemperature;
            var target = 5.0E9;

            var guess1 = MoleculeLimit(mass, radius, temp);
            var guess2 = guess1;

            double life = GasLife(guess1, planet);

            int loops = 0;

            if (null != planet.Primary)
            {
                target = planet.Primary.Age;
            }

            if (life > target)
            {
                while ((life > target) && (loops++ < 25))
                {
                    guess1 = guess1 / 2.0;
                    life = GasLife(guess1, planet);
                }
            }
            else
            {
                while ((life < target) && (loops++ < 25))
                {
                    guess2 = guess2 * 2.0;
                    life = GasLife(guess2, planet);
                }
            }

            loops = 0;

            while (((guess2 - guess1) > 0.1) && (loops++ < 25))
            {
                double guess3 = (guess1 + guess2) / 2.0;
                life = GasLife(guess3, planet);

                if (life < target)
                    guess1 = guess3;
                else
                    guess2 = guess3;
            }

            //TODO: find out why guess2 is used instead of life
            //TODO: find calculation for this function and double check math
            life = GasLife(guess2, planet);

            return guess2;
        }
        
        /// <summary>
        /// The temperature calculated is in degrees Kelvin.						
        ///	 Quantities already known which are used in these calculations:			
        ///		 planet->molec_weight												
        ///		 planet->surf_pressure												
        ///		 R_ecosphere														
        ///		 planet->a															
        ///		 planet->volatile_gas_inventory										
        ///		 planet->radius														
        ///		 planet->boil_point													
        /// </summary>
        /// <param name="planet"></param>
        /// <param name="first"></param>
        /// <param name="lastWater"></param>
        /// <param name="lastClouds"></param>
        /// <param name="lastIce"></param>
        /// <param name="lastTemp"></param>
        /// <param name="lastAlbedo"></param>
        public static void CalculateSurfaceTemp(Planet planet,
                                                bool first,
                                                double lastWater,
                                                double lastClouds,
                                                double lastIce,
                                                double lastTemp,
                                                double lastAlbedo)
        {
            double effectiveTemp;
            double greenhouseTemp;
            bool boilOff = false;

            if (first)
            {
                planet.Albedo = Constants.Sol.Earth.ALBEDO;

                effectiveTemp = EffTemp(planet.Primary.EcoSphereRadius, planet.SemiMajorAxis, planet.Albedo);
                greenhouseTemp = GreenRise(Opacity(planet.MolecularWeightRetained, planet.SurfacePressure), effectiveTemp, planet.SurfacePressure);
                planet.SurfaceTemperature = effectiveTemp + greenhouseTemp;

                SetTempRange(planet);
            }

            if (planet.HaGreenhouseEffect && planet.MaxTemperature < planet.BoilingPoint)
            {
                planet.HaGreenhouseEffect = false;

                planet.VolatileGasInventory = VolInventory(planet.Mass,
                                                           planet.EscapeVelocity,
                                                           planet.RootMeanSquaredVelocity,
                                                           planet.Primary.Mass,
                                                           planet.OrbitZone,
                                                           planet.HaGreenhouseEffect,
                                                           (planet.MassOfGas / planet.Mass) > 0.000001);
                planet.SurfacePressure = Pressure(planet.VolatileGasInventory, planet.Radius, planet.SurfaceGravity);

                planet.BoilingPoint = BoilingPoint(planet.SurfacePressure);
            }

            planet.HydrosphereCover = HydroFraction(planet.VolatileGasInventory, planet.Radius);
            planet.CloudCover = CloudFraction(planet.SurfaceTemperature,
                                               planet.MolecularWeightRetained,
                                               planet.Radius,
                                               planet.HydrosphereCover);
            planet.IceCover = IceFraction(planet.HydrosphereCover, planet.SurfaceTemperature);

            if ((planet.HaGreenhouseEffect) && (planet.SurfacePressure > 0.0))
                planet.CloudCover = 1.0;

            if ((planet.HighTemperature >= planet.BoilingPoint) && (!first)
                && !((int)planet.Day == (int)(planet.OrbitalPeriod * 24.0) || (planet.IsInResonantRotation)))
            {
                planet.HydrosphereCover = 0.0;
                boilOff = true;

                if (planet.MolecularWeightRetained > Constants.Gasses.MolecularWeights.WATER_VAPOR)
                    planet.CloudCover = 0.0;
                else
                    planet.CloudCover = 1.0;
            }

            if (planet.SurfaceTemperature < (Constants.Sol.Earth.FREEZING_POINT_OF_WATER - 3.0))
                planet.HydrosphereCover = 0.0;

            planet.Albedo = PlanetAlbedo(planet.HydrosphereCover,
                                                    planet.CloudCover,
                                                    planet.IceCover,
                                                    planet.SurfacePressure);

            effectiveTemp = EffTemp(planet.Primary.EcoSphereRadius, planet.SemiMajorAxis, planet.Albedo);
            greenhouseTemp = GreenRise(Opacity(planet.MolecularWeightRetained, planet.SurfacePressure), effectiveTemp, planet.SurfacePressure);
            planet.SurfaceTemperature = effectiveTemp + greenhouseTemp;

            if (!first)
            {
                if (!boilOff)
                {
                    planet.HydrosphereCover = (planet.HydrosphereCover + (lastWater * 2)) / 3;
                }
                planet.CloudCover = (planet.CloudCover + (lastClouds * 2)) / 3;
                planet.IceCover = (planet.IceCover + (lastIce * 2)) / 3;
                planet.Albedo = (planet.Albedo + (lastAlbedo * 2)) / 3;
                planet.SurfaceTemperature = (planet.SurfaceTemperature + (lastTemp * 2)) / 3;
            }

            SetTempRange(planet);
        }

        public static void IterateSurfaceTemp(Planet planet)
        {
            var initialTemp = EstTemp(planet.Primary.EcoSphereRadius, planet.SemiMajorAxis, planet.Albedo);

            CalculateSurfaceTemp(planet, true, 0, 0, 0, 0, 0);

            for (int count = 0; count <= 25; count++)
            {
                var lastWater = planet.HydrosphereCover;
                var lastClouds = planet.CloudCover;
                var lastIce = planet.IceCover;
                var lastTemp = planet.SurfaceTemperature;
                var lastAlbedo = planet.Albedo;

                CalculateSurfaceTemp(planet, true, lastWater, lastClouds, lastIce, lastTemp, lastAlbedo);

                if (Math.Abs(planet.SurfaceTemperature - lastTemp) < 0.25)
                    break;
            }

            planet.RiseInTemperatureDueToGreenhouse = planet.SurfaceTemperature - initialTemp;
        }

        /// <summary>
        ///	 Inspired partial pressure, taking into account humidification of the	
        ///	 air in the nasal passage and throat This formula is on Dole's p. 14	
        /// </summary>
        /// <param name="surfPressure"></param>
        /// <param name="gasPressure"></param>
        /// <returns></returns>
        public static double InspiredPartialPressure(double surfPressure, double gasPressure)
        {
            var fraction = gasPressure / surfPressure;
            return (surfPressure - Constants.Gasses.InspiredPartialPressure.H20_ASSUMED_PRESSURE) * fraction;
        }
        
        /// <summary>
        ///  This function uses figures on the maximum inspired partial pressures   
        ///  of Oxygen, other atmospheric and traces gases as laid out on pages 15,
        ///  16 and 18 of Dole's Habitable Planets for Man to derive breathability 
        ///  of the planet's atmosphere.                                       
        ///  JLB 
        /// </summary>
        /// <param name="planet"></param>
        /// <returns></returns>
        public static Breathability Breathable(Planet planet)
        {
            // TODO: Not all races breathe the same, each race should have a check, not one on the planet
            bool oxygenOk = false;
            int index;

            if (planet.Gases.Count == 0)
                return Breathability.None;

            for (index = 0; index < planet.Gases.Count; index++)
            {
                Molecule gas = Constants.Gasses.GasLookup[planet.Gases[index].ElementId];

                double ipp = InspiredPartialPressure(planet.SurfacePressure, planet.Gases[index].SurfacePressure);

                if (ipp > gas.MaximumInspiredPartialPressure)
                    return Breathability.Poisonous;

                if (gas.Id == Constants.Gasses.O.Id)
                    oxygenOk = ((ipp >= Constants.Gasses.InspiredPartialPressure.MIN_O2_IPP) && (ipp <= Constants.Gasses.InspiredPartialPressure.MAX_O2_IPP));
            }

            if (oxygenOk)
                return Breathability.Breathable;

            return Breathability.UnBreathable;
        }

        public static void SetTempRange(Planet planet)
        {
            var pressmod = 1 / Math.Sqrt(1 + 20 * planet.SurfacePressure / 1000.0);
            var ppmod = 1 / Math.Sqrt(10 + 5 * planet.SurfacePressure / 1000.0);
            var tiltmod = Math.Abs(Math.Cos(planet.AxialTilt * Math.PI / 180) * Math.Pow(1 + planet.Eccentricity, 2));
            var daymod = 1 / (200 / planet.Day + 1);
            var mh = Math.Pow(1 + daymod, pressmod);
            var ml = Math.Pow(1 - daymod, pressmod);
            var hi = mh * planet.SurfaceTemperature;
            var lo = ml * planet.SurfaceTemperature;
            var sh = hi + Math.Pow((100 + hi) * tiltmod, Math.Sqrt(ppmod));
            var wl = lo - Math.Pow((150 + lo) * tiltmod, Math.Sqrt(ppmod));
            var max = planet.SurfaceTemperature + Math.Sqrt(planet.SurfaceTemperature) * 10;
            var min = planet.SurfaceTemperature / Math.Sqrt(planet.Day + 24);

            if (lo < min) lo = min;
            if (wl < 0) wl = 0;

            planet.HighTemperature = MathUtilities.Soft(hi, max, min);
            planet.LowTemperature = MathUtilities.Soft(lo, max, min);
            planet.MaxTemperature = MathUtilities.Soft(sh, max, min);
            planet.MinTemperature = MathUtilities.Soft(wl, max, min);
        }
    }
}
