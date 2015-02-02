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
    ///    If editing this list, moving things around should be fine, except for Bridge, RecFacility,Rail, and Adv Particle. Those sections have to stay contiguous due to how 
    ///    BuildComponentDataGrid works.
    /// In Economics.cs new components need to be handled for the build components list.
    /// </summary>
    public enum ComponentTypeTN
    {
        Crew,
        Fuel,
        Engineering,

        /// <summary>
        /// Don't change around Bridge and RecFacility here or else you'll have to change the logic in ClassDesign.cs that references them in BuildComponentDataGrid.
        /// </summary>
        Bridge,
        MaintenanceBay,
        FlagBridge,
        DamageControl,
        OrbitalHabitat,
        RecFacility,

        Armor,

        Engine,

        PassiveSensor, //isElectronic
        ActiveSensor,  //isElectronic

        CargoHold,
        CargoHandlingSystem,
        CryoStorage,

        BeamFireControl, //isElectronic

        /// <summary>
        /// As with Bridge and RecFacility, don't change around beam weapons without changing BuildComponentDataGrid. That function expects Rail to be the first, and AdvParticle to be the last.
        /// other then that adding to the middle should work fine.
        /// </summary>
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
        MissileFireControl, //isElectronic

        CIWS,
        Turret,

        JumpEngine,

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
        private int m_oCrew;
        public int crew
        {
            get { return m_oCrew; }
            set { m_oCrew = value; }
        }

        /// <summary>
        /// Cost the component, each component has its own scheme to determine cost.
        /// </summary>
        private decimal m_oCost;
        public decimal cost
        {
            get { return m_oCost; }
            set { m_oCost = value; }
        }

        /// <summary>
        /// Size of the component in question. 
        /// </summary>
        private float m_oSize;
        public float size
        {
            get { return m_oSize; }
            set { m_oSize = value; }
        }

        /// <summary>
        /// Likelyhood of destruction due to normal damage. Armor blocks have a uniform htk value, and this might be a way of handling that.
        /// </summary>
        private byte m_oHTK;
        public byte htk
        {
            get { return m_oHTK; }
            set { m_oHTK = value; }
        }

        /// <summary>
        /// Does this component incur maintenance failures?
        /// </summary>
        private bool m_oIsMilitary;
        public bool isMilitary
        {
            get { return m_oIsMilitary; }
            set { m_oIsMilitary = value; }
        }

        /// <summary>
        /// Is this component marked obsolete by the player.
        /// </summary>
        private bool m_oIsObsolete;
        public bool isObsolete
        {
            get { return m_oIsObsolete; }
            set { m_oIsObsolete = value; }
        }

        /// <summary>
        /// Components are salvageable, which means that they can be used even if the player has no idea how to build them.
        /// </summary>
        private bool m_oIsSalvaged;
        public bool isSalvaged
        {
            get { return m_oIsSalvaged; }
            set { m_oIsSalvaged = value; }
        }

        /// <summary>
        /// Can this component be divided into smaller pieces during a cargo load?
        /// </summary>
        private bool m_oIsDivisible;
        public bool isDivisible
        {
            get { return m_oIsDivisible; }
            set { m_oIsDivisible = value; }
        }

        /// <summary>
        /// Is this an electronic component that belongs in the EDAC, and is vulnerable to microwave damage?
        /// </summary>
        private bool m_oIsElectronic;
        public bool isElectronic
        {
            get { return m_oIsElectronic; }
            set { m_oIsElectronic = value; }
        }

        /// <summary>
        /// What type of component is this?
        /// </summary>
        private ComponentTypeTN m_oComponentType;
        public ComponentTypeTN componentType
        {
            get { return m_oComponentType; }
            set { m_oComponentType = value; }
        }


        /// <summary>
        /// Cost of this component in minerals
        /// </summary>
        private decimal[] m_aiMinerialsCost;
        public decimal[] minerialsCost
        {
            get
            {
                return m_aiMinerialsCost;
            }
            set
            {
                m_aiMinerialsCost = value;
            }
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
        private bool m_oIsDestroyed;
        public bool isDestroyed
        {
            get { return m_oIsDestroyed; }
            set { m_oIsDestroyed = value; }
        }

        /// <summary>
        /// which component on the ship is this?
        /// </summary>
        private int m_oComponentIndex;
        public int componentIndex
        {
            get { return m_oComponentIndex; }
            set { m_oComponentIndex = value; }
        }
    }
}
