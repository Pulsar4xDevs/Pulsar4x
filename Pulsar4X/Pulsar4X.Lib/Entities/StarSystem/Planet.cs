using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json;

#if LOG4NET_ENABLED
using log4net;
#endif

namespace Pulsar4X.Entities
{
    public class Planet : OrbitingEntity
    {

#if LOG4NET_ENABLED
        public static readonly ILog logger = LogManager.GetLogger(typeof(Planet));
#endif
        public BindingList<Planet> Moons { get; set; } //moons orbiting the planet
        public BindingList<Population> Populations { get; set; } // list of Populations (colonies) on this planet.
        /// <summary>
        /// Are any taskgroups orbiting with this body?
        /// </summary>
        public BindingList<TaskGroupTN> TaskGroupsInOrbit { get; set; }

        /// <summary>
        /// Entry for whether or not this planet has ruins on it.
        /// </summary>
        public Ruins PlanetaryRuins { get; set; }

        /// <summary>
        /// What mineral resources does this planet have to be mined?
        /// </summary>
        float[] m_aiMinerialReserves;
        public float[] MinerialReserves
        {
            get
            {
                return m_aiMinerialReserves;
            }
        }

        /// <summary>
        /// What is the accessibility of this mineral?
        /// </summary>
        private float[] m_aiMinerialAccessibility;
        public float[] MinerialAccessibility
        {
            get
            {
                return m_aiMinerialAccessibility;
            }
        }

        public Planet(OrbitingEntity parent)
            : base()
        {
            /// <summary>
            /// create these or else anything that relies on a unique global id will break.
            /// </summary>
            Id = Guid.NewGuid();

            Moons = new BindingList<Planet>();
            Populations = new BindingList<Population>();

            SSEntity = StarSystemEntityType.Body;

            Parent = parent;
            Position = parent.Position;

            TaskGroupsInOrbit = new BindingList<TaskGroupTN>();

#warning planet generation needs minerals, anomalies, and ruins generation.
            PlanetaryRuins = new Ruins();

            /// <summary>
            /// Default mineral amount is zero.
            /// do mineral generation elsewhere.
            /// </summary>
            m_aiMinerialReserves = new float[Constants.Minerals.NO_OF_MINERIALS];
            m_aiMinerialAccessibility = new float[Constants.Minerals.NO_OF_MINERIALS];
            for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
            {
                m_aiMinerialReserves[mineralIterator] = 0.0f;
                m_aiMinerialAccessibility[mineralIterator] = 0.0f;
            }
        }

        /// <summary>
        /// Update the planet's position, Parent positions must be updated in sequence obviously.
        /// </summary>
        /// <param name="tickValue"></param>
        public void UpdatePosition(int tickValue)
        {
            Pulsar4X.Lib.OrbitTable.Instance.UpdatePosition(this, tickValue);

            /// <summary>
            /// Adjust planet position based on the primary. Right now Position.X and Position.Y assume orbiting around 0,0. secondary stars, and eventually moons will have this issue.
            /// </summary>
            Position.X = Position.X + Parent.Position.X;
            Position.Y = Position.Y + Parent.Position.Y;

            /// <summary>
            /// Update all the moons.
            /// </summary>
            foreach (Planet CurrentMoon in Moons)
            {
                CurrentMoon.UpdatePosition(tickValue);
            }

            ///<summary>
            ///Update taskgroup positions.
            ///</summary>
            foreach (TaskGroupTN TaskGroup in TaskGroupsInOrbit)
            {
                TaskGroup.Contact.Position.X = Position.X;
                TaskGroup.Contact.Position.Y = Position.Y;
            }
        }

        /// <summary>
        /// This generates the rich assortment of all minerals for a homeworld. non-hw planets have less, or even no resources.
        /// should some resources be scarcer than others?
        /// </summary>
        public void HomeworldMineralGeneration()
        {
            m_aiMinerialReserves[0] = 150000.0f + (100000.0f * ((float)GameState.RNG.Next(0, 100000) / 100000.0f));
            m_aiMinerialAccessibility[0] = 1.0f;
            for (int mineralIterator = 1; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
            {
                m_aiMinerialReserves[mineralIterator] = 50000.0f + (70000.0f * ((float)GameState.RNG.Next(0, 100000) / 100000.0f));
                m_aiMinerialAccessibility[mineralIterator] = 1.0f * ((float)GameState.RNG.Next(2, 10) / 10.0f);
            }
        }
    }
}
