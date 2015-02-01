using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;
using System.ComponentModel;

namespace Pulsar4X.Entities.Components
{
    public class JumpEngineDefTN : ComponentDefTN
    {
        public enum JDType
        {
            Military,
            Commercial
        }

        /// <summary>
        /// How space efficient is this jump drive?  4 points of efficiency means that 1 HS of jump drive can cover 4 HS of ship(1 for the JD and 3 for the rest of the ship).
        /// </summary>
        private float JumpEngineEfficiency;
        public float jumpEngineEfficiency
        {
            get { return JumpEngineEfficiency; }
        }

        /// <summary>
        /// How many ships may this jump drive permit to travel with it?
        /// </summary>
        private int SquadronSize;
        public int squadronSize
        {
            get { return SquadronSize; }
        }

        /// <summary>
        /// How many Km can this jump drive displace itself from a jump point during a squadron transit?
        /// </summary>
        private int JumpRadius;
        public int jumpRadius
        {
            get { return JumpRadius; }
        }

        /// <summary>
        /// Is this a military or commercial jump drive? both have differing characteristics for tonnage supported, squadron size, efficiency, and jump radius.
        /// </summary>
        private JDType JumpDriveType;
        public JDType jumpDriveType
        {
            get { return JumpDriveType; }
        }

        public JumpEngineDefTN(string JEngName, float Efficiency, int SquadSize, int Radius, JDType JType)
        {

        }
    }

    public class JumpEngineTN : ComponentTN
    {

    }
}
