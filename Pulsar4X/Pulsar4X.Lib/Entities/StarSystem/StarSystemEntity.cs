using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities
{
    public enum StarSystemEntityType
    {
        Invalid,
        Body,
        Waypoint,
        JumpPoint,
        TaskGroup,
        Population,
        Missile,
        TypeCount
    }

    public struct SystemPosition
    {
        /// <summary>
        /// System currently in.
        /// </summary>
        public StarSystem System { get; set; }

        /// <summary>
        /// System X coordinante in AU
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// System Y coordinante in AU
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Initilized constructor.
        /// </summary>
        /// <param name="system">StarSystem value.</param>
        /// <param name="x">X value.</param>
        /// <param name="y">Y value.</param>
        public SystemPosition(StarSystem system, double x, double y) : this()
        {
            System = system;
            X = x;
            Y = y;
        }

        /// <summary>
        /// Static function to find the distance between two positions.
        /// </summary>
        /// <param name="posA"></param>
        /// <param name="posB"></param>
        /// <returns>distance between posA and posB</returns>
        public static float GetDistanceBetween(SystemPosition posA, SystemPosition posB)
        {
            if (posA.System != posB.System)
            {
                throw new InvalidOperationException("Cannont compare distances between positions in different systems.");
            }
            float distX = (float)(posA.X - posB.X);
            float distY = (float)(posA.Y - posB.Y);

            return (float)Math.Sqrt((distX * distX) + (distY * distY));
        }

        /// <summary>
        /// Instance function for those who don't like static functions.
        /// </summary>
        /// <param name="otherPos"></param>
        /// <returns></returns>
        public float GetDistanceTo(SystemPosition otherPos)
        {
            return GetDistanceBetween(this, otherPos);
        }

        /// <summary>
        /// Adds two SystemPositions together.
        /// </summary>
        /// <param name="posA"></param>
        /// <param name="posB"></param>
        /// <returns></returns>
        public static SystemPosition operator +(SystemPosition posA, SystemPosition posB)
        {
            if (posA.System != posB.System)
            {
                throw new InvalidOperationException("Cannot add positions in different systems.");
            }

            posA.X += posB.X;
            posA.Y += posB.Y;
            return posA;
        }
    }

    public abstract class StarSystemEntity : GameEntity
    {
        /// <summary>
        /// Current System and Position of the entity.
        /// </summary>
        public SystemPosition Position;

        /// <summary>
        /// Type of entity that is represented here.
        /// </summary>
        public StarSystemEntityType SSEntity { get; set; }

        public StarSystemEntity()
            : base()
        {

            //default leagal orders.
            _legalOrders = new List<Constants.ShipTN.OrderType>
            {
                (Constants.ShipTN.OrderType.MoveTo),
                (Constants.ShipTN.OrderType.ExtendedOrbit),
                (Constants.ShipTN.OrderType.Picket),
                (Constants.ShipTN.OrderType.SendMessage),
                (Constants.ShipTN.OrderType.EqualizeFuel),
                (Constants.ShipTN.OrderType.EqualizeMSP),
                (Constants.ShipTN.OrderType.ActivateTransponder),
                (Constants.ShipTN.OrderType.DeactivateTransponder),
                (Constants.ShipTN.OrderType.ActivateSensors),
                (Constants.ShipTN.OrderType.DeactivateSensors),
                (Constants.ShipTN.OrderType.ActivateShields),
                (Constants.ShipTN.OrderType.DeactivateShields),
                (Constants.ShipTN.OrderType.DivideFleetToSingleShips),
                (Constants.ShipTN.OrderType.DetachNonGeoSurvey),
                (Constants.ShipTN.OrderType.DetachNonGravSurvey),
                (Constants.ShipTN.OrderType.RefuelFromOwnTankers),
                (Constants.ShipTN.OrderType.DetachTankers),
                (Constants.ShipTN.OrderType.ResupplyFromOwnSupplyShips),
                (Constants.ShipTN.OrderType.DetachSupplyShips),
                (Constants.ShipTN.OrderType.ReloadFromOwnColliers),
                (Constants.ShipTN.OrderType.DetachColliers),
                (Constants.ShipTN.OrderType.ReleaseAt),
            };
        }

        /// <summary>      
        /// generic orders a selected tg targeting anything will have all these options if the selecteed tg can do these.
        /// </summary>
        protected List<Constants.ShipTN.OrderType> _legalOrders;

        /// <summary>
        /// list of legal orders a taskgroup or unit can use againsed this entity ie when this entity is the target.
        /// </summary>
        /// <param name="faction"></param>
        /// <returns></returns>
        public virtual List<Constants.ShipTN.OrderType> LegalOrders(Faction faction)
        {
            List<Constants.ShipTN.OrderType> legalOrders = new List<Constants.ShipTN.OrderType>();
            legalOrders.AddRange(_legalOrders);

            return legalOrders;
        }
    }
}
