using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities.Components
{
    /// <summary>
    /// List of Components:
    /// Be especially sure to update DestroyComponent and RepairComponent in Ship.cs when updating this list.
    /// in Ship.cs OnDamaged() new electronic components need to be handled as well for microwave damage.
    /// in Shipclass.cs add the Add_Component() function, and update the class summary.
    /// when adding components in general do the declarations, at them to shipclass, add them to ship, work out their mechanics and place those appropriately.
    /// In Class Design.cs, and Components.cs any new components need to be filled in for just about every component type switch(lots of them). also BuildDesignTab and GetListBoxComponent
    /// </summary>
    public enum ComponentTypeTN
    {
        Crew,
        Fuel,
        Engineering,
        Bridge,
        MaintenanceBay,
        FlagBridge,
        DamageControl,
        OrbitalHabitat,
        RecFacility,

        Armor,
        
        Engine,
        
        PassiveSensor,
        ActiveSensor,
        
        CargoHold,
        CargoHandlingSystem,
        CryoStorage,
        
        BeamFireControl,
        Rail,
        Gauss,
        Plasma,
        Laser,
        Meson,
        Microwave,
        Particle,
        AdvRail,
        AdvLaser,
        AdvPlasma,
        AdvParticle,

        Reactor,

        Shield,
        AbsorptionShield,

        MissileLauncher,
        Magazine,
        MissileFireControl,

        CIWS,

#warning Need all frontend work.
        Turret,

        TypeCount
    }

    /// <summary>
    /// Parent class for all TN component definitions.
    /// </summary>
    public abstract class ComponentDefTN : GameEntity
    {
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

        /// <summary>
        /// Can this component be divided into smaller pieces during a cargo load?
        /// </summary>
        private bool IsDivisible;
        public bool isDivisible
        {
            get { return IsDivisible; }
            set { IsDivisible = value; }
        }

        /// <summary>
        /// Is this an electronic component that belongs in the EDAC, and is vulnerable to microwave damage?
        /// </summary>
        private bool IsElectronic;
        public bool isElectronic
        {
            get { return IsElectronic; }
            set { IsElectronic = value; }
        }

        /// <summary>
        /// What type of component is this?
        /// </summary>
        private ComponentTypeTN ComponentType;
        public ComponentTypeTN componentType
        {
            get { return ComponentType; }
            set { ComponentType = value; }
        }
    }
    /// <summary>
    /// End ComponentDefTN class
    /// </summary>

    /// <summary>
    /// ComponentTN is the parent class for all TN components.
    /// </summary>
    public abstract class ComponentTN : GameEntity
    {
        /// <summary>
        /// Need a component type, probably an enum, and an index for the componentList of this faction
        /// </summary>

        /// <summary>
        /// Is this component working, or has it suffered critical damage
        /// </summary>
        private bool IsDestroyed;
        public bool isDestroyed
        {
            get { return IsDestroyed; }
            set { IsDestroyed = value; }
        }

        /// <summary>
        /// which component on the ship is this?
        /// </summary>
        private int ComponentIndex;
        public int componentIndex
        {
            get { return ComponentIndex; }
            set { ComponentIndex = value; }
        }
    }
}
