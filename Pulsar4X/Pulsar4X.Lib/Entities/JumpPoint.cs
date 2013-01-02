using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities
{
    public class JumpPoint : StarSystemEntity
    {
        /// <summary>
        /// Mass is totally unnecessary here.
        /// </summary>
        public override double Mass 
        { 
            get { return 0.0; } 
            set { value = 0.0; } 
        }

        /// <summary>
        /// StarSystem which originates this JP. Source side is never closed.
        /// </summary>
        public StarSystem System { get; set; }

        /// <summary>
        /// StarSystem the JP connects to. Destination is the closed end.
        /// </summary>
        public JumpPoint Connect { get; set; }

        /// <summary>
        /// Is this end of the JP closed?
        /// </summary>
        public bool IsClosed { get; set; }

        /// <summary>
        /// Has this JP been explored and either connected to an existing system or had a new system created?
        /// </summary>
        public bool IsExplored { get; set; }

        /// <summary>
        /// Did someone build a gate for this JP side?
        /// </summary>
        public bool IsGated { get; set; }

        /// <summary>
        /// Which faction owns the gate? only influences display for the time being.
        /// </summary>
        public Faction GateOwner {  get; set; }

        /// <summary>
        /// Creates a new JP at location in AU X,Y
        /// </summary>
        /// <param name="X">X in AU</param>
        /// <param name="Y">Y in AU</param>
        public JumpPoint(double X, double Y)
        {
            XSystem = X;
            YSystem = Y;
            ZSystem = 0.0;

            SSEntity = StarSystemEntityType.JumpPoint;
        }

        /// <summary>
        /// Builds a gate at this JP
        /// </summary>
        /// <param name="F">Faction of the gate builder</param>
        public void BuildGate(Faction F)
        {
            IsGated = true;
            GateOwner = F;
        }

        /// <summary>
        /// Will ultimately handle exploring JPs
        /// </summary>
        public void ExploreJP()
        {
            /// <summary>
            /// A new system needs to be created.
            /// Is the JP we transited closed on this new end?
            /// The Ship in question needs to have its data updated.
            /// The system we left needs its contacts and faction detection lists updated.
            /// The faction might need to have some updating done to it.
            /// </summary>
        }

        /// <summary>
        /// Simple transits happen here.
        /// </summary>
        public void TransitJP()
        {

        }
    }
}
