using Newtonsoft.Json;
using System;

namespace Pulsar4X.ECSLib
{
    public interface IPosition
    {
        Vector4 AbsolutePosition_AU { get; }
        Vector4 RelativePosition_AU { get; }

    }
    //TODO: get rid of AU, why are we using AU.
    public class PositionDB : TreeHierarchyDB, IGetValuesHash, IPosition
    {

        [JsonProperty]
        public Guid SystemGuid;

        /// <summary>
        /// The Position as a Vec4, in AU.
        /// </summary>
        public Vector4 AbsolutePosition_AU
        {
            get
            {
                if (Parent == null)
                    return _position;
                else if (Parent == OwningEntity)
                    throw new Exception("Infinite loop triggered");
                else
                {
                    PositionDB parentpos = (PositionDB)ParentDB;
                    if(parentpos == this)
                        throw new Exception("Infinite loop triggered");
                    return parentpos.AbsolutePosition_AU + _position;
                }
            }
            internal set
            {
                if (Parent == null)
                    _position = value;
                else
                {
                    PositionDB parentpos = (PositionDB)ParentDB;
                    _position = value - parentpos.AbsolutePosition_AU;
                }
            }
        }
        [JsonProperty]
        private Vector4 _position;

        /// <summary>
        /// Get or Set the position relative to the parent Entity's abolutePositon
        /// </summary>
        public Vector4 RelativePosition_AU
        {
            get { return _position; }
            internal set { _position = value; }
        }


        /// <summary>
        /// System X coordinate in AU
        /// </summary>
        public double X
        {
            get { return AbsolutePosition_AU.X; }
            internal set { _position.X = value; }
        }

        /// <summary>
        /// System Y coordinate in AU
        /// </summary>
        public double Y
        {
            get { return AbsolutePosition_AU.Y; }
            internal set { _position.Y = value; }
        }

        /// <summary>
        /// System Z coordinate in AU
        /// </summary>
        public double Z
        {
            get { return AbsolutePosition_AU.Z; }
            internal set { _position.Z = value; }
        }

        #region Unit Conversion Properties

        /// <summary>
        /// Position as a vec4. This is a utility property that converts Position to Km on get and to AU on set.
        /// </summary>
        public Vector4 PositionInKm
        {
            get { return new Vector4(Distance.AuToKm(AbsolutePosition_AU.X), Distance.AuToKm(AbsolutePosition_AU.Y), Distance.AuToKm(AbsolutePosition_AU.Z), 0); }
            set { AbsolutePosition_AU = new Vector4(Distance.KmToAU(value.X), Distance.KmToAU(value.Y), Distance.KmToAU(value.Z), 0); }
        }

        /// <summary>
        /// System X coordinante. This is a utility property that converts the X Coord. to Km on get and to AU on set.
        /// </summary>
        public double XInKm
        {
            get { return Distance.AuToKm(AbsolutePosition_AU.X); }
            set { _position.X = Distance.KmToAU(value); }
        }

        /// <summary>
        /// System Y coordinante. This is a utility property that converts the Y Coord. to Km on get and to AU on set.
        /// </summary>
        public double YInKm
        {
            get { return Distance.AuToKm(AbsolutePosition_AU.Y); }
            set { _position.Y = Distance.KmToAU(value); }
        }

        /// <summary>
        /// System Z coordinate. This is a utility property that converts the Z Coord. to Km on get and to AU on set.
        /// </summary>
        public double ZInKm
        {
            get { return Distance.AuToKm(AbsolutePosition_AU.Z); }
            set { _position.Z = Distance.KmToAU(value); }
        }

        public void AddMeters(Vector4 addVector)
        {
            _position += Distance.MToAU(addVector);
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
            AbsolutePosition_AU = new Vector4(x, y, z, 0);
            SystemGuid = systemGuid;
        }

        public PositionDB(Vector4 pos, Guid systemGuid, Entity parent = null) : base(parent)
        {
            AbsolutePosition_AU = pos;
            SystemGuid = systemGuid;
        }

        public PositionDB(Guid systemGuid, Entity parent = null) : base(parent)
        {
            Vector4? parentPos = (ParentDB as PositionDB)?.AbsolutePosition_AU;
            AbsolutePosition_AU = parentPos ?? Vector4.Zero;
            SystemGuid = systemGuid;
        }

        public PositionDB(PositionDB positionDB)
            : base(positionDB.Parent)
        {
            this.X = positionDB.X;
            this.Y = positionDB.Y;
            this.Z = positionDB.Z;
            this.SystemGuid = positionDB.SystemGuid;
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
            Vector4 currentAbsolute = this.AbsolutePosition_AU;
            Vector4 newRelative;
            if (newParent == null)
            {
                newRelative = currentAbsolute;
            }
            else
            {
                newRelative = currentAbsolute - newParent.GetDataBlob<PositionDB>().AbsolutePosition_AU;
            }
            base.SetParent(newParent);
            _position = newRelative;
        }

        /// <summary>
        /// Static function to find the distance in AU between two positions.
        /// </summary>
        /// <returns>Distance between posA and posB.</returns>
        public static double GetDistanceBetween(IPosition posA, IPosition posB)
        {
            return (posA.AbsolutePosition_AU - posB.AbsolutePosition_AU).Length();
        }

        public static double GetDistanceBetween(Vector4 posA, PositionDB posB)
        {
            return (posA - posB.AbsolutePosition_AU).Length();
        }

        /// <summary>
        /// Instance function for those who don't like static functions. In AU
        /// </summary>
        public double GetDistanceTo(IPosition otherPos)
        {
            return GetDistanceBetween(this, otherPos);
        }

        /// <summary>
        /// Static Function to find the Distance Squared betweeen two positions.
        /// </summary>
        public static double GetDistanceBetweenSqrd(PositionDB posA, PositionDB posB)
        {
            return (posA.AbsolutePosition_AU - posB.AbsolutePosition_AU).LengthSquared();
        }

        /// <summary>
        /// Instance function for those who don't like static functions.
        /// </summary>
        public double GetDistanceToSqrd(PositionDB otherPos)
        {
            return GetDistanceBetweenSqrd(this, otherPos);
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
            hash = Misc.ValueHash(X, hash);
            hash = Misc.ValueHash(Y, hash);
            hash = Misc.ValueHash(Z, hash);
            hash = Misc.ValueHash(SystemGuid, hash);
            return hash;
        }
    }
}
