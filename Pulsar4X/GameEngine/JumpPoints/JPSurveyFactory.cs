using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Engine
{
    internal static class JPSurveyFactory
    {
        internal static void GenerateJPSurveyPoints(StarSystem system, Dictionary<double, int>? ringSettings = null)
        {
            if(ringSettings == null)
            {
                ringSettings = new Dictionary<double, int>
                {
                    { Distance.AuToMt(2), 6 },
                    { Distance.AuToMt(10), 8 }
                };
            }

            var surveyPoints = new List<ProtoEntity>();
            int numGenerated = 0;
            foreach (var (distance, numPoints) in ringSettings)
            {
                surveyPoints.AddRange(GenerateSurveyRing(distance, numPoints, numGenerated));
                numGenerated += numPoints;
            }

            foreach (ProtoEntity surveyPoint in surveyPoints)
            {
                var realPoint = Entity.Create();
                system.AddEntity(realPoint, surveyPoint.DataBlobs);
            }
        }

        public static List<ProtoEntity> GenerateSurveyRing(double distance, int numToGenerate, int startingNumber = 0)
        {
            double degreeOffsetPerPoint = 2*Math.PI / numToGenerate;

            var surveyRingList = new List<ProtoEntity>(numToGenerate);

            for (int i = startingNumber; i < numToGenerate + startingNumber; i++)
            {
                double thisPointDegreeOffset = i * degreeOffsetPerPoint;

                double y = distance * Math.Cos(thisPointDegreeOffset);
                double x = distance * Math.Sin(thisPointDegreeOffset);

                surveyRingList.Add(CreateSurveyPoint(x, y, i + 1));
            }

            return surveyRingList;
        }

        private static ProtoEntity CreateSurveyPoint(double x, double y, int nameNumber)
        {
            // TODO: Rebalance "pointsRequired" here.
            // TODO: Load "pointsRequired" from GalaxyGen settings
            const int pointsRequired = 400;

            var surveyDB = new JPSurveyableDB(pointsRequired, new SafeDictionary<int, uint>(), 10000000);
            var posDB = new PositionDB(x, y, 0);
            var nameDB = new NameDB($"Gravitational Anomaly #{nameNumber}");
            //for testing purposes
            // var sensorProfileDB = new SensorProfileDB();
            // sensorProfileDB.EmittedEMSpectra.Add(new Sensors.EMWaveForm(0, 500, 1000), 1E9);
            // sensorProfileDB.Reflectivity = 0;
            var visibleByDefaultDB = new VisibleByDefaultDB();

            var protoEntity = new ProtoEntity(new List<BaseDataBlob>() { surveyDB, posDB, nameDB, visibleByDefaultDB });

            return protoEntity;
        }
    }
}
