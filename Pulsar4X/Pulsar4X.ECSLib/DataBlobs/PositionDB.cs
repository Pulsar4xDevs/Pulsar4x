using Newtonsoft.Json;
using System;
using Pulsar4X.Orbital;

namespace Pulsar4X.ECSLib
{
    public interface IPosition
    {
        Vector3 AbsolutePosition_AU { get; }
        Vector3 AbsolutePosition_m { get; }
        Vector3 RelativePosition_AU { get; }
        Vector3 RelativePosition_m { get; }
    }
    //TODO: get rid of AU, why are we using AU.
    public class PositionDB : TreeHierarchyDB, IGetValuesHash, IPosition
    {

        [JsonProperty]
        public Guid SystemGuid;

        /// <summary>
        /// The Position as a Vec3, in AU.
        /// </summary>
        public Vector3 AbsolutePosition_AU
        {
            get { return Distance.MToAU(AbsolutePosition_m); }
            internal set { AbsolutePosition_m = Distance.AuToMt(value); }
        }

        public Vector3 AbsolutePosition_m 
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
                    return parentpos.AbsolutePosition_m + _positionInMeters;
                }
            }
            internal set
            {
                if (Parent == null)
                    _positionInMeters = value;
                else
                {
                    PositionDB parentpos = (PositionDB)ParentDB;
                    _positionInMeters = value - parentpos.AbsolutePosition_m;
                }
            } }

        [JsonProperty]
        private Vector3 _positionInMeters;

        /// <summary>
        /// Get or Set the position relative to the parent Entity's abolutePositon
        /// </summary>
        public Vector3 RelativePosition_AU
        {
            get { return Distance.MToAU(_positionInMeters); }
            internal set { _positionInMeters = Distance.AuToMt(value); }
        }

        public Vector3 RelativePosition_m         
        {
            get { return _positionInMeters; }
            internal set { _positionInMeters = value; }
        }

        /// <summary>
        /// System X coordinate in AU
        /// </summary>
        public double X_AU
        {
            get { return AbsolutePosition_AU.X; }
            internal set {  _positionInMeters.X = Distance.AuToMt(value); }
        }

        /// <summary>
        /// System Y coordinate in AU
        /// </summary>
        public double Y_AU
        {
            get { return AbsolutePosition_AU.Y; }
            internal set { _positionInMeters.Y = Distance.AuToMt(value); }
        }

        /// <summary>
        /// System Z coordinate in AU
        /// </summary>
        public double Z_AU
        {
            get { return AbsolutePosition_AU.Z; }
            internal set { _positionInMeters.Z = Distance.AuToMt(value); }
        }

        #region Unit Conversion Properties

        /// <summary>
        /// Position as a vec4. This is a utility property that converts Position to Km on get and to AU on set.
        /// </summary>
        public Vector3 PositionInKm
        {
            get { return new Vector3(Distance.AuToKm(AbsolutePosition_AU.X), Distance.AuToKm(AbsolutePosition_AU.Y), Distance.AuToKm(AbsolutePosition_AU.Z)); }
            set { AbsolutePosition_AU = new Vector3(Distance.KmToAU(value.X), Distance.KmToAU(value.Y), Distance.KmToAU(value.Z)); }
        }

        /// <summary>
        /// System X coordinante. This is a utility property that converts the X Coord. to Km on get and to AU on set.
        /// </summary>
        public double XInKm
        {
            get { return Distance.AuToKm(AbsolutePosition_AU.X); }
            set { _positionInMeters.X = Distance.KmToAU(value); }
        }

        /// <summary>
        /// System Y coordinante. This is a utility property that converts the Y Coord. to Km on get and to AU on set.
        /// </summary>
        public double YInKm
        {
            get { return Distance.AuToKm(AbsolutePosition_AU.Y); }
            set { _positionInMeters.Y = Distance.KmToAU(value); }
        }

        /// <summary>
        /// System Z coordinate. This is a utility property that converts the Z Coord. to Km on get and to AU on set.
        /// </summary>
        public double ZInKm
        {
            get { return Distance.AuToKm(AbsolutePosition_AU.Z); }
            set { _positionInMeters.Z = Distance.KmToAU(value); }
        }

        public void AddMeters(Vector3 addVector)
        {
            _positionInMeters += Distance.MToAU(addVector);
        }

        #endregion

        /// <summary>
        /// Initialized 
        /// .
        /// </summary>
        /// <param name="x">X value.</param>
        /// <param name="y">Y value.</param>
        /// <param name="z">Z value.</param>
        public PositionDB(double x, double y, double z, Guid systemGuid, Entity parent = null) : base(parent)
        {
            AbsolutePosition_AU = new Vector3(x, y, z);
            SystemGuid = systemGuid;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="absolutePos_AU"></param>
        /// <param name="systemGuid"></param>
        /// <param name="parent"></param>
        public PositionDB(Vector3 absolutePos_AU, Guid systemGuid, Entity parent = null) : base(parent)
        {
            AbsolutePosition_AU = absolutePos_AU;
            SystemGuid = systemGuid;
        }

        public PositionDB(Guid systemGuid, Entity parent = null) : base(parent)
        {
            Vector3? parentPos = (ParentDB as PositionDB)?.AbsolutePosition_m;
            AbsolutePosition_m = parentPos ?? Vector3.Zero;
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
            RelativePosition_m = relativePos_m;
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
            Vector3 currentAbsolute = this.AbsolutePosition_m;
            Vector3 newRelative;
            if (newParent == null)
            {
                newRelative = currentAbsolute;
            }
            else
            {
                newRelative = currentAbsolute - newParent.GetDataBlob<PositionDB>().AbsolutePosition_m;
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
            hash = Misc.ValueHash(AbsolutePosition_m, hash);
            return hash;
        }
    }
}
