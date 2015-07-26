using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities.Components
{
    /// <summary>
    /// Survey sensors are required for finding TN mineral deposits or for finding jump points within a starsystem.
    /// </summary>
    public class SurveySensorDefTN : ComponentDefTN
    {
        public enum SurveySensorType
        {
            Geological,
            Gravitational,
            Count,
        }

        /// <summary>
        /// What type of sensor is this?
        /// </summary>
        private SurveySensorType SensorType;
        public SurveySensorType sensorType
        {
            get { return SensorType; }
        }

        /// <summary>
        /// How strong of a sensor is this? how quickly does it perform its task. Points will accumulate for every unit of time this sensor has been at work, until the amount of points gathered
        /// is sufficient to meet the point requirement for the item being surveyed.
        /// </summary>
        private float SensorStrength;
        public float sensorStrength
        {
            get { return SensorStrength; }
        }

        /// <summary>
        /// Constructor for Survey sensors.
        /// </summary>
        /// <param name="Title">Name of the sensor.</param>
        /// <param name="sType">its type.</param>
        /// <param name="sStrength">how many points it accumulates per unit of time on a survey job.</param>
        public SurveySensorDefTN(String Title, SurveySensorType sType, float sStrength)
        {
            Name = Title;
            Id = Guid.NewGuid();

            componentType = ComponentTypeTN.SurveySensor;

            SensorType = sType;
            SensorStrength = sStrength;

            size = 5.0f;
            crew = 25;
            cost = 50.0m + ((decimal)sStrength * 50.0m);
            htk = 1;

            minerialsCost = new decimal[Constants.Minerals.NO_OF_MINERIALS];
            for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
            {
                minerialsCost[mineralIterator] = 0;
            }

            minerialsCost[(int)Constants.Minerals.MinerialNames.Uridium] = cost;

            isSalvaged = false;
            isObsolete = false;
            isMilitary = false;
            isDivisible = false;
            isElectronic = false;
        }
    }

    public class SurveySensorTN : ComponentTN
    {
        /// <summary>
        /// Definition for this sensor.
        /// </summary>
        private SurveySensorDefTN SurveyDef;
        public SurveySensorDefTN surveyDef
        {
            get { return SurveyDef; }
        }

        /// <summary>
        /// Simple constructor for survey sensors.
        /// </summary>
        /// <param name="definition">Definition to use.</param>
        public SurveySensorTN(SurveySensorDefTN definition)
        {
            SurveyDef = definition;
            Name = SurveyDef.Name;

            isDestroyed = false;
        }
    }
}
