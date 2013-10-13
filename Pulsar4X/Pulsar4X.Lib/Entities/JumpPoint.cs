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
        public JumpPoint(StarSystem Sys, double X, double Y)
        {
            System = Sys;
            XSystem = X;
            YSystem = Y;
            ZSystem = 0.0;

            SSEntity = StarSystemEntityType.JumpPoint;

            Name = System.Name + " #" + System.JumpPoints.Count.ToString();
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
            /// A new system needs to be created, likewise atleast 1 connection JP needs to be created for this new system.
            /// Is the JP we transited closed on this new end?
            /// The Ship in question needs to have its data updated.
            /// The system we left needs its contacts and faction detection lists updated.
            /// The faction needs a contact list for the new/"new" system
            /// </summary>
        }

        /// <summary>
        /// Simple transits happen here. Civilian ships, and military ships that are simply travelling will use this function.
        /// there is a higher penalty associated with a standard transit, ships appear directly on the JP, but there is no TG size limitation.
        /// </summary>
        /// <param name="TransitTG"> Transiting TG</param>
        /// <returns>Success or failure of transit as an integer code.</returns>
        public int StandardTransit(TaskGroupTN TransitTG)
        {
            /// <summary>
            /// Jump Engine/Gate logic needs to be done here.
            /// </summary>
            /// 

            System.RemoveContact(TransitTG.Contact);
            Connect.System.AddContact(TransitTG.Contact);

            TransitTG.Contact.UpdateLocationInSystem(XSystem, YSystem);

            /// <summary>
            /// Likewise, set Standard transit penalties for the TG
            /// </summary>

            return 1;
        }

        /// <summary>
        /// Military Squadron jumps into a system are handled here. Ships jump away from the jp, and have a lower transit penalty than a standard transit,
        /// but only the squadron size may make the jump.
        /// </summary>
        /// <param name="TransitTG">Transiting TG</param>
        /// <returns>Success or failure of transit as an integer code.</returns>
        public int SquadronTransit(TaskGroupTN TransitTG)
        {
            /// <summary>
            /// Check Jump Engine logic here.
            /// </summary>

            System.RemoveContact(TransitTG.Contact);
            Connect.System.AddContact(TransitTG.Contact);

            /// <summary>
            /// Add/subtract offset to X/Y for this.
            /// <summary>
            TransitTG.Contact.UpdateLocationInSystem(XSystem, YSystem);

            /// <summary>
            /// Set Squadron Transit penalties here
            /// </summary>

            return 1;
        }
    }
}
