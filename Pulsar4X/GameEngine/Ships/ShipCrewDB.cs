using Newtonsoft.Json;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Ships
{
    /// <summary>
    /// People aboard a ship (crew plan phase 3). Never more than min(required, berths) of the parts
    /// mounted. Crew aboard is one number: it is not tracked by species yet
    /// (TODO(crew): per-species crew aboard, so scrapping returns the same species).
    /// </summary>
    public class ShipCrewDB : BaseDataBlob
    {
        /// <summary>People aboard. Setting it rebooks the ship's life support (crew plan phase 4); loading a save does not.</summary>
        [JsonIgnore]
        public int Aboard
        {
            get => _aboard;
            internal set
            {
                _aboard = value;
                if (OwningEntity != null)
                    ShipCrew.RefreshLifeSupport(OwningEntity);
            }
        }

        [JsonProperty("Aboard")] int _aboard;

        /// <summary>Fractions of a survey point the crew factor left over, so a slowed surveyor still adds up.</summary>
        [JsonProperty]
        internal double GeoSurveyCarry { get; set; }

        [JsonProperty]
        internal double GravSurveyCarry { get; set; }

        /// <summary>Life support this ship has booked into its plant's must-run demand, kW (crew plan phase 4).</summary>
        [JsonProperty]
        public double LifeSupport_kW { get; internal set; }

        /// <summary>True from a "life support failing" event until the plant carries life support again (one event per failure).</summary>
        [JsonProperty]
        public bool LifeSupportFailing { get; internal set; }

        // Crew split of the mounted parts, refreshed when the part count changes (ShipCrew.Split).
        [JsonIgnore] internal int CachedParts = -1;
        [JsonIgnore] internal ShipCrew.CrewSplit CachedSplit;

        public ShipCrewDB() { }

        public ShipCrewDB(int aboard)
        {
            _aboard = aboard;
        }

        public ShipCrewDB(ShipCrewDB db)
        {
            _aboard = db.Aboard;
            GeoSurveyCarry = db.GeoSurveyCarry;
            GravSurveyCarry = db.GravSurveyCarry;
            LifeSupport_kW = db.LifeSupport_kW;
            LifeSupportFailing = db.LifeSupportFailing;
        }

        public override object Clone() => new ShipCrewDB(this);
    }
}
