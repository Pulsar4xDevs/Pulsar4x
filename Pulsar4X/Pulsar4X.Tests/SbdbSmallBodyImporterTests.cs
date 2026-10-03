using System;
using NUnit.Framework;
using Pulsar4X.Galaxy;

namespace Pulsar4X.Tests
{
    [TestFixture]
    public class SbdbSmallBodyImporterTests
    {
        // Two-row SBDB payload. Numbers are strings, which is how the query API returns them.
        const string CeresAndVesta = """
            {
              "fields": ["spkid","full_name","name","pdes","class","epoch_cal","e","a","i","om","w","ma","diameter","albedo","density","GM","rot_per"],
              "data": [
                [20000001,"1 Ceres (A801 AA)","Ceres","1","MBA","2026-06-09.0000000","0.07969229514816586","2.765552595034094","10.58802780183462","80.24862682043221","73.29421453021587","274.4193463761342","939.4","0.090","2.162","62.6284","9.074170"],
                [20000004,"4 Vesta (A807 FA)","Vesta","4","MBA","2026-06-09.0000000","0.09020374382834395","2.361365965127599","7.143925545058711","103.701293265032","151.4686478221564","81.19015607686903","522.77","0.4228","3.460","17.2882844","5.3421276322"]
              ]
            }
            """;

        [Test]
        public void SkipsCeresAndKeepsVesta()
        {
            var result = SbdbSmallBodyImporter.Import(new SbdbImportRequest
            {
                Count = 1,
                SkipNames = new[] { "Ceres" },
            }, http: null, responseJson: CeresAndVesta);

            Assert.AreEqual(1, result.SkippedExisting);
            Assert.AreEqual(1, result.Bodies.Count);
            var body = result.Bodies[0];
            Assert.AreEqual("main-belt", body.Field);
            Assert.AreEqual("asteroids-main-belt.json", body.FileName);
            Assert.AreEqual("asteroid-4", body.Blueprint.UniqueID);
            Assert.AreEqual("Vesta", body.Blueprint.Name);
            Assert.AreEqual("asteroid", body.Blueprint.Info.Type);
            Assert.AreEqual(false, body.Blueprint.Colonizable);
            Assert.AreEqual("random", body.Blueprint.GenerateMinerals);

            double expectedMass = 17.2882844 / SbdbSmallBodyImporter.GravitationalConstantKm;
            Assert.AreEqual(expectedMass, body.Blueprint.Info.Mass!.Value, expectedMass * 1e-12);
            Assert.AreEqual(522.77 / 2.0, body.Blueprint.Info.Radius!.Value, 1e-9);
            Assert.AreEqual(2.361365965127599 * SbdbSmallBodyImporter.AuInKm, body.Blueprint.Orbit.SemiMajorAxis!.Value, 1e-3);
            Assert.AreEqual(7.143925545058711, body.Blueprint.Orbit.EclipticInclination!.Value, 1e-9);
            Assert.AreEqual(new DateTime(2026, 6, 9), body.Blueprint.Orbit.Epoch);

            var kept = SbdbSmallBodyImporter.Import(new SbdbImportRequest
            {
                Count = 1,
            }, http: null, responseJson: CeresAndVesta);
            double ceresMass = 62.6284 / SbdbSmallBodyImporter.GravitationalConstantKm;
            Assert.AreEqual("asteroid-1", kept.Bodies[0].Blueprint.UniqueID);
            Assert.AreEqual(ceresMass, kept.Bodies[0].Blueprint.Info.Mass!.Value, ceresMass * 1e-12);
        }
    }
}
