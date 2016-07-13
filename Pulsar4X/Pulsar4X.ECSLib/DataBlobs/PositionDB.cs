using Newtonsoft.Json;
using System;

namespace Pulsar4X.ECSLib
{
    public class PositionDB : TreeHierarchyDB
    {
        /// <summary>
        /// The Position as a Vec4, in AU.
        /// </summary>
        public Vector4 AbsolutePosition
        {
            get
            {
                if (Parent == null)
                    return _position;
                else
                {
                    PositionDB parentpos = (PositionDB)ParentDB;
                    return parentpos.AbsolutePosition + _position;
                }
            }
            internal set
            {
                if (Parent == null)
                    _position = value;
                else
                {
                    PositionDB parentpos = (PositionDB)ParentDB;
                    _position = value - parentpos.AbsolutePosition;
                }
            }
        }
        [JsonProperty]
        private Vector4 _position;

        /// <summary>
        /// Get or Set the position relative to the parent Entity's abolutePositon
        /// </summary>
        public Vector4 RelativePosition
        {
            get { return _position; }
            internal set { _position = value; }
        }


        /// <summary>
        /// System X coordinate in AU
        /// </summary>
        public double X
        {
            get { return AbsolutePosition.X; }
            internal set { _position.X = value; }
        }

        /// <summary>
        /// System Y coordinate in AU
        /// </summary>
        public double Y
        {
            get { return AbsolutePosition.Y; }
            internal set { _position.Y = value; }
        }

        /// <summary>
        /// System Z coordinate in AU
        /// </summary>
        public double Z
        {
            get { return AbsolutePosition.Z; }
            internal set { _position.Z = value; }
        }

        #region Unit Conversion Properties

        /// <summary>
        /// Position as a vec4. This is a utility property that converts Position to Km on get and to AU on set.
        /// </summary>
        public Vector4 PositionInKm
        {
            get { return new Vector4(Distance.ToKm(AbsolutePosition.X), Distance.ToKm(AbsolutePosition.Y), Distance.ToKm(AbsolutePosition.Z), 0); }
            set { AbsolutePosition = new Vector4(Distance.ToAU(value.X), Distance.ToAU(value.Y), Distance.ToAU(value.Z), 0); }
        }

        /// <summary>
        /// System X coordinante. This is a utility property that converts the X Coord. to Km on get and to AU on set.
        /// </summary>
        public double XInKm
        {
            get { return Distance.ToKm(AbsolutePosition.X); }
            set { _position.X = Distance.ToAU(value); }
        }

        /// <summary>
        /// System Y coordinante. This is a utility property that converts the Y Coord. to Km on get and to AU on set.
        /// </summary>
        public double YInKm
        {
            get { return Distance.ToKm(AbsolutePosition.Y); }
            set { _position.Y = Distance.ToAU(value); }
        }

        /// <summary>
        /// System Z coordinate. This is a utility property that converts the Z Coord. to Km on get and to AU on set.
        /// </summary>
        public double ZInKm
        {
            get { return Distance.ToKm(AbsolutePosition.Z); }
            set { _position.Z = Distance.ToAU(value); }
        }
        
        [JsonProperty]
        public Guid SystemGuid;

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
            AbsolutePosition = new Vector4(x, y, z, 0);
            SystemGuid = systemGuid;
        }

        public PositionDB(Vector4 pos, Guid systemGuid, Entity parent = null) : base(parent)
        {
            AbsolutePosition = pos;
            SystemGuid = systemGuid;
        }

        public PositionDB(Guid systemGuid, Entity parent = null) : base(parent)
        {
            AbsolutePosition = Vector4.Zero;
            SystemGuid = systemGuid;
        }

        public PositionDB(PositionDB positionDB)
            : this(positionDB.X, positionDB.Y, positionDB.Z, positionDB.SystemGuid)
        {
        }

        [UsedImplicitly]
        private PositionDB() : this(Guid.Empty) { }

        /// <summary>
        /// changes the positions relative to
        /// </summary>
        /// <param name="newParent"></param>
        internal override void SetParent(Entity newParent)
        {
            if (newParent != null && !newParent.HasDataBlob<PositionDB>())
                throw new Exception("newParent must have a PositionDB");
            Vector4 currentAbsolute = this.AbsolutePosition;
            Vector4 newRelative = currentAbsolute - newParent.GetDataBlob<PositionDB>().AbsolutePosition;
            base.SetParent(newParent);
            _position = newRelative;
        }

        /// <summary>
        /// Static function to find the distance between two positions.
        /// </summary>
        /// <returns>Distance between posA and posB.</returns>
        public static double GetDistanceBetween(PositionDB posA, PositionDB posB)
        {
            return (posA.AbsolutePosition - posB.AbsolutePosition).Length();
        }

        /// <summary>
        /// Instance function for those who don't like static functions.
        /// </summary>
        public double GetDistanceTo(PositionDB otherPos)
        {
            return GetDistanceBetween(this, otherPos);
        }

        /// <summary>
        /// Static Function to find the Distance Squared betweeen two positions.
        /// </summary>
        public static double GetDistanceBetweenSqrd(PositionDB posA, PositionDB posB)
        {
            return (posA.AbsolutePosition - posB.AbsolutePosition).LengthSquared();
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
    }
}
