using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities.Components
{
    /// <summary>
    /// I really should have rolled all of this into contact(along with perhaps a detectionEntity class) and used it as a parent class for both TaskGroupTN and OrdnanceGroupTN
    /// </summary>
    public class TargetTN : GameEntity
    {
        /// <summary>
        /// Missiles can be fired at many potential targets, hence falling back to the SSE
        /// </summary>
        private StarSystemEntityType TargetType;
        public StarSystemEntityType targetType
        {
            get { return TargetType; }
            set { TargetType = value; }
        }

        /// <summary>
        /// Ship killer ordnance usually.
        /// </summary>
        private ShipTN Ship;
        public ShipTN ship
        {
            get { return Ship; }
        }

        /// <summary>
        /// Survey typically.
        /// </summary>
        private Planet Body;
        public Planet body
        {
            get { return body; }
        }

        /// <summary>
        /// Planetary bombardment typically.
        /// </summary>
        private Population Pop;
        public Population pop
        {
            get { return Pop; }
        }

        /// <summary>
        /// Waypoint target, Drones, sensor missiles, and mines will make use of this for now.
        /// specialized minelaying code may be put in later.
        /// </summary>
        private Waypoint WP;
        public Waypoint wp
        {
            get { return WP; }
        }

        /// <summary>
        /// The target for this ordnance is another missile.
        /// </summary>
        private OrdnanceGroupTN MissileGroup;
        public OrdnanceGroupTN missileGroup
        {
            get { return MissileGroup; }
        }

        /// <summary>
        /// Constructor for ship targets.
        /// </summary>
        /// <param name="ShipTarget">Ship that will be the target</param>
        public TargetTN(ShipTN ShipTarget)
        {
            Id = Guid.NewGuid();
            TargetType = ShipTarget.ShipsTaskGroup.SSEntity;
            Ship = ShipTarget;
        }

        /// <summary>
        /// Constructor for planetary targets.
        /// </summary>
        /// <param name="BodyTarget">Body which is the target</param>
        public TargetTN(Planet BodyTarget)
        {
            Id = Guid.NewGuid();
            TargetType = BodyTarget.SSEntity;
            Body = BodyTarget;
        }

        /// <summary>
        /// Constructor for population targets.
        /// </summary>
        /// <param name="PopTarget">Population that is targeted.</param>
        public TargetTN(Population PopTarget)
        {
            Id = Guid.NewGuid();
            TargetType = StarSystemEntityType.Population;
            Pop = PopTarget;
        }

        /// <summary>
        /// Constructor for Waypoint targets.
        /// </summary>
        /// <param name="WPTarget">waypoint to be targeted.</param>
        public TargetTN(Waypoint WPTarget)
        {
            Id = Guid.NewGuid();
            TargetType = WPTarget.SSEntity;
            WP = WPTarget;
        }

        /// <summary>
        /// Constructor for Missile group targets.
        /// </summary>
        /// <param name="OGTarget">missile targeted on.</param>
        public TargetTN(OrdnanceGroupTN OGTarget)
        {
            Id = Guid.NewGuid();
            TargetType = OGTarget.SSEntity;
            MissileGroup = OGTarget;
        }
    }
}
