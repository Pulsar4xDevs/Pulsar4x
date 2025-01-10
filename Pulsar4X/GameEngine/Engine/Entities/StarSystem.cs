using Newtonsoft.Json;
using System;
using System.Diagnostics;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;
using Pulsar4X.Names;

namespace Pulsar4X.Engine
{
    [DebuggerDisplay("{NameDB.DefaultName} - {ID.ToString()}")]
    [JsonObject(MemberSerialization.OptIn)]
    public class StarSystem : EntityManager
    {


        [PublicAPI]
        public string ID
        {
            get
            {
                return ManagerID;
            }
        }

        [JsonProperty]
        internal int SystemIndex { get; set; }

        [PublicAPI]
        [JsonProperty]
        public NameDB NameDB { get;  set; }

        //[PublicAPI]
        //public EntityManager SystemManager { get { return this; } }





 
        [JsonConstructor]
        public StarSystem()
        {
        }

        public void Initialize(Game game, string name, int seed = -1, bool postLoad = false, string systemID = "")
        {
            base.Initialize(game, seed, postLoad);

            NameDB = new NameDB(name);
            
            if(systemID.IsNotNullOrEmpty())
                ManagerID = systemID;

            game.Systems.Add(this);
        }

        // public StarSystem(SerializationInfo info, StreamingContext context) : base(info, context)
        // {

        //     ManagerGuid = (string)info.GetValue("ID", typeof(string));
        //     Seed = (int)info.GetValue("Seed", typeof(int));
        //     NameDB = (NameDB)info.GetValue("Name", typeof(NameDB));
        // }

        // public void ExportBodies(SerializationInfo info)
        // {
        //     List<Entity> bodies = this.GetAllEntitiesWithDataBlob<StarInfoDB>();
        //     bodies.AddRange(this.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>());

        //     info.AddValue("ID", Guid);
        //     info.AddValue("Seed", Seed);
        //     info.AddValue("Name", NameDB);
        //     info.AddValue("Bodies", bodies);
        // }

        // public override void GetObjectData(SerializationInfo info, StreamingContext context)
        // {

        //     info.AddValue("ID", Guid);
        //     info.AddValue("Seed", Seed);
        //     info.AddValue("Name", NameDB);
        //     base.GetObjectData(info, context);
        // }


        // [OnDeserialized]
        // public void OnDeserialized(StreamingContext context)
        // {
        //     Game = (Game)context.Context;
        //     Game.PostLoad += GameOnPostLoad;

        // }

        private void GameOnPostLoad(object sender, EventArgs eventArgs)
        {
            // if (!Game.Systems.ContainsKey(Guid))
            // {
            //     Game.Systems.Add(Guid, this);
            //     if(Game.GameMasterFaction != null) //clients wont have a GameMaster
            //         Game.GameMasterFaction.GetDataBlob<FactionInfoDB>().KnownSystems.Add(Guid);
            // }
        }
    }
}
