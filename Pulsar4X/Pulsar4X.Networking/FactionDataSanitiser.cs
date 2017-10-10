using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Networking
{
    public static class EntityDataSanitiser
    {
        private static Game Game { get; set; }
        
        public static Entity FactionEntity { get; set; }
        static Dictionary<Type, Delegate> TypeProcessorMap { get; set; }
        private static BaseDataBlob CurrentDataBlob { get; set; }
        
        /// <summary>
        /// Add Datablobs to this dictionary that need to be sanitised (and write the datablob specific sanitiser)
        /// </summary>
        public static void Initialise(Game game)
        {
            Game = game;
            TypeProcessorMap = new Dictionary<Type, Delegate>
            {
                { typeof(NameDB), new Action<NameDB>(processor => { NameDBSanitiser(); }) }, 
                { typeof(AuthDB), new Action<AuthDB>(processor => { AuthDBSanitiser(); }) }, 

        
            };
        }

        public static ProtoEntity SanitisedEntity(Entity entity, Guid factionGuid)
        {
            return SanitisedEntity(entity, Game.GlobalManager.GetEntityByGuid(factionGuid));
        }

        public static ProtoEntity SanitisedEntity(Entity entity, Entity factionEntity)
        {
            FactionEntity = factionEntity;
            List<BaseDataBlob> dataBlobs = new List<BaseDataBlob>();

            foreach (var datablob in entity.DataBlobs)
            {
                CurrentDataBlob = datablob;
                var t = datablob.GetType();
                if (TypeProcessorMap.ContainsKey(t))
                    dataBlobs.Add((BaseDataBlob)TypeProcessorMap[t].DynamicInvoke());              
                else             
                    dataBlobs.Add(datablob);              
            }


            ProtoEntity protoEntity = ProtoEntity.Create(entity.Guid, dataBlobs);
            return protoEntity;
        }


        private static NameDB NameDBSanitiser()
        {
            NameDB actualNameDB = (NameDB)CurrentDataBlob;
            NameDB newNameDB = new NameDB(actualNameDB.GetName(FactionEntity));
            newNameDB.SetName(FactionEntity, actualNameDB.GetName(FactionEntity));

            return newNameDB;
        }

        private static AuthDB AuthDBSanitiser()
        {
            AuthDB actualNameDB = (AuthDB)CurrentDataBlob;
            AuthDB newAuthDB = new AuthDB();
          
            return newAuthDB;
        }

        private static FactionInfoDB FactionInfoDBSanitiser()
        {
            FactionInfoDB actualDB = (FactionInfoDB)CurrentDataBlob;
            FactionInfoDB newDB = new FactionInfoDB();

            return newDB;
        }

    }

}
