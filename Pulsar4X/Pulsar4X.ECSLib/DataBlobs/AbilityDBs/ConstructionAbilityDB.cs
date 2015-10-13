using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{

    public class  ConstructInstationsAbilityDB : BaseDataBlob
    {
        [JsonProperty] private int _constructionPoints;
        public int ConstructionPoints
        {
            get { return _constructionPoints; } 
            internal set { _constructionPoints = value; }
        }

        public ConstructInstationsAbilityDB()
        {
        }

        public ConstructInstationsAbilityDB(double constructionPoints)
        {           
            _constructionPoints = (int)constructionPoints;
        }

        public ConstructInstationsAbilityDB(ConstructInstationsAbilityDB db)
        {
            _constructionPoints = db.ConstructionPoints;
        }

        public override object Clone()
        {
            return new ConstructInstationsAbilityDB(this);
        }
    }

    public class ConstructShipComponentsAbilityDB : BaseDataBlob
    {
        [JsonProperty]
        private int _constructionPoints;
        public int ConstructionPoints
        {
            get { return _constructionPoints; }
            internal set { _constructionPoints = value; }
        }

        public ConstructShipComponentsAbilityDB()
        {
        }

        public ConstructShipComponentsAbilityDB(double constructionPoints)
        {
            _constructionPoints = (int)constructionPoints;
        }

        public ConstructShipComponentsAbilityDB(ConstructShipComponentsAbilityDB db)
        {
            _constructionPoints = db.ConstructionPoints;
        }

        public override object Clone()
        {
            return new ConstructShipComponentsAbilityDB(this);
        }
    }

    public class ConstructAmmoAbilityDB : BaseDataBlob
    {
        [JsonProperty]
        private int _constructionPoints;
        public int ConstructionPoints
        {
            get { return _constructionPoints; }
            internal set { _constructionPoints = value; }
        }

        public ConstructAmmoAbilityDB()
        {
        }

        public ConstructAmmoAbilityDB(double constructionPoints)
        {
            _constructionPoints = (int)constructionPoints;
        }

        public ConstructAmmoAbilityDB(ConstructAmmoAbilityDB db)
        {
            _constructionPoints = db.ConstructionPoints;
        }

        public override object Clone()
        {
            return new ConstructAmmoAbilityDB(this);
        }
    }

    public class ConstructFightersAbilityDB : BaseDataBlob
    {
        [JsonProperty]
        private int _constructionPoints;
        public int ConstructionPoints
        {
            get { return _constructionPoints; }
            internal set { _constructionPoints = value; }
        }

        public ConstructFightersAbilityDB()
        {
        }

        public ConstructFightersAbilityDB(double constructionPoints)
        {
            _constructionPoints = (int)constructionPoints;
        }

        public ConstructFightersAbilityDB(ConstructFightersAbilityDB db)
        {
            _constructionPoints = db.ConstructionPoints;
        }

        public override object Clone()
        {
            return new ConstructFightersAbilityDB(this);
        }
    }
}