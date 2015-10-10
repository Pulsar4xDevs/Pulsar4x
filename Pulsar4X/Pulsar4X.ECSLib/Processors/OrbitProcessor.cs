using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.dotMemoryUnit;
using NUnit.Framework;

namespace Pulsar4X.ECSLib
{
    public static class OrbitProcessor
    {
        /// <summary>
        /// TypeIndexes for several dataBlobs used frequently by this processor.
        /// </summary>
        private static int _orbitTypeIndex = -1;
        private static int _positionTypeIndex = -1;
        private static int _starInfoTypeIndex = -1;

        /// <summary>
        /// Determines if this processor should use multithreading.
        /// </summary>
        public const bool UseMultiThread = false;

        /// <summary>
        /// Initializes this Processor.
        /// </summary>
        internal static void Initialize()
        {
            // Resolve TypeIndexes.
            _orbitTypeIndex = EntityManager.GetTypeIndex<OrbitDB>();
            _positionTypeIndex = EntityManager.GetTypeIndex<PositionDB>();
            _starInfoTypeIndex = EntityManager.GetTypeIndex<StarInfoDB>();
        }

        /// <summary>
        /// Function called by Game.RunProcessors to run this processor.
        /// </summary>
        internal static int Process(Game game, List<StarSystem> systems, int deltaSeconds)
        {

            DateTime currentTime = game.CurrentDateTime;

            int orbitsProcessed = 0;

            if (UseMultiThread)
            {
                Parallel.ForEach(systems, system => UpdateSystemOrbits(system, currentTime, ref orbitsProcessed));
            }
            else
            {
                foreach (StarSystem system in systems)
                {
                    UpdateSystemOrbits(system, currentTime, ref orbitsProcessed);
                }
            }
            return orbitsProcessed;
        }

        private static void UpdateSystemOrbits(StarSystem system, DateTime currentTime, ref int orbitsProcessed)
        {
            EntityManager currentManager = system.SystemManager;

            // Find the first orbital entity.
            Entity firstOrbital = currentManager.GetFirstEntityWithDataBlob(_starInfoTypeIndex);

            if (!firstOrbital.IsValid)
            {
                // No orbitals in this manager.
                return;
            }

            Entity root = firstOrbital.GetDataBlob<OrbitDB>(_orbitTypeIndex).Root;
            PositionDB rootPositionDB = root.GetDataBlob<PositionDB>(_positionTypeIndex);

            // Call recursive function to update every orbit in this system.
            UpdateOrbit(root, rootPositionDB, currentTime, ref orbitsProcessed);
        }

        private static void UpdateOrbit(Entity entity, PositionDB parentPositionDB, DateTime currentTime, ref int orbitsProcessed)
        {
            OrbitDB entityOrbitDB = entity.GetDataBlob<OrbitDB>(_orbitTypeIndex);
            PositionDB entityPosition = entity.GetDataBlob<PositionDB>(_positionTypeIndex);

            // Get our Parent-Relative coordinates.
            Vector4 newPosition = GetPosition(entityOrbitDB, currentTime);

            // Get our Absolute coordinates.
            entityPosition.Position = parentPositionDB.Position + newPosition;

            Interlocked.Increment(ref orbitsProcessed);

            // Update our children.
            foreach (Entity child in entityOrbitDB.Children)
            {
                // RECURSION!
                UpdateOrbit(child, entityPosition, currentTime, ref orbitsProcessed);
            }
            // use this to dump positions to plot orbits
            /*
            string name = entity.GetDataBlob<NameDB>().DefaultName;
            if (name.Equals("Mercury")) {
              Console.WriteLine(newPosition.X + " " + newPosition.Y);
            }
            */
        }

        #region Orbit Position Calculations

        /// <summary>
        /// Calculates the parent-relative cartesian coordinate of an orbit for a given time.
        /// </summary>
        /// <param name="orbit">OrbitDB to calculate position from.</param>
        /// <param name="time">Time position desired from.</param>
        public static Vector4 GetPosition(OrbitDB orbit, DateTime time)
        {
            if (orbit.IsStationary)
            {
                return new Vector4(0, 0, 0, 0);
            }
            return GetPosition(orbit, GetTrueAnomaly(orbit, time));
        }

        public static double GetTrueAnomaly(OrbitDB orbit, DateTime time)
        {
            TimeSpan timeSinceEpoch = time - orbit.Epoch;

            // Don't attempt to calculate large timeframes.
            if (timeSinceEpoch > orbit.OrbitalPeriod && orbit.OrbitalPeriod.Ticks != 0)
            {
                long years = (timeSinceEpoch.Ticks / orbit.OrbitalPeriod.Ticks);
                timeSinceEpoch -= TimeSpan.FromTicks(years * orbit.OrbitalPeriod.Ticks);
                orbit.Epoch += TimeSpan.FromTicks(years * orbit.OrbitalPeriod.Ticks);
            }

            // http://en.wikipedia.org/wiki/Mean_anomaly (M = M0 + nT)
            // Convert MeanAnomaly to radians.
            double currentMeanAnomaly = Angle.ToRadians(orbit.MeanAnomaly);
            // Add nT
            currentMeanAnomaly += Angle.ToRadians(orbit.MeanMotion) * timeSinceEpoch.TotalSeconds;


            double eccentricAnomaly = GetEccentricAnomaly(orbit, currentMeanAnomaly);
            return Math.Atan2(Math.Sqrt(1 - orbit.Eccentricity * orbit.Eccentricity) * Math.Sin(eccentricAnomaly), Math.Cos(eccentricAnomaly) - orbit.Eccentricity);
        }

        /// <summary>
        /// Calculates the cartesian coordinates (relative to it's parent) of an orbit for a given angle.
        /// </summary>
        /// <param name="orbit">OrbitDB to calculate position from.</param>
        /// <param name="trueAnomaly">Angle in Radians.</param>
        public static Vector4 GetPosition(OrbitDB orbit, double trueAnomaly)
        {
            
            if (orbit.IsStationary)
            {
                return new Vector4(0, 0, 0, 0);
            }

            // http://en.wikipedia.org/wiki/True_anomaly#Radius_from_true_anomaly
            double radius = Distance.ToKm(orbit.SemiMajorAxis) * (1 - orbit.Eccentricity * orbit.Eccentricity) / (1 + orbit.Eccentricity * Math.Cos(trueAnomaly));

            // Adjust TrueAnomaly by the Argument of Periapsis (converted to radians)
            trueAnomaly += Angle.ToRadians(orbit.ArgumentOfPeriapsis);
            double inclination = Angle.ToRadians(orbit.Inclination);

            // Convert KM to AU
            radius = Distance.ToAU(radius);

            //https://downloads.rene-schwarz.com/download/M001-Keplerian_Orbit_Elements_to_Cartesian_State_Vectors.pdf
            double lofAN = Angle.ToRadians(orbit.LongitudeOfAscendingNode);
            double aofP = Angle.ToRadians(orbit.ArgumentOfPeriapsis);
            double tA = trueAnomaly;
            double incl = inclination;

            double x = Math.Cos(lofAN) * Math.Cos(aofP + tA) - Math.Sin(lofAN) * Math.Sin(aofP + tA) * Math.Cos(incl);
            double y = Math.Sin(lofAN) * Math.Cos(aofP + tA) + Math.Cos(lofAN) * Math.Sin(aofP + tA) * Math.Cos(incl);
            double z = Math.Sin(incl) * Math.Sin(aofP + tA);

            return new Vector4(x, y, z, 0) * radius;
        }

        /// <summary>
        /// Calculates the current Eccentric Anomaly given certain orbital parameters.
        /// </summary>
        private static double GetEccentricAnomaly(OrbitDB orbit, double currentMeanAnomaly)
        {
            //Kepler's Equation
            const int numIterations = 100;
            var e = new double[numIterations];
            const double epsilon = 1E-12; // Plenty of accuracy.
            int i = 0;

            if (orbit.Eccentricity > 0.8)
            {
                e[i] = Math.PI;
            }
            else
            {
                e[i] = currentMeanAnomaly;
            }

            do
            {
                // Newton's Method.
                /*					 E(n) - e sin(E(n)) - M(t)
                 * E(n+1) = E(n) - ( ------------------------- )
                 *					      1 - e cos(E(n)
                 * 
                 * E == EccentricAnomaly, e == Eccentricity, M == MeanAnomaly.
                 * http://en.wikipedia.org/wiki/Eccentric_anomaly#From_the_mean_anomaly
                */
                e[i + 1] = e[i] - ((e[i] - orbit.Eccentricity * Math.Sin(e[i]) - currentMeanAnomaly) / (1 - orbit.Eccentricity * Math.Cos(e[i])));
                i++;
            } while (Math.Abs(e[i] - e[i - 1]) > epsilon && i + 1 < numIterations);

            if (i + 1 >= numIterations)
            {
                // <? todo: Flag an error about non-convergence of Newton's method.
            }

            return e[i - 1];
        }

        #endregion
    }

    public class OrbitProcessorTests
    {
        private Game _game;
        private List<StarSystem> _systems;
        private const int NumSystems = 10;

        Stopwatch timer = new Stopwatch();

        // Declare variables before usage to keep memory usage constant.
        long startMemory;
        long endMemory;
        int orbitsProcessed;
        private string output;

        public void Init()
        {
            _game = Game.NewGame("Unit Test Game", DateTime.Now, NumSystems); // init the game class as we will need it for these tests.
        }

        // Note: This is a memory test, and is designed to be used with an external profiler called from the UI.
        public void OrbitStressTest()
        {
            // use a stop watch to get more accurate time.
            timer = new Stopwatch();

            // Declare variables before usage to keep memory usage constant.

            // lets get our memory before starting:
            GC.Collect();
            startMemory = GC.GetTotalMemory(true);

            timer.Start();

            orbitsProcessed = OrbitProcessor.Process(_game, _game.StarSystems, 60);

            timer.Stop();

            // Check memory afterwords.
            // Note: dotMemory.Check doesn't work unless run with dotMemory unit.
            GC.Collect();
        }
    }
}