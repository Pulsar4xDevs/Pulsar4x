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

            /// <summary>
            /// Point defense functionality.
            /// </summary>
            m_oDetailsPanel.PDComboBox.SelectedIndexChanged += new EventHandler(PDComboBox_SelectedIndexChanged);
            m_oDetailsPanel.SetPDModeButton.Click += new EventHandler(SetPDModeButton_Click);
            m_oDetailsPanel.PDRangeTextBox.TextChanged += new EventHandler(PDRangeTextBox_TextChanged);

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

            /// <summary>
            /// Ordnance Tab:
            /// </summary>
            m_oDetailsPanel.StandardReloadButton.Click += new EventHandler(StandardReloadButton_Click);
            m_oDetailsPanel.AssignTubeButton.Click += new EventHandler(AssignTubeButton_Click);
            m_oDetailsPanel.AssignAllTubesButton.Click += new EventHandler(AssignAllTubesButton_Click);
            m_oDetailsPanel.ClearAllTubesButton.Click += new EventHandler(ClearAllTubesButton_Click);
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

        /// <summary>
        /// not fully implemented.
        /// </summary>
        public void SMOn()
        {
            m_oDetailsPanel.StandardReloadButton.Enabled = true;
        }

        /// <summary>
        /// not fully implemented.
        /// </summary>
        public void SMOff()
        {
            m_oDetailsPanel.StandardReloadButton.Enabled = false;
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

        /// <summary>
        /// If a new active is selected that needs to be set as the _CurrnetSensor. also print whether this active is on or off.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        #region Point defense
        /// <summary>
        /// if any change needs to be made to the FC on new PD selection do it here.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PDComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// PD mode and range will be handled in this function
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetPDModeButton_Click(object sender, EventArgs e)
        {
            int index = m_oDetailsPanel.PDComboBox.SelectedIndex;
            float PointDefenseRange = 0.0f;
            bool res = float.TryParse(m_oDetailsPanel.PDRangeTextBox.Text, out PointDefenseRange);
            if (isBFC)
            {
                if (index <= (int)PointDefenseState.FinalDefensiveFireSelf)
                {
                    if (res)
                        _CurrnetShip.ShipBFC[_CurrnetFC.componentIndex].SetPointDefenseRange(PointDefenseRange);
                    _CurrnetShip.ShipBFC[_CurrnetFC.componentIndex].SetPointDefenseMode((PointDefenseState)index);


                    StarSystem CurrentSystem = _CurrnetShip.ShipsTaskGroup.Contact.Position.System;

                    if (index != 0)
                    {
                        /// <summary>
                        /// Add the star system list if one does not exist.
                        /// </summary>
                        if (_CurrnetFaction.PointDefense.ContainsKey(CurrentSystem) == false)
                        {
                            PointDefenseList PDL = new PointDefenseList();
                            _CurrnetFaction.PointDefense.Add(CurrentSystem, PDL);
                        }

                        /// <summary>
                        /// Add the FC to the point defense FC list.  BFC is ALWAYS false.
                        /// </summary>
                        if (_CurrnetFaction.PointDefense[CurrentSystem].PointDefenseFC.ContainsKey(_CurrnetShip.ShipBFC[_CurrnetFC.componentIndex]) == false)
                        {
                            _CurrnetFaction.PointDefense[CurrentSystem].AddComponent(_CurrnetShip.ShipBFC[_CurrnetFC.componentIndex], _CurrnetShip, false);
                        }
                    }
                    else
                    {
                        /// <summary>
                        /// This bug will pop up if a ship gets transported to another star system and it isn't handled properly.
                        /// </summary>
                        if (_CurrnetFaction.PointDefense.ContainsKey(CurrentSystem) == false)
                        {
                            String Error = String.Format("Star System {0} not found in point defense listing for {1} on {2}.", CurrentSystem, _CurrnetShip.ShipBFC[_CurrnetFC.componentIndex], _CurrnetShip);
                            MessageEntry MessageEnter = new MessageEntry(MessageEntry.MessageType.Error, _CurrnetShip.ShipsTaskGroup.Contact.Position.System, _CurrnetShip.ShipsTaskGroup.Contact,
                                                                  GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, Error);
                            _CurrnetFaction.MessageLog.Add(MessageEnter);
                        }
                        else
                        {
                            /// <summary>
                            /// Remove the FC from the point defense FC list.
                            /// </summary>
                            if (_CurrnetFaction.PointDefense[CurrentSystem].PointDefenseFC.ContainsKey(_CurrnetShip.ShipBFC[_CurrnetFC.componentIndex]) == true)
                            {
                                _CurrnetFaction.PointDefense[CurrentSystem].RemoveComponent(_CurrnetShip.ShipBFC[_CurrnetFC.componentIndex]);
                            }

                            /// <summary>
                            /// cleanup the starsystem so that the point defense list isn't cluttered.
                            /// </summary>
                            if (_CurrnetFaction.PointDefense[CurrentSystem].PointDefenseFC.Count == 0)
                            {
                                _CurrnetFaction.PointDefense.Remove(CurrentSystem);
                            }
                        }
                    }
                }
                else
                {
                    String Error = String.Format("Improper point defense state {0} assigned to BFC {1} on {2}", (PointDefenseState)index, _CurrnetFC, _CurrnetShip);
                    MessageEntry MessageEnter = new MessageEntry(MessageEntry.MessageType.Error, _CurrnetShip.ShipsTaskGroup.Contact.Position.System, _CurrnetShip.ShipsTaskGroup.Contact,
                                                          GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, Error);
                    _CurrnetFaction.MessageLog.Add(MessageEnter);
                }
            }
            else
            {
                if (index != 0)
                    index = index + 3;
                if (index == 0 || (index >= (int)PointDefenseState.AMM1v2 && index <= (int)PointDefenseState.AMM5v1))
                {
                    if (res)
                        _CurrnetShip.ShipMFC[_CurrnetFC.componentIndex].SetPointDefenseRange(PointDefenseRange);
                    _CurrnetShip.ShipMFC[_CurrnetFC.componentIndex].SetPointDefenseMode((PointDefenseState)index);

                    StarSystem CurrentSystem = _CurrnetShip.ShipsTaskGroup.Contact.Position.System;

                    if (index != 0)
                    {
                        /// <summary>
                        /// Add the star system list if one does not exist.
                        /// </summary>
                        if (_CurrnetFaction.PointDefense.ContainsKey(CurrentSystem) == false)
                        {
                            PointDefenseList PDL = new PointDefenseList();
                            _CurrnetFaction.PointDefense.Add(CurrentSystem, PDL);
                        }

                        /// <summary>
                        /// Add the FC to the point defense FC list. MFC is ALWAYS true.
                        /// </summary>
                        if (_CurrnetFaction.PointDefense[CurrentSystem].PointDefenseFC.ContainsKey(_CurrnetShip.ShipMFC[_CurrnetFC.componentIndex]) == false)
                        {
                            _CurrnetFaction.PointDefense[CurrentSystem].AddComponent(_CurrnetShip.ShipMFC[_CurrnetFC.componentIndex], _CurrnetShip, true);
                        }
                    }
                    else
                    {
                        /// <summary>
                        /// This bug will pop up if a ship gets transported to another star system and it isn't handled properly.
                        /// </summary>
                        if (_CurrnetFaction.PointDefense.ContainsKey(CurrentSystem) == false)
                        {
#warning leave this error message in for now
                            String Error = String.Format("Star System {0} not found in point defense listing for {1} on {2}. Not necessarily a bug.", CurrentSystem, _CurrnetShip.ShipMFC[_CurrnetFC.componentIndex], _CurrnetShip);
                            MessageEntry MessageEnter = new MessageEntry(MessageEntry.MessageType.Error, _CurrnetShip.ShipsTaskGroup.Contact.Position.System, _CurrnetShip.ShipsTaskGroup.Contact,
                                                                  GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, Error);
                            _CurrnetFaction.MessageLog.Add(MessageEnter);
                        }

                        /// <summary>
                        /// Remove the FC from the point defense FC list.
                        /// </summary>
                        if (_CurrnetFaction.PointDefense.Count != 0)
                        {
                            if (_CurrnetFaction.PointDefense.ContainsKey(CurrentSystem) == true)
                            {
                                if (_CurrnetFaction.PointDefense[CurrentSystem].PointDefenseFC.ContainsKey(_CurrnetShip.ShipMFC[_CurrnetFC.componentIndex]) == true)
                                {
                                    _CurrnetFaction.PointDefense[CurrentSystem].RemoveComponent(_CurrnetShip.ShipMFC[_CurrnetFC.componentIndex]);
                                }

                                /// <summary>
                                /// Cleanup the starsystem so that the point defense list isn't cluttered.
                                /// </summary>
                                if (_CurrnetFaction.PointDefense[CurrentSystem].PointDefenseFC.Count == 0)
                                {
                                    _CurrnetFaction.PointDefense.Remove(CurrentSystem);
                                }
                            }
                        }


                    }
                }
                else
                {
                    String Error = String.Format("Improper point defense state {0} assigned to MFC {1} on {2}", (PointDefenseState)index, _CurrnetFC, _CurrnetShip);
                    MessageEntry MessageEnter = new MessageEntry(MessageEntry.MessageType.Error, _CurrnetShip.ShipsTaskGroup.Contact.Position.System, _CurrnetShip.ShipsTaskGroup.Contact,
                                                          GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, Error);
                    _CurrnetFaction.MessageLog.Add(MessageEnter);
                }
            }

            BuildCombatSummary();
        }

        /// <summary>
        /// On text range change if anything needs to be done do it here.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PDRangeTextBox_TextChanged(object sender, EventArgs e)
        {
        }
        #endregion

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
                RefreshFCInfo();
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
                RefreshFCInfo();
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
            if (_CurrnetShip.ShipASensor.Count == 0)
            {
                _CurrnetSensor = null;
            }

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
            if (_CurrnetShip.ShipASensor.Count == 0)
            {
                _CurrnetSensor = null;
            }

            if (_CurrnetShip != null && _CurrnetSensor != null)
            {
                _CurrnetShip.ShipsTaskGroup.SetActiveSensor(_CurrnetShip.ShipsTaskGroup.Ships.IndexOf(_CurrnetShip), _CurrnetSensor.componentIndex, false);

                if (_CurrnetSensor.isActive == false || _CurrnetSensor.isDestroyed == true)
                    m_oDetailsPanel.ActiveGroupBox.Text = "Selected Active(Off)";
            }
        }

        /// <summary>
        /// Assign the selected target to the FC.
        /// I have to rebuild the contact list to find out which contact goes with what entry.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AssignTargetButton_Click(object sender, EventArgs e)
        {
#warning Planetary targetting not yet implemented.
            if (m_oDetailsPanel.ContactListBox.SelectedIndex != -1 && _CurrnetFC != null)
            {


                if (isBFC == true)
                {
                    int count = 0;
                    if (_CurrnetFaction.DetectedContactLists.ContainsKey(_CurrnetShip.ShipsTaskGroup.Contact.Position.System) == true)
                    {

                        foreach (KeyValuePair<ShipTN, FactionContact> pair in _CurrnetFaction.DetectedContactLists[_CurrnetShip.ShipsTaskGroup.Contact.Position.System].DetectedContacts)
                        {
                            if (pair.Value.active == true)
                            {
                                if (count == m_oDetailsPanel.ContactListBox.SelectedIndex)
                                {
                                    _CurrnetShip.ShipBFC[_CurrnetFC.componentIndex].assignTarget(pair.Key);
                                    pair.Key.ShipsTargetting.Add(_CurrnetShip);
                                    count++;
                                    break;
                                }
                                count++;
                            }
                            else
                            {
                                if (count == m_oDetailsPanel.ContactListBox.SelectedIndex)
                                {
                                    /// <summary>
                                    /// not a valid assignment. just exit loop.
                                    /// </summary>
                                    count++;
                                    break;
                                }
                                count++;
                            }
                        }

                        if (count <= m_oDetailsPanel.ContactListBox.SelectedIndex)
                        {
                            foreach (KeyValuePair<OrdnanceGroupTN, FactionContact> pair in _CurrnetFaction.DetectedContactLists[_CurrnetShip.ShipsTaskGroup.Contact.Position.System].DetectedMissileContacts)
                            {
                                if (pair.Value.active == true)
                                {
                                    if (count == m_oDetailsPanel.ContactListBox.SelectedIndex)
                                    {
                                        _CurrnetShip.ShipBFC[_CurrnetFC.componentIndex].assignTarget(pair.Key);
                                        pair.Key.shipsTargetting.Add(_CurrnetShip);
                                        count++;
                                        break;
                                    }
                                    count++;
                                }
                                else
                                {
                                    if (count == m_oDetailsPanel.ContactListBox.SelectedIndex)
                                    {
                                        /// <summary>
                                        /// not a valid assignment. just exit loop.
                                        /// </summary>
                                        count++;
                                        break;
                                    }
                                    count++;
                                }
                            }
                        }
                    }
                }
                else
                {
                    int count = 0;
                    if (_CurrnetFaction.DetectedContactLists.ContainsKey(_CurrnetShip.ShipsTaskGroup.Contact.Position.System) == true)
                    {
                        foreach (KeyValuePair<ShipTN, FactionContact> pair in _CurrnetFaction.DetectedContactLists[_CurrnetShip.ShipsTaskGroup.Contact.Position.System].DetectedContacts)
                        {
                            float distance;
                            _CurrnetShip.ShipsTaskGroup.Contact.DistTable.GetDistance(pair.Key.ShipsTaskGroup.Contact, out distance);
                            int TCS = pair.Key.TotalCrossSection;
                            int detectFactor = _CurrnetShip.ShipMFC[_CurrnetFC.componentIndex].mFCSensorDef.GetActiveDetectionRange(TCS, -1);

                            bool det = _CurrnetShip.ShipsFaction.LargeDetection(distance, detectFactor);

                            /// <summary>
                            /// if det is not true then this contact does not appear in the contact list.
                            /// </summary>
                            if (det == true)
                            {
                                /// <summary>
                                /// Only active detection allows for tracking, EM and thermal contacts can't be targeted but will be displayed.
                                /// </summary>
                                if (pair.Value.active == true)
                                {
                                    if (count == m_oDetailsPanel.ContactListBox.SelectedIndex)
                                    {
                                        _CurrnetShip.ShipMFC[_CurrnetFC.componentIndex].assignTarget(pair.Key);
                                        pair.Key.ShipsTargetting.Add(_CurrnetShip);
                                        count++;
                                        break;
                                    }
                                    count++;
                                }
                                else
                                {
                                    if (count == m_oDetailsPanel.ContactListBox.SelectedIndex)
                                    {
                                        /// <summary>
                                        /// not a valid assignment. just exit loop.
                                        /// </summary>
                                        count++;
                                        break;
                                    }
                                    count++;
                                }
                            }
                        }

                        if (count <= m_oDetailsPanel.ContactListBox.SelectedIndex)
                        {
                            foreach (KeyValuePair<OrdnanceGroupTN, FactionContact> pair in _CurrnetFaction.DetectedContactLists[_CurrnetShip.ShipsTaskGroup.Contact.Position.System].DetectedMissileContacts)
                            {
                                float distance;
                                _CurrnetShip.ShipsTaskGroup.Contact.DistTable.GetDistance(pair.Key.contact, out distance);
                                int MSP = (int)Math.Ceiling(pair.Key.missiles[0].missileDef.size);
                                int sig = -1;
                                int detectFactor = -1;
                                if (MSP <= ((Constants.OrdnanceTN.MissileResolutionMaximum + 6) + 1))
                                {
                                    if (MSP <= (Constants.OrdnanceTN.MissileResolutionMinimum + 6))
                                    {
                                        sig = Constants.OrdnanceTN.MissileResolutionMinimum;
                                    }
                                    else if (MSP <= (Constants.OrdnanceTN.MissileResolutionMaximum + 6))
                                    {
                                        sig = MSP - 6;
                                    }
                                    detectFactor = _CurrnetShip.ShipMFC[_CurrnetFC.componentIndex].mFCSensorDef.GetActiveDetectionRange(0, sig);
                                }
                                else
                                {
                                    /// <summary>
                                    /// Big missiles will be treated in HS terms: 21-40 MSP = 2 HS, 41-60 = 3 HS, 61-80 = 4 HS, 81-100 = 5 HS. The same should hold true for greater than 100 sized missiles.
                                    /// but those are impossible to build.
                                    /// </summary>
                                    sig = (int)Math.Ceiling((float)MSP / 20.0f);
                                    detectFactor = _CurrnetShip.ShipMFC[_CurrnetFC.componentIndex].mFCSensorDef.GetActiveDetectionRange(sig, -1);
                                }

                                bool det = _CurrnetShip.ShipsFaction.LargeDetection(distance, detectFactor);

                                /// <summary>
                                /// if det is not true then this contact will not appear in the contact list.
                                /// </summary>
                                if (det == true)
                                {
                                    /// <summary>
                                    /// Only active detection allows for tracking, EM and thermal contacts can't be targeted but will be displayed.
                                    /// </summary>
                                    if (pair.Value.active == true)
                                    {
                                        if (count == m_oDetailsPanel.ContactListBox.SelectedIndex)
                                        {
                                            _CurrnetShip.ShipMFC[_CurrnetFC.componentIndex].assignTarget(pair.Key);
                                            pair.Key.shipsTargetting.Add(_CurrnetShip);
                                            count++;
                                            break;
                                        }
                                        count++;
                                    }
                                    else
                                    {
                                        if (count == m_oDetailsPanel.ContactListBox.SelectedIndex)
                                        {
                                            /// <summary>
                                            /// not a valid assignment. just exit loop.
                                            /// </summary>
                                            count++;
                                            break;
                                        }
                                        count++;
                                    }
                                }
                            }//end foreach contact
                        }//end if count < selection
                    }//end if detectedlist contains system
                }//end else if not BFC
            }//end if FC and there is a selection.
            BuildCombatSummary();
            RefreshFCInfo();
        }

        /// <summary>
        /// Clears the selected FC of its target and any PD state.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearTargetButton_Click(object sender, EventArgs e)
        {
            if (_CurrnetFC != null)
            {
                if (isBFC == true)
                {
                    /// <summary>
                    /// Clear point defense.
                    /// </summary>
                    _CurrnetShip.ShipBFC[_CurrnetFC.componentIndex].SetPointDefenseMode(PointDefenseState.None);

                    StarSystem CurrentSystem = _CurrnetShip.ShipsTaskGroup.Contact.Position.System;

                    /// <summary>
                    /// This FC might not necessarily be in the point defense list at all, this is just a precautionary check.
                    /// </summary>
                    if (_CurrnetFaction.PointDefense.ContainsKey(CurrentSystem) == true)
                    {
                        /// <summary>
                        /// Remove the FC from the point defense FC list.
                        /// </summary>
                        if (_CurrnetFaction.PointDefense[CurrentSystem].PointDefenseFC.ContainsKey(_CurrnetShip.ShipBFC[_CurrnetFC.componentIndex]) == true)
                        {
                            _CurrnetFaction.PointDefense[CurrentSystem].RemoveComponent(_CurrnetShip.ShipBFC[_CurrnetFC.componentIndex]);
                        }

                        /// <summary>
                        /// cleanup the starsystem so that the point defense list isn't cluttered.
                        /// </summary>
                        if (_CurrnetFaction.PointDefense[CurrentSystem].PointDefenseFC.Count == 0)
                        {
                            _CurrnetFaction.PointDefense.Remove(CurrentSystem);
                        }
                    }



                    TargetTN Target = _CurrnetShip.ShipBFC[_CurrnetFC.componentIndex].getTarget();
                    if (Target != null)
                    {
                        switch (Target.targetType)
                        {
                            case StarSystemEntityType.Population:
                                /// <summary>
                                /// Populations are not yet implemented. I may have a shipsTargetting list for population as well, in which case remove that here.
                                /// If I don't do that however, just delete this.
                                /// </summary>
#warning Pop Not Yet Implemented
                                break;
                            case StarSystemEntityType.TaskGroup:
                                ShipTN vessel = Target.ship;
                                if (vessel.ShipsTargetting.Contains(_CurrnetShip) == true)
                                {
                                    vessel.ShipsTargetting.Remove(_CurrnetShip);
                                }
                                break;
                            case StarSystemEntityType.Missile:
                                OrdnanceGroupTN MissileGroup = Target.missileGroup;
                                if (MissileGroup.shipsTargetting.Contains(_CurrnetShip) == true)
                                {
                                    MissileGroup.shipsTargetting.Remove(_CurrnetShip);
                                }
                                break;
                        }
                    }
                    _CurrnetShip.ShipBFC[_CurrnetFC.componentIndex].clearTarget();
                }
                else
                {
                    /// <summary>
                    /// Clear point defense.
                    /// </summary>
                    _CurrnetShip.ShipMFC[_CurrnetFC.componentIndex].SetPointDefenseMode(PointDefenseState.None);

                    StarSystem CurrentSystem = _CurrnetShip.ShipsTaskGroup.Contact.Position.System;

                    /// <summary>
                    /// This FC might not necessarily be in the point defense list at all, this is just a precautionary check.
                    /// </summary>
                    if (_CurrnetFaction.PointDefense.ContainsKey(CurrentSystem) == true)
                    {
                        /// <summary>
                        /// Remove the FC from the point defense FC list.
                        /// </summary>
                        if (_CurrnetFaction.PointDefense[CurrentSystem].PointDefenseFC.ContainsKey(_CurrnetShip.ShipMFC[_CurrnetFC.componentIndex]) == true)
                        {
                            _CurrnetFaction.PointDefense[CurrentSystem].RemoveComponent(_CurrnetShip.ShipMFC[_CurrnetFC.componentIndex]);
                        }

                        /// <summary>
                        /// cleanup the starsystem so that the point defense list isn't cluttered.
                        /// </summary>
                        if (_CurrnetFaction.PointDefense[CurrentSystem].PointDefenseFC.Count == 0)
                        {
                            _CurrnetFaction.PointDefense.Remove(CurrentSystem);
                        }
                    }

                    TargetTN Target = _CurrnetShip.ShipMFC[_CurrnetFC.componentIndex].getTarget();
                    if (Target != null)
                    {
                        switch (Target.targetType)
                        {
                            case StarSystemEntityType.Population:
                                /// <summary>
                                /// Populations are not yet implemented. I may have a shipsTargetting list for population as well, in which case remove that here.
                                /// If I don't do that however, just delete this.
                                /// </summary>
#warning Pop Not Yet Implemented
                                break;
                            case StarSystemEntityType.TaskGroup:
                                ShipTN vessel = Target.ship;
                                if (vessel.ShipsTargetting.Contains(_CurrnetShip) == true)
                                {
                                    vessel.ShipsTargetting.Remove(_CurrnetShip);
                                }
                                break;
                            case StarSystemEntityType.Missile:
                                OrdnanceGroupTN MissileGroup = Target.missileGroup;
                                if (MissileGroup.shipsTargetting.Contains(_CurrnetShip) == true)
                                {
                                    MissileGroup.shipsTargetting.Remove(_CurrnetShip);
                                }
                                break;
                            case StarSystemEntityType.Waypoint:
#warning Waypoint and Body may need additional work as well.
                                break;
                            case StarSystemEntityType.Body:
                                break;
                        }

                        if (_CurrnetShip.ShipMFC[_CurrnetFC.componentIndex].getTarget().targetType == StarSystemEntityType.TaskGroup)
                        {
                            ShipTN vessel = _CurrnetShip.ShipMFC[_CurrnetFC.componentIndex].getTarget().ship;
                            if (vessel.ShipsTargetting.Contains(_CurrnetShip) == true)
                            {
                                vessel.ShipsTargetting.Remove(_CurrnetShip);
                            }
                        }

                        _CurrnetShip.ShipMFC[_CurrnetFC.componentIndex].clearTarget();
                    }
                }
            }
            BuildCombatSummary();
            RefreshFCInfo();
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
                    foreach (int Index in m_oDetailsPanel.WeaponListBox.SelectedIndices)
                    {
                        if (Index < _CurrnetShip.ShipBeam.Count)
                        {
                            BeamTN SelectedBeam = _CurrnetShip.ShipBeam[Index];
                            if (SelectedBeam.fireController != null)
                            {
                                BeamFireControlTN BFC = SelectedBeam.fireController;
                                BFC.unlinkWeapon(SelectedBeam);
                            }
                            _CurrnetShip.ShipBFC[_CurrnetFC.componentIndex].linkWeapon(SelectedBeam);
                        }
                        else
                        {
                            TurretTN SelectedTurret = _CurrnetShip.ShipTurret[Index];
                            if (SelectedTurret.fireController != null)
                            {
                                BeamFireControlTN BFC = SelectedTurret.fireController;
                                BFC.unlinkWeapon(SelectedTurret);
                            }
                            _CurrnetShip.ShipBFC[_CurrnetFC.componentIndex].linkWeapon(SelectedTurret);
                        }
                    }
                }
                else
                {
                    foreach (int Index in m_oDetailsPanel.WeaponListBox.SelectedIndices)
                    {
                        MissileLauncherTN SelectedTube = _CurrnetShip.ShipMLaunchers[Index];

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
            RefreshFCInfo();
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

                    for (int loop = 0; loop < _CurrnetShip.ShipTurret.Count; loop++)
                    {
                        TurretTN SelectedTurret = _CurrnetShip.ShipTurret[loop];
                        if (SelectedTurret.fireController != null)
                        {
                            BeamFireControlTN BFC = SelectedTurret.fireController;
                            BFC.unlinkWeapon(SelectedTurret);
                        }
                        _CurrnetShip.ShipBFC[_CurrnetFC.componentIndex].linkWeapon(SelectedTurret);
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
            RefreshFCInfo();
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
                        /// <summary>
                        /// Armor Damage is Healthy = 4, 1 damage = 3, 2 damage = 2, 3 damage = 1, and 4 damage = 4.
                        /// count down from health.
                        /// </summary>
                        for (int loop = (_CurrnetShip.ShipArmor.armorDef.depth - pair.Value - 1); loop >= 0; loop--)
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
                    TargetTN Target = _CurrnetShip.ShipBFC[loop].getTarget();

                    String PD = "";
                    if (_CurrnetShip.ShipBFC[loop].pDState != PointDefenseState.None)
                        PD = String.Format(" ({0} {1:N1})", _CurrnetShip.ShipBFC[loop].pDState, _CurrnetShip.ShipBFC[loop].pDRange);

                    if (Target == null)
                    {
                        Entry = String.Format("{0}{1}: No Target Assignment\n", _CurrnetShip.ShipBFC[loop].Name, PD);
                    }
                    else
                    {
                        if (_CurrnetShip.ShipBFC[loop].openFire == true)
                            fireAuth = "Weapons Firing";
                        else
                            fireAuth = "Holding Fire";

                        String TargetName = "N/A";
                        switch (Target.targetType)
                        {
                            case StarSystemEntityType.Population:
                                TargetName = Target.pop.Name;
                                break;
                            case StarSystemEntityType.TaskGroup:
                                TargetName = Target.ship.Name;
                                break;
                            case StarSystemEntityType.Missile:
                                TargetName = Target.missileGroup.Name;
                                break;

                            /// <summary>
                            /// BFCs can't target these.
                            /// </summary>
                            case StarSystemEntityType.Waypoint:
                                break;
                            case StarSystemEntityType.Body:
                                break;
                        }

                        Entry = String.Format("{0}{1}: Targeting {2} - {3}\n", _CurrnetShip.ShipBFC[loop].Name, PD, TargetName, fireAuth);
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
                    for (int loop2 = 0; loop2 < _CurrnetShip.ShipBFC[loop].linkedTurrets.Count; loop2++)
                    {
                        if (_CurrnetShip.ShipBFC[loop].linkedTurrets[loop2].currentCapacitor == _CurrnetShip.ShipBFC[loop].linkedTurrets[loop2].turretDef.powerRequirement)
                        {
                            Entry = String.Format("{0}: (Ready to Fire)\n", _CurrnetShip.ShipBFC[loop].linkedTurrets[loop2].Name);
                            m_oDetailsPanel.CombatSummaryTextBox.AppendText(Entry);
                        }
                        else
                        {
                            Entry = String.Format("{0}: ({1} / {2} power recharged)\n", _CurrnetShip.ShipBFC[loop].linkedTurrets[loop2].Name,
                                                                        _CurrnetShip.ShipBFC[loop].linkedTurrets[loop2].currentCapacitor.ToString(), _CurrnetShip.ShipBFC[loop].linkedTurrets[loop2].turretDef.powerRequirement.ToString());
                            m_oDetailsPanel.CombatSummaryTextBox.AppendText(Entry);
                        }
                    }
                    m_oDetailsPanel.CombatSummaryTextBox.AppendText("\n");
                }

                for (int loop = 0; loop < _CurrnetShip.ShipMFC.Count; loop++)
                {
                    TargetTN Target = _CurrnetShip.ShipMFC[loop].getTarget();

                    String PD = "";
                    if (_CurrnetShip.ShipMFC[loop].pDState != PointDefenseState.None)
                        PD = String.Format(" ({0} {1:N1})", _CurrnetShip.ShipMFC[loop].pDState, _CurrnetShip.ShipMFC[loop].pDRange);

                    if (Target == null)
                    {
                        Entry = String.Format("{0}{1}: No Target Assignment\n", _CurrnetShip.ShipMFC[loop].Name, PD);
                    }
                    else
                    {
                        if (_CurrnetShip.ShipMFC[loop].openFire == true)
                            fireAuth = "Weapons Firing";
                        else
                            fireAuth = "Holding Fire";

                        switch (Target.targetType)
                        {
                            case StarSystemEntityType.Population:
                                Entry = String.Format("{0}{1}: {2} - {3}\n", _CurrnetShip.ShipMFC[loop].Name, PD, Target.pop.Name, fireAuth);
                                break;
                            case StarSystemEntityType.TaskGroup:
                                Entry = String.Format("{0}{1}: {2} - {3}\n", _CurrnetShip.ShipMFC[loop].Name, PD, Target.ship.Name, fireAuth);
                                break;
                            case StarSystemEntityType.Missile:
                                Entry = String.Format("{0}{1}: {2} - {3}\n", _CurrnetShip.ShipMFC[loop].Name, PD, Target.missileGroup.Name, fireAuth);
                                break;
                            case StarSystemEntityType.Waypoint:
                                Entry = String.Format("{0}{1}: {2} - {3}\n", _CurrnetShip.ShipMFC[loop].Name, PD, Target.wp.Name, fireAuth);
                                break;
                            case StarSystemEntityType.Body:
                                Entry = String.Format("{0}{1}: {2} - {3}\n", _CurrnetShip.ShipMFC[loop].Name, PD, Target.body.Name, fireAuth);
                                break;
                        }
                    }

                    m_oDetailsPanel.CombatSummaryTextBox.AppendText(Entry);

                    for (int loop2 = 0; loop2 < _CurrnetShip.ShipMFC[loop].linkedWeapons.Count; loop2++)
                    {
                        Entry = String.Format("{0}", _CurrnetShip.ShipMFC[loop].linkedWeapons[loop2].Name);
                        if (_CurrnetShip.ShipMFC[loop].linkedWeapons[loop2].loadedOrdnance != null)
                        {
                            Entry = String.Format("{0} - {1}:", Entry, _CurrnetShip.ShipMFC[loop].linkedWeapons[loop2].loadedOrdnance.Name);
                        }
                        if (_CurrnetShip.ShipMFC[loop].linkedWeapons[loop2].readyToFire() == true)
                        {
                            Entry = String.Format("{0} (Ready to Fire)\n", Entry);
                        }
                        else
                        {
                            Entry = String.Format("{0} ({1} secs to reload)\n", Entry, _CurrnetShip.ShipMFC[loop].linkedWeapons[loop2].loadTime);
                        }

                        m_oDetailsPanel.CombatSummaryTextBox.AppendText(Entry);
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

                        if (_CurrnetShip.ShipMLaunchers[loop].readyToFire() == true)
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
                m_oDetailsPanel.ClassDesignTextBox.Text = _CurrnetShip.ShipClass.Summary;
            }
        }

        /// <summary>
        /// Print the names of every weapon on this ship.
        /// </summary>
        private void BuildWeaponList()
        {
            String Entry = "N/A";
            m_oDetailsPanel.WeaponListBox.Items.Clear();

            if (_CurrnetShip != null && _CurrnetFC != null)
            {

                if (isBFC == true)
                {
                    for (int loop = 0; loop < _CurrnetShip.ShipBeam.Count; loop++)
                    {
                        Entry = String.Format("{0}", _CurrnetShip.ShipBeam[loop].Name);
                        if (_CurrnetShip.ShipBeam[loop].fireController != null)
                            Entry = String.Format("{0} - {1}", Entry, _CurrnetShip.ShipBeam[loop].fireController.Name);
                        m_oDetailsPanel.WeaponListBox.Items.Add(Entry);
                    }

                    for (int loop = 0; loop < _CurrnetShip.ShipTurret.Count; loop++)
                    {
                        Entry = String.Format("{0}", _CurrnetShip.ShipTurret[loop].Name);
                        if (_CurrnetShip.ShipTurret[loop].fireController != null)
                            Entry = String.Format("{0} - {1}", Entry, _CurrnetShip.ShipTurret[loop].fireController.Name);
                        m_oDetailsPanel.WeaponListBox.Items.Add(Entry);
                    }
                }
                else
                {
                    for (int loop = 0; loop < _CurrnetShip.ShipMLaunchers.Count; loop++)
                    {
                        Entry = String.Format("{0}", _CurrnetShip.ShipMLaunchers[loop].Name);
                        if (_CurrnetShip.ShipMLaunchers[loop].mFC != null)
                            Entry = String.Format("{0} - {1}", Entry, _CurrnetShip.ShipMLaunchers[loop].mFC.Name);
                        m_oDetailsPanel.WeaponListBox.Items.Add(Entry);
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
                    if (_CurrnetFaction.DetectedContactLists.ContainsKey(_CurrnetShip.ShipsTaskGroup.Contact.Position.System) == true)
                    {
                        #region BFC contacts, All are printed.
                        foreach (KeyValuePair<ShipTN, FactionContact> pair in _CurrnetFaction.DetectedContactLists[_CurrnetShip.ShipsTaskGroup.Contact.Position.System].DetectedContacts)
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

                        foreach (KeyValuePair<OrdnanceGroupTN, FactionContact> pair in _CurrnetFaction.DetectedContactLists[_CurrnetShip.ShipsTaskGroup.Contact.Position.System].DetectedMissileContacts)
                        {
                            if (pair.Key.missiles.Count == 0)
                            {
                                String Error = String.Format("BuildContactList has an empty missileGroup in detectedMissileContacts.");
                                MessageEntry MessageEnter = new MessageEntry(MessageEntry.MessageType.Error, _CurrnetShip.ShipsTaskGroup.Contact.Position.System, _CurrnetShip.ShipsTaskGroup.Contact,
                                                                      GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, Error);
                                _CurrnetFaction.MessageLog.Add(MessageEnter);
                                continue;
                            }

                            String TH = "";
                            if (pair.Value.thermal == true)
                            {
                                TH = String.Format("[Thermal {0}]", (int)Math.Ceiling(pair.Key.missiles[0].missileDef.totalThermalSignature));
                            }

                            String EM = "";
                            if (pair.Value.EM == true)
                            {
                                if (pair.Key.missiles[0].missileDef.aSD != null)
                                {
                                    EM = String.Format("[EM {0}]", pair.Key.missiles[0].missileDef.aSD.gps);
                                }
                                else
                                {
                                    String Error = String.Format("BuildContactList has a missile detected via EM that has no Active sensor(which is the only way it can be detected via EM)");
                                    MessageEntry MessageEnter = new MessageEntry(MessageEntry.MessageType.Error, _CurrnetShip.ShipsTaskGroup.Contact.Position.System, _CurrnetShip.ShipsTaskGroup.Contact,
                                                                          GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, Error);
                                    _CurrnetFaction.MessageLog.Add(MessageEnter);
                                }

                            }

                            String ACT = "";
                            if (pair.Value.active == true)
                            {
                                ACT = String.Format("[ACT {0}]", (int)Math.Ceiling(pair.Key.missiles[0].missileDef.size));
                            }

                            String Entry = String.Format("{0} {1}{2}{3} x{4}", pair.Key.Name, TH, EM, ACT, pair.Key.missiles.Count);

                            m_oDetailsPanel.ContactListBox.Items.Add(Entry);
                        }
                        #endregion
                    }
                }
                else
                {
                    /// <summary>
                    /// Each MFC entry will be range checked, also there have been some tick errors, this may be the place to hunt them down.
                    /// </summary>
                    if (_CurrnetFaction.DetectedContactLists.ContainsKey(_CurrnetShip.ShipsTaskGroup.Contact.Position.System) == true)
                    {
                        foreach (KeyValuePair<ShipTN, FactionContact> pair in _CurrnetFaction.DetectedContactLists[_CurrnetShip.ShipsTaskGroup.Contact.Position.System].DetectedContacts)
                        {
                            float distance;
                            _CurrnetShip.ShipsTaskGroup.Contact.DistTable.GetDistance(pair.Key.ShipsTaskGroup.Contact, out distance);

                            int TCS = pair.Key.TotalCrossSection;
                            int detectFactor = _CurrnetShip.ShipMFC[_CurrnetFC.componentIndex].mFCSensorDef.GetActiveDetectionRange(TCS, -1);

                            bool det = _CurrnetShip.ShipsFaction.LargeDetection(distance, detectFactor);

                            if (det == true)
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

                        foreach (KeyValuePair<OrdnanceGroupTN, FactionContact> pair in _CurrnetFaction.DetectedContactLists[_CurrnetShip.ShipsTaskGroup.Contact.Position.System].DetectedMissileContacts)
                        {
                            if (pair.Key.missiles.Count == 0)
                            {
                                String Error = String.Format("BuildContactList has an empty missileGroup in detectedMissileContacts.");
                                MessageEntry MessageEnter = new MessageEntry(MessageEntry.MessageType.Error, _CurrnetShip.ShipsTaskGroup.Contact.Position.System, _CurrnetShip.ShipsTaskGroup.Contact,
                                                                      GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, Error);
                                _CurrnetFaction.MessageLog.Add(MessageEnter);
                                continue;
                            }

                            float distance;
                            _CurrnetShip.ShipsTaskGroup.Contact.DistTable.GetDistance(pair.Key.contact, out distance);

                            int MSP = (int)Math.Ceiling(pair.Key.missiles[0].missileDef.size);
                            int sig = -1;
                            int detectFactor = 0;

                            /// <summary>
                            /// Missile detection goes from 1-6 = minimum, 7-19 MSP specific, 20 and above treated as HS
                            /// </summary>
                            if (MSP <= ((Constants.OrdnanceTN.MissileResolutionMaximum + 6) + 1))
                            {
                                if (MSP <= (Constants.OrdnanceTN.MissileResolutionMinimum + 6))
                                {
                                    sig = Constants.OrdnanceTN.MissileResolutionMinimum;
                                }
                                else if (MSP <= (Constants.OrdnanceTN.MissileResolutionMaximum + 6))
                                {
                                    sig = MSP - 6;
                                }
                                detectFactor = _CurrnetShip.ShipMFC[_CurrnetFC.componentIndex].mFCSensorDef.GetActiveDetectionRange(0, sig);
                            }
                            else
                            {
                                /// <summary>
                                /// Big missiles will be treated in HS terms: 21-40 MSP = 2 HS, 41-60 = 3 HS, 61-80 = 4 HS, 81-100 = 5 HS. The same should hold true for greater than 100 sized missiles.
                                /// but those are impossible to build.
                                /// </summary>
                                sig = (int)Math.Ceiling((float)MSP / 20.0f);
                                detectFactor = _CurrnetShip.ShipMFC[_CurrnetFC.componentIndex].mFCSensorDef.GetActiveDetectionRange(sig, -1);
                            }

                            bool det = _CurrnetShip.ShipsFaction.LargeDetection(distance, detectFactor);

                            if (det == true)
                            {

                                String TH = "";
                                if (pair.Value.thermal == true)
                                {
                                    TH = String.Format("[Thermal {0}]", (int)Math.Ceiling(pair.Key.missiles[0].missileDef.totalThermalSignature));
                                }

                                String EM = "";
                                if (pair.Value.EM == true)
                                {
                                    if (pair.Key.missiles[0].missileDef.aSD != null)
                                    {
                                        EM = String.Format("[EM {0}]", pair.Key.missiles[0].missileDef.aSD.gps);
                                    }
                                    else
                                    {
                                        String Error = String.Format("BuildContactList has a missile detected via EM that has no Active sensor(which is the only way it can be detected via EM)");
                                        MessageEntry MessageEnter = new MessageEntry(MessageEntry.MessageType.Error, _CurrnetShip.ShipsTaskGroup.Contact.Position.System, _CurrnetShip.ShipsTaskGroup.Contact,
                                                                              GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, Error);
                                        _CurrnetFaction.MessageLog.Add(MessageEnter);
                                    }

                                }

                                String ACT = "";
                                if (pair.Value.active == true)
                                {
                                    ACT = String.Format("[ACT {0}]", (int)Math.Ceiling(pair.Key.missiles[0].missileDef.size));
                                }

                                String Entry = String.Format("{0} {1}{2}{3} x{4}", pair.Key.Name, TH, EM, ACT, pair.Key.missiles.Count);

                                m_oDetailsPanel.ContactListBox.Items.Add(Entry);
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

#warning Message log code here with a magic number
            if (_CurrnetFaction.MessageLog.Count > 200)
            {
                while (_CurrnetFaction.MessageLog.Count > 200)
                {
                    _CurrnetFaction.MessageLog.RemoveAt(0);
                }
            }


            for (int loop = 0; loop < _CurrnetFaction.MessageLog.Count; loop++)
            {
                String Entry = String.Format("{0} | {1} | {2} - {3}: {4}\n", _CurrnetFaction.MessageLog[loop].TypeOf, _CurrnetFaction.MessageLog[loop].TimeOfMessage, _CurrnetFaction.MessageLog[loop].Location, _CurrnetFaction.MessageLog[loop].TimeSlice, _CurrnetFaction.MessageLog[loop].Text);

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
            /// General Area info:
            /// </summary>
            m_oDetailsPanel.MaxShieldTextBox.Text = Math.Floor(_CurrnetShip.ShipClass.TotalShieldPool).ToString();
            m_oDetailsPanel.CurShieldTextBox.Text = Math.Floor(_CurrnetShip.CurrentShieldPool).ToString();

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

            /// <summary>
            /// Ordnance Tab:
            /// </summary>
            BuildOrdnanceManagementTab();
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

        #region Ordnance Management

        /// <summary>
        /// Handle button clicks and such
        /// </summary>

        /// <summary>
        /// Provide SM reload of current ship
        /// </summary>
        private void StandardReloadButton_Click(object sender, EventArgs e)
        {
            if (_CurrnetShip.ShipClass.ShipClassOrdnance.Count != 0)
            {
                _CurrnetShip.ShipOrdnance.Clear();
                foreach (KeyValuePair<OrdnanceDefTN, int> pair in _CurrnetShip.ShipClass.ShipClassOrdnance)
                {
                    _CurrnetShip.ShipOrdnance.Add(pair.Key, pair.Value);
                }

                _CurrnetShip.CurrentMagazineCapacity = _CurrnetShip.ShipClass.PreferredOrdnanceSize;

                BuildOrdnanceManagementTab();
            }
        }

        /// <summary>
        /// Assign Selected ordnance to selected launch tubes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AssignTubeButton_Click(object sender, EventArgs e)
        {
            if (m_oDetailsPanel.CurrentMagazineListBox.SelectedIndex != -1 && m_oDetailsPanel.CurrentMagazineListBox.SelectedIndex < (m_oDetailsPanel.CurrentMagazineListBox.Items.Count - 1))
            {
                int count = 0;
                OrdnanceDefTN SelectedOrdnance = null;
                foreach (KeyValuePair<OrdnanceDefTN, int> pair in _CurrnetShip.ShipOrdnance)
                {
                    if (count == m_oDetailsPanel.CurrentMagazineListBox.SelectedIndex)
                    {
                        SelectedOrdnance = pair.Key;
                        break;
                    }
                    count++;
                }
                foreach (int Index in m_oDetailsPanel.LaunchTubeListBox.SelectedIndices)
                {
                    if (_CurrnetShip.ShipMLaunchers[Index].missileLauncherDef.launchMaxSize >= (int)Math.Ceiling(SelectedOrdnance.size))
                        _CurrnetShip.ShipMLaunchers[Index].loadedOrdnance = SelectedOrdnance;
                }

                BuildOrdnanceManagementTab();
                BuildCombatSummary();
            }
        }

        /// <summary>
        /// Assign Selected ordnance to all launch tubes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AssignAllTubesButton_Click(object sender, EventArgs e)
        {
            if (m_oDetailsPanel.CurrentMagazineListBox.SelectedIndex != -1 && m_oDetailsPanel.CurrentMagazineListBox.SelectedIndex < (m_oDetailsPanel.CurrentMagazineListBox.Items.Count - 1))
            {
                int count = 0;
                OrdnanceDefTN SelectedOrdnance = null;
                foreach (KeyValuePair<OrdnanceDefTN, int> pair in _CurrnetShip.ShipOrdnance)
                {
                    if (count == m_oDetailsPanel.CurrentMagazineListBox.SelectedIndex)
                    {
                        SelectedOrdnance = pair.Key;
                        break;
                    }
                    count++;
                }
                for (int loop = 0; loop < _CurrnetShip.ShipMLaunchers.Count; loop++)
                {
                    if (_CurrnetShip.ShipMLaunchers[loop].missileLauncherDef.launchMaxSize >= (int)Math.Ceiling(SelectedOrdnance.size))
                        _CurrnetShip.ShipMLaunchers[loop].loadedOrdnance = SelectedOrdnance;
                }

                BuildOrdnanceManagementTab();
                BuildCombatSummary();
            }
        }

        /// <summary>
        /// Clears all launch tubes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearAllTubesButton_Click(object sender, EventArgs e)
        {
            for (int loop = 0; loop < _CurrnetShip.ShipMLaunchers.Count; loop++)
            {
                _CurrnetShip.ShipMLaunchers[loop].loadedOrdnance = null;
            }

            BuildOrdnanceManagementTab();
            BuildCombatSummary();
        }

        /// <summary>
        /// Build the Ordnance Tab
        /// </summary>
        private void BuildOrdnanceManagementTab()
        {
            BuildCurrentMagazine();
            BuildTargetMagazine();
            BuildLaunchTubeList();
        }

        /// <summary>
        /// Prints out the contents of the current ship magazine.
        /// </summary>
        private void BuildCurrentMagazine()
        {
            String Entry = "N/A";
            m_oDetailsPanel.CurrentMagazineListBox.Items.Clear();
            foreach (KeyValuePair<OrdnanceDefTN, int> pair in _CurrnetShip.ShipOrdnance)
            {
                Entry = String.Format("{0}x {1}", pair.Value, pair.Key.Name);
                m_oDetailsPanel.CurrentMagazineListBox.Items.Add(Entry);
            }

            Entry = String.Format("Mg Capacity Used: {0}/{1}", _CurrnetShip.CurrentMagazineCapacity, _CurrnetShip.CurrentMagazineCapacityMax);
            m_oDetailsPanel.CurrentMagazineListBox.Items.Add(Entry);
        }

        /// <summary>
        /// Not Yet Implemented
        /// </summary>
        private void BuildTargetMagazine()
        {
#warning build target magazine not yet implemented. this is for ordnance transfer purposes.
        }

        /// <summary>
        /// Prints out all launch tubes and their selected ordnance loads.
        /// </summary>
        private void BuildLaunchTubeList()
        {
            String Entry = "N/A";
            m_oDetailsPanel.LaunchTubeListBox.Items.Clear();
            foreach (MissileLauncherTN Tube in _CurrnetShip.ShipMLaunchers)
            {
                Entry = String.Format("{0}", Tube.Name);

                if (Tube.loadedOrdnance != null)
                {
                    Entry = String.Format("{0} - {1}", Entry, Tube.loadedOrdnance.Name);
                }

                m_oDetailsPanel.LaunchTubeListBox.Items.Add(Entry);
            }
        }
        #endregion
        #endregion
    }
}
