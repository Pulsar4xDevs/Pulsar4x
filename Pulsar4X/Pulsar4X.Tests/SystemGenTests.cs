using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    [TestFixture]
    public class SystemGenTests
    {
        private Game _game;

        [OneTimeSetUpAttribute]
        public void GlobalInit()
        {
            _game = Game.NewGame("Unit Test Game", DateTime.Now, 10); // init the game class as we will need it for these tests.
        }

        [Test]
        [Ignore("TODO: why is this test ignored?")]
        [Description("Outputs all the systems generated in the init of this test to XML")]
        public void OutputToXML()
        {
            XmlSerializer ser = new XmlSerializer(typeof(XmlNode));
            TextWriter writer = new StreamWriter(".\\SystemsExport.xml");

            XmlDocument xmlDoc = new XmlDocument();
            XmlNode toplevelNode = xmlDoc.CreateNode(XmlNodeType.Element, "Systems", "NS");

            foreach (KeyValuePair<Guid, StarSystem> kvp in _game.Systems)
            {
                StarSystem system = kvp.Value;
                var systemBody = system.SystemManager.GetFirstEntityWithDataBlob<OrbitDB>();

                // get root star:
                var tree = systemBody.GetDataBlob<OrbitDB>();
                systemBody = tree.Root;

                XmlNode systemNode = xmlDoc.CreateNode(XmlNodeType.Element, "System", "NS");

                // the following we serialize the body to xml, and will do the same for all child bodies:
                SerializeBodyToXML(xmlDoc, systemNode, systemBody, tree);

                // add xml to to level node:
                toplevelNode.AppendChild(systemNode);
            }

            // save xml to file:
            ser.Serialize(writer, toplevelNode);
            writer.Close();
        }

        private void SerializeBodyToXML(XmlDocument xmlDoc, XmlNode systemNode, Entity systemBody, OrbitDB orbit)
        {
            // get the datablobs:
            var systemBodyDB = systemBody.GetDataBlob<SystemBodyDB>();
            var starIfnoDB = systemBody.GetDataBlob<StarInfoDB>();
            var positionDB = systemBody.GetDataBlob<PositionDB>();
            var massVolumeDB = systemBody.GetDataBlob<MassVolumeDB>();
            var nameDB = systemBody.GetDataBlob<NameDB>();
            var atmosphereDB = systemBody.GetDataBlob<AtmosphereDB>();
            var ruinsDB = systemBody.GetDataBlob<RuinsDB>();

            // create the body node:
            XmlNode bodyNode = xmlDoc.CreateNode(XmlNodeType.Element, "Body", "NS");

            // save parent id first:
            XmlNode varNode = xmlDoc.CreateNode(XmlNodeType.Element, "ParentID", "NS");
            if (orbit.Parent != null)
                varNode.InnerText = orbit.Parent.Guid.ToString();
            else
                varNode.InnerText = Guid.Empty.ToString();
            bodyNode.AppendChild(varNode);

            // then add our ID to at the end:
            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "ID", "NS");
            varNode.InnerText = systemBody.Guid.ToString();
            bodyNode.AppendChild(varNode);

            if (nameDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Name", "NS");
                varNode.InnerText = nameDB.DefaultName;
                bodyNode.AppendChild(varNode);
            }
            
            if (starIfnoDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Type", "NS");
                varNode.InnerText = starIfnoDB.SpectralType.ToString();
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Class", "NS");
                varNode.InnerText = starIfnoDB.Class;
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Age", "NS");
                varNode.InnerText = starIfnoDB.Age.ToString("N0");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "AverageEcoSphereRadius", "NS");
                varNode.InnerText = starIfnoDB.EcoSphereRadius.ToString("N3");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "MinEcoSphereRadius", "NS");
                varNode.InnerText = starIfnoDB.MinHabitableRadius.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "MaxEcoSphereRadius", "NS");
                varNode.InnerText = starIfnoDB.MaxHabitableRadius.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Luminosity", "NS");
                varNode.InnerText = starIfnoDB.Luminosity.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Temperature", "NS");
                varNode.InnerText = starIfnoDB.Temperature.ToString("N0");
                bodyNode.AppendChild(varNode);
            }

            if (massVolumeDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "MassInKG", "NS");
                varNode.InnerText = massVolumeDB.Mass.ToString("N0");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "MassInEarthMasses", "NS");
                varNode.InnerText = (massVolumeDB.Mass / GameConstants.Units.EarthMassInKG).ToString("N2");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Density", "NS");
                varNode.InnerText = massVolumeDB.Density.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Radius", "NS");
                varNode.InnerText = massVolumeDB.RadiusInKM.ToString("N0");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Volume", "NS");
                varNode.InnerText = massVolumeDB.Volume.ToString("N0");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "SurfaceGravity", "NS");
                varNode.InnerText = massVolumeDB.SurfaceGravity.ToString("N4");
                bodyNode.AppendChild(varNode);
            }

            // add orbit details:
            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "SemiMajorAxis", "NS");
            varNode.InnerText = orbit.SemiMajorAxis.ToString("N3");
            bodyNode.AppendChild(varNode);

            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Apoapsis", "NS");
            varNode.InnerText = orbit.Apoapsis.ToString("N3");
            bodyNode.AppendChild(varNode);

            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Periapsis", "NS");
            varNode.InnerText = orbit.Periapsis.ToString("N3");
            bodyNode.AppendChild(varNode);

            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Year", "NS");
            varNode.InnerText = orbit.OrbitalPeriod.ToString("dd\\:hh\\:mm");
            bodyNode.AppendChild(varNode);

            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Eccentricity", "NS");
            varNode.InnerText = orbit.Eccentricity.ToString("N3");
            bodyNode.AppendChild(varNode);

            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Inclination", "NS");
            varNode.InnerText = orbit.Inclination.ToString("N2");
            bodyNode.AppendChild(varNode);

            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Children", "NS");
            varNode.InnerText = orbit.Children.Count.ToString();
            bodyNode.AppendChild(varNode);

            if (positionDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "PositionInAU", "NS");
                varNode.InnerText = "(" + positionDB.X.ToString("N3") + ", " + positionDB.Y.ToString("N3") + ", " + positionDB.Z.ToString("N3") + ")";
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "PositionInKm", "NS");
                varNode.InnerText = "(" + positionDB.XInKm.ToString("N3") + ", " + positionDB.YInKm.ToString("N3") + ", " + positionDB.ZInKm.ToString("N3") + ")";
                bodyNode.AppendChild(varNode);
            }

            if (systemBodyDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Type", "NS");
                varNode.InnerText = systemBodyDB.Type.ToString();
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "AxialTilt", "NS");
                varNode.InnerText = systemBodyDB.AxialTilt.ToString("N1");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Temperature", "NS");
                varNode.InnerText = systemBodyDB.BaseTemperature.ToString("N1");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "LengthOfDay", "NS");
                varNode.InnerText = systemBodyDB.LengthOfDay.ToString("g");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "MagneticField", "NS");
                varNode.InnerText = systemBodyDB.MagneticField.ToString("N2");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Tectonics", "NS");
                varNode.InnerText = systemBodyDB.Tectonics.ToString();
                bodyNode.AppendChild(varNode);
            }

            if (atmosphereDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Atmosphere", "NS");
                varNode.InnerText = atmosphereDB.AtomsphereDescriptionInPercent;
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "AtmosphereInATM", "NS");
                varNode.InnerText = atmosphereDB.AtomsphereDescriptionAtm;
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Pressure", "NS");
                varNode.InnerText = atmosphereDB.Pressure.ToString("N2");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Albedo", "NS");
                varNode.InnerText = atmosphereDB.Albedo.ToString("p1");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "SurfaceTemperature", "NS");
                varNode.InnerText = atmosphereDB.SurfaceTemperature.ToString("N1");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "GreenhouseFactor", "NS");
                varNode.InnerText = atmosphereDB.GreenhouseFactor.ToString("N2");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "GreenhousePressure", "NS");
                varNode.InnerText = atmosphereDB.GreenhousePressure.ToString("N2");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "HasHydrosphere", "NS");
                varNode.InnerText = atmosphereDB.Hydrosphere.ToString();
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "HydrosphereExtent", "NS");
                varNode.InnerText = atmosphereDB.HydrosphereExtent.ToString();
                bodyNode.AppendChild(varNode);
            }

            if (ruinsDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "RuinCount", "NS");
                varNode.InnerText = ruinsDB.RuinCount.ToString();
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "RuinQuality", "NS");
                varNode.InnerText = ruinsDB.RuinQuality.ToString();
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "RuinSize", "NS");
                varNode.InnerText = ruinsDB.RuinSize.ToString();
                bodyNode.AppendChild(varNode);
            }

            // add body node to system node:
            systemNode.AppendChild(bodyNode);

            // call recursively for children:
            foreach (var child in orbit.Children)
            {
                OrbitDB o = child.GetDataBlob<OrbitDB>();
                if (o != null)
                    SerializeBodyToXML(xmlDoc, systemNode, o.OwningEntity, o);
            }
        }

        [Test]
        [Description("Creates and tests a single star system")]
        public void CreateAndFillStarSystem()
        {
            _game = Game.NewGame("Unit Test Game", DateTime.Now, 0); // reinit with empty game, so we can do a clean test.
            StarSystemFactory ssf = new StarSystemFactory(_game);
            var system = ssf.CreateSystem(_game, "Argon Prime"); // Keeping with the X3 theme :P

            // lets test that the stars generated okay:
            var stars = system.SystemManager.GetAllEntitiesWithDataBlob<StarInfoDB>();
            Assert.IsNotEmpty(stars);

            if (stars.Count > 1)
            {
                Entity rootStar = stars[0].GetDataBlob<OrbitDB>().Root;
                double highestMass = rootStar.GetDataBlob<MassVolumeDB>().Mass;
                Entity highestMassStar = rootStar;
                foreach (Entity star in stars)
                {
                    var massDB = star.GetDataBlob<MassVolumeDB>();
                    if (massDB.Mass > highestMass)
                        highestMassStar = star;
                }

                // the first star in the system should have the highest mass:
                Assert.AreSame(rootStar, highestMassStar);
            }
        }

        [Test]
        [Description("generates 1000 test systems to test performance of the run.")]
        public void PerformanceTest()
        {
            // use a stop watch to get more accurate time.
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();

            const int numSystems = 1000;
            _game = Game.NewGame("Unit Test Game", DateTime.Now, 0); // reinit with empty game, so we can do a clean test.
            GC.Collect();

            StarSystemFactory ssf = new StarSystemFactory(_game);

            // lets get our memory before starting:
            long startMemory = GC.GetTotalMemory(true); 
            timer.Start();

            for (int i = 0; i < numSystems; i++)
            {
                ssf.CreateSystem(_game, "Performance Test No " + i, i);
            }

            timer.Stop();
            double totalTime = timer.Elapsed.TotalSeconds;

            int totalEntities = 0;
            foreach (KeyValuePair<Guid, StarSystem> system in _game.Systems)
            {
                List<Entity> entities = system.Value.SystemManager.GetAllEntitiesWithDataBlob<OrbitDB>();
                totalEntities += entities.Count;
            }

            long endMemory = GC.GetTotalMemory(true); 
            double totalMemory = (endMemory - startMemory) / 1024.0;  // in KB

            // note that because we do 1000 systems total time taken as milliseconds is the time for a single system, on average.
            string output = String.Format("Total run time: {0}s, per system: {1}ms. total memory used: {2} MB, per system: {3} KB. Total Entities: {4}, per system: {5}. Memory per entity: {6}KB", 
                totalTime.ToString("N4"), 
                ((totalTime/numSystems) * 1000).ToString("N2"), 
                (totalMemory / 1024.0).ToString("N2"),
                (totalMemory / numSystems).ToString("N2"), 
                totalEntities, totalEntities / (float)numSystems, 
                (totalMemory / totalEntities).ToString("N2"));

            // print results:
            Console.WriteLine(output);
            Assert.Pass(output);
        }

        //[Test]
        //[Description("generates 1000 test systems using pre-ECS system Gen to test performance of the run.")]
        //[Ignore("TODO: why is this test ignored?")]
        //public void OldSystemGenPerformanceTest()
        //{
        //    // use a stop watch to get more accurate time.
        //    System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();

        //    // lets get our memory before starting:
        //    long startMemory = GC.GetTotalMemory(true);

        //    timer.Start();
        //    for (int i = 0; i < 1000; i++)
        //    {
        //        SystemGen.CreateSystem("Performance Test No " + i.ToString());
        //    }

        //    timer.Stop();
        //    double totalTime = timer.Elapsed.TotalSeconds;

        //    long endMemory = GC.GetTotalMemory(true);
        //    double totalMemory = (endMemory - startMemory) / 1024.0;  // in KB

        //    // note that because we do 1000 systems total time taken as milliseconds is the time for a single system, on average.
        //    string output = string.Format("Total run time: {0}s, per system: {1}ms. total memory used: {2} MB, per system: {3} KB.",
        //        totalTime.ToString("N4"), (totalTime).ToString("N2"), (totalMemory / 1024.0).ToString("N2"), (totalMemory / 1000).ToString("N2"));

        //    // print results:
        //    Console.WriteLine(output);
        //    Assert.Pass(output);
        //}
    }
}