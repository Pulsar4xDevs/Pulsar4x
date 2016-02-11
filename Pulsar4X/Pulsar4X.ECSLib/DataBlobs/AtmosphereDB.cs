using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class AtmosphereDB : BaseDataBlob
    {
        /// <summary>
        /// Atmospheric Pressure
        /// In Earth Atmospheres (atm).
        /// </summary>
        public float Pressure
        {
            get { return _pressure; }
            internal set { _pressure = value; }
        }
        [JsonProperty]
        private float _pressure;

        /// <summary>
        /// Weather or not the planet has abundent water.
        /// </summary>
        public bool Hydrosphere
        {
            get { return _hydrosphere; }
            internal set { _hydrosphere = value; }
        }
        [JsonProperty]
        private bool _hydrosphere;

        /// <summary>
        /// The percentage of the bodies sureface covered by water.
        /// </summary>
        public short HydrosphereExtent
        {
            get { return _hydrosphereExtent; }
            internal set { _hydrosphereExtent = value; }
        }
        [JsonProperty]
        private short _hydrosphereExtent;

        /// <summary>
        /// A measure of the greenhouse factor provided by this Atmosphere.
        /// </summary>
        public float GreenhouseFactor
        {
            get { return _greenhouseFactor; }
            internal set { _greenhouseFactor = value; }
        }
        [JsonProperty]
        private float _greenhouseFactor;

        /// <summary>
        /// Pressure (in atm) of greenhouse gasses. but not really.
        /// to get this figure for a given gass toy would take its pressure 
        /// in the atmosphere and times it by the gasses GreenhouseEffect 
        /// which is a number between 1 and -1 normally.
        /// </summary>
        public float GreenhousePressure
        {
            get { return _greenhousePressure; }
            internal set { _greenhousePressure = value; }
        }
        [JsonProperty]
        private float _greenhousePressure;

        /// <summary>
        /// How much light the body reflects. Affects temp.
        /// a number from 0 to 1.
        /// </summary>
        public float Albedo
        {
            get { return _albedo; }
            internal set { _albedo = value; }
        }
        [JsonProperty]
        private float _albedo;

        /// <summary>
        /// Temperature of the planet AFTER greenhouse effects are taken into consideration. 
        /// This is a factor of the base temp and Green House effects.
        /// In Degrees C.
        /// </summary>
        public float SurfaceTemperature
        {
            get { return _surfaceTemperature; }
            internal set { _surfaceTemperature = value; }
        }
        [JsonProperty]
        private float _surfaceTemperature;

        //<summary>
        //The composition of the atmosphere, i.e. what gases make it up and in what ammounts.
        //In Earth Atmospheres (atm).
        //</summary>
        public Dictionary<AtmosphericGasSD, float> Composition
        {
            get { return _composition; }
            internal set { _composition = value; }
        }
        [JsonProperty]
        private Dictionary<AtmosphericGasSD, float> _composition;

        /// <summary>
        /// A sting describing the Atmosphere in Percentages, like this:
        /// "75% Nitrogen (N), 21% Oxygen (O), 3% Carbon dioxide (CO2), 1% Argon (Ar)"
        /// By Default ToString return this.
        /// </summary>
        public string AtomsphereDescriptionInPercent { get; internal set; }

        /// <summary>
        /// A sting describing the Atmosphere in Atmospheres (atm), like this:
        /// "0.75atm Nitrogen (N), 0.21atm Oxygen (O), 0.03atm Carbon dioxide (CO2), 0.01atm Argon (Ar)"
        /// </summary>
        public string AtomsphereDescriptionAtm { get; internal set; }

        /// <summary>
        /// indicates if the body as a valid atmosphere.
        /// </summary>
        public bool Exists
        {
            get
            {
                if (Composition.Count == 0)
                    return false;

                return true;
            }
        }

        /// <summary>
        /// Empty constructor for AtmosphereDataBlob.
        /// </summary>
        public AtmosphereDB()
        {
            Composition = new Dictionary<AtmosphericGasSD, float>();
        }

        /// <summary>
        /// Constructor for AtmosphereDataBlob. 
        /// </summary>
        /// <param name="pressure">In Earth Atmospheres (atm).</param>
        /// <param name="hydrosphere">Weather or not the planet has abundent water.</param>
        /// <param name="hydroExtent">The percentage of the bodies sureface covered by water.</param>
        /// <param name="greenhouseFactor">Greenhouse factor provided by this Atmosphere.</param>
        /// <param name="greenhousePressue"></param>
        /// <param name="albedo">from 0 to 1.</param>
        /// <param name="surfaceTemp">AFTER greenhouse effects, In Degrees C.</param>
        /// <param name="composition">a Dictionary of gas types as keys and amounts as values</param>
        internal AtmosphereDB(float pressure, bool hydrosphere, short hydroExtent, float greenhouseFactor, float greenhousePressue, float albedo, float surfaceTemp, Dictionary<AtmosphericGasSD,float> composition)
        {
            Pressure = pressure;
            Hydrosphere = hydrosphere;
            HydrosphereExtent = hydroExtent;
            GreenhouseFactor = greenhouseFactor;
            GreenhousePressure = greenhousePressue;
            Albedo = albedo;
            SurfaceTemperature = surfaceTemp;
            Composition = composition;
        }

        public AtmosphereDB(AtmosphereDB atmosphereDB)
            : this(atmosphereDB.Pressure, atmosphereDB.Hydrosphere, atmosphereDB.HydrosphereExtent, 
            atmosphereDB.GreenhouseFactor, atmosphereDB.GreenhousePressure, atmosphereDB.Albedo, 
            atmosphereDB.SurfaceTemperature, 
            new Dictionary<AtmosphericGasSD, float>(atmosphereDB.Composition)
            )
        {

        }

        /// <summary>
        /// This function generates the different text discriptions of the atmosphere.
        /// It should be run after any changes to the atmosphere which may effect the description.
        /// </summary>
        public void GenerateDescriptions()
        {
            if (Exists == false)
            {
                AtomsphereDescriptionInPercent = "This body has no Atmosphere.  ";
                AtomsphereDescriptionAtm = AtomsphereDescriptionInPercent;
                return;
            }

            foreach (KeyValuePair<AtmosphericGasSD, float> gas in Composition)
            {
                AtomsphereDescriptionAtm += gas.Value.ToString("N4") + "atm " + gas.Key.Name + " " + gas.Key.ChemicalSymbol + ", ";

                if (Pressure != 0) // for extra safety.
                    AtomsphereDescriptionInPercent += (gas.Value / Pressure).ToString("P1") + " " + gas.Key.Name + " " + gas.Key.ChemicalSymbol + ", "; ///< @todo this is not right!!
                else
                {
                    // this is here for safty, to prevent any oif these being null.
                    AtomsphereDescriptionAtm = "This body has no Atmosphere.  ";
                    AtomsphereDescriptionInPercent = AtomsphereDescriptionAtm;
                }
            }

            // trim trailing", " from the strings.
            AtomsphereDescriptionAtm = AtomsphereDescriptionAtm.Remove(AtomsphereDescriptionAtm.Length - 2);
            AtomsphereDescriptionInPercent = AtomsphereDescriptionInPercent.Remove(AtomsphereDescriptionInPercent.Length - 2);
        }

        public override object Clone()
        {
            return new AtmosphereDB(this);
        }
    }
}
