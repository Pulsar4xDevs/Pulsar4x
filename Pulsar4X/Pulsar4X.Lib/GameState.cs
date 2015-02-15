using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Pulsar4X.Entities;
using System.ComponentModel;

namespace Pulsar4X
{
    public class GameState
    {

        #region Singleton

        private static GameState instance;
        public static GameState Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameState();
                }
                return instance;
            }
        }

        /// <summary>
        /// top level random number generator
        /// </summary>
        private static Random _RNG;
        public static Random RNG
        {
            get
            {
                if (_RNG == null)
                {
                    _RNG = new Random();
                }
                return _RNG;
            }
        }

        /// <summary>
        /// The simulation proper, all the game mechanics are brought together here.
        /// </summary>
        private static SimEntity _SE;
        public static SimEntity SE
        {
            get
            {
                if (_SE == null)
                {
                    _SE = new SimEntity(2, 0);
                }
                return _SE;
            }
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        public static void Initialise()
        {
            if (instance == null)
            {
                instance = new GameState();
            }

            if (_RNG == null)
            {
                _RNG = new Random();
            }

            if (_SE == null)
            {
                _SE = new SimEntity(2, 0);
            }
        }

        private GameState()
        {
            CurrentYear = 2025;
            m_currentSecond = 0;

            StarSystemCurrentIndex = 0;

            m_oGameDateTime = new DateTime(CurrentYear, 1, 1); // sets the date to 1 Jan 2025, just like aurora!!!
        }

        #endregion

        public void Load()
        {
            // Load From db
            throw new NotImplementedException();
        }

        public void Commit()
        {
            // Save all changes to db
            throw new NotImplementedException();
        }

        #region Game Meta data
        public string Name { get; set; }
        public string SaveDirectoryPath { get; set; }
        #endregion

        #region Game Data

        DateTime m_oGameDateTime;

        public DateTime GameDateTime
        {
            get
            {
                return m_oGameDateTime;
            }
            set
            {
                m_oGameDateTime = value;
            }
        }

        /// <summary>
        /// Current Second of the year.
        /// </summary>
        private int m_currentSecond;
        public int CurrentSecond
        {
            get { return m_currentSecond; }
            set
            {
                while (value >= Constants.TimeInSeconds.Year)
                {
                    value -= (int)Constants.TimeInSeconds.Year;
                    CurrentYear++;
                }
                m_currentSecond = value;
            }
        }

        /// <summary>
        /// Current Year.
        /// </summary>
        public int CurrentYear { get; set; }

        /// <summary>
        /// deltaTime in seconds of the last update to the current update.
        /// </summary>
        public int LastTimestep { get; set; }

        #endregion

        #region Entities

        private BindingList<Species> _species;
        public BindingList<Species> Species
        {
            get
            {
                if (_species == null)
                {
                    //Load from DB here
                    _species = new BindingList<Species>();
                }
                return _species;
            }
            set { _species = value; }
        }

        private BindingList<Faction> _factions;
        public BindingList<Faction> Factions
        {
            get
            {
                if (_factions == null)
                {
                    //Load from DB here
                    _factions = new BindingList<Faction>();
                }
                return _factions;
            }
            set { _factions = value; }
        }

        private BindingList<StarSystem> _starsystems;
        public BindingList<StarSystem> StarSystems
        {
            get
            {
                if (_starsystems == null)
                {
                    // Load from DB here
                    _starsystems = new BindingList<StarSystem>();
                }
                return _starsystems;
            }
            set { _starsystems = value; }
        }

        public int StarSystemCurrentIndex;

        private BindingList<Star> _stars;
        public BindingList<Star> Stars
        {
            get
            {
                if (_stars == null)
                {
                    // Load from DB here
                    _stars = new BindingList<Star>();
                }
                return _stars;
            }
            set { _stars = value; }
        }

        private BindingList<Planet> _planets;
        public BindingList<Planet> Planets
        {
            get
            {
                if (_planets == null)
                {
                    // Load from DB here
                    _planets = new BindingList<Planet>();
                }
                return _planets;
            }
            set { _planets = value; }
        }

        private BindingList<String> _compResearchTechs;
        public BindingList<String> CompResearchTechs
        {
            get
            {
                if (_compResearchTechs == null)
                {
                    //Load from DB here
                    _compResearchTechs = new BindingList<String>();

                    _compResearchTechs.Add("Active Sensor / Missile Fire Control");
                    _compResearchTechs.Add("Beam Fire Control");
                    _compResearchTechs.Add("CIWS");
                    _compResearchTechs.Add("Cloaking Device");
                    _compResearchTechs.Add("EM Detection Sensors");
                    _compResearchTechs.Add("Engines");
                    _compResearchTechs.Add("Gauss Cannon");
                    _compResearchTechs.Add("High Power Microwave");
                    _compResearchTechs.Add("Jump Engines");
                    _compResearchTechs.Add("Lasers");
                    _compResearchTechs.Add("Magazine");
                    _compResearchTechs.Add("Meson Cannon");
                    _compResearchTechs.Add("Missile Engines");
                    _compResearchTechs.Add("Missile Launchers");
                    _compResearchTechs.Add("New Species");
                    _compResearchTechs.Add("Particle Beam");
                    _compResearchTechs.Add("Plasma Carronade");
                    _compResearchTechs.Add("Plasma Torpedo");
                    _compResearchTechs.Add("Power Plants");
                    _compResearchTechs.Add("Railgun");
                    _compResearchTechs.Add("Shields - Absorption");
                    _compResearchTechs.Add("Shields - Standard");
                    _compResearchTechs.Add("Thermal Sensors");
                }
                return _compResearchTechs;
            }
            set { _compResearchTechs = value; }
        }


        #endregion Entities

    }
}
