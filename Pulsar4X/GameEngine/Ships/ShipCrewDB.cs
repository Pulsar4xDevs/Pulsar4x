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
        [JsonProperty]
        public int Aboard { get; internal set; }

        /// <summary>Fractions of a survey point the crew factor left over, so a slowed surveyor still adds up.</summary>
        [JsonProperty]
        internal double GeoSurveyCarry { get; set; }

        [JsonProperty]
        internal double GravSurveyCarry { get; set; }

        // Crew split of the mounted parts, refreshed when the part count changes (ShipCrew.Split).
        [JsonIgnore] internal int CachedParts = -1;
        [JsonIgnore] internal ShipCrew.CrewSplit CachedSplit;

        public ShipCrewDB() { }

        public ShipCrewDB(int aboard)
        {
            Aboard = aboard;
        }

        public ShipCrewDB(ShipCrewDB db)
        {
            Aboard = db.Aboard;
            GeoSurveyCarry = db.GeoSurveyCarry;
            GravSurveyCarry = db.GravSurveyCarry;
        }

        public override object Clone() => new ShipCrewDB(this);
    }
}
