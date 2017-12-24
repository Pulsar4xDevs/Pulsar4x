using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public static class SystemAsEntityFactory
    {
        public static Entity CreateSystemAsEntity(EntityManager sysMan, StarSystem starSys, Entity factionEntity)
        {
            var sysdb = new StarSystemDB(starSys);

            var ownddb = new OwnedDB(factionEntity);
            //var changeListnerDB = new EntityChangeListner();

            List<BaseDataBlob> datablobs = new List<BaseDataBlob>() {
                sysdb,
                ownddb,
                //changeListnerDB
            };
            return new Entity(sysMan, datablobs);
        }
    }


    /// <summary>
    /// The idea of this is so that we can get to a system from a manager (or from an entity within a system)
    /// currently not used, but may be usefull in the future when we do sensors etc. 
    /// </summary>
    public class StarSystemDB : BaseDataBlob
    {
        internal StarSystem StarSystem { get;  set; }

        public StarSystemDB()
        { }

        public StarSystemDB(StarSystem starSys)
        { StarSystem = starSys; }

        public StarSystemDB(StarSystemDB db)
        {
            StarSystem = db.StarSystem;
        }

        public override object Clone()
        {
            return new StarSystemDB(this);
        }
    }
}
