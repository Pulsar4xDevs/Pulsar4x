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

            // JumpPoints won't start off connected, which means that any attempt to transit them must create a new system.
            Connect = null;

            // How should gate at startup be decided?
            IsGated = true; // TODO: Make setting to determine if all JP's have gates.
            GateOwner = null;

            Name = "(G) JumpPoint #" + (Sys.JumpPoints.Count + 1); // Temp: Since all JP's have gates, add "(G)".
        }

        /// <summary>
        /// Adds a JumpGate at this JumpPoint.
        /// </summary>
        /// <param name="F">Faction of the gate builder</param>
        public void AddGate(Faction F)
        {
            IsGated = true;
            GateOwner = F;
            Name = "(G) " + Name;
        }

        /// <summary>
        /// Removes the JumpGate at this JumpPoint.
        /// </summary>
        public void RemoveGate()
        {
            IsGated = false;
            GateOwner = null;
            Name = Name.Substring(4, Name.Length); // Remove the "(G)"
        }

        /// <summary>
        /// Handles exploring unconnected jump points.
        /// This function only handles the connection of JumpPoints to new JumpPoints/Systems
        /// 
        /// TODO: Implement connecting to existing systems.
        /// Design Questions: 
        /// How do we determine we want to connect to an existing system? (X% Chance?, X% Chance if we already have other connections?, Other?)
        /// How do we decide what system to connect to? (Random?, "System Proximity" based?, Other?)
        /// </summary>
        public void ExploreJP()
        {
            // Generate a new system.
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
            // Check Jump Drive logic.
            if (!CanJump(TransitTG, true))
            {
                return 0;
            }

            // Ensure we have a connection, if not create one.
            if (Connect == null)
            {
                ExploreJP();
            }

            System.RemoveContact(TransitTG.Contact);
            Connect.System.AddContact(TransitTG.Contact);

            // Move us to the JP in the other system.
            TransitTG.Contact.UpdateLocationInSystem(Connect.XSystem, Connect.YSystem);

            // TODO: Set Standard transit penalties here

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
            // Check Jump Drive logic.
            if (!CanJump(TransitTG, false))
            {
                return 0;
            }

            // Ensure we have a connection, if not create one.
            if (Connect == null)
            {
                ExploreJP();
            }

            System.RemoveContact(TransitTG.Contact);
            Connect.System.AddContact(TransitTG.Contact);

            // Move us to the JP in the other system.
            TransitTG.Contact.UpdateLocationInSystem(Connect.XSystem, Connect.YSystem);

            // TODO: Set Squadron transit penalties here

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

        /// <summary>
        /// Determines if a TaskGroup has the ability to jump through this JumpPoint.
        /// Checks Gate, Gate Ownership, and JumpDrives.
        /// </summary>
        /// <param name="TransitTG">TG requesting Transit.</param>
        /// <param name="IsStandardTransit">True if StandardTransit, False if SquadronTransit</param>
        /// <returns>True if TG is capable of doing this jump.</returns>
        public bool CanJump(TaskGroupTN TransitTG, bool IsStandardTransit)
        {
            if (IsGated)
            {
                // TODO: Add "Allow Friendly factions to use Gates"
                if (GateOwner == null || GateOwner == TransitTG.TaskGroupFaction)
                {
                    // If nobody owns the gate, or we do, allow the jump.
                    return true;
                }
            }
            // TODO: Expand this to take into account JumpDrives.
            // Currently, JumpDrives don't exist, so how could we possibly jump?
            return false; 
        }
    }
}
