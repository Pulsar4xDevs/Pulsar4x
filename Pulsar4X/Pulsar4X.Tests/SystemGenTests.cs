using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using NUnit.Framework;
using Pulsar4X.ECSLib;
using Pulsar4X.Entities;
using StarSystem = Pulsar4X.ECSLib.StarSystem;

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
                while (tree.ParentDB != null)
                {
                    tree = tree.ParentDB as OrbitDB;
                }
                systemBody = tree.OwningEntity;

                XmlNode systemNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "System", "NS");

                // the following we serilize the body to xml, and will do the same for all child bodies:
                SerilizeBodyToXML(xmlDoc, systemNdoe, systemBody, tree);

                // add xml to to level node:
                toplevelNode.AppendChild(systemNdoe);
            }

            // save xml to file:
            ser.Serialize(writer, toplevelNode);
            writer.Close();
        }

        private void SerilizeBodyToXML(XmlDocument xmlDoc, XmlNode systemNdoe, Entity systemBody, OrbitDB orbit)
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
            XmlNode varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "ParentID", "NS");
            if (orbit.Parent != null)
                varNdoe.InnerText = orbit.Parent.Guid.ToString();
            else
                varNdoe.InnerText = Guid.Empty.ToString();
            bodyNode.AppendChild(varNdoe);

            if (nameDB != null)
            {
                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "Name", "NS");
                string temp = "";

                foreach (var name in nameDB.Name)
                {
                    temp += name.Value + ", ";
                }

                varNdoe.InnerText = temp;
                bodyNode.AppendChild(varNdoe);
            }

            if (systemBodyDB != null)
            {
                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "Type", "NS");
                varNdoe.InnerText = systemBodyDB.Type.ToString();
                bodyNode.AppendChild(varNdoe);

                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "AxialTilt", "NS");
                varNdoe.InnerText = systemBodyDB.AxialTilt.ToString("N4");
                bodyNode.AppendChild(varNdoe);

                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "BaseTemperature", "NS");
                varNdoe.InnerText = systemBodyDB.BaseTemperature.ToString("N4");
                bodyNode.AppendChild(varNdoe);

                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "LengthOfDay", "NS");
                varNdoe.InnerText = systemBodyDB.LengthOfDay.ToString();
                bodyNode.AppendChild(varNdoe);

                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "MagneticField", "NS");
                varNdoe.InnerText = systemBodyDB.MagneticField.ToString("N4");
                bodyNode.AppendChild(varNdoe);

                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "Tectonics", "NS");
                varNdoe.InnerText = systemBodyDB.Tectonics.ToString();
                bodyNode.AppendChild(varNdoe);
            }
            
            if (starIfnoDB != null)
            {
                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "Class", "NS");
                varNdoe.InnerText = starIfnoDB.Class;
                bodyNode.AppendChild(varNdoe);

                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "Age", "NS");
                varNdoe.InnerText = starIfnoDB.Age.ToString("N4");
                bodyNode.AppendChild(varNdoe);

                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "EcoSphereRadius", "NS");
                varNdoe.InnerText = starIfnoDB.EcoSphereRadius.ToString("N4");
                bodyNode.AppendChild(varNdoe);

                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "Luminosity", "NS");
                varNdoe.InnerText = starIfnoDB.Luminosity.ToString("N4");
                bodyNode.AppendChild(varNdoe);

                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "SpectralType", "NS");
                varNdoe.InnerText = starIfnoDB.SpectralType.ToString();
                bodyNode.AppendChild(varNdoe);

                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "Temperature", "NS");
                varNdoe.InnerText = starIfnoDB.Temperature.ToString("N4");
                bodyNode.AppendChild(varNdoe);
            }

            if (massVolumeDB != null)
            {
                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "Mass", "NS");
                varNdoe.InnerText = massVolumeDB.Mass.ToString("N4");
                bodyNode.AppendChild(varNdoe);

                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "Radius", "NS");
                varNdoe.InnerText = massVolumeDB.Radius.ToString("N4");
                bodyNode.AppendChild(varNdoe);

                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "Density", "NS");
                varNdoe.InnerText = massVolumeDB.Density.ToString("N4");
                bodyNode.AppendChild(varNdoe);

                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "Volume", "NS");
                varNdoe.InnerText = massVolumeDB.Volume.ToString("N4");
                bodyNode.AppendChild(varNdoe);

                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "SurfaceGravity", "NS");
                varNdoe.InnerText = massVolumeDB.SurfaceGravity.ToString("N4");
                bodyNode.AppendChild(varNdoe);
            }

            // add orbit details:
            varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "Apoapsis", "NS");
            varNdoe.InnerText = orbit.Apoapsis.ToString("N4");
            bodyNode.AppendChild(varNdoe);

            varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "Periapsis", "NS");
            varNdoe.InnerText = orbit.Periapsis.ToString("N4");
            bodyNode.AppendChild(varNdoe);

            varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "Year", "NS");
            varNdoe.InnerText = orbit.OrbitalPeriod.ToString();
            bodyNode.AppendChild(varNdoe);

            varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "Eccentricity", "NS");
            varNdoe.InnerText = orbit.Eccentricity.ToString("N4");
            bodyNode.AppendChild(varNdoe);

            varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "Inclination", "NS");
            varNdoe.InnerText = orbit.Inclination.ToString("N4");
            bodyNode.AppendChild(varNdoe);

            if (atmosphereDB != null)
            {
                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "Atmosphere", "NS");
                varNdoe.InnerText = atmosphereDB.AtomsphereDescriptionInPercent;
                bodyNode.AppendChild(varNdoe);

                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "AtmosphereInATM", "NS");
                varNdoe.InnerText = atmosphereDB.AtomsphereDescriptionATM;
                bodyNode.AppendChild(varNdoe);

                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "Pressure", "NS");
                varNdoe.InnerText = atmosphereDB.Pressure.ToString("N4");
                bodyNode.AppendChild(varNdoe);

                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "Albedo", "NS");
                varNdoe.InnerText = atmosphereDB.Albedo.ToString("N4");
                bodyNode.AppendChild(varNdoe);

                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "SurfaceTemperature", "NS");
                varNdoe.InnerText = atmosphereDB.SurfaceTemperature.ToString("N4");
                bodyNode.AppendChild(varNdoe);

                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "GreenhouseFactor", "NS");
                varNdoe.InnerText = atmosphereDB.GreenhouseFactor.ToString("N4");
                bodyNode.AppendChild(varNdoe);

                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "GreenhousePressure", "NS");
                varNdoe.InnerText = atmosphereDB.GreenhousePressure.ToString("N4");
                bodyNode.AppendChild(varNdoe);

                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "Hydrosphere", "NS");
                varNdoe.InnerText = atmosphereDB.Hydrosphere.ToString();
                bodyNode.AppendChild(varNdoe);

                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "HydrosphereExtent", "NS");
                varNdoe.InnerText = atmosphereDB.HydrosphereExtent.ToString();
                bodyNode.AppendChild(varNdoe);
            }

            if (positionDB != null)
            {
                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "PosX", "NS");
                varNdoe.InnerText = positionDB.X.ToString("N5");
                bodyNode.AppendChild(varNdoe);

                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "PosY", "NS");
                varNdoe.InnerText = positionDB.Y.ToString("N5");
                bodyNode.AppendChild(varNdoe);

                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "PosZ", "NS");
                varNdoe.InnerText = positionDB.Z.ToString("N5");
                bodyNode.AppendChild(varNdoe);
            }

            if (ruinsDB != null)
            {
                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "RuinCount", "NS");
                varNdoe.InnerText = ruinsDB.RuinCount.ToString();
                bodyNode.AppendChild(varNdoe);

                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "RuinQuality", "NS");
                varNdoe.InnerText = ruinsDB.RuinQuality.ToString();
                bodyNode.AppendChild(varNdoe);

                varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "RuinSize", "NS");
                varNdoe.InnerText = ruinsDB.RuinSize.ToString();
                bodyNode.AppendChild(varNdoe);
            }

            // add our ID to at the end:
            varNdoe = xmlDoc.CreateNode(XmlNodeType.Element, "ID", "NS");
            varNdoe.InnerText = systemBody.Guid.ToString();
            bodyNode.AppendChild(varNdoe);

            // add body node to system node:
            systemNdoe.AppendChild(bodyNode);

            // call recursivly for children:
            foreach (var child in orbit.Children)
            {
                OrbitDB o = child.GetDataBlob<OrbitDB>();
                if (o != null)
                    SerilizeBodyToXML(xmlDoc, systemNdoe, o.OwningEntity, o);
            }
        }

        [Test]
        [Description("Creates and tests a single star sytem")]
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

            int totalEntitys = 0;
            foreach (StarSystem system in _game.Systems)
            {
                List<Entity> entities = system.SystemManager.GetAllEntitiesWithDataBlob<PositionDB>();
                totalEntitys += entities.Count;
            }

            long endMemory = GC.GetTotalMemory(true); 
            double totalMemory = (endMemory - startMemory) / 1024.0;  // in KB

            // note that because we do 1000 systems total time taken as miliseconds is the time for a single sysmte, on average.
            string output = String.Format("Total run time: {0}s, per system: {1}ms. total memory used: {2} MB, per system: {3} KB. Total Entities: {4}, per system: {5}. Memory per entity: {6}KB", 
                totalTime.ToString("N4"), 
                ((totalTime/numSystems) * 1000).ToString("N2"), 
                (totalMemory / 1024.0).ToString("N2"),
                (totalMemory / numSystems).ToString("N2"), 
                totalEntitys, totalEntitys / (float)numSystems, 
                (totalMemory / totalEntitys).ToString("N2"));

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

            // note that because we do 1000 systems total time taken as miliseconds is the time for a single sysmte, on average.
            string output = String.Format("Total run time: {0}s, per system: {1}ms. total memory used: {2} MB, per system: {3} KB.",
                totalTime.ToString("N4"), (totalTime).ToString("N2"), (totalMemory / 1024.0).ToString("N2"), (totalMemory / 1000).ToString("N2"));

            // print results:
            Console.WriteLine(output);
            Assert.Pass(output);
        }
    }
}