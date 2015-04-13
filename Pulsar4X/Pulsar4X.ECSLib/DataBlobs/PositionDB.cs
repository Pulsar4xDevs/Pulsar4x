using System;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class PositionDB : BaseDataBlob
    {
        /// <summary>
        /// The Position as a Vec4, in AU.
        /// </summary>
        public Vector4 Position;

        /// <summary>
        /// System X coordinante in AU
        /// </summary>
        [JsonIgnore]
        public double X
        {
            get { return Position.X; }
            set { Position.X = value; }
        }

        /// <summary>
        /// System Y coordinante in AU
        /// </summary>
        [JsonIgnore]
        public double Y
        {
            get { return Position.Y; }
            set { Position.Y = value; }
        }

        /// <summary>
        /// System Z coordinate in AU
        /// </summary>
        [JsonIgnore]
        public double Z
        {
            get { return Position.Z; }
            set { Position.Z = value; }
        }

        #region Unit Conversion Properties

        /// <summary>
        /// Position as a vec4. This is a utility property that converts Position to Km on get and to AU on set.
        /// </summary>
        [JsonIgnore]
        public Vector4 PositionInKm
        {
            get { return new Vector4(Distance.ToKm(Position.X), Distance.ToKm(Position.Y), Distance.ToKm(Position.Z), 0); }
            set { Position = new Vector4(Distance.ToAU(value.X), Distance.ToAU(value.Y), Distance.ToAU(value.Z), 0); }
        }

        /// <summary>
        /// System X coordinante. This is a utility property that converts the X Coord. to Km on get and to AU on set.
        /// </summary>
        [JsonIgnore]
        public double XInKm
        {
            get { return Distance.ToKm(Position.X); }
            set { Position.X = Distance.ToAU(value); }
        }

        /// <summary>
        /// System Y coordinante. This is a utility property that converts the Y Coord. to Km on get and to AU on set.
        /// </summary>
        [JsonIgnore]
        public double YInKm
        {
            get { return Distance.ToKm(Position.Y); }
            set { Position.Y = Distance.ToAU(value); }
        }

        /// <summary>
        /// System Z coordinate. This is a utility property that converts the Z Coord. to Km on get and to AU on set.
        /// </summary>
        [JsonIgnore]
        public double ZInKm
        {
            get { return Distance.ToKm(Position.Z); }
            set { Position.Z = Distance.ToAU(value); }
        }

        #endregion

        /// <summary>
        /// Initilized constructor.
        /// </summary>
        /// <param name="system">StarSystem value.</param>
        /// <param name="x">X value.</param>
        /// <param name="y">Y value.</param>
        public PositionDB(double x, double y, double z)
        {
            Position = new Vector4(x, y, z, 0);
        }

        public PositionDB(Vector4 pos)
        {
            Position = pos;
        }

        public PositionDB()
        {
            Position = Vector4.Zero;
        }

        public PositionDB(PositionDB positionDB)
            : this(positionDB.X, positionDB.Y, positionDB.Z)
        {
        }

        /// <summary>
        /// Static function to find the distance between two positions.
        /// </summary>
        /// <returns>Distance between posA and posB.</returns>
        public static double GetDistanceBetween(PositionDB posA, PositionDB posB)
        {
            return (posA.Position - posB.Position).Length();
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
            return (posA.Position - posB.Position).LengthSquared();
        }

        /// <summary>
        /// Instance function for those who don't like static functions.
        /// </summary>
        public double GetDistanceToSqrd(PositionDB otherPos)
        {
            return GetDistanceBetweenSqrd(this, otherPos);
        }

        /// <summary>
        /// Adds two PositionDBs together.
        /// </summary>
        /// <param name="posA"></param>
        /// <param name="posB"></param>
        /// <returns></returns>
        public static PositionDB operator +(PositionDB posA, PositionDB posB)
        {
            return new PositionDB(posA.Position + posB.Position);
        }
    }
}
