using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using NCalc.Domain;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib.DataBlobs
{
    //All datablobs must be public.
    public class BasicExampleDB
        : BaseDataBlob
    // All datablobs MUST derive from BaseDataBlob
    // If they don't they wont work with the EntityManager.
    // EntityManager does quite a bit of work to shift things around
    // and it's not easy to understand everything the EntityManager does. So just derive from BaseDataBlob.
    {
        private int importantNumber;
        private ICloneable cloneableObj;

        private BasicExampleDB()
        {
            // This is called the "default parameterless constructor" most datablobs will need one of these
            // so that the code that restores a game from the disk can instinate an object of this type.
            // It doesn't need to actually do anything, it just has to exist. See: ComponentDB
        }

        // Datablobs need to implement a deep-copy constructor.
        public BasicExampleDB(BasicExampleDB clone)
        {
            importantNumber = clone.importantNumber;
            cloneableObj = (ICloneable)cloneableObj.Clone();
        }

        // Datablobs must implement the IClonable interface.
        // Most datablobs simply call their own constructor like so:
        public override object Clone()
        {
            return new BasicExampleDB(this);
        }
    }

    public class IntermediateExampleDB : BaseDataBlob
    {
        // Most variables need to be viewable outside this library by the UI.
        // They should be marked with a [PublicAPI] attribute.
        [PublicAPI]
        public int ViewableInt
        {
            get { return _viewableInt; }

            // But we don't want the UI to be able to set the value.
            // So we use a backing field with an internal or private setter.
            internal set { _viewableInt = value; }
        }

        // If we want this value to be saved in the savegame for this datablob, we need to mark it for JSON.
        [JsonProperty]
        private int _viewableInt;
        
        [PublicAPI]
        public void BadFunction()
        {
            // DataBlobs are DATA, not LOGIC. You should have MINIMAL logic in datablobs.
            // About the only acceptable LOGIC in a datablob is for unit conversion, derived property calculation,
            // or automatic object resolution. See MassVolumeDB, OrbitDB, and TreeHierarchyDB for respective examples.

            // So where do you put the logic? In the Processors! See OrbitProcessor for a great example!
        }

        #region Stuff we already talked about.

        private IntermediateExampleDB()
        {
        }

        public IntermediateExampleDB(IntermediateExampleDB clone)
        {
            _viewableInt = clone.ViewableInt;
        }

        public override object Clone()
        {
            return new IntermediateExampleDB(this);
        }

        #endregion
    }

    public class AdvancedExampleDB : BaseDataBlob
    {
        // References to other entities are ok. The EntityManager will handle serializing and deserializing the references properly.
        [PublicAPI]
        public Entity FriendEntity
        {
            get { return _friendEntity; }
            internal set { _friendEntity = value; }
        }

        [JsonProperty]
        private Entity _friendEntity;

        // References to StarSystems that are SERIALIZED are NOT ok.
        [JsonProperty]
        public StarSystem MySystem; // BAD

        // Instead, either store the guid and look up the system when needed (from the Game.Systems dictionary)
        [JsonProperty]
        public Guid mySystemGuid;

        // Or if you want to get really fancy, use a deserialization callback to resolve the star system after load-time.
        public StarSystem myStarSystem;

        // JSON deserialization callback.
        [OnDeserialized]
        private void Deserialized(StreamingContext context)
        {
            // Star system resolver loads myStarSystem from mySystemGuid after the game is done loading.
            SaveGame.CurrentGame.PostLoad += (sender, args) => { if (!SaveGame.CurrentGame.StarSystems.TryGetValue(mySystemGuid, out myStarSystem)) throw new GuidNotFoundException(mySystemGuid); };
        }

        #region Stuff we already talked about.

        private AdvancedExampleDB()
        {
        }

        public AdvancedExampleDB(AdvancedExampleDB clone)
        {
            mySystemGuid = clone.mySystemGuid;
            _friendEntity = clone.FriendEntity;
        }

        public override object Clone()
        {
            return new AdvancedExampleDB(this);
        }

        #endregion
    }
}
