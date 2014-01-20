using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using Pulsar4X.UI.ViewModels;
using Pulsar4X.Entities;
using Pulsar4X.Stargen;
using Newtonsoft.Json;
using log4net.Config;
using log4net;
using Pulsar4X.Entities.Components;
using System.Runtime.InteropServices;

namespace Pulsar4X.UI.Handlers
{
    public class MissileDesignHandler
    {
        public static readonly ILog logger = LogManager.GetLogger(typeof(MissileDesignHandler));

        /// <summary>
        /// Display panel for missile design.
        /// </summary>
        Panels.MissileDesign m_oMissileDesignPanel { get; set; }

        /// <summary>
        /// View model for MDH
        /// </summary>
        public MissileDesignViewModel VM { get; set; }

        /// <summary>
        /// Currently selected faction.
        /// </summary>
        private Faction _CurrnetFaction;
        public Faction CurrentFaction
        {
            get { return _CurrnetFaction; }
            set
            {
                _CurrnetFaction = value;

                if (_CurrnetFaction == null)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Currently selected missile engine
        /// </summary>
        private MissileEngineDefTN _CurrnetMissileEngine;
        public MissileEngineDefTN CurrentMissileEngine
        {
            get { return _CurrnetMissileEngine; }
            set
            {
                _CurrnetMissileEngine = value;
                if (_CurrnetMissileEngine == null)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Constructor for missile design handler.
        /// </summary>
        public MissileDesignHandler()
        {
            m_oMissileDesignPanel = new Panels.MissileDesign();

            VM = new MissileDesignViewModel();

            /// <summary>
            /// Bind factions to the empire selection combo box.
            /// </summary>
            m_oMissileDesignPanel.EmpireComboBox.Bind(c => c.DataSource, VM, d => d.Factions);
            m_oMissileDesignPanel.EmpireComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentFaction, DataSourceUpdateMode.OnPropertyChanged);
            m_oMissileDesignPanel.EmpireComboBox.DisplayMember = "Name";
            VM.FactionChanged += (s, args) => _CurrnetFaction = VM.CurrentFaction;
            _CurrnetFaction = VM.CurrentFaction;
            m_oMissileDesignPanel.EmpireComboBox.SelectedIndexChanged += (s, args) => m_oMissileDesignPanel.EmpireComboBox.DataBindings["SelectedItem"].WriteValue();
            m_oMissileDesignPanel.EmpireComboBox.SelectedIndexChanged += new EventHandler(EmpireComboBox_SelectedIndexChanged);

            /// <summary>
            /// Binding missile engines to the appropriate combo box.
            m_oMissileDesignPanel.MissileEngineComboBox.Bind(c => c.DataSource, VM, d => d.MissileEngines);
            m_oMissileDesignPanel.MissileEngineComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentMissileEngine, DataSourceUpdateMode.OnPropertyChanged);
            m_oMissileDesignPanel.MissileEngineComboBox.DisplayMember = "Name";
            VM.MissileEngineChanged += (s, args) => _CurrnetMissileEngine = VM.CurrentMissileEngine;
            _CurrnetMissileEngine = VM.CurrentMissileEngine;
            m_oMissileDesignPanel.MissileEngineComboBox.SelectedIndexChanged += (s, args) => m_oMissileDesignPanel.MissileEngineComboBox.DataBindings["SelectedItem"].WriteValue();
            m_oMissileDesignPanel.MissileEngineComboBox.SelectedIndexChanged += new EventHandler(MissileEngineComboBox_SelectedIndexChanged);

            m_oMissileDesignPanel.CloseButton.Click += new EventHandler(CloseButton_Click);
        }


        #region Public methods
        /// <summary>
        /// Opens as a popup the missile design page
        /// </summary>
        public void Popup()
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oMissileDesignPanel.ShowDialog();
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }
        #endregion


        #region Private methods
        /// <summary>
        /// When a new empire/faction is selected this will be run
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EmpireComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// When a new missile engine is selected this will be run
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MissileEngineComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Closes the dialogbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseButton_Click(object sender, EventArgs e)
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oMissileDesignPanel.Hide();
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }
        #endregion
    }
}
