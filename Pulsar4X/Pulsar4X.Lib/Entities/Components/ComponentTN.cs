using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities.Components
{
    /// <summary>
    /// Parent class for all TN component definitions.
    /// </summary>
    public abstract class ComponentDefTN
    {
        /// <summary>
        /// The Name of this component that will be user entered, and displayed to the player.
        /// </summary>
        private string Name;
        public string name
        {
            get { return Name; }
            set { Name = value; }
        }

        /// <summary>
        /// Crew requirement for the component, some components have a crew requirement of 0.
        /// </summary>
        private byte Crew;
        public byte crew
        {
            get { return Crew; }
            set { Crew = value; }
        }

        /// <summary>
        /// Cost the component, each component has its own scheme to determine cost.
        /// </summary>
        private decimal Cost;
        public decimal cost
        {
            get { return Cost; }
            set { Cost = value; }
        }

        /// <summary>
        /// Size of the component in question. 
        /// </summary>
        private float Size;
        public float size
        {
            get { return Size; }
            set { Size = value; }
        }

        /// <summary>
        /// Likelyhood of destruction due to normal damage. Armor blocks have a uniform htk value, and this might be a way of handling that.
        /// </summary>
        private byte HTK;
        public byte htk
        {
            get { return HTK; }
            set { HTK = value; }
        }

        /// <summary>
        /// Does this component incur maintenance failures?
        /// </summary>
        private bool IsMilitary;
        public bool isMilitary
        {
            get { return IsMilitary; }
            set { IsMilitary = value; }
        }

        /// <summary>
        /// Is this component marked obsolete by the player.
        /// </summary>
        private bool IsObsolete;
        public bool isObsolete
        {
            get { return IsObsolete; }
            set { IsObsolete = value; }
        }

        /// <summary>
        /// Components are salvageable, which means that they can be used even if the player has no idea how to build them.
        /// </summary>
        private bool IsSalvaged;
        public bool isSalvaged
        {
            get { return IsSalvaged; }
            set { IsSalvaged = value; }
        }
    }
    /// <summary>
    /// End ComponentDefTN class
    /// </summary>

    /// <summary>
    /// ComponentTN is the parent class for all TN components.
    /// </summary>
    public abstract class ComponentTN
    {
        /// <summary>
        /// Is this component working, or has it suffered critical damage
        /// </summary>
        private bool IsDestroyed;
        public bool isDestroyed
        {
            get { return IsDestroyed; }
            set { IsDestroyed = value; }
        }
    }
}
