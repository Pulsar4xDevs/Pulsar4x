using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public enum ConstructionType
    {
        Component,
        Amunition,
        Fighter,
        Facility
    }

    public class  ConstructionAbilityDB : BaseDataBlob
    {
        [JsonProperty] private int _constructionPoints;
        public int ConstructionPoints
        {
            get { return _constructionPoints; } 
            internal set { _constructionPoints = value; }
        }

        [JsonProperty] private List<ConstructionType> _constructionTypes;

        public List<ConstructionType> ConstructionTypes
        {
            get { return _constructionTypes; } 
            internal set { _constructionTypes = value; }
        } 


        public ConstructionAbilityDB(double constructionPoints)
        {
            _constructionPoints = (int)constructionPoints;
        }

        public ConstructionAbilityDB(ConstructionAbilityDB db)
        {
            _constructionPoints = db.ConstructionPoints;
        }

        public override object Clone()
        {
            return new ConstructionAbilityDB(this);
        }
    }
}