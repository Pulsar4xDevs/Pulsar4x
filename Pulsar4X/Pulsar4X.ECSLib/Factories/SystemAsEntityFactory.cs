using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public static class SystemAsEntityFactory
    {
        //TODO: #Deadcode: The idea here was to have an entity in the manager so that we can find the starsystem for a specific manager. (currently impossible?)
        //the ownership was so that we could do this per faction maybe as well, and have faction specific data here? 
        //maybe the first problem could be solved via a dictionary<manager, starSystem> in Game or something. 
        public static Entity CreateSystemAsEntity(EntityManager sysMan, StarSystem starSys, Entity factionEntity)
        {
            var sysdb = new StarSystemDB(starSys);


            //var changeListnerDB = new EntityChangeListner();

            List<BaseDataBlob> datablobs = new List<BaseDataBlob>() {
                sysdb,
                //changeListnerDB
            };
            var sysEnt = new Entity(sysMan, datablobs);
            new OwnedDB(factionEntity, sysEnt);
            return sysEnt;
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
