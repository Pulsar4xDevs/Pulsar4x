using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Pulsar4X.Entities.Components;

namespace Pulsar4X.Entities
{
    public class JumpPoint : StarSystemEntity
    {
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
        /// If set to false, along with Constants.GameSettings.AllowHostileGateJump,
        /// then hostiles will not be able to use the JumpGate on this JumpPoint.
        /// 
        /// Note: This is intended to be a user-specific setting on specific JumpPoints.
        /// </summary>
        public bool AllowHostileJumps { get; set; }

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
            Position.System = Sys;
            Parent = Par;
            XOffset = X;
            YOffset = Y;
            Position.X = Parent.Position.X + XOffset;
            Position.Y = Parent.Position.Y + YOffset;

            SSEntity = StarSystemEntityType.JumpPoint;

            Id = Guid.NewGuid();

            // Add ourselves to our system's list of JumpPoints.
            Sys.JumpPoints.Add(this);

            // JumpPoints won't start off connected, which means that any attempt to transit them must create a new connection.
            Connect = null;

            Name = "JumpPoint #" + (Sys.JumpPoints.Count);

            IsGated = false;
            if (Constants.GameSettings.JumpGatesOnEveryJumpPoint || Constants.GameSettings.JumpPointGatedChance > GameState.RNG.Next(101))
            {
                IsGated = true;
                Name = "(G)" + Name;
            }

            GateOwner = null;

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
            AllowHostileJumps = true; // Default setting for new JumpGates
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
        /// Determines if a TaskGroup has the ability to jump through this JumpPoint.
        /// Ensures a connection, and checks Gate, Gate Ownership, and JumpDrives.
        /// </summary>
        /// <param name="TransitTG">TG requesting Transit.</param>
        /// <param name="IsStandardTransit">True if StandardTransit, False if SquadronTransit</param>
        /// <returns>True if TG is capable of doing this jump.</returns>
        public bool CanJump(TaskGroupTN TransitTG, bool IsStandardTransit, out Dictionary<JumpEngineTN,ShipTN> usedJumpEngines)
        {
            usedJumpEngines = null;

            // Ensure we have a connection, if not create one.
            if (Connect == null)
            {
                CreateConnection();
            }

            if (IsGated && IsStandardTransit == true)
            {
                if (Constants.GameSettings.AllowHostileGateJump || AllowHostileJumps)
                {
                    // Gate/Game settings are not setup to allow blocking of hostiles.
                    return true;
                }
                if (GateOwner == null || GateOwner == TransitTG.TaskGroupFaction)
                {
                    // Nobody owns the gate, or we do, allow the jump.
                    ///< @todo Check if a friendly faction owns the gate, and allow.
                    return true;
                }
            }
            
            /// <summary>
            /// jump Transit code:
            /// </summary>
            int ComCount = 0;
            int MilCount = 0;
            float ComMaxHS = 0;
            float MilMaxHS = 0;
            int MilAccom = 0;
            int ComAccom = 0;
            

            /// <summary>
            /// For every ship:
            /// How many of each type of ship are there?
            /// What are the largest ship sizes in HS?
            /// Go through every jump engine
            /// Is this jump engine ready?
            /// Can this jump engine accomodate the other ships in the taskgroup?
            /// </summary>
            TransitTG.CountShips(out MilCount, out ComCount, out MilMaxHS, out ComMaxHS);

            /// <summary>
            /// if one ship can be accomodated all can be.
            /// </summary>
            if (IsStandardTransit == true)
            {
                if(MilCount > 1)
                    MilCount = 1;
                if(ComCount > 1)
                    ComCount = 1;
            }

            TransitTG.GetJDAccom(MilMaxHS, MilCount, ComMaxHS, ComCount, out MilAccom, out ComAccom, out usedJumpEngines);

            /// <summary>
            /// This jump can happen.
            /// </summary>
            if (MilAccom >= MilCount && ComAccom >= ComCount)
            {
                return true;
            }
            
            /// <summary>
            /// Something caused this jump to fail, will need to find out what for the faction messagelog though.
            /// </summary>
            return false;
        }

        /// <summary>
        /// Handles connecting unconnected jump points.
        /// This function only handles the connection of JumpPoints to new JumpPoints/Systems
        /// </summary>
        public void CreateConnection()
        {
            int systemIndex = -1;
            StarSystem connectedSystem = null;

            if (Constants.GameSettings.JumpPointLocalGroupConnectionChance >= GameState.RNG.Next(101))
            {
                do
                {
                    // We will connect to an 'existing system'
                    // Note, existing system doesn't necessarily exist.
                    systemIndex = Position.System.SystemIndex + GameState.RNG.Next(-Constants.GameSettings.JumpPointLocalGroupSize / 2, (Constants.GameSettings.JumpPointLocalGroupSize / 2) + 1);
                    if (systemIndex < 0)
                    {
                        // Sorry, we gotta keep you positive.
                        systemIndex = Math.Abs(systemIndex);
                    }
                }
                // Prevent linking to self.
                while (systemIndex == Position.System.SystemIndex);
            }
            else
            {
                // Generating a 'new system'
                // Note, new system isn't necessarily non-existant.
                systemIndex = GameState.Instance.StarSystemCurrentIndex;
            }


            if (systemIndex >= GameState.Instance.StarSystemCurrentIndex)
            {
                if (systemIndex == GameState.Instance.StarSystemCurrentIndex)
                {
                    // We're connecting to the next system on the list.
                    // This system will be generated, so increase our current index.
                    // This can happen with either 'existing system' or 'new system'
                    // however, it is intended for 'new system' generation.
                    GameState.Instance.StarSystemCurrentIndex++;
                }

                while (systemIndex > GameState.Instance.StarSystems.Count)
                {
                    // We're connecting to an 'existing system' that doesn't exist.
                    // If systemIndex 15 was selected above, and we've only got 10 systems, fill in the gap with new systems.
                    GameState.Instance.StarSystemFactory.Create("Unexplored System S-" + GameState.Instance.StarSystems.Count);

                    // Note, we didn't set our StarSystemCurrentIndex. This is intentional.
                    // When we make another connection, and we RNG a 'new system' connection, we may have just made the 'new system' here.
                    // Since we want 'existing system' to make 2 links once all JP's are explored, we ensure
                    // the 'new system' connection will actually connect to the 'existing system'.
                }
            }

            if (systemIndex == GameState.Instance.StarSystems.Count)
            {
                // Generate a new system.
                connectedSystem = GameState.Instance.StarSystemFactory.Create("Unexplored System S-" + GameState.Instance.StarSystems.Count);
            }
            else
            {
                // Select an existing system.
                connectedSystem = GameState.Instance.StarSystems[systemIndex];
            }

            // Choose a random jump point in the new system.
            JumpPoint connectedJP = null;

            List<JumpPoint> systemJumpPoints = new List<JumpPoint>();
            systemJumpPoints.AddRange(connectedSystem.JumpPoints); // Deep copy so we don't ruin the connectedSystem.JumpPoints list.

            while (systemJumpPoints.Count > 0)
            {
                // Select a random jump point.
                int i = GameState.RNG.Next(systemJumpPoints.Count);
                if (systemJumpPoints[i].Connect == null)
                {
                    // If selected JP doesn't have a connection, we use it.
                    connectedJP = systemJumpPoints[i];
                    break;
                }

                // If selected JP has a connection, remove it from the list, and select another.
                systemJumpPoints.RemoveAt(i);
            }

            if (connectedJP == null)
            {
                // All JP's are already connected, create a new one.
                Star parentStar = connectedSystem.Stars[GameState.RNG.Next(connectedSystem.Stars.Count)];
                connectedJP = JumpPoint.CreateJumpPoint(connectedSystem, parentStar);
            }

            // Connect us to them.
            Connect = connectedJP;
            Name = Name + "(" + Connect.Position.System.Name + ")";

            // Connect them to us.
            Connect.Connect = this;
            Connect.Name = Connect.Name + "(" + Position.System.Name + ")";
        }

        /// <summary>
        /// Update the JumpPoint position after the Parent has been updated.
        /// </summary>
        public void UpdatePosition()
        {
            Position.X = Parent.Position.X + XOffset;
            Position.Y = Parent.Position.Y + YOffset;
        }

        /// <summary>
        /// Creates a new jump point in the designated system around the designated star.
        /// </summary>
        /// <param name="system">System the JumpPoint will be created in.</param>
        /// <param name="parent">Star the JumpPoint will be created around.</param>
        /// <returns>New JumpPoint for use.</returns>
        public static JumpPoint CreateJumpPoint(StarSystem system, Star parent)
        {
            double minDistance = double.MaxValue;
            double maxDistance = double.MinValue;

            // Find the Min/Max limits for JP creation.
            foreach (Planet p in parent.Planets)
            {
                // Clamp Min/Max distances for JP
                if (p.SemiMajorAxis < minDistance)
                {
                    minDistance = p.SemiMajorAxis;
                }
                if (p.SemiMajorAxis > maxDistance)
                {
                    maxDistance = p.SemiMajorAxis;
                }
            }

            // Determine a location for the new JP.
            // Location will be between minDistance and 75% of maxDistance.
            double offsetX = ((maxDistance - minDistance) * GameState.RNG.Next(76) / 100) + minDistance;
            double offsetY = ((maxDistance - minDistance) * GameState.RNG.Next(76) / 100) + minDistance;

            // Randomly flip the sign of the offsets.
            if (GameState.RNG.Next(2) == 0)
            {
                offsetX = -offsetX;
            }
            if (GameState.RNG.Next(2) == 0)
            {
                offsetY = -offsetY;
            }

            // Create the new jumpPoint and link it to it's parent system.
            JumpPoint newJumpPoint = new JumpPoint(system, parent, offsetX, offsetY);

            return newJumpPoint;
        }
    }
}
