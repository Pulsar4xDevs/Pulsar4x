using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel;
using WeifenLuo.WinFormsUI.Docking;
using Pulsar4X.UI.ViewModels;
using Pulsar4X.Entities;

#if LOG4NET_ENABLED
using log4net.Config;
using log4net;
#endif

using Newtonsoft.Json;
using Pulsar4X.Entities.Components;

namespace Pulsar4X.UI.Handlers
{
    public class FastOOB
    {

#if LOG4NET_ENABLED
        public static readonly ILog logger = LogManager.GetLogger(typeof(FastOOB));
#endif

        /// <summary>
        /// Currently selected empire.
        /// </summary>
        private Faction m_oCurrnetFaction;
        public Faction CurrentFaction
        {
            get { return m_oCurrnetFaction; }
            set
            {

                if (m_oCurrnetFaction != value)
                {
                    m_oCurrnetFaction = value;

                    if (m_oCurrnetFaction.TaskGroups.Count != 0)
                    {
                        m_oCurrnetTaskGroup = m_oCurrnetFaction.TaskGroups[0];
                    }
                    else
                        m_oCurrnetShipClass = null;

                    if (m_oCurrnetFaction.ShipDesigns.Count != 0)
                    {
                        m_oCurrnetShipClass = m_oCurrnetFaction.ShipDesigns[0];
                    }
                    else
                        m_oCurrnetShipClass = null;

                    UpdateDisplay();
                }
            }
        }

        /// <summary>
        /// Taskgroup I'll want to add a ship to.
        /// </summary>
        private TaskGroupTN m_oCurrnetTaskGroup;
        public TaskGroupTN CurrentTaskGroup
        {
            get { return m_oCurrnetTaskGroup; }
            set
            {
                if (m_oCurrnetTaskGroup != value)
                {
                    m_oCurrnetTaskGroup = value;

                    UpdateDisplay();
                }
            }
        }


        /// <summary>
        /// Ship class to add to current taskgroup
        /// </summary>
        private ShipClassTN m_oCurrnetShipClass;
        public ShipClassTN CurrentShipClass
        {
            get { return m_oCurrnetShipClass; }
            set
            {
                if (m_oCurrnetShipClass != value)
                {
                    m_oCurrnetShipClass = value;

                    UpdateDisplay();
                }
            }
        }

        public ViewModels.FastOOBViewModel VM;

        Panels.FastOOB_Panel m_oFastOOBPanel;

        public FastOOB()
        {
            m_oFastOOBPanel = new Panels.FastOOB_Panel();


            VM = new ViewModels.FastOOBViewModel();

            /// <summary>
            /// setup bindings:
            /// </summary>
            m_oFastOOBPanel.EmpireComboBox.Bind(c => c.DataSource, VM, d => d.Factions);
            m_oFastOOBPanel.EmpireComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentFaction, DataSourceUpdateMode.OnPropertyChanged);
            m_oFastOOBPanel.EmpireComboBox.DisplayMember = "Name";
            VM.FactionChanged += (s, args) => CurrentFaction = VM.CurrentFaction;
            CurrentFaction = VM.CurrentFaction;

            m_oFastOOBPanel.EmpireComboBox.SelectedIndexChanged += (s, args) => m_oFastOOBPanel.EmpireComboBox.DataBindings["SelectedItem"].WriteValue();
            m_oFastOOBPanel.EmpireComboBox.SelectedIndexChanged += new EventHandler(EmpireComboBox_SelectedIndexChanged);


            m_oFastOOBPanel.TaskGroupComboBox.Bind(c => c.DataSource, VM, d => d.TaskGroups);
            m_oFastOOBPanel.TaskGroupComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentTaskGroup, DataSourceUpdateMode.OnPropertyChanged);
            m_oFastOOBPanel.TaskGroupComboBox.DisplayMember = "Name";
            VM.TaskGroupChanged += (s, args) => CurrentTaskGroup = VM.CurrentTaskGroup;
            CurrentTaskGroup = VM.CurrentTaskGroup;

            m_oFastOOBPanel.TaskGroupComboBox.SelectedIndexChanged += (s, args) => m_oFastOOBPanel.TaskGroupComboBox.DataBindings["SelectedItem"].WriteValue();
            m_oFastOOBPanel.TaskGroupComboBox.SelectedIndexChanged += new EventHandler(TaskGroupComboBox_SelectedIndexChanged);

            m_oFastOOBPanel.ClassComboBox.Bind(c => c.DataSource, VM, d => d.ShipDesigns);
            m_oFastOOBPanel.ClassComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentShipClass, DataSourceUpdateMode.OnPropertyChanged);
            m_oFastOOBPanel.ClassComboBox.DisplayMember = "Name";
            VM.ShipClassChanged += (s, args) => CurrentShipClass = VM.CurrentShipClass;
            CurrentShipClass = VM.CurrentShipClass;

            m_oFastOOBPanel.ClassComboBox.SelectedIndexChanged += (s, args) => m_oFastOOBPanel.ClassComboBox.DataBindings["SelectedItem"].WriteValue();
            m_oFastOOBPanel.ClassComboBox.SelectedIndexChanged += new EventHandler(ClassComboBox_SelectedIndexChanged);

            m_oFastOOBPanel.AddButton.Click += new EventHandler(AddButton_Click);
            m_oFastOOBPanel.CloseButton.Click += new EventHandler(CloseButton_Click);

            m_oFastOOBPanel.NumberTextBox.TextChanged += new EventHandler(NumberTextBox_TextChanged);

        }

        /// <summary>
        /// On different faction selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EmpireComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// On Different taskgroup selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TaskGroupComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// On different class design selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClassComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Add the currently selected shipclass to the currently selected taskgroup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddButton_Click(object sender, EventArgs e)
        {
            if (m_oCurrnetTaskGroup != null && m_oCurrnetShipClass != null)
            {

                int Number = 0;

                bool res = Int32.TryParse(m_oFastOOBPanel.NumberTextBox.Text, out Number);

                if (res == true && Number > 0)
                {
                    /// <summary>
                    /// Ship designs have to be locked if they are not.
                    /// </summary>
                    if (m_oCurrnetShipClass.IsLocked == false)
                        m_oCurrnetShipClass.IsLocked = true;

                    for (int loop = 0; loop < Number; loop++)
                    {

                        /// <summary>
                        /// Make the ship and subtract the appropriate amount from the faction Ship PB total.
                        /// </summary>
                        m_oCurrnetTaskGroup.AddShip(m_oCurrnetShipClass);
                        m_oCurrnetFaction.ShipBPTotal = m_oCurrnetFaction.ShipBPTotal - m_oCurrnetShipClass.BuildPointCost;

                        /// <summary>
                        /// Name the ship properly
                        /// </summary>
                        m_oCurrnetTaskGroup.Ships[m_oCurrnetTaskGroup.Ships.Count - 1].Name = m_oCurrnetShipClass.Name + " " + (m_oCurrnetShipClass.ShipsInClass.Count).ToString();

                        /// <summary>
                        /// Ship specific information that SMed ships will want to have, fuel, ordnance,etc.
                        /// </summary>
                        m_oCurrnetTaskGroup.Ships[m_oCurrnetTaskGroup.Ships.Count - 1].Refuel(1000000.0f);
                        if (m_oCurrnetShipClass.ShipClassOrdnance.Count != 0)
                        {
                            foreach (KeyValuePair<OrdnanceDefTN, int> pair in m_oCurrnetShipClass.ShipClassOrdnance)
                            {
                                m_oCurrnetTaskGroup.Ships[m_oCurrnetTaskGroup.Ships.Count - 1].ShipOrdnance.Add(pair.Key, pair.Value);
                            }

                            m_oCurrnetTaskGroup.Ships[m_oCurrnetTaskGroup.Ships.Count - 1].CurrentMagazineCapacity = m_oCurrnetShipClass.PreferredOrdnanceSize;
                        }
                    }

                    UpdateDisplay();
                }
            }
        }

        /// <summary>
        /// close the dialog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseButton_Click(object sender, EventArgs e)
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oFastOOBPanel.Hide();
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }


        /// <summary>
        /// Check to see what number of ships the user entered.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NumberTextBox_TextChanged(object sender, EventArgs e)
        {
            int Number;

            Int32.TryParse(m_oFastOOBPanel.NumberTextBox.Text, out Number);

            UpdateDisplay();
        }

        /// <summary>
        /// Opens as a popup the RP creation page.
        /// </summary>
        public void Popup()
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oFastOOBPanel.ShowDialog();
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        public void UpdateDisplay()
        {
            if (m_oCurrnetShipClass != null)
            {
                m_oFastOOBPanel.SummaryTextBox.Text = m_oCurrnetShipClass.Summary;

                int Number = 0;

                Int32.TryParse(m_oFastOOBPanel.NumberTextBox.Text, out Number);

                m_oFastOOBPanel.CostTextBox.Text = (m_oCurrnetShipClass.BuildPointCost * Number).ToString();

                m_oFastOOBPanel.ShipBPTextBox.Text = m_oCurrnetFaction.ShipBPTotal.ToString();
                m_oFastOOBPanel.PDCBPTextBox.Text = m_oCurrnetFaction.PDCBPTotal.ToString();
            }
        }
    }
}
