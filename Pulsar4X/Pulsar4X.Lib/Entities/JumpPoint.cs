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
        /// Star that the JP is offset from.
        /// </summary>
        public Star Parent { get; set; }

        /// <summary>
        /// Coordinate offsets from the Parent.
        /// </summary>
        public double XOffset { get; set; }
        public double YOffset { get; set; }

        /// <summary>
        /// StarSystem the JP connects to. Destination is the closed end.
        /// </summary>
        public JumpPoint Connect { get; set; }

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
        public Faction GateOwner { get; set; }

        /// <summary>
        /// The contact that this jumppoint is associated with.
        /// </summary>
        public SystemContact Contact { get; set; }

        /// <summary>
        /// Creates a new JP at a location offset from the parent star by X and Y AU.
        /// </summary>
        /// <param name="Par">Parent Star</param>
        /// <param name="X">X in AU offset from parent star.</param>
        /// <param name="Y">Y in AU offset from parent star.</param>
        public JumpPoint(StarSystem Sys, Star Par, double X, double Y)
        {
            System = Sys;
            Parent = Par;
            XOffset = X;
            YOffset = Y;
            XSystem = Parent.XSystem + XOffset;
            YSystem = Parent.YSystem + YOffset;
            ZSystem = 0.0;

            SSEntity = StarSystemEntityType.JumpPoint;

            Id = Guid.NewGuid();

            /// <summary>
            /// Starsystems won't start off with explored jump points, which means that any attempt to transit them must create a new system.
            /// </summary>
            IsExplored = false;
            Connect = null;

            /// <summary>
            /// How should gate at startup be decided?
            /// </summary>
            IsGated = true; // TODO: Make setting to determine if all JP's have gates.
            GateOwner = null;

            Name = "JumpPoint #" + Sys.JumpPoints.Count;
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
        /// A new system needs to be created, likewise atleast 1 connection JP needs to be created for this new system.
        /// Is the JP we transited closed on this new end?
        /// The Ship in question needs to have its data updated.
        /// The system we left needs its contacts and faction detection lists updated.
        /// The faction needs a contact list for the new/"new" system
        /// </summary>
        public void ExploreJP()
        {
            // Generate a new system.
#warning JumpPoints cannot yet connect to existing systems.
            StarSystem newSystem = GameState.Instance.StarSystemFactory.Create("Unexplored System S-" + GameState.Instance.StarSystems.Count);
            GameState.Instance.StarSystems.Add(newSystem);

            // Choose a random jump point in the new system.
            int i = GameState.RNG.Next(newSystem.JumpPoints.Count - 1);

            // Connect us to them.
            Connect = newSystem.JumpPoints[i];
            Name = Name + "(" + Connect.System.Name + ")";
            // Connect them to us.
            Connect.Connect = this;
            Connect.Name = Connect.Name + "(" + System.Name + ")";

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

            // Ensure we have a connection, if not create one.
            if (Connect == null)
            {
                ExploreJP();
            }
#warning Not sure why, but the old contact is still displayed in the old system.
            System.RemoveContact(TransitTG.Contact);
            Connect.System.AddContact(TransitTG.Contact);

            TransitTG.Contact.UpdateLocationInSystem(Connect.XSystem, Connect.YSystem);

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

            // Ensure we have a connection, if not create one.
            if (Connect == null)
            {
                ExploreJP();
            }

            System.RemoveContact(TransitTG.Contact);
            Connect.System.AddContact(TransitTG.Contact);
            if (!TransitTG.TaskGroupFaction.SystemContacts.ContainsKey(Connect.System))
            {
                TransitTG.TaskGroupFaction.AddNewContactList(Connect.System);
            }

            /// <summary>
            /// Add/subtract offset to X/Y for this.
            /// <summary>
            TransitTG.Contact.UpdateLocationInSystem(Connect.XSystem, Connect.YSystem);

            /// <summary>
            /// Set Squadron Transit penalties here
            /// </summary>

            return 1;
        }

        /// <summary>
        /// Update the JumpPoint position after the Parent has been updated.
        /// </summary>
        public void UpdatePosition()
        {
            XSystem = Parent.XSystem + XOffset;
            YSystem = Parent.YSystem + YOffset;
        }
    }
}
