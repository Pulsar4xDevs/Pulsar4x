using Newtonsoft.Json;
using System;
using Pulsar4X.Orbital;

namespace Pulsar4X.ECSLib
{
    public interface IPosition
    {
        Vector3 AbsolutePosition { get; }
        Vector3 RelativePosition { get; }
    }
    //TODO: get rid of AU, why are we using AU.
    public class PositionDB : TreeHierarchyDB, IGetValuesHash, IPosition
    {

        [JsonProperty]
        public Guid SystemGuid;

        /// <summary>
        /// The Position as a Vec3, in m.
        /// </summary>
        public Vector3 AbsolutePosition 
        {             
            get
            {
                if ( Parent == null || !Parent.IsValid ) //migth be better than crashing if parent is suddenly not valid. should be handled before this though. 
                    return _positionInMeters;
                else if (Parent == OwningEntity)
                    throw new Exception("Infinite loop triggered");
                else
                {
                    PositionDB parentpos = (PositionDB)ParentDB;
                    if(parentpos == this)
                        throw new Exception("Infinite loop triggered");
                    return parentpos.AbsolutePosition + _positionInMeters;
                }
            }
            internal set
            {
                if (Parent == null)
                    _positionInMeters = value;
                else
                {
                    PositionDB parentpos = (PositionDB)ParentDB;
                    _positionInMeters = value - parentpos.AbsolutePosition;
                }
            } }

        [JsonProperty]
        private Vector3 _positionInMeters;

        /// <summary>
        /// Get or Set the position relative to the parent Entity's abolutePositon
        /// </summary>
        public Vector3 RelativePosition         
        {
            get { return _positionInMeters; }
            internal set { _positionInMeters = value; }
        }

        /// <summary>
        /// Initialized 
        /// .
        /// </summary>
        /// <param name="x">X value.</param>
        /// <param name="y">Y value.</param>
        /// <param name="z">Z value.</param>
        public PositionDB(double x, double y, double z, Guid systemGuid, Entity parent = null) : base(parent)
        {
            AbsolutePosition = new Vector3(x, y, z);
            SystemGuid = systemGuid;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="relativePos_m"></param>
        /// <param name="systemGuid"></param>
        /// <param name="parent"></param>
        public PositionDB(Vector3 relativePos, Guid systemGuid, Entity parent = null) : base(parent)
        {
            RelativePosition = relativePos;
            SystemGuid = systemGuid;
        }

        public PositionDB(Guid systemGuid, Entity parent = null) : base(parent)
        {
            Vector3? parentPos = (ParentDB as PositionDB)?.AbsolutePosition;
            AbsolutePosition = parentPos ?? Vector3.Zero;
            SystemGuid = systemGuid;
        }

        public PositionDB(PositionDB positionDB)
            : base(positionDB.Parent)
        {
            _positionInMeters = positionDB._positionInMeters;

            this.SystemGuid = positionDB.SystemGuid;
        }

        public PositionDB(Vector3 relativePos_m, Entity SOIParent) : base(SOIParent)
        {
            SystemGuid = SOIParent.Manager.ManagerGuid;
            RelativePosition = relativePos_m;
        }

        
        [UsedImplicitly]
        private PositionDB() : this(Guid.Empty) { }

        /// <summary>
        /// changes the positions relative to
        /// Can be null.
        /// </summary>
        /// <param name="newParent"></param>
        internal override void SetParent(Entity newParent)
        {
            if (newParent != null && !newParent.HasDataBlob<PositionDB>())
                throw new Exception("newParent must have a PositionDB");
            Vector3 currentAbsolute = this.AbsolutePosition;
            Vector3 newRelative;
            if (newParent == null)
            {
                newRelative = currentAbsolute;
            }
            else
            {
                newRelative = currentAbsolute - newParent.GetDataBlob<PositionDB>().AbsolutePosition;
            }
            base.SetParent(newParent);
            _positionInMeters = newRelative;
        }

        public static PositionDB operator +(PositionDB posA, PositionDB posB)
        {
            throw new NotSupportedException("Do not add two PositionDBs. See comments in PositonDB.cs");

            /* Operator not supported as it can lead to unintended consequences,
             * especially when trying to do "posA += posB;"
             * Instead of posA += posB, do "posA.Position += posB.Position;"
             * 
             * Datablobs are stored in an entity manager, and contain important metadata.
             * posA += posB evaluates to posA = posA + posB;
             * This operator has to return a "new" datablob. This new datablob is not the
             * one current stored in the EntityManager. Further requests to get the positionDB
             * will return the old positionDB after a += operation.
             * 
             * Ask a senior developer for further clarification if required.
             * 
             * Explicitly thrown to prevent new developers from adding this.
            */
        }

        public override object Clone()
        {
            return new PositionDB(this);
        }

        public int GetValueCompareHash(int hash = 17)
        {
            hash = Misc.ValueHash(AbsolutePosition, hash);
            return hash;
        }
    }
}
