using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Engine
{
    internal static class JPSurveyFactory
    {
        internal static void GenerateJPSurveyPoints(StarSystem system)
        {
            // TODO: Make these settings load from GalaxyGen settings.
            var ringSettings = new Dictionary<double, int>
            {
                { Distance.AuToMt(2), 6 }
            };

            var surveyPoints = new List<ProtoEntity>();
            foreach (KeyValuePair<double, int> ringSetting in ringSettings)
            {
                double distance = ringSetting.Key;
                int numPoints = ringSetting.Value;

                surveyPoints.AddRange(GenerateSurveyRing(distance, numPoints));
            }

            foreach (ProtoEntity surveyPoint in surveyPoints)
            {
                var realPoint = Entity.Create();
                system.AddEntity(realPoint, surveyPoint.DataBlobs);
                realPoint.GetDataBlob<PositionDB>().SystemGuid = system.Guid;
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
            var posDB = new PositionDB(x, y, 0, String.Empty);
            var nameDB = new NameDB($"Survey Point #{nameNumber}");
            //for testing purposes
            var sensorProfileDB = new SensorProfileDB();

            var protoEntity = new ProtoEntity(new List<BaseDataBlob>() { surveyDB, posDB, nameDB, sensorProfileDB });

            return protoEntity;
        }
    }
}
