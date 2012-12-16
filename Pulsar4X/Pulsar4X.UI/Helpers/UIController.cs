using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WeifenLuo.WinFormsUI.Docking;

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

        Handlers.Economics m_oEconomics;

        /// <summary>
        /// Handler for all the Economic Screens.
        /// </summary>
        public Handlers.Economics Economics
        {
            get
            {
                return m_oEconomics;
            }
        }

        Handlers.Ships m_oShips;

        /// <summary>
        /// Handler for all the Ship detials screens.
        /// </summary>
        public Handlers.Ships Ships
        {
            get
            {
                return m_oShips;
            }
        }

        Handlers.ClassDesign m_oClassDesign;

        /// <summary>
        /// Handler for all Class Design Screens.
        /// </summary>
        public Handlers.ClassDesign ClassDesign
        {
            get
            {
                return m_oClassDesign;
            }
        }

        public bool SuspendAutoPanelDisplay { get; set; }

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

            SuspendAutoPanelDisplay = false;

            // now init ui handlers
            m_oSystemGenAndDisplay = new Handlers.SystemGenAndDisplay();
            m_oSystemMap = new Handlers.SystemMap();
            m_oEconomics = new Handlers.Economics();
            m_oShips = new Handlers.Ships();
            m_oClassDesign = new Handlers.ClassDesign();
        }

        #region PublicMethods

        /// <summary>
        /// Enables SM mode.
        /// </summary>
        public void SMOn()
        {
            m_oSystemGenAndDisplay.SMOn();
            m_oShips.SMOn();
            m_oClassDesign.SMOn();
        }

        /// <summary>
        /// Disables SM mode
        /// </summary>
        public void SMOff()
        {
            m_oSystemGenAndDisplay.SMOff();
            m_oShips.SMOff();
            m_oClassDesign.SMOff();
        }

        /// <summary>
        /// This Function will update the active/shown Panels on a DockPanel based on the current active panel.
        /// </summary>
        /// <param name="a_oDockPanel">The Dockpanel</param>
        public void DockPanelActiveDocumentChanged(DockPanel a_oDockPanel)
        {
            if (SuspendAutoPanelDisplay)
            {
                return; // do nothing because we dont want to :)
            }

            if (a_oDockPanel == null || m_oSystemGenAndDisplay == null || m_oSystemMap == null
                || m_oEconomics == null || m_oShips == null || m_oClassDesign == null)
            {
                return; // do nothing if we dont have the full UI!!
            }

            if (a_oDockPanel.ActiveDocument == null)
            {
                return; // another sainty check.
            }

            if (a_oDockPanel.ActiveDocument.GetType() == typeof(Panels.SGaD_DataPanel))
            {
                m_oSystemGenAndDisplay.ActivateControlsPanel();
            }
            else if (a_oDockPanel.ActiveDocument.GetType() == typeof(Panels.SysMap_ViewPort))
            {
                m_oSystemMap.ActivateControlsPanel();
            }
            else if (a_oDockPanel.ActiveDocument.GetType() == typeof(Panels.Eco_Summary))
            {
                m_oEconomics.ActivatePopulationsPanel();
            }
            else if (a_oDockPanel.ActiveDocument.GetType() == typeof(Panels.Ships_Design))
            {
                m_oShips.ActivateShipListPanel();
            }
            else if (a_oDockPanel.ActiveDocument.GetType() == typeof(Panels.ClassDes_DesignAndInfo))
            {
                m_oClassDesign.ActivateOptionsPanel();
                m_oClassDesign.ActivatePropertiesPanel();
            }
        }

        #endregion
    }
}
