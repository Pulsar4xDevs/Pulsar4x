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

                    if (_CurrnetShip.ShipASensor.Count != 0)
                        _CurrnetSensor = _CurrnetShip.ShipASensor[0];
                    else
                        _CurrnetSensor = null;

                    if (_CurrnetShip.ShipFireControls.Count != 0)
                        _CurrnetFC = _CurrnetShip.ShipFireControls[0];
                    else
                        _CurrnetFC = null;

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
                    {
                        _CurrnetShip = _CurrnetFaction.Ships[0];
                        if (_CurrnetShip.ShipASensor.Count != 0)
                            _CurrnetSensor = _CurrnetShip.ShipASensor[0];
                        else
                            _CurrnetSensor = null;


                        if (_CurrnetShip.ShipFireControls.Count != 0)
                        {
                            _CurrnetFC = _CurrnetShip.ShipFireControls[0];
                        }
                        else
                            _CurrnetFC = null;
                    }
                    else
                    {
                        _CurrnetShip = null;
                        _CurrnetSensor = null;
                        _CurrnetFC = null;
                    }
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
        /// Currently selected sensor.
        /// </summary>
        private Pulsar4X.Entities.Components.ActiveSensorTN _CurrnetSensor;
        public Pulsar4X.Entities.Components.ActiveSensorTN CurrentSensor
        {
            get { return _CurrnetSensor; }
            set
            {
                if (value != _CurrnetSensor)
                {
                    _CurrnetSensor = value;
                }
            }
        }

        /// <summary>
        /// I need to know what type of FC I have.
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
            VM.FactionChanged += (s, args) => _CurrnetFaction = VM.CurrentFaction;
            _CurrnetFaction = VM.CurrentFaction;
            m_oShipListPanel.FactionSelectionComboBox.SelectedIndexChanged += (s, args) => m_oShipListPanel.FactionSelectionComboBox.DataBindings["SelectedItem"].WriteValue();
            m_oShipListPanel.FactionSelectionComboBox.SelectedIndexChanged += new EventHandler(FactionSelectComboBox_SelectedIndexChanged);

            m_oDetailsPanel.SFCComboBox.SelectedIndexChanged += new EventHandler(SFCComboBox_SelectedIndexChanged);
            m_oDetailsPanel.SelectedActiveComboBox.SelectedIndexChanged += new EventHandler(SelectedActiveComboBox_SelectIndexChanged);

            m_oShipListPanel.ShipsListBox.SelectedIndexChanged += new EventHandler(ShipListBox_SelectedIndexChanged);

            m_oDetailsPanel.OpenFireButton.Click += new EventHandler(OpenFireButton_Click);
            m_oDetailsPanel.CeaseFireButton.Click += new EventHandler(CeaseFireButton_Click);
            m_oDetailsPanel.RaiseShieldsButton.Click += new EventHandler(RaiseShieldsButton_Click);
            m_oDetailsPanel.LowerShieldsButton.Click += new EventHandler(LowerShieldsButton_Click);
            m_oDetailsPanel.ActiveButton.Click += new EventHandler(ActiveButton_Click);
            m_oDetailsPanel.InactiveButton.Click += new EventHandler(InactiveButton_Click);
            m_oDetailsPanel.AssignTargetButton.Click += new EventHandler(AssignTargetButton_Click);
            m_oDetailsPanel.ClearTargetButton.Click += new EventHandler(ClearTargetButton_Click);
            m_oDetailsPanel.AssignWeaponButton.Click += new EventHandler(AssignWeaponButton_Click);
            m_oDetailsPanel.AssignAllWeaponsButton.Click += new EventHandler(AssignAllWeaponsButton_Click);
            m_oDetailsPanel.ClearWeaponsButton.Click += new EventHandler(ClearWeaponsButton_Click);
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
            /// <summary>
            /// This is a part of the kludge below for the SFC and SA combo boxes.
            /// Order of operations matters, set the index to -1 to delete BEFORE clearing everything.
            /// </summary>
            

            m_oDetailsPanel.SelectedActiveComboBox.SelectedIndex = -1;
            m_oDetailsPanel.SelectedActiveComboBox.Items.Clear();

            m_oDetailsPanel.SFCComboBox.SelectedIndex = -1;
            m_oDetailsPanel.SFCComboBox.Items.Clear();

            RefreshShipPanels();
        }

        /// <summary>
        /// Ship selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShipListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (m_oShipListPanel.ShipsListBox.SelectedIndex != -1)
            {
                _CurrnetShip = _CurrnetFaction.Ships[m_oShipListPanel.ShipsListBox.SelectedIndex];

                /// <summary>
                /// This is a kludge, plain and simple, I was not able to successfully bind the SFCComboBox, so I am doing this.
                /// </summary>
                m_oDetailsPanel.SFCComboBox.Items.Clear();
                for (int loop = 0; loop < _CurrnetShip.ShipFireControls.Count; loop++)
                {
                    m_oDetailsPanel.SFCComboBox.Items.Add(_CurrnetShip.ShipFireControls[loop].Name);
                }

                if (m_oDetailsPanel.SFCComboBox.Items.Count != 0)
                    m_oDetailsPanel.SFCComboBox.SelectedIndex = 0;

                /// <summary>
                /// Same will probably be true for sensors.
                /// </summary>
                m_oDetailsPanel.SelectedActiveComboBox.Items.Clear();
                for (int loop = 0; loop < _CurrnetShip.ShipASensor.Count; loop++)
                {
                    m_oDetailsPanel.SelectedActiveComboBox.Items.Add(_CurrnetShip.ShipASensor[loop].Name);
                }

                if (m_oDetailsPanel.SelectedActiveComboBox.Items.Count != 0)
                    m_oDetailsPanel.SelectedActiveComboBox.SelectedIndex = 0;


                if (_CurrnetShip.ShieldIsActive == true && _CurrnetShip.CurrentShieldPoolMax != 0.0f)
                {
                    m_oDetailsPanel.ShieldGroupBox.Text = "Shields(On)";
                }
                else
                {
                    m_oDetailsPanel.ShieldGroupBox.Text = "Shields(Off)";
                }
            }

            RefreshShipInfo();
        }

        /// <summary>
        /// Handle Fire control selection and its various kludges.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SFCComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_CurrnetShip != null && m_oDetailsPanel.SFCComboBox.SelectedIndex != -1)
            {
                if (m_oDetailsPanel.SFCComboBox.SelectedIndex < _CurrnetShip.ShipBFC.Count)
                {
                    _CurrnetFC = _CurrnetShip.ShipBFC[m_oDetailsPanel.SFCComboBox.SelectedIndex];
                    isBFC = true;
                }
                else
                {
                    int newIndex = m_oDetailsPanel.SFCComboBox.SelectedIndex - _CurrnetShip.ShipBFC.Count;
                    _CurrnetFC = _CurrnetShip.ShipMFC[newIndex];
                    isBFC = false;
                }
            }

            RefreshFCInfo();
        }

        private void SelectedActiveComboBox_SelectIndexChanged(object sender, EventArgs e)
        {
            if (_CurrnetShip != null && m_oDetailsPanel.SelectedActiveComboBox.SelectedIndex != -1)
            {
                _CurrnetSensor = _CurrnetShip.ShipASensor[m_oDetailsPanel.SelectedActiveComboBox.SelectedIndex];

                if (_CurrnetSensor.isActive == true && _CurrnetSensor.isDestroyed == false)
                    m_oDetailsPanel.ActiveGroupBox.Text = "Selected Active(On)";
                else
                    m_oDetailsPanel.ActiveGroupBox.Text = "Selected Active(Off)";
            }
        }

        /// <summary>
        /// Handle open fire button clicked. Faction stores a list of FCs with fire authorization, add this FC to that list if it isn't there already.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFireButton_Click(object sender, EventArgs e)
        {
            if (_CurrnetFC != null)
            {
                if (_CurrnetFaction.OpenFireFC.ContainsKey(_CurrnetFC) == false)
                {
                    _CurrnetFaction.OpenFireFC.Add(_CurrnetFC, _CurrnetShip);
                    _CurrnetFaction.OpenFireFCType.Add(_CurrnetFC, isBFC);
                }

                if (isBFC == true)
                {
                    _CurrnetShip.ShipBFC[_CurrnetFC.componentIndex].openFire = true;
                }
                else
                {
                    _CurrnetShip.ShipMFC[_CurrnetFC.componentIndex].openFire = true;
                }
                BuildCombatSummary();
            }
        }

        /// <summary>
        /// Handle cease fire button clicked. Faction stores a list of FCs with fire authorization, remove this FC from that list if it is there.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CeaseFireButton_Click(object sender, EventArgs e)
        {
            if (_CurrnetFC != null)
            {
                if (_CurrnetFaction.OpenFireFC.ContainsKey(_CurrnetFC) == true)
                {
                    _CurrnetFaction.OpenFireFC.Remove(_CurrnetFC);
                    _CurrnetFaction.OpenFireFCType.Remove(_CurrnetFC);
                }

                if (isBFC == true)
                {
                    _CurrnetShip.ShipBFC[_CurrnetFC.componentIndex].openFire = false;
                }
                else
                {
                    _CurrnetShip.ShipMFC[_CurrnetFC.componentIndex].openFire = false;
                }
                BuildCombatSummary();
            }
        }

        /// <summary>
        /// Handle Raise Shields button clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RaiseShieldsButton_Click(object sender, EventArgs e)
        {
            if (_CurrnetShip != null)
            {
                _CurrnetShip.SetShields(true);

                if (_CurrnetFaction.RechargeList.ContainsKey(_CurrnetShip) == false)
                {
                    _CurrnetFaction.RechargeList.Add(_CurrnetShip, (int)Faction.RechargeStatus.Shields);
                }

                if (_CurrnetShip.ShieldIsActive == true && _CurrnetShip.CurrentShieldPoolMax != 0.0f)
                {
                    m_oDetailsPanel.ShieldGroupBox.Text = "Shields(On)";
                }
            }
        }

        /// <summary>
        /// Handle Lower Shields button clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LowerShieldsButton_Click(object sender, EventArgs e)
        {
            if (_CurrnetShip != null)
            {
                _CurrnetShip.SetShields(false);

                if (_CurrnetFaction.RechargeList.ContainsKey(_CurrnetShip) == true)
                {
                    int value = _CurrnetFaction.RechargeList[_CurrnetShip];

                    /// <summary>
                    /// Value here is a bitwise status word.
                    /// </summary>
                    if ((value & (int)Faction.RechargeStatus.Shields) == 1)
                    {
                        value = value - (int)Faction.RechargeStatus.Shields;

                        if (value == 0)
                        {
                            _CurrnetFaction.RechargeList.Remove(_CurrnetShip);
                        }
                        else
                        {
                            _CurrnetFaction.RechargeList[_CurrnetShip] = value;
                        }
                    }
                }

                m_oDetailsPanel.ShieldGroupBox.Text = "Shields(Off)";
            }
        }

        /// <summary>
        /// Activate the currently selected sensor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ActiveButton_Click(object sender, EventArgs e)
        {
            if (_CurrnetShip != null && _CurrnetSensor != null)
            {
                _CurrnetShip.ShipsTaskGroup.SetActiveSensor(_CurrnetShip.ShipsTaskGroup.Ships.IndexOf(_CurrnetShip), _CurrnetSensor.componentIndex, true);

                if (_CurrnetSensor.isActive == true && _CurrnetSensor.isDestroyed == false)
                    m_oDetailsPanel.ActiveGroupBox.Text = "Selected Active(On)";
            }
        }

        /// <summary>
        /// Deactivate the currently selected sensor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InactiveButton_Click(object sender, EventArgs e)
        {
            _CurrnetShip.ShipsTaskGroup.SetActiveSensor(_CurrnetShip.ShipsTaskGroup.Ships.IndexOf(_CurrnetShip), _CurrnetSensor.componentIndex, false);

            if (_CurrnetSensor.isActive == false || _CurrnetSensor.isDestroyed == true)
                m_oDetailsPanel.ActiveGroupBox.Text = "Selected Active(Off)";
        }

        /// <summary>
        /// Assign the selected target to the FC.
        /// I have to rebuild the contact list to find out which contact goes with what entry.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AssignTargetButton_Click(object sender, EventArgs e)
        {
            if (m_oDetailsPanel.ContactListBox.SelectedIndex != -1 && _CurrnetFC != null)
            {
                if (isBFC == true)
                {
                    int count = 0;
                    foreach (KeyValuePair<ShipTN, FactionContact> pair in _CurrnetFaction.DetectedContacts)
                    {
                        if (pair.Key.ShipsTaskGroup.Contact.CurrentSystem == _CurrnetShip.ShipsTaskGroup.Contact.CurrentSystem)
                        {
                            if (count == m_oDetailsPanel.ContactListBox.SelectedIndex)
                            {
                                _CurrnetShip.ShipBFC[_CurrnetFC.componentIndex].assignTarget(pair.Key);
                                pair.Key.ShipsTargetting.Add(_CurrnetShip);
                                break;
                            }

                            count++;
                        }
                    }



                }
                else
                {
                    int count = 0;
                    foreach (KeyValuePair<ShipTN, FactionContact> pair in _CurrnetFaction.DetectedContacts)
                    {
                        if (pair.Key.ShipsTaskGroup.Contact.CurrentSystem == _CurrnetShip.ShipsTaskGroup.Contact.CurrentSystem)
                        {
                            StarSystem CurSystem = _CurrnetShip.ShipsTaskGroup.Contact.CurrentSystem;
                            int MyID = CurSystem.SystemContactList.IndexOf(_CurrnetShip.ShipsTaskGroup.Contact);
                            int TargetID = CurSystem.SystemContactList.IndexOf(pair.Key.ShipsTaskGroup.Contact);

                            /// <summary>
                            /// Validate tick here?
                            /// </summary>
                            int Targettick = _CurrnetShip.ShipsTaskGroup.Contact.DistanceUpdate[TargetID];


                            float distance = _CurrnetShip.ShipsTaskGroup.Contact.DistanceTable[TargetID];
                            int TCS = pair.Key.TotalCrossSection;
                            int detectFactor = _CurrnetShip.ShipMFC[_CurrnetFC.componentIndex].mFCSensorDef.GetActiveDetectionRange(TCS, -1);

                            bool det = _CurrnetShip.ShipsFaction.LargeDetection(CurSystem, distance, detectFactor);

                            if (det == true)
                            {
                                if (count == m_oDetailsPanel.ContactListBox.SelectedIndex)
                                {
                                    _CurrnetShip.ShipMFC[_CurrnetFC.componentIndex].assignTarget(pair.Key);
                                    pair.Key.ShipsTargetting.Add(_CurrnetShip);
                                    break;
                                }
                                count++;
                            }
                        }
                    }
                }
            }
            BuildCombatSummary();
        }

        /// <summary>
        /// Clears the selected FC of its target.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearTargetButton_Click(object sender, EventArgs e)
        {
            if (_CurrnetFC != null)
            {
                if (isBFC == true)
                {
                    if (_CurrnetShip.ShipBFC[_CurrnetFC.componentIndex].getTarget() != null)
                    {
                        ShipTN vessel = _CurrnetShip.ShipBFC[_CurrnetFC.componentIndex].getTarget();
                        if (vessel.ShipsTargetting.Contains(_CurrnetShip) == true)
                        {
                            vessel.ShipsTargetting.Remove(_CurrnetShip);
                        }
                    }
                    _CurrnetShip.ShipBFC[_CurrnetFC.componentIndex].clearTarget();
                }
                else
                {
                    _CurrnetShip.ShipMFC[_CurrnetFC.componentIndex].clearTarget();

                    if (_CurrnetShip.ShipMFC[_CurrnetFC.componentIndex].getTarget().targetType == StarSystemEntityType.TaskGroup)
                    {
                        ShipTN vessel = _CurrnetShip.ShipMFC[_CurrnetFC.componentIndex].getTarget().ship;
                        if (vessel.ShipsTargetting.Contains(_CurrnetShip) == true)
                        {
                            vessel.ShipsTargetting.Remove(_CurrnetShip);
                        }
                    }
                }
            }
            BuildCombatSummary();
        }

        /// <summary>
        /// Assigns selected weapon to the current FC.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AssignWeaponButton_Click(object sender, EventArgs e)
        {
            if (_CurrnetFC != null && m_oDetailsPanel.WeaponListBox.SelectedIndex != -1)
            {
                if (isBFC == true)
                {
                    BeamTN SelectedBeam = _CurrnetShip.ShipBeam[m_oDetailsPanel.WeaponListBox.SelectedIndex];
                    if (SelectedBeam.fireController != null)
                    {
                        BeamFireControlTN BFC = SelectedBeam.fireController;
                        BFC.unlinkWeapon(SelectedBeam);
                    }
                    _CurrnetShip.ShipBFC[_CurrnetFC.componentIndex].linkWeapon(SelectedBeam);
                }
                else
                {
                    MissileLauncherTN SelectedTube = _CurrnetShip.ShipMLaunchers[m_oDetailsPanel.WeaponListBox.SelectedIndex];

                    if (SelectedTube.mFC != null)
                    {
                        MissileFireControlTN MFC = SelectedTube.mFC;

                        MFC.removeLaunchTube(SelectedTube);
                    }

                    _CurrnetShip.ShipMFC[_CurrnetFC.componentIndex].assignLaunchTube(SelectedTube);
                }
            }
            BuildCombatSummary();
        }

        /// <summary>
        /// Assigns all weapons to the selected FC.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AssignAllWeaponsButton_Click(object sender, EventArgs e)
        {
            if (_CurrnetFC != null)
            {
                if (isBFC == true)
                {
                    for (int loop = 0; loop < _CurrnetShip.ShipBeam.Count; loop++)
                    {
                        BeamTN SelectedBeam = _CurrnetShip.ShipBeam[loop];
                        if (SelectedBeam.fireController != null)
                        {
                            BeamFireControlTN BFC = SelectedBeam.fireController;
                            BFC.unlinkWeapon(SelectedBeam);
                        }
                        _CurrnetShip.ShipBFC[_CurrnetFC.componentIndex].linkWeapon(SelectedBeam);
                    }
                }
                else
                {
                    for (int loop = 0; loop < _CurrnetShip.ShipMLaunchers.Count; loop++)
                    {
                        MissileLauncherTN SelectedTube = _CurrnetShip.ShipMLaunchers[loop];

                        if (SelectedTube.mFC != null)
                        {
                            MissileFireControlTN MFC = SelectedTube.mFC;

                            MFC.removeLaunchTube(SelectedTube);
                        }

                        _CurrnetShip.ShipMFC[_CurrnetFC.componentIndex].assignLaunchTube(SelectedTube);
                    }
                }
            }
            BuildCombatSummary();
        }

        /// <summary>
        /// Clears assigned weapons from the current FC
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearWeaponsButton_Click(object sender, EventArgs e)
        {
            if (isBFC == true)
            {
                _CurrnetShip.ShipBFC[_CurrnetFC.componentIndex].clearWeapons();
            }
            else
            {
                _CurrnetShip.ShipMFC[_CurrnetFC.componentIndex].ClearAllWeapons();
            }

            BuildCombatSummary();
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

            if (_CurrnetShip != null)
            {

                Padding newPadding = new Padding(2, 0, 2, 0);
                for (int loop = 0; loop < _CurrnetShip.ShipArmor.armorDef.cNum; loop++)
                {
                    AddColumn(newPadding);
                }

                for (int loop = 0; loop < _CurrnetShip.ShipArmor.armorDef.depth; loop++)
                {
                    AddRow(loop);
                }

                if (_CurrnetShip.ShipArmor.isDamaged == true)
                {
                    foreach (KeyValuePair<ushort, ushort> pair in _CurrnetShip.ShipArmor.armorDamage)
                    {
                        for (int loop = 0; loop < pair.Value; loop++)
                        {
                            m_oDetailsPanel.ArmorDisplayDataGrid.Rows[loop].Cells[pair.Key].Style.BackColor = Color.Red;
                        }
                    }
                }

                m_oDetailsPanel.ArmorDisplayDataGrid.ClearSelection();
            }

        }

        /// <summary>
        /// Print the damage allocation chart to the appropriate place under the damage control tab.
        /// </summary>
        private void BuildDACInfo()
        {
            m_oDetailsPanel.DACListBox.Items.Clear();

            if (_CurrnetShip != null)
            {

                int DAC = 1;
                String Entry = "N/A";
                for (int loop = 0; loop < _CurrnetShip.ShipClass.ListOfComponentDefs.Count; loop++)
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

                    String DAC2 = _CurrnetShip.ShipClass.DamageAllocationChart[_CurrnetShip.ShipClass.ListOfComponentDefs[loop]].ToString();
                    if (_CurrnetShip.ShipClass.DamageAllocationChart[_CurrnetShip.ShipClass.ListOfComponentDefs[loop]] < 10)
                    {
                        DAC2 = "00" + _CurrnetShip.ShipClass.DamageAllocationChart[_CurrnetShip.ShipClass.ListOfComponentDefs[loop]].ToString();
                    }
                    else if (_CurrnetShip.ShipClass.DamageAllocationChart[_CurrnetShip.ShipClass.ListOfComponentDefs[loop]] < 100)
                    {
                        DAC2 = "0" + _CurrnetShip.ShipClass.DamageAllocationChart[_CurrnetShip.ShipClass.ListOfComponentDefs[loop]].ToString();
                    }



                    Entry = DACString + "-" + DAC2 + " " + _CurrnetShip.ShipClass.ListOfComponentDefs[loop].Name +
                        "(" + _CurrnetShip.ShipClass.ListOfComponentDefsCount[loop].ToString() + "/" +
                        _CurrnetShip.ShipClass.ListOfComponentDefs[loop].htk.ToString() + ")";

                    m_oDetailsPanel.DACListBox.Items.Add(Entry);

                    DAC = _CurrnetShip.ShipClass.DamageAllocationChart[_CurrnetShip.ShipClass.ListOfComponentDefs[loop]] + 1;


                }

                m_oDetailsPanel.DACListBox.Items.Add("");
                m_oDetailsPanel.DACListBox.Items.Add("Electronic Only DAC");

                DAC = 1;

                foreach (KeyValuePair<ComponentDefTN, int> pair in _CurrnetShip.ShipClass.ElectronicDamageAllocationChart)
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

                    int index = _CurrnetShip.ShipClass.ListOfComponentDefs.IndexOf(pair.Key);

                    Entry = DACString + "-" + DAC2 + " " + pair.Key.Name + "(" + _CurrnetShip.ShipClass.ListOfComponentDefsCount[index].ToString() + "/" +
                        pair.Key.htk.ToString() + ")";

                    m_oDetailsPanel.DACListBox.Items.Add(Entry);

                    DAC = _CurrnetShip.ShipClass.ElectronicDamageAllocationChart[pair.Key] + 1;
                }
            }
        }

        /// <summary>
        /// print the names of all the destroyed components.
        /// </summary>
        private void BuildDamagedSystemsList()
        {
            m_oDetailsPanel.DamagedSystemsListBox.Items.Clear();

            if (_CurrnetShip != null)
            {

                for (int loop = 0; loop < _CurrnetShip.DestroyedComponents.Count; loop++)
                {


                    m_oDetailsPanel.DamagedSystemsListBox.Items.Add(_CurrnetShip.ShipComponents[_CurrnetShip.DestroyedComponents[loop]].Name);
                }
            }
        }

        /// <summary>
        /// List information about all FCs and Weapons.
        /// </summary>
        private void BuildCombatSummary()
        {
            m_oDetailsPanel.CombatSummaryTextBox.Clear();

            if (_CurrnetShip != null)
            {
                String Entry = "N/A";
                String fireAuth = "N/A";

                for (int loop = 0; loop < _CurrnetShip.ShipBFC.Count; loop++)
                {
                    ShipTN Target = _CurrnetShip.ShipBFC[loop].getTarget();

                    if (Target == null)
                    {
                        Entry = String.Format("{0}: No Target Assignment\n", _CurrnetShip.ShipBFC[loop].Name);
                    }
                    else
                    {
                        if (_CurrnetShip.ShipBFC[loop].openFire == true)
                            fireAuth = "Weapons Firing";
                        else
                            fireAuth = "Holding Fire";

                        Entry = String.Format("{0}: Targeting {1} - {2}\n", _CurrnetShip.ShipBFC[loop].Name, Target.Name, fireAuth);
                    }

                    m_oDetailsPanel.CombatSummaryTextBox.AppendText(Entry);

                    for (int loop2 = 0; loop2 < _CurrnetShip.ShipBFC[loop].linkedWeapons.Count; loop2++)
                    {
                        if (_CurrnetShip.ShipBFC[loop].linkedWeapons[loop2].currentCapacitor == _CurrnetShip.ShipBFC[loop].linkedWeapons[loop2].beamDef.powerRequirement)
                        {
                            Entry = String.Format("{0}: (Ready to Fire)\n", _CurrnetShip.ShipBFC[loop].linkedWeapons[loop2].Name);
                            m_oDetailsPanel.CombatSummaryTextBox.AppendText(Entry);
                        }
                        else
                        {
                            Entry = String.Format("{0}: ({1} / {2} power recharged)\n", _CurrnetShip.ShipBFC[loop].linkedWeapons[loop2].Name,
                                                                        _CurrnetShip.ShipBFC[loop].linkedWeapons[loop2].currentCapacitor.ToString(), _CurrnetShip.ShipBFC[loop].linkedWeapons[loop2].beamDef.powerRequirement.ToString());
                            m_oDetailsPanel.CombatSummaryTextBox.AppendText(Entry);
                        }
                    }
                    m_oDetailsPanel.CombatSummaryTextBox.AppendText("\n");
                }

                for (int loop = 0; loop < _CurrnetShip.ShipMFC.Count; loop++)
                {
                    OrdnanceTargetTN Target = _CurrnetShip.ShipMFC[loop].getTarget();

                    if (Target == null)
                    {
                        Entry = String.Format("{0}: No Target Assignment\n", _CurrnetShip.ShipMFC[loop].Name);
                    }
                    else
                    {
                        if (_CurrnetShip.ShipBFC[loop].openFire == true)
                            fireAuth = "Weapons Firing";
                        else
                            fireAuth = "Holding Fire";

                        switch (Target.targetType)
                        {
                            case StarSystemEntityType.TaskGroup:
                                Entry = String.Format("{0}: {1} - {2}\n", _CurrnetShip.ShipMFC[loop].Name, Target.ship.Name, fireAuth);
                                break;
                        }

                        m_oDetailsPanel.CombatSummaryTextBox.AppendText(Entry);

                        for (int loop2 = 0; loop2 < _CurrnetShip.ShipMFC[loop].linkedWeapons.Count; loop2++)
                        {
                            if (_CurrnetShip.ShipMFC[loop].linkedWeapons[loop2].ReadyToFire() == true)
                            {
                                Entry = String.Format("{0} - {1}: (Ready to Fire)\n", _CurrnetShip.ShipMFC[loop].linkedWeapons[loop2].Name,
                                                                                      _CurrnetShip.ShipMFC[loop].linkedWeapons[loop2].loadedOrdnance.Name);

                                m_oDetailsPanel.CombatSummaryTextBox.AppendText(Entry);
                            }
                            else
                            {
                                Entry = String.Format("{0} - {1}: ({2} secs to reload)\n", _CurrnetShip.ShipMFC[loop].linkedWeapons[loop2].Name,
                                                                                      _CurrnetShip.ShipMFC[loop].linkedWeapons[loop2].loadedOrdnance.Name,
                                                                                      _CurrnetShip.ShipMFC[loop].linkedWeapons[loop2].loadTime);

                                m_oDetailsPanel.CombatSummaryTextBox.AppendText(Entry);
                            }
                        }
                    }
                    m_oDetailsPanel.CombatSummaryTextBox.AppendText("\n");
                }

                bool hasPrinted = false;

                for (int loop = 0; loop < _CurrnetShip.ShipMLaunchers.Count; loop++)
                {
                    if (_CurrnetShip.ShipMLaunchers[loop].loadedOrdnance != null && _CurrnetShip.ShipMLaunchers[loop].mFC == null)
                    {
                        if (hasPrinted == false)
                        {
                            Entry = "Assigned Missiles/Buoys without Shipboard Fire Control\n";
                            m_oDetailsPanel.CombatSummaryTextBox.AppendText(Entry);

                            hasPrinted = true;
                        }

                        if (_CurrnetShip.ShipMLaunchers[loop].ReadyToFire() == true)
                        {
                            Entry = String.Format("{0} - {1}: (Ready to Fire)\n", _CurrnetShip.ShipMLaunchers[loop].Name, _CurrnetShip.ShipMLaunchers[loop].loadedOrdnance.Name);
                            m_oDetailsPanel.CombatSummaryTextBox.AppendText(Entry);
                        }
                        else
                        {
                            Entry = String.Format("{0} - {1}: ({2} secs to reload)\n", _CurrnetShip.ShipMLaunchers[loop].Name, _CurrnetShip.ShipMLaunchers[loop].loadedOrdnance.Name,
                                                                                       _CurrnetShip.ShipMLaunchers[loop].loadTime);
                            m_oDetailsPanel.CombatSummaryTextBox.AppendText(Entry);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Build the class design info for the current ship. Ship class, geo rating, grav rating not done but displayed. Maintenance by and large isn't done. fighters aren't done.
        /// Ultimately I want to move this to Class design proper so that this code isn't copy pasted everywhere, and keeping everything the same version is simpler.
        /// </summary>
        private void BuildClassDesign()
        {
            m_oDetailsPanel.ClassDesignTextBox.Clear();

            if (_CurrnetShip != null)
            {

                String Entry = String.Format("{0} class Warship   {1} tons   {2} Crew   {3} BP   TCS {4} TH {5} EM {6}\n", _CurrnetShip.ShipClass.Name, _CurrnetShip.ShipClass.SizeTons.ToString(),
                    _CurrnetShip.ShipClass.TotalRequiredCrew.ToString(), Math.Floor(_CurrnetShip.ShipClass.BuildPointCost).ToString(), _CurrnetShip.ShipClass.TotalCrossSection.ToString(),
                    _CurrnetShip.ShipClass.MaxThermalSignature.ToString(), _CurrnetShip.ShipClass.MaxEMSignature.ToString());
                m_oDetailsPanel.ClassDesignTextBox.AppendText(Entry);

                string ShieldR = "0";

                if (_CurrnetShip.ShipClass.ShipShieldDef != null)
                {
                    if (_CurrnetShip.ShipClass.ShipShieldDef.shieldGen == _CurrnetShip.ShipClass.ShipShieldDef.shieldPool)
                    {
                        ShieldR = "300";
                    }
                    else
                    {
                        float shield = (float)Math.Floor((_CurrnetShip.ShipClass.ShipShieldDef.shieldPool / _CurrnetShip.ShipClass.ShipShieldDef.shieldGen) * 300.0f);
                        ShieldR = shield.ToString();
                    }
                }

                Entry = String.Format("{0} km/s   Armour {1}-{2}   Shields {3}-{4}   Sensors {5}/{6}/{7}/{8}   Damage Control Rating {9}  PPV {10}\n", _CurrnetShip.ShipClass.MaxSpeed,
                                                                   _CurrnetShip.ShipClass.ShipArmorDef.depth, _CurrnetShip.ShipClass.ShipArmorDef.cNum,
                                                                   _CurrnetShip.ShipClass.TotalShieldPool, ShieldR,
                                                                   _CurrnetShip.ShipClass.BestThermalRating, _CurrnetShip.ShipClass.BestEMRating, 0, 0,
                                                                   _CurrnetShip.ShipClass.MaxDamageControlRating, _CurrnetShip.ShipClass.PlanetaryProtectionValue);

                m_oDetailsPanel.ClassDesignTextBox.AppendText(Entry);

                Entry = String.Format("Maint Life {0} Years   MSP {1}   AFR {2}   IFR {3}   1YR {4}   5YR {5}   Max Repair {6} MSP\n", _CurrnetShip.ShipClass.MaintenanceLife,
                                                                   _CurrnetShip.ShipClass.TotalMSPCapacity, _CurrnetShip.ShipClass.AnnualFailureRate, _CurrnetShip.ShipClass.InitialFailureRate,
                                                                   _CurrnetShip.ShipClass.YearOneFailureTotal, _CurrnetShip.ShipClass.YearFiveFailureTotal, _CurrnetShip.ShipClass.MaxRepair);

                m_oDetailsPanel.ClassDesignTextBox.AppendText(Entry);

                Entry = String.Format("Intended Deployment Time: {0} months   Spare Berths {1}\n\n", _CurrnetShip.ShipClass.MaxDeploymentTime, _CurrnetShip.ShipClass.SpareCrewQuarters);

                m_oDetailsPanel.ClassDesignTextBox.AppendText(Entry);

                if (_CurrnetShip.ShipClass.ShipEngineDef != null)
                {
                    float fuelCon = (float)Math.Floor(_CurrnetShip.ShipClass.ShipEngineDef.fuelConsumptionMod * 100.0f);
                    String FuelString = String.Format("{0}", fuelCon);

                    Entry = String.Format("{0} ({1})   Power {2}   Fuel Use {3}%    Signature {4}    Armour 0    Exp {5}%\n", _CurrnetShip.ShipClass.ShipEngineDef.Name, _CurrnetShip.ShipClass.ShipEngineCount,
                                                                   _CurrnetShip.ShipClass.ShipEngineDef.enginePower, FuelString, _CurrnetShip.ShipClass.ShipEngineDef.thermalSignature, _CurrnetShip.ShipClass.ShipEngineDef.expRisk);

                    m_oDetailsPanel.ClassDesignTextBox.AppendText(Entry);
                }

                if (_CurrnetShip.ShipClass.TotalFuelCapacity != 0)
                {
                    String Range = "Range N/A";
                    if (_CurrnetShip.ShipClass.ShipEngineDef != null)
                    {
                        String Time = "N/A";
                        float HoursOfFuel = _CurrnetShip.ShipClass.TotalFuelCapacity / _CurrnetShip.ShipClass.MaxFuelUsePerHour;


                        if (HoursOfFuel < 72)
                        {
                            Time = String.Format("({0} hours at full power)", Math.Floor(HoursOfFuel).ToString());
                        }
                        else
                        {
                            float DaysOfFuel = HoursOfFuel / 24.0f;
                            Time = String.Format("({0} days at full power)", Math.Floor(DaysOfFuel).ToString());
                        }

                        float SecondsPerBillion = 1000000000.0f / _CurrnetShip.ShipClass.MaxSpeed;
                        float HoursPerBillion = SecondsPerBillion / 3600.0f;

                        float billions = HoursOfFuel / HoursPerBillion;

                        if (billions >= 0.1)
                        {
                            billions = (float)(Math.Floor(10.0 * billions) / 10.0f);
                            Range = String.Format("Range {0} B km {1}", billions, Time);
                        }
                        else
                        {
                            float millions = billions * 1000.0f;
                            millions = (float)(Math.Floor(10.0 * millions) / 10.0f);
                            Range = String.Format("Range {0} M km {1}", millions, Time);
                        }



                    }

                    Entry = String.Format("Fuel Capacity {0} Litres   {1}\n", _CurrnetShip.ShipClass.TotalFuelCapacity, Range);

                    m_oDetailsPanel.ClassDesignTextBox.AppendText(Entry);
                }

                if (_CurrnetShip.ShipClass.ShipShieldDef != null)
                {

                    float FuelCostPerHour = _CurrnetShip.ShipClass.ShipShieldDef.fuelCostPerHour * _CurrnetShip.ShipClass.ShipShieldCount;
                    float FuelCostPerDay = FuelCostPerHour * 24.0f;

                    Entry = String.Format("{0} ({1})   Total Fuel Cost  {2} Litres per hour  ({3} per day)\n", _CurrnetShip.ShipClass.ShipShieldDef, _CurrnetShip.ShipClass.ShipShieldCount,
                                                                             FuelCostPerHour, FuelCostPerDay);

                    m_oDetailsPanel.ClassDesignTextBox.AppendText(Entry);
                }

                Entry = "\n";
                m_oDetailsPanel.ClassDesignTextBox.AppendText(Entry);

                for (int loop = 0; loop < _CurrnetShip.ShipClass.ShipBeamDef.Count; loop++)
                {
                    String Range = "N/A";

                    float MaxRange = 0;

                    for (int loop2 = 0; loop2 < _CurrnetShip.ShipClass.ShipBFCDef.Count; loop2++)
                    {
                        if (_CurrnetShip.ShipClass.ShipBFCDef[loop2].range > MaxRange)
                            MaxRange = _CurrnetShip.ShipClass.ShipBFCDef[loop2].range;
                    }

                    if (MaxRange > _CurrnetShip.ShipClass.ShipBeamDef[loop].range)
                    {
                        Range = String.Format("Range {0}km", _CurrnetShip.ShipClass.ShipBeamDef[loop].range);
                    }
                    else
                    {
                        Range = String.Format("Range {0}km", MaxRange);
                    }

                    String Tracking = "N/A";
                    if (_CurrnetShip.ShipClass.MaxSpeed > _CurrnetShip.ShipsFaction.BaseTracking)
                    {
                        Tracking = String.Format("TS: {0} km/s", _CurrnetShip.ShipClass.MaxSpeed);
                    }
                    else
                    {
                        Tracking = String.Format("TS: {0} km/s", _CurrnetShip.ShipsFaction.BaseTracking);
                    }

                    String Power = "N/A";
                    if (_CurrnetShip.ShipClass.ShipBeamDef[loop].componentType == ComponentTypeTN.Gauss)
                    {
                        Power = "0-0";
                    }
                    else
                    {
                        Power = String.Format("{0}-{1}", _CurrnetShip.ShipClass.ShipBeamDef[loop].powerRequirement, _CurrnetShip.ShipClass.ShipBeamDef[loop].weaponCapacitor);
                    }

                    float ROF = (_CurrnetShip.ShipClass.ShipBeamDef[loop].powerRequirement / _CurrnetShip.ShipClass.ShipBeamDef[loop].weaponCapacitor) * 5;

                    if (ROF < 5)
                        ROF = 5;
                    String DamageString = _CurrnetShip.ShipClass.ShipBeamDef[loop].damage[0].ToString();

                    for (int loop2 = 1; loop2 < 10; loop2++)
                    {
                        int value = -1;
                        if (loop2 >= _CurrnetShip.ShipClass.ShipBeamDef[loop].damage.Count)
                        {
                            value = 0;
                        }
                        else
                        {
                            value = _CurrnetShip.ShipClass.ShipBeamDef[loop].damage[loop2];
                        }
                        DamageString = String.Format("{0} {1}", DamageString, value);
                    }

                    Entry = String.Format("{0} ({1})   {2}   {3}   Power {4}   RM {5}   ROF {6}   {7}\n",
                                          _CurrnetShip.ShipClass.ShipBeamDef[loop].Name, _CurrnetShip.ShipClass.ShipBeamCount[loop], Range, Tracking, Power,
                                          (_CurrnetShip.ShipClass.ShipBeamDef[loop].damage.Count - 1), ROF, DamageString);

                    m_oDetailsPanel.ClassDesignTextBox.AppendText(Entry);
                }

                for (int loop = 0; loop < _CurrnetShip.ShipClass.ShipBFCDef.Count; loop++)
                {
                    String AccString = String.Format("{0}", Math.Floor(_CurrnetShip.ShipClass.ShipBFCDef[loop].rangeAccuracyTable[0] * 100.0f));

                    for (int loop2 = 1; loop2 < 10; loop2++)
                    {
                        if (loop2 < _CurrnetShip.ShipClass.ShipBFCDef[loop].rangeAccuracyTable.Count)
                        {
                            AccString = String.Format("{0} {1}", AccString, Math.Floor(_CurrnetShip.ShipClass.ShipBFCDef[loop].rangeAccuracyTable[loop2] * 100.0f));
                        }
                        else
                        {
                            AccString = String.Format("{0} 0", AccString);
                        }
                    }


                    Entry = String.Format("{0} ({1})   Max Range: {2} km   TS: {3} km/s   {4}\n",
                                          _CurrnetShip.ShipClass.ShipBFCDef[loop].Name, _CurrnetShip.ShipClass.ShipBFCCount[loop], _CurrnetShip.ShipClass.ShipBFCDef[loop].range,
                                          _CurrnetShip.ShipClass.ShipBFCDef[loop].tracking, AccString);

                    m_oDetailsPanel.ClassDesignTextBox.AppendText(Entry);
                }

                for (int loop = 0; loop < _CurrnetShip.ShipClass.ShipReactorDef.Count; loop++)
                {
                    int TPO = _CurrnetShip.ShipClass.ShipReactorDef[loop].powerGen * _CurrnetShip.ShipClass.ShipReactorCount[loop];

                    Entry = String.Format("{0} ({1})   Total Power Output {2}   Armour 0    Exp {3}%\n", _CurrnetShip.ShipClass.ShipReactorDef[loop].Name,
                                          _CurrnetShip.ShipClass.ShipReactorCount[loop], TPO, _CurrnetShip.ShipClass.ShipReactorDef[loop].expRisk);

                    m_oDetailsPanel.ClassDesignTextBox.AppendText(Entry);
                }

                Entry = "\n";
                m_oDetailsPanel.ClassDesignTextBox.AppendText(Entry);

                for (int loop = 0; loop < _CurrnetShip.ShipClass.ShipASensorDef.Count; loop++)
                {
                    String RangeString = "-4.2m";

                    if (_CurrnetShip.ShipClass.ShipASensorDef[loop].maxRange >= 100000)
                    {
                        float RangeB = (float)Math.Floor((double)_CurrnetShip.ShipClass.ShipASensorDef[loop].maxRange / 10000.0) / 10.0f;

                        RangeString = String.Format("{0}B", RangeB);
                    }
                    else if (_CurrnetShip.ShipClass.ShipASensorDef[loop].maxRange >= 100)
                    {
                        float RangeM = (float)Math.Floor((double)_CurrnetShip.ShipClass.ShipASensorDef[loop].maxRange / 10.0) / 10.0f;

                        RangeString = String.Format("{0}M", RangeM);
                    }
                    else
                    {
                        RangeString = String.Format("{0}K", ((float)Math.Floor((double)_CurrnetShip.ShipClass.ShipASensorDef[loop].maxRange) * 10.0f));
                    }

                    String MCRString = " ";

                    if (_CurrnetShip.ShipClass.ShipASensorDef[loop].resolution == 1)
                    {
                        int minRange = _CurrnetShip.ShipClass.ShipASensorDef[loop].lookUpMT[0];

                        if (minRange >= 100000)
                        {
                            float RangeB = (float)Math.Floor((double)minRange / 10000.0) / 10.0f;
                            MCRString = String.Format(" MCR {0}B km   ", RangeB);
                        }
                        else if (minRange >= 100)
                        {
                            float RangeM = (float)Math.Floor((double)minRange / 10.0) / 10.0f;
                            MCRString = String.Format(" MCR {0}M km   ", RangeM);
                        }
                        else
                        {
                            MCRString = String.Format(" MCR {0}K km   ", ((float)Math.Floor((double)minRange) * 10.0f));
                        }
                    }


                    Entry = String.Format("{0} ({1})   GPS {2}   Range {3} km  {4}Resolution {5}\n", _CurrnetShip.ShipClass.ShipASensorDef[loop].Name,
                                          _CurrnetShip.ShipClass.ShipASensorCount[loop], _CurrnetShip.ShipClass.ShipASensorDef[loop].gps, RangeString, MCRString,
                                          _CurrnetShip.ShipClass.ShipASensorDef[loop].resolution);

                    m_oDetailsPanel.ClassDesignTextBox.AppendText(Entry);
                }

                for (int loop = 0; loop < _CurrnetShip.ShipClass.ShipPSensorDef.Count; loop++)
                {
                    String RangeString = "20m";

                    int range = _CurrnetShip.ShipClass.ShipPSensorDef[loop].range;

                    if (range >= 100000)
                    {
                        float RangeB = (float)Math.Floor((double)range / 10000.0) / 10.0f;
                        RangeString = String.Format("{0}B", RangeB);
                    }
                    else
                    {
                        float RangeM = (float)Math.Floor((double)range / 10.0) / 10.0f;
                        RangeString = String.Format("{0}M", RangeM);
                    }

                    Entry = String.Format("{0} ({1})     Sensitivity {2}     Detect Sig Strength 1000:  {3} km\n", _CurrnetShip.ShipClass.ShipPSensorDef[loop].Name,
                                          _CurrnetShip.ShipClass.ShipPSensorCount[loop], _CurrnetShip.ShipClass.ShipPSensorDef[loop].rating, RangeString);

                    m_oDetailsPanel.ClassDesignTextBox.AppendText(Entry);
                }

                if (_CurrnetShip.ShipClass.IsMilitary == true)
                {
                    Entry = "\nThis design is classed as a Military Vessel for maintenance purposes\n";
                }
                else
                {
                    Entry = "\nThis design is classed as a Commercial Vessel for maintenance purposes\n";
                }

                m_oDetailsPanel.ClassDesignTextBox.AppendText(Entry);
            }
        }

        /// <summary>
        /// Print the names of every weapon on this ship.
        /// </summary>
        private void BuildWeaponList()
        {
            m_oDetailsPanel.WeaponListBox.Items.Clear();

            if (_CurrnetShip != null && _CurrnetFC != null)
            {

                if (isBFC == true)
                {
                    for (int loop = 0; loop < _CurrnetShip.ShipBeam.Count; loop++)
                    {
                        m_oDetailsPanel.WeaponListBox.Items.Add(_CurrnetShip.ShipBeam[loop].Name);
                    }
                }
                else
                {
                    for (int loop = 0; loop < _CurrnetShip.ShipMLaunchers.Count; loop++)
                    {
                        m_oDetailsPanel.WeaponListBox.Items.Add(_CurrnetShip.ShipMLaunchers[loop].Name);
                    }
                }
            }
        }

        /// <summary>
        /// Build the pd modes.
        /// </summary>
        private void BuildPDComboBox()
        {
            m_oDetailsPanel.PDComboBox.Items.Clear();

            if (_CurrnetShip != null && _CurrnetFC != null)
            {

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

                m_oDetailsPanel.PDComboBox.SelectedIndex = 0;
            }
        }


        /// <summary>
        /// Add all available system contacts to the contact list.
        /// </summary>
        private void BuildContactsList()
        {
            m_oDetailsPanel.ContactListBox.Items.Clear();

            if (_CurrnetShip != null && _CurrnetFC != null)
            {

                /// <summary>
                /// Planetary enemy populations should always be displayed.
                /// </summary>

                if (isBFC == true)
                {
                    /// <summary>
                    /// BFC range is so short that we'll just print all contacts and let the user sort em out.
                    /// </summary>
                    foreach (KeyValuePair<ShipTN, FactionContact> pair in _CurrnetFaction.DetectedContacts)
                    {
                        if (pair.Key.ShipsTaskGroup.Contact.CurrentSystem == _CurrnetShip.ShipsTaskGroup.Contact.CurrentSystem && pair.Value.active == true)
                        {
                            String TH = "";
                            if (pair.Value.thermal == true)
                            {
                                TH = String.Format("[Thermal {0}]", pair.Key.CurrentThermalSignature);
                            }

                            String EM = "";
                            if (pair.Value.EM == true)
                            {
                                EM = String.Format("[EM {0}]", pair.Key.CurrentEMSignature);
                            }

                            String ACT = "";
                            if (pair.Value.active == true)
                            {
                                ACT = String.Format("[ACT {0}]", pair.Key.TotalCrossSection);
                            }

                            String Entry = String.Format("{0} {1}{2}{3}", pair.Key.Name, TH, EM, ACT);

                            m_oDetailsPanel.ContactListBox.Items.Add(Entry);
                        }
                    }
                }
                else
                {
                    /// <summary>
                    /// Each MFC entry will be range checked, also there have been some tick errors, this may be the place to hunt them down.
                    /// </summary>
                    foreach (KeyValuePair<ShipTN, FactionContact> pair in _CurrnetFaction.DetectedContacts)
                    {
                        if (pair.Key.ShipsTaskGroup.Contact.CurrentSystem == _CurrnetShip.ShipsTaskGroup.Contact.CurrentSystem)
                        {
                            StarSystem CurSystem = _CurrnetShip.ShipsTaskGroup.Contact.CurrentSystem;
                            int MyID = CurSystem.SystemContactList.IndexOf(_CurrnetShip.ShipsTaskGroup.Contact);
                            int TargetID = CurSystem.SystemContactList.IndexOf(pair.Key.ShipsTaskGroup.Contact);

                            /// <summary>
                            /// Validate tick here?
                            /// </summary>
                            int Targettick = _CurrnetShip.ShipsTaskGroup.Contact.DistanceUpdate[TargetID];


                            float distance = _CurrnetShip.ShipsTaskGroup.Contact.DistanceTable[TargetID];
                            int TCS = pair.Key.TotalCrossSection;
                            int detectFactor = _CurrnetShip.ShipMFC[_CurrnetFC.componentIndex].mFCSensorDef.GetActiveDetectionRange(TCS, -1);

                            bool det = _CurrnetShip.ShipsFaction.LargeDetection(CurSystem, distance, detectFactor);

                            if (det == true)
                            {
                                m_oDetailsPanel.ContactListBox.Items.Add(pair.Key);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// this is a TEMPORARY messagelog builder
        /// </summary>
        private void TEMPBuildMessageLog()
        {
            m_oDetailsPanel.TEMPPRINTTextBox.Clear();

            for (int loop = 0; loop < _CurrnetFaction.MessageLog.Count; loop++)
            {
                String Entry = String.Format("{0} - {1}: {2}\n", _CurrnetFaction.MessageLog[loop].Location, _CurrnetFaction.MessageLog[loop].TimeSlice, _CurrnetFaction.MessageLog[loop].Text);

                m_oDetailsPanel.TEMPPRINTTextBox.AppendText(Entry);
            }
        }


        /// <summary>
        /// Updates the display for all relevant ship panels.
        /// </summary>
        private void RefreshShipPanels()
        {
            m_oShipListPanel.ShipsListBox.Items.Clear();

            for (int loop = 0; loop < _CurrnetFaction.Ships.Count; loop++)
            {
                m_oShipListPanel.ShipsListBox.Items.Add(_CurrnetFaction.Ships[loop]);
            }

            if (m_oShipListPanel.ShipsListBox.Items.Count != 0)
                m_oShipListPanel.ShipsListBox.SelectedIndex = 0;

            RefreshShipInfo();
        }

        /// <summary>
        /// Build info about the ship.
        /// </summary>
        private void RefreshShipInfo()
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

            BuildCombatSummary();
            BuildClassDesign();

            TEMPBuildMessageLog();

            RefreshFCInfo();
        }

        /// <summary>
        /// Build info about the fire control.
        /// </summary>
        private void RefreshFCInfo()
        {
            /// <summary>
            /// Combat Settings Tab:
            /// </summary>
                
            BuildWeaponList();
            BuildPDComboBox();
            BuildContactsList();
        }
        #endregion
    }
}
