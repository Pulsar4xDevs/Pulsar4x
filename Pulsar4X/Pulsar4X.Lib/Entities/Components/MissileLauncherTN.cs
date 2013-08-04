using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities.Components
{
    public class MissileLauncherDefTN : ComponentDefTN
    {
        /// <summary>
        /// Maximum size missile that this launcher may load. Due to size reductions launcher size and LaunchMaxSize will not always be the same.
        /// </summary>
        private float LaunchMaxSize;
        public float launchMaxSize
        {
            get { return LaunchMaxSize; }
        }

        /// <summary>
        /// Reload rate tech, will modify reload times with IsPDCSilo and Reduction.
        /// </summary>
        private int ReloadRateTech;
        public int reloadRateTech
        {
            get { return ReloadRateTech; }
        }

        /// <summary>
        /// Is this a PDC Silo? halves loading time.
        /// </summary>
        private bool IsPDCSilo;
        public bool isPDCSilo
        {
            get { return IsPDCSilo; }
        }

        /// <summary>
        /// Reload time of the missile in seconds.
        /// </summary>
        private int RateOfFire;
        public int rateOfFire
        {
            get { return RateOfFire; }
        }

        /// <summary>
        /// This launcher cannot be reloaded from a magazine directly and has no crew requirements. Instead it has hangar/Maintenance facility loading times.
        /// </summary>
        private bool IsBoxLauncher;
        public bool isBoxLauncher
        {
            get { return IsBoxLauncher; }
        }

        /// <summary>
        /// Hangar reload time for box launchers.
        /// </summary>
        private int HangarReload;
        public int hangarReload
        {
            get { return HangarReload; }
        }

        /// <summary>
        /// Maintenance facility reload for box launchers. will be 10x the hangar reload time.
        /// </summary>
        private int MFReload;
        public int mFReload
        {
            get { return MFReload; }
        }
        /// <summary>
        /// Constructor for missile launcher definitions.
        /// </summary>
        /// <param name="title">Name of the launcher itself.</param>
        /// <param name="hs">Original size of the launcher, also launch max.</param>
        /// <param name="ReloadTech">Missile reload techlevel.</param>
        /// <param name="ShipOrPDC">Is this a ship launch tube, or a PDC launch silo? reload time is shorter for silos.</param>
        /// <param name="Reduction">Size reduction of the launcher, effects reload rate, cost, and crew as well.</param>
        public MissileLauncherDefTN(string title, float hs, int ReloadTech, bool ShipOrPDC, float Reduction)
        {
            Id = Guid.NewGuid();

            componentType = ComponentTypeTN.MissileLauncher;

            /// <summary>
            /// Basic Stats:
            /// </summary>
            Name = title;
            size = hs * Reduction;

            /// <summary>
            /// Boxlaunchers have no crew requirement, and reload rate should not affect their cost as it has no other effects.
            /// </summary>
            if (Reduction == 0.15)
            {
                crew = 0;
                cost = (decimal)(size * 4.0f);
            }
            else
            {
                crew = (byte)Math.Round(size * 3.0f);
                cost = (decimal)(size * 4.0f * (float)(ReloadTech - 1));
            }

            /// <summary>
            /// Missile Launcher Specific stats:
            /// </summary>
            LaunchMaxSize = hs;
            ReloadRateTech = ReloadTech;
            IsPDCSilo = ShipOrPDC;

            float ReloadFactor = ( 30.0f * hs ) / (float)ReloadTech;

            if (Reduction == 0.75)
            {
                ReloadFactor = ReloadFactor * 2.0f;
            }
            else if (Reduction == 0.5)
            {
                ReloadFactor = ReloadFactor * 5.0f;
            }
            else if (Reduction == 0.33)
            {
                ReloadFactor = ReloadFactor * 20.0f;
            }
            else if (Reduction == 0.25)
            {
                ReloadFactor = ReloadFactor * 100.0f;
            }

            if (isPDCSilo == true)
            {
                ReloadFactor = ReloadFactor / 2.0f;
            }

            RateOfFire = (int)ReloadFactor;

            if (Reduction == 0.15)
            {
                IsBoxLauncher = true;
                RateOfFire = -1;
                HangarReload = 450 * (int)hs;
                MFReload = HangarReload * 10;
                if (isPDCSilo == true)
                {
                    /// <summary>
                    /// PDCS don't actually have to load from maintenance facilities, but they use this time in any event.
                    MFReload = MFReload / 2;
                    RateOfFire = MFReload;
                }
            }
            else
            {
                HangarReload = -1;
                MFReload = -1;
            }


            isMilitary = true;
            isObsolete = false;
            isSalvaged = false;
            isElectronic = false;
            isDivisible = false;
        }
    }

    public class MissileLauncherTN : ComponentTN
    {

        /// <summary>
        /// Definition for this launcher.
        /// </summary>
        private MissileLauncherDefTN MissileLauncherDef;
        public MissileLauncherDefTN missileLauncherDef
        {
            get { return MissileLauncherDef; }
        }

        /// <summary>
        /// How long until this tube reloads?(from rate of fire, hangar, or MF as appropriate). 0 is ready to launch.
        /// </summary>
        private int LoadTime;
        public int loadTime
        {
            get { return LoadTime; }
        }

        /// <summary>
        /// Missile Fire Controller and loaded missile are needed here.
        /// </summary>
        



        /// <summary>
        /// Constructor for individual launchers.
        /// </summary>
        /// <param name="definition">Definition for this launcher</param>
        public MissileLauncherTN(MissileLauncherDefTN definition)
        {
            MissileLauncherDef = definition;

            LoadTime = 0;

            isDestroyed = false;
        }

        /// <summary>
        /// Is this launcher ready to fire its loaded missile?
        /// </summary>
        /// <returns>True = yes, false = no.</returns>
        public bool ReadyToFire()
        {
            if (LoadTime == 0)
                return true;
            else
                return false;
        }

        /// <summary>
        /// This function sets the load time on this launcher correctly, and clears the missile tube. missile creation happens elsewhere.
        /// </summary>
        public void Fire()
        {
            /// <summary>
            /// Clear the launch tube of the loaded missile.
            /// </summary>
            



            /// <summary>
            /// Set the time to load the next missile.
            /// </summary>
            if (MissileLauncherDef.isBoxLauncher == true && MissileLauncherDef.isPDCSilo == false)
            {
                /// <summary>
                /// MFReloadtime won't come into play unless the ship visits a planet.
                /// likewise hangar reload time won't come into play unless the ship lands in a hangar.
                /// </summary>
                LoadTime = MissileLauncherDef.hangarReload;
            }
            else
            {
                LoadTime = MissileLauncherDef.rateOfFire;
            }
        }
    }
}
