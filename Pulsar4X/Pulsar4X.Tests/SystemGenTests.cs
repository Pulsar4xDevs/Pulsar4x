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

        [TestFixtureSetUp]
        public void GlobalInit()
        {
            _game = new Game("Unit Test Game", 10); // init the game class as we will need it for these tests.
        }

        [Test]
        [Description("Outputs all the systems generated in the init of this test to XML")]
        public void OutputToXML()
        {
            XmlSerializer ser = new XmlSerializer(typeof(XmlNode));
            TextWriter writer = new StreamWriter(".\\test.xml");

            XmlDocument xmlDoc = new XmlDocument();
            XmlNode toplevelNode = xmlDoc.CreateNode(XmlNodeType.Element, "Systems", "NS");

            foreach (var system in _game.Systems)
            {
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

            if (nameDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Name", "NS");
                try
                {
                    varNode.InnerText = nameDB.DefaultName;
                }
                catch (Exception)
                {
                    varNode.InnerText = "Error, no default Name!";
                }
                
                bodyNode.AppendChild(varNode);
            }

            if (systemBodyDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Type", "NS");
                varNode.InnerText = systemBodyDB.Type.ToString();
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "AxialTilt", "NS");
                varNode.InnerText = systemBodyDB.AxialTilt.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "BaseTemperature", "NS");
                varNode.InnerText = systemBodyDB.BaseTemperature.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "LengthOfDay", "NS");
                varNode.InnerText = systemBodyDB.LengthOfDay.ToString();
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "MagneticField", "NS");
                varNode.InnerText = systemBodyDB.MagneticField.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Tectonics", "NS");
                varNode.InnerText = systemBodyDB.Tectonics.ToString();
                bodyNode.AppendChild(varNode);
            }
            
            if (starIfnoDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Class", "NS");
                varNode.InnerText = starIfnoDB.Class;
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Age", "NS");
                varNode.InnerText = starIfnoDB.Age.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "EcoSphereRadius", "NS");
                varNode.InnerText = starIfnoDB.EcoSphereRadius.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Luminosity", "NS");
                varNode.InnerText = starIfnoDB.Luminosity.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "SpectralType", "NS");
                varNode.InnerText = starIfnoDB.SpectralType.ToString();
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Temperature", "NS");
                varNode.InnerText = starIfnoDB.Temperature.ToString("N4");
                bodyNode.AppendChild(varNode);
            }

            if (massVolumeDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Mass", "NS");
                varNode.InnerText = massVolumeDB.Mass.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Radius", "NS");
                varNode.InnerText = massVolumeDB.RadiusInKM.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Density", "NS");
                varNode.InnerText = massVolumeDB.Density.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Volume", "NS");
                varNode.InnerText = massVolumeDB.Volume.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "SurfaceGravity", "NS");
                varNode.InnerText = massVolumeDB.SurfaceGravity.ToString("N4");
                bodyNode.AppendChild(varNode);
            }

            // add orbit details:
            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "SemiMajorAxis", "NS");
            varNode.InnerText = orbit.SemiMajorAxis.ToString("N4");
            bodyNode.AppendChild(varNode);

            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Apoapsis", "NS");
            varNode.InnerText = orbit.Apoapsis.ToString("N4");
            bodyNode.AppendChild(varNode);

            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Periapsis", "NS");
            varNode.InnerText = orbit.Periapsis.ToString("N4");
            bodyNode.AppendChild(varNode);

            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Year", "NS");
            varNode.InnerText = orbit.OrbitalPeriod.ToString();
            bodyNode.AppendChild(varNode);

            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Eccentricity", "NS");
            varNode.InnerText = orbit.Eccentricity.ToString("N4");
            bodyNode.AppendChild(varNode);

            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Inclination", "NS");
            varNode.InnerText = orbit.Inclination.ToString("N4");
            bodyNode.AppendChild(varNode);

            if (atmosphereDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Atmosphere", "NS");
                varNode.InnerText = atmosphereDB.AtomsphereDescriptionInPercent;
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "AtmosphereInATM", "NS");
                varNode.InnerText = atmosphereDB.AtomsphereDescriptionAtm;
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Pressure", "NS");
                varNode.InnerText = atmosphereDB.Pressure.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Albedo", "NS");
                varNode.InnerText = atmosphereDB.Albedo.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "SurfaceTemperature", "NS");
                varNode.InnerText = atmosphereDB.SurfaceTemperature.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "GreenhouseFactor", "NS");
                varNode.InnerText = atmosphereDB.GreenhouseFactor.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "GreenhousePressure", "NS");
                varNode.InnerText = atmosphereDB.GreenhousePressure.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Hydrosphere", "NS");
                varNode.InnerText = atmosphereDB.Hydrosphere.ToString();
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "HydrosphereExtent", "NS");
                varNode.InnerText = atmosphereDB.HydrosphereExtent.ToString();
                bodyNode.AppendChild(varNode);
            }

            if (positionDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "PosX", "NS");
                varNode.InnerText = positionDB.X.ToString("N5");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "PosY", "NS");
                varNode.InnerText = positionDB.Y.ToString("N5");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "PosZ", "NS");
                varNode.InnerText = positionDB.Z.ToString("N5");
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

            // add our ID to at the end:
            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "ID", "NS");
            varNode.InnerText = systemBody.Guid.ToString();
            bodyNode.AppendChild(varNode);

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
            foreach (StarSystem system in _game.Systems)
            {
                List<Entity> entities = system.SystemManager.GetAllEntitiesWithDataBlob<PositionDB>();
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

        [Test]
        [Description("generates 1000 test systems to test performance of the run.")]
        [Ignore]
        public void OldSystemGenPerformanceTest()
        {
            // use a stop watch to get more accurate time.
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();

            // lets get our memory before starting:
            long startMemory = GC.GetTotalMemory(true);

            timer.Start();
            for (int i = 0; i < 1000; i++)
            {
                SystemGen.CreateSystem("Performance Test No " + i.ToString());
            }

            timer.Stop();
            double totalTime = timer.Elapsed.TotalSeconds;

            long endMemory = GC.GetTotalMemory(true);
            double totalMemory = (endMemory - startMemory) / 1024.0;  // in KB

            // note that because we do 1000 systems total time taken as milliseconds is the time for a single system, on average.
            string output = string.Format("Total run time: {0}s, per system: {1}ms. total memory used: {2} MB, per system: {3} KB.",
                totalTime.ToString("N4"), (totalTime).ToString("N2"), (totalMemory / 1024.0).ToString("N2"), (totalMemory / 1000).ToString("N2"));

            // print results:
            Console.WriteLine(output);
            Assert.Pass(output);
        }
    }
}