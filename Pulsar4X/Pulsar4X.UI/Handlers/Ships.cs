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

namespace Pulsar4X.UI.Handlers
{
    public class Ships
    {

        #region Properties

        Panels.Individual_Unit_Details_Panel m_oDetailsPanel;
        Panels.Ships_ShipList m_oShipListPanel;

        Panels.Ships_Design m_oDesignPanel;

        /// <summary>
        /// Ship Logger:
        /// </summary>
        public static readonly ILog logger = LogManager.GetLogger(typeof(Ships));

        /// <summary>
        /// Currently selected ship.
        /// </summary>
        private Pulsar4X.Entities.ShipTN _CurrnetShip;
        public Pulsar4X.Entities.ShipTN CurrentShip 
        {
            get { return _CurrnetShip; }
            set 
            {
                if (value != _CurrnetShip)
                {
                    _CurrnetShip = value;
                    RefreshShipInfo();
                }
            }
        }

        /// <summary>
        /// Currently selected faction.
        /// </summary>
        private Pulsar4X.Entities.Faction _CurrnetFaction;
        public Pulsar4X.Entities.Faction CurrentFaction
        {
            get { return _CurrnetFaction; }
            set
            {
                if (value != _CurrnetFaction)
                {
                    _CurrnetFaction = value;

                    if (_CurrnetFaction.Ships.Count != 0)
                        _CurrnetShip = _CurrnetFaction.Ships[0];
                    else
                        _CurrnetShip = null;
                    RefreshShipPanels();
                }
            }
        }

        /// <summary>
        /// Currently selected FC.
        /// </summary>
        private Pulsar4X.Entities.Components.ComponentTN _CurrnetFC;
        public Pulsar4X.Entities.Components.ComponentTN CurrentFC
        {
            get { return _CurrnetFC; }
            set
            {
                if (value != _CurrnetFC)
                {
                    _CurrnetFC = value;

                    RefreshFCInfo();
                }
            }
        }

        /// <summary>
        /// I need to know what type of BFC I have.
        /// </summary>
        public bool isBFC { get; set; }
        
        /// <summary>
        /// View Model used by Ships
        /// </summary>
        public ViewModels.ShipsViewModel VM { get; set; }

        #endregion


        public Ships()
        {
            isBFC = false;

            m_oDetailsPanel = new Panels.Individual_Unit_Details_Panel();
            m_oDesignPanel = new Panels.Ships_Design();
            m_oShipListPanel = new Panels.Ships_ShipList();

            VM = new ViewModels.ShipsViewModel();

            /// <summary>
            /// Set up faction binding.
            /// </summary>
            m_oShipListPanel.FactionSelectionComboBox.Bind(c => c.DataSource, VM, d => d.Factions);
            m_oShipListPanel.FactionSelectionComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentFaction, DataSourceUpdateMode.OnPropertyChanged);
            m_oShipListPanel.FactionSelectionComboBox.DisplayMember = "Name";
            VM.FactionChanged += (s, args) => CurrentFaction = VM.CurrentFaction;
            CurrentFaction = VM.CurrentFaction;
            m_oShipListPanel.FactionSelectionComboBox.SelectedIndexChanged += (s, args) => m_oShipListPanel.FactionSelectionComboBox.DataBindings["SelectedItem"].WriteValue();
            m_oShipListPanel.FactionSelectionComboBox.SelectedIndexChanged += new EventHandler(FactionSelectComboBox_SelectedIndexChanged);

            m_oDetailsPanel.SFCComboBox.SelectedIndexChanged += new EventHandler(SFCComboBox_SelectedIndexChanged);

            m_oShipListPanel.ShipsListBox.SelectedIndexChanged += new EventHandler(ShipListBox_SelectedIndexChanged);
        }


        #region PublicMethods

        public void ShowAllPanels(DockPanel a_oDockPanel)
        {
            ShowShipListPanel(a_oDockPanel);
            ShowDetailsPanel(a_oDockPanel);
            //ShowDesignPanel(a_oDockPanel);
        }

        public void ShowShipListPanel(DockPanel a_oDockPanel)
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oShipListPanel.Show(a_oDockPanel, DockState.DockLeft);
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false; 
        }

        public void ActivateShipListPanel()
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oShipListPanel.Activate();
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;

            RefreshShipPanels();
        }

        public void ShowDetailsPanel(DockPanel a_oDockPanel)
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oDetailsPanel.Show(a_oDockPanel, DockState.Document);
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false; 
        }

        public void ActivateDetailsPanel()
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oDetailsPanel.Activate();
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        public void ShowDesignPanel(DockPanel a_oDockPanel)
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oDesignPanel.Show(a_oDockPanel, DockState.Document);
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false; 
        }

        public void ActivateDesignPanel()
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oDesignPanel.Activate();
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        public void SMOn()
        {
            // todo
        }

        public void SMOff()
        {
            // todo
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Handle Faction Changes here.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FactionSelectComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshShipPanels();
        }

        /// <summary>
        /// Ship selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShipListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            //RefreshShipInfo();
            CurrentShip = CurrentFaction.Ships[m_oShipListPanel.ShipsListBox.SelectedIndex];

            /// <summary>
            /// This is a kludge, plain and simple, I was not able to successfully bind the SFCComboBox, so I am doing this.
            /// </summary>
            m_oDetailsPanel.SFCComboBox.Items.Clear();
            for (int loop = 0; loop < CurrentShip.ShipFireControls.Count; loop++)
            {
                m_oDetailsPanel.SFCComboBox.Items.Add(CurrentShip.ShipFireControls[loop].Name);
            }
        }

        /// <summary>
        /// Handle Fire control selection and its various kludges.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SFCComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (m_oDetailsPanel.SFCComboBox.SelectedIndex < CurrentShip.ShipBFC.Count)
            {
                CurrentFC = CurrentShip.ShipBFC[m_oDetailsPanel.SFCComboBox.SelectedIndex];
                isBFC = true;
            }
            else
            {
                int newIndex = m_oDetailsPanel.SFCComboBox.SelectedIndex - CurrentShip.ShipBFC.Count;
                CurrentFC = CurrentShip.ShipMFC[newIndex];
                isBFC = false;
            }

            RefreshFCInfo();
        }

        /// <summary>
        /// Add a column to the armor display.
        /// </summary>
        /// <param name="Header"></param>
        /// <param name="newPadding"></param>
        private void AddColumn(Padding newPadding)
        {
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                //col.HeaderText = Header;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                col.DefaultCellStyle.Padding = newPadding;
                col.Width = 10;
                if (col != null)
                {
                    m_oDetailsPanel.ArmorDisplayDataGrid.Columns.Add(col);
                }
            }
        }

        /// <summary>
        /// Adds a row to the armor display.
        /// </summary>
        /// <param name="row"></param>
        private void AddRow(int row)
        {
            using (DataGridViewRow newRow = new DataGridViewRow())
            {
                /// <summary>
                /// setup row height. note that by default they are 22 pixels in height!
                /// </summary>
                newRow.Height = 10;
                m_oDetailsPanel.ArmorDisplayDataGrid.Rows.Add(newRow);
            }
        }

        /// <summary>
        /// BuildArmor displays the armor status of the current ship, how big it is and what damage it has sustained.
        /// </summary>
        private void BuildArmor()
        {
            m_oDetailsPanel.ArmorDisplayDataGrid.Rows.Clear();
            m_oDetailsPanel.ArmorDisplayDataGrid.Columns.Clear();

            Padding newPadding = new Padding(2, 0, 2, 0);
            for (int loop = 0; loop < CurrentShip.ShipArmor.armorDef.cNum; loop++)
            {
                AddColumn(newPadding);
            }

            for (int loop = 0; loop < CurrentShip.ShipArmor.armorDef.depth; loop++)
            {
                AddRow(loop);
            }

            m_oDetailsPanel.ArmorDisplayDataGrid.ClearSelection();


            if (CurrentShip.ShipArmor.isDamaged == true)
            {
                foreach( KeyValuePair<ushort, ushort>  pair in CurrentShip.ShipArmor.armorDamage)
                {
                    for (int loop = 0; loop < pair.Value; loop++)
                    {
                        m_oDetailsPanel.ArmorDisplayDataGrid.Rows[loop].Cells[pair.Key].Style.BackColor = Color.Red;
                    }
                }
            }
        }

        /// <summary>
        /// Print the damage allocation chart to the appropriate place under the damage control tab.
        /// </summary>
        private void BuildDACInfo()
        {
            m_oDetailsPanel.DACListBox.Items.Clear();

            int DAC = 1;
            String Entry = "N/A";
            for (int loop = 0; loop < CurrentShip.ShipClass.ListOfComponentDefs.Count; loop++)
            {
                String DACString = DAC.ToString();
                if (DAC < 10)
                {
                    DACString = "00" + DAC.ToString();
                }
                else if(DAC < 100)
                {
                    DACString = "0" + DAC.ToString();
                }

                String DAC2 = CurrentShip.ShipClass.DamageAllocationChart[CurrentShip.ShipClass.ListOfComponentDefs[loop]].ToString();
                if (CurrentShip.ShipClass.DamageAllocationChart[CurrentShip.ShipClass.ListOfComponentDefs[loop]] < 10)
                {
                    DAC2 = "00" + CurrentShip.ShipClass.DamageAllocationChart[CurrentShip.ShipClass.ListOfComponentDefs[loop]].ToString();
                }
                else if (CurrentShip.ShipClass.DamageAllocationChart[CurrentShip.ShipClass.ListOfComponentDefs[loop]] < 100)
                {
                    DAC2 = "0" + CurrentShip.ShipClass.DamageAllocationChart[CurrentShip.ShipClass.ListOfComponentDefs[loop]].ToString();
                }



                Entry = DACString + "-" + DAC2 + " " + CurrentShip.ShipClass.ListOfComponentDefs[loop].Name + 
                    "(" + CurrentShip.ShipClass.ListOfComponentDefsCount[loop].ToString() + "/" + 
                    CurrentShip.ShipClass.ListOfComponentDefs[loop].htk.ToString() + ")";

                m_oDetailsPanel.DACListBox.Items.Add(Entry);

                DAC = CurrentShip.ShipClass.DamageAllocationChart[CurrentShip.ShipClass.ListOfComponentDefs[loop]] + 1;

                
            }

            m_oDetailsPanel.DACListBox.Items.Add("");
            m_oDetailsPanel.DACListBox.Items.Add("Electronic Only DAC");

            DAC = 1;

            foreach (KeyValuePair<ComponentDefTN, int> pair in CurrentShip.ShipClass.ElectronicDamageAllocationChart)
            {
                String DACString = DAC.ToString();
                if (DAC < 10)
                {
                    DACString = "00" + DAC.ToString();
                }
                else if (DAC < 100)
                {
                    DACString = "0" + DAC.ToString();
                }

                String DAC2 = pair.Value.ToString();
                if (pair.Value < 10)
                {
                    DAC2 = "00" + pair.Value.ToString();
                }
                else if (pair.Value < 100)
                {
                    DAC2 = "0" + pair.Value.ToString();
                }

                int index = CurrentShip.ShipClass.ListOfComponentDefs.IndexOf(pair.Key);

                Entry = DACString + "-" + DAC2 + " " + pair.Key.Name + "(" + CurrentShip.ShipClass.ListOfComponentDefsCount[index].ToString() + "/" +
                    pair.Key.htk.ToString() + ")";

                m_oDetailsPanel.DACListBox.Items.Add(Entry);

                DAC = CurrentShip.ShipClass.ElectronicDamageAllocationChart[pair.Key] + 1;
            }
        }

        /// <summary>
        /// print the names of all the destroyed components.
        /// </summary>
        private void BuildDamagedSystemsList()
        {
            m_oDetailsPanel.DamagedSystemsListBox.Items.Clear();

            for (int loop = 0; loop < CurrentShip.DestroyedComponents.Count; loop++)
            {
                m_oDetailsPanel.DamagedSystemsListBox.Items.Add(CurrentShip.ShipComponents[CurrentShip.DestroyedComponents[loop]].Name);
            }
        }

        /// <summary>
        /// Print the names of every weapon on this ship.
        /// </summary>
        private void BuildWeaponList()
        {
            m_oDetailsPanel.WeaponListBox.Items.Clear();

            if (isBFC == true)
            {
                for (int loop = 0; loop < CurrentShip.ShipBeam.Count; loop++)
                {
                    m_oDetailsPanel.WeaponListBox.Items.Add(CurrentShip.ShipBeam[loop].Name);
                }
            }
            else
            {
                for (int loop = 0; loop < CurrentShip.ShipMLaunchers.Count; loop++)
                {
                    m_oDetailsPanel.WeaponListBox.Items.Add(CurrentShip.ShipMLaunchers[loop].Name);
                }
            }
        }

        /// <summary>
        /// Build the pd modes.
        /// </summary>
        private void BuildPDComboBox()
        {
            m_oDetailsPanel.PDComboBox.Items.Clear();

            m_oDetailsPanel.PDComboBox.Items.Add(PointDefenseState.None);

            if (isBFC == true)
            {
                m_oDetailsPanel.PDComboBox.Items.Add(PointDefenseState.AreaDefense);
                m_oDetailsPanel.PDComboBox.Items.Add(PointDefenseState.FinalDefensiveFire);
                m_oDetailsPanel.PDComboBox.Items.Add(PointDefenseState.FinalDefensiveFireSelf);
            }
            else
            {
                m_oDetailsPanel.PDComboBox.Items.Add(PointDefenseState.AMM1v2);
                m_oDetailsPanel.PDComboBox.Items.Add(PointDefenseState.AMM1v1);
                m_oDetailsPanel.PDComboBox.Items.Add(PointDefenseState.AMM2v1);
                m_oDetailsPanel.PDComboBox.Items.Add(PointDefenseState.AMM3v1);
                m_oDetailsPanel.PDComboBox.Items.Add(PointDefenseState.AMM4v1);
                m_oDetailsPanel.PDComboBox.Items.Add(PointDefenseState.AMM5v1);
            }
        }


        /// <summary>
        /// Add all available system contacts to the contact list.
        /// </summary>
        private void BuildContactsList()
        {
            m_oDetailsPanel.ContactListBox.Items.Clear();

            /// <summary>
            /// Planetary enemy populations should always be displayed.
            /// </summary>

            if (isBFC == true)
            {
                /// <summary>
                /// BFC range is so short that we'll just print all contacts and let the user sort em out.
                /// </summary>
                foreach (KeyValuePair<ShipTN, FactionContact> pair in CurrentFaction.DetectedContacts)
                {
                    if (pair.Key.ShipsTaskGroup.Contact.CurrentSystem == CurrentShip.ShipsTaskGroup.Contact.CurrentSystem)
                    {
                        m_oDetailsPanel.ContactListBox.Items.Add(pair.Key);
                    }
                }
            }
            else
            {

            }
        }


        /// <summary>
        /// Updates the display for all relevant ship panels.
        /// </summary>
        private void RefreshShipPanels()
        {
            m_oShipListPanel.ShipsListBox.Items.Clear();

            for (int loop = 0; loop < CurrentFaction.Ships.Count; loop++)
            {
                m_oShipListPanel.ShipsListBox.Items.Add(CurrentFaction.Ships[loop]);
            }

            RefreshShipInfo();
            RefreshFCInfo();
        }

        /// <summary>
        /// Build info about the ship.
        /// </summary>
        private void RefreshShipInfo()
        {
            if (CurrentShip != null)
            {
                /// <summary>
                /// Armor tab:
                /// </summary>
                BuildArmor();

                /// <summary>
                /// Damage Control Tab:
                /// </summary>
                BuildDACInfo();
                BuildDamagedSystemsList();
            }
        }

        /// <summary>
        /// Build info about the fire control.
        /// </summary>
        private void RefreshFCInfo()
        {
            if (CurrentFC != null)
            {
                /// <summary>
                /// Combat Settings Tab:
                /// </summary>
                
                BuildWeaponList();
                BuildPDComboBox();
            }
        }
        #endregion
    }
}
