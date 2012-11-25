using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.UI.Helpers
{
    public class UIController
    {
        /// <summary>
        /// The Instance of this class/singelton
        /// </summary>
        private static readonly UIController m_oInstance = new UIController();

        /// <summary>
        /// Returns the instance of the OpenTKUtilities class.
        /// </summary>
        public static UIController Instance
        {
            get
            {
                return m_oInstance;
            }
        }

        /// <summary>
        /// True if running on the mono framework instead of .Net
        /// </summary>
        public bool IsRunningOnMono { get; set; }


        Handlers.SystemGenAndDisplay m_oSystemGenAndDisplay;

        /// <summary>
        /// Handler for all the System Information and Generation Panels, can be used to control them all or just individual ones.
        /// </summary>
        public Handlers.SystemGenAndDisplay SystemGenAndDisplay
        {
            get
            {
                return m_oSystemGenAndDisplay;
            }
        }

        Handlers.SystemMap m_oSystemMap;

        /// <summary>
        /// Handler for all the System Map Panels.
        /// </summary>
        public Handlers.SystemMap SystemMap
        {
            get
            {
                return m_oSystemMap;
            }
        }

        /// <summary>
        /// Constructor that prevents a default instance of this class from being created.
        /// </summary>
        private UIController() { }

        /// <summary>
        /// Initialises this singelton.
        /// </summary>
        public void Initialise()
        {
            // Test to see if we are running on mono:
            Type t = Type.GetType("Mono.Runtime");
            if (t != null)
            {
                IsRunningOnMono = true;
            }
            else
            {
                IsRunningOnMono = false;
            }

            // now init ui handlers
            m_oSystemGenAndDisplay = new Handlers.SystemGenAndDisplay();
            m_oSystemMap = new Handlers.SystemMap();
        }

        #region PublicMethods

        /// <summary>
        /// Enables SM mode.
        /// </summary>
        public void SMOn()
        {
            m_oSystemGenAndDisplay.SMOn();
        }

        /// <summary>
        /// Disables SM mode
        /// </summary>
        public void SMOff()
        {
            m_oSystemGenAndDisplay.SMOff();
        }

        #endregion
    }
}
