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
using Pulsar4X.Stargen;

#if LOG4NET_ENABLED
using log4net.Config;
using log4net;
#endif

using Newtonsoft.Json;
using Pulsar4X.Entities.Components;


/// <summary>
/// BuildDesignTab and getListBoxComponents both need to be updated if a component is added in addition to everything else.
/// </summary>
namespace Pulsar4X.UI.Handlers
{
    public class ClassDesign
    {

        /// <summary>
        /// Current faction, need this to update display under all circumstances I think.
        /// </summary>
        private Faction _CurrnetFaction;
        public Faction CurrentFaction
        {
            get { return _CurrnetFaction; }
            set
            {

                if (_CurrnetFaction != value)
                {
                    _CurrnetFaction = value;
                    if (_CurrnetFaction.ShipDesigns.Count != 0)
                    {
                        _CurrnetShipClass = _CurrnetFaction.ShipDesigns[0];
                        UpdateDisplay();
                    }
                }

            }
        }

        /// <summary>
        /// Current shipclass, need this to update display under all circumstances I think.
        /// </summary>
        private ShipClassTN _CurrnetShipClass;
        public ShipClassTN CurrentShipClass
        {
            get { return _CurrnetShipClass; }
            set
            {
                if (_CurrnetShipClass != value)
                {
                    _CurrnetShipClass = value;

                    if (_CurrnetShipClass != null)
                    {
                        UpdateDisplay();
                    }
                }
            }
        }

        /// <summary>
        /// Class Design Logger:
        /// </summary>
#if LOG4NET_ENABLED
        public static readonly ILog logger = LogManager.GetLogger(typeof(TaskGroup));
#endif

#warning DesignAndInfo and Properties are deprecated ClassDes_Options does all the work.
        //Panels.ClassDes_DesignAndInfo m_oDesignAndInformationPanel;
        Panels.ClassDes_Options m_oOptionsPanel;
        //Panels.ClassDes_Properties m_oClassPropertiesPanel;
        Panels.ClassDes_RenameClass m_oRenameClassPanel;

        ClassDesignViewModel VM;

        /// <summary>
        /// amount to add/subtract from a design for a specified component.
        /// </summary>
        private int ComponentAmt { get; set; }

        /// <summary>
        /// Amount of missiles to add/subtract from preferred loadout. 
        /// </summary>
        private int MissileAmt { get; set; }

        /// <summary>
        /// Build Error box will set this appropriately.
        /// </summary>
        private bool IsDesignGood { get; set; }

        /// <summary>
        /// Component columns for the datagrid that displays components in the faction list.
        /// </summary>
        public enum ComponentCell
        {
            Name,
            RatingType,
            Rating,
            Cost,
            Size,
            Crew,
            Materials,
            CType,
            CIndex,
            Obsolete,
            TypeCount
        }

        /// <summary>
        /// how many components are in the component list data grid?
        /// </summary>
        private int TotalComponents { get; set; }

        /// <summary>
        /// Location of each "grouping" of components.
        /// </summary>
        private BindingList<int> CompLocation { get; set; }

        /// <summary>
        /// Each Group type of components.
        /// </summary>
        public enum ComponentGroup
        {
            Basic,
            Engines,
            FireControl,
            Beam,
            Missiles,
            Magazines,
            Ground,
            Reactors,
            Actives,
            Passives,
            Shields,
            Industry,
            TypeCount
        }

        /// <summary>
        /// Each row for the missile data grid.
        /// </summary>
        public enum MissileCell
        {
            Name,
            Size,
            Cost,
            Speed,
            Endurance,
            Range,
            WH,
            ManRating,
            ECM,
            Armour,
            Radiation,
            Sensor,
            StageTwo,
            TypeCount
        }

        /// <summary>
        /// ComponentListBox and ComponentDataGrid will mutually annihilate each other's selections without these control variables.
        /// SelectedIndexchanged is where they are called.
        /// </summary>
        private bool CLBSet { get; set; }
        private bool CDGSet { get; set; }

        public ClassDesign()
        {

            ComponentAmt = 1;
            MissileAmt = 1;

            TotalComponents = 0;

            CLBSet = false;
            CDGSet = false;

            CompLocation = new BindingList<int>();

            // create panels:
            //m_oClassPropertiesPanel = new Panels.ClassDes_Properties();
            //m_oDesignAndInformationPanel = new Panels.ClassDes_DesignAndInfo();
            m_oOptionsPanel = new Panels.ClassDes_Options();
            m_oRenameClassPanel = new Panels.ClassDes_RenameClass();

            //m_oClassPropertiesPanel.ClassPropertyGrid.PropertySort = PropertySort.CategorizedAlphabetical;

            // creat ViewModel.
            VM = new ClassDesignViewModel();

            /// <summary>
            /// setup bindings:
            /// </summary>
            m_oOptionsPanel.FactionComboBox.Bind(c => c.DataSource, VM, d => d.Factions);
            m_oOptionsPanel.FactionComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentFaction, DataSourceUpdateMode.OnPropertyChanged);
            m_oOptionsPanel.FactionComboBox.DisplayMember = "Name";
            VM.FactionChanged += (s, args) => CurrentFaction = VM.CurrentFaction;
            CurrentFaction = VM.CurrentFaction;

            m_oOptionsPanel.FactionComboBox.SelectedIndexChanged += (s, args) => m_oOptionsPanel.FactionComboBox.DataBindings["SelectedItem"].WriteValue();
            m_oOptionsPanel.FactionComboBox.SelectedIndexChanged += new EventHandler(FactionComboBox_SelectedIndexChanged);


            m_oOptionsPanel.ClassComboBox.Bind(c => c.DataSource, VM, d => d.ShipDesigns);
            m_oOptionsPanel.ClassComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentShipClass, DataSourceUpdateMode.OnPropertyChanged);
            m_oOptionsPanel.ClassComboBox.DisplayMember = "Name";
            VM.ShipClassChanged += (s, args) => CurrentShipClass = VM.CurrentShipClass;
            CurrentShipClass = VM.CurrentShipClass;
            m_oOptionsPanel.ClassComboBox.SelectedIndexChanged += (s, args) => m_oOptionsPanel.ClassComboBox.DataBindings["SelectedItem"].WriteValue();
            m_oOptionsPanel.ClassComboBox.SelectedIndexChanged += new EventHandler(ClassComboBox_SelectedIndexChanged);

            m_oOptionsPanel.Notes.Bind(c => c.Text, VM, d => d.CurrentShipClassNotes);

            //if (VM.CurrentShipClass != null)
            //{
            //    m_oClassPropertiesPanel.ClassPropertyGrid.SelectedObject = VM.CurrentShipClass;
            //}


            // Setup Events:
            m_oOptionsPanel.NewButton.Click += new EventHandler(NewButton_Click);

            m_oOptionsPanel.SizeInTonsCheckBox.CheckStateChanged += new EventHandler(SizeInTonsCheckBox_CheckedStateChanged);
            m_oOptionsPanel.GroupComponentsCheckBox.CheckStateChanged += new EventHandler(GroupComponentsCheckBox_CheckStateChanged);

            m_oOptionsPanel.OneRadioButton.CheckedChanged += new EventHandler(AMTRadioButton_CheckedChanged);
            m_oOptionsPanel.FiveRadioButton.CheckedChanged += new EventHandler(AMTRadioButton_CheckedChanged);
            m_oOptionsPanel.TenRadioButton.CheckedChanged += new EventHandler(AMTRadioButton_CheckedChanged);
            m_oOptionsPanel.HundredRadioButton.CheckedChanged += new EventHandler(AMTRadioButton_CheckedChanged);

            m_oOptionsPanel.ObsoleteCompButton.Click += new EventHandler(ObsoleteCompButton_Click);

            if (CurrentFaction != null)
            {
                if (CurrentFaction.ShipDesigns.Count != 0)
                    CurrentShipClass = CurrentFaction.ShipDesigns[0];
            }

            m_oOptionsPanel.ComponentDataGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            m_oOptionsPanel.ComponentDataGrid.RowHeadersVisible = false;
            m_oOptionsPanel.ComponentDataGrid.AutoGenerateColumns = false;
            m_oOptionsPanel.ComponentDataGrid.SelectionChanged += new EventHandler(ComponentDataGrid_SelectionChanged);
            m_oOptionsPanel.ComponentDataGrid.DoubleClick += new EventHandler(ComponentDataGrid_DoubleClick);
            SetupComponentDataGrid();

            m_oOptionsPanel.ComponentsListBox.SelectedIndexChanged += new EventHandler(ComponentsListBox_SelectedIndexChanged);
            m_oOptionsPanel.ComponentsListBox.DoubleClick += new EventHandler(ComponentsListBox_DoubleClick);

            /// <summary>
            /// options checkbox handlers:
            /// </summary>
            m_oOptionsPanel.TankerCheckBox.CheckedChanged += new System.EventHandler(this.TankerCheckbox_Changed);
            m_oOptionsPanel.CollierCheckBox.CheckedChanged += new System.EventHandler(this.CollierCheckbox_Changed);
            m_oOptionsPanel.SupplyShipCheckBox.CheckedChanged += new System.EventHandler(this.SupplyCheckbox_Changed);

            /// <summary>
            /// Button click handlers:
            /// </summary>
            m_oOptionsPanel.RefreshTechButton.Click += new EventHandler(RefreshTechButton_Click);
            m_oOptionsPanel.AddButton.Click += new EventHandler(AddButton_Click);
            m_oOptionsPanel.RemoveButton.Click += new EventHandler(RemoveButton_Click);
            m_oOptionsPanel.ArmourUpButton.Click += new EventHandler(ArmourUpButton_Click);
            m_oOptionsPanel.ArmourDownButton.Click += new EventHandler(ArmourDownButton_Click);
            m_oOptionsPanel.NewArmorButton.Click += new EventHandler(NewArmorButton_Click);
            m_oOptionsPanel.DesignTechButton.Click += new EventHandler(DesignTechButton_Click);
            m_oOptionsPanel.RenameButton.Click += new EventHandler(RenameButton_Click);
            m_oOptionsPanel.SaveNotes.Click += new EventHandler(SaveNotesButton_Click);

            /// <summary>
            /// Rename Class Button Handlers:
            /// </summary>
            m_oRenameClassPanel.OKButton.Click += new EventHandler(OKButton_Click);
            m_oRenameClassPanel.CancelRenameButton.Click += new EventHandler(CancelRenameButton_Click);
            m_oRenameClassPanel.RenameClassTextBox.KeyPress += new KeyPressEventHandler(RenameClassTextBox_KeyPress);

            m_oOptionsPanel.DeploymentTimeTextBox.TextChanged += new EventHandler(DeploymentTimeTextBox_TextChanged);


            /// <summary>
            /// Ordnance and fighter tab:
            /// </summary>
            m_oOptionsPanel.OF1xRadioButton.CheckedChanged += new EventHandler(MslAMTRadioButton_CheckedChanged);
            m_oOptionsPanel.OF10xRadioButton.CheckedChanged += new EventHandler(MslAMTRadioButton_CheckedChanged);
            m_oOptionsPanel.OF100xRadioButton.CheckedChanged += new EventHandler(MslAMTRadioButton_CheckedChanged);
            m_oOptionsPanel.OF1000xRadioButton.CheckedChanged += new EventHandler(MslAMTRadioButton_CheckedChanged);
            m_oOptionsPanel.MissileDataGrid.DoubleClick += new EventHandler(MissileDataGrid_DoubleClick);
            m_oOptionsPanel.PreferredOrdnanceListBox.DoubleClick += new EventHandler(PreferredOrdnanceListBox_DoubleClick);
            m_oOptionsPanel.MslObsButton.Click += new EventHandler(MslObsButton_Click);
            m_oOptionsPanel.ShowObsMslCheckBox.CheckedChanged += new EventHandler(ShowObsMslCheckBox_CheckedChanged);
            m_oOptionsPanel.IgnoreMslSizeCheckBox.CheckedChanged += new EventHandler(IgnoreMslSizeCheckBox_CheckedChanged);
            SetupMissileDataGrid();

            UpdateDisplay();

        }

        #region event handlers
        /// <summary>
        ///  BuildComponentDataGrid is an intensive function that should only be run under two circumstances. Obs Comb and salvaged comp to be handled later.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FactionComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            TotalComponents = 0;
            BuildComponentDataGrid();
        }

        #region Buttons and doubleclicks for design tab and main
        /// <summary>
        /// Refreshes the tech list in a slightly more optimal way than a total rebuild
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshTechButton_Click(object sender, EventArgs e)
        {
            BuildComponentDataGrid();
            BuildMissileDataGrid();
        }

        /// <summary>
        /// Sets the currently selected component to obsolete.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ObsoleteCompButton_Click(object sender, EventArgs e)
        {
            ComponentDefListTN List = _CurrnetFaction.ComponentList;

            if (m_oOptionsPanel.ComponentDataGrid.CurrentCell.RowIndex != -1)
            {
                if (m_oOptionsPanel.ComponentDataGrid.Rows[m_oOptionsPanel.ComponentDataGrid.CurrentCell.RowIndex].Cells[(int)ComponentCell.CIndex].Value != null)
                {

                    int CType;
                    int CIndex = (int)m_oOptionsPanel.ComponentDataGrid.Rows[m_oOptionsPanel.ComponentDataGrid.CurrentCell.RowIndex].Cells[(int)ComponentCell.CIndex].Value;

                    Int32.TryParse((string)m_oOptionsPanel.ComponentDataGrid.Rows[m_oOptionsPanel.ComponentDataGrid.CurrentCell.RowIndex].Cells[(int)ComponentCell.CType].Value, out CType);

                    if ((string)m_oOptionsPanel.ComponentDataGrid.Rows[m_oOptionsPanel.ComponentDataGrid.CurrentCell.RowIndex].Cells[(int)ComponentCell.Obsolete].Value == "False")
                    {
                        m_oOptionsPanel.ComponentDataGrid.Rows[m_oOptionsPanel.ComponentDataGrid.CurrentCell.RowIndex].Cells[(int)ComponentCell.Obsolete].Value = "True";

                        #region CType True Switch (Absorption listed but not implemented)
                        switch ((ComponentTypeTN)CType)
                        {
                            case ComponentTypeTN.Crew:
                                List.CrewQuarters[CIndex].isObsolete = true;
                                break;
                            case ComponentTypeTN.Fuel:
                                List.FuelStorage[CIndex].isObsolete = true;
                                break;
                            case ComponentTypeTN.Engineering:
                                List.EngineeringSpaces[CIndex].isObsolete = true;
                                break;
                            case ComponentTypeTN.Bridge:
                            case ComponentTypeTN.MaintenanceBay:
                            case ComponentTypeTN.FlagBridge:
                            case ComponentTypeTN.DamageControl:
                            case ComponentTypeTN.OrbitalHabitat:
                            case ComponentTypeTN.RecFacility:
                                List.OtherComponents[CIndex].isObsolete = true;
                                break;
                            case ComponentTypeTN.Engine:
                                List.Engines[CIndex].isObsolete = true;
                                break;
                            case ComponentTypeTN.PassiveSensor:
                                List.PassiveSensorDef[CIndex].isObsolete = true;
                                break;
                            case ComponentTypeTN.ActiveSensor:
                                List.ActiveSensorDef[CIndex].isObsolete = true;
                                break;
                            case ComponentTypeTN.CargoHold:
                                List.CargoHoldDef[CIndex].isObsolete = true;
                                break;
                            case ComponentTypeTN.CargoHandlingSystem:
                                List.CargoHandleSystemDef[CIndex].isObsolete = true;
                                break;
                            case ComponentTypeTN.CryoStorage:
                                List.ColonyBayDef[CIndex].isObsolete = true;
                                break;
                            case ComponentTypeTN.BeamFireControl:
                                List.BeamFireControlDef[CIndex].isObsolete = true;
                                break;
                            case ComponentTypeTN.Rail:
                            case ComponentTypeTN.Gauss:
                            case ComponentTypeTN.Plasma:
                            case ComponentTypeTN.Laser:
                            case ComponentTypeTN.Meson:
                            case ComponentTypeTN.Microwave:
                            case ComponentTypeTN.Particle:
                            case ComponentTypeTN.AdvRail:
                            case ComponentTypeTN.AdvLaser:
                            case ComponentTypeTN.AdvPlasma:
                            case ComponentTypeTN.AdvParticle:
                                List.BeamWeaponDef[CIndex].isObsolete = true;
                                break;
                            case ComponentTypeTN.Reactor:
                                List.ReactorDef[CIndex].isObsolete = true;
                                break;
                            case ComponentTypeTN.Shield:
                                List.ShieldDef[CIndex].isObsolete = true;
                                break;
                            case ComponentTypeTN.AbsorptionShield:
                                break;
                            case ComponentTypeTN.MissileLauncher:
                                List.MLauncherDef[CIndex].isObsolete = true;
                                break;
                            case ComponentTypeTN.Magazine:
                                List.MagazineDef[CIndex].isObsolete = true;
                                break;
                            case ComponentTypeTN.MissileFireControl:
                                List.MissileFireControlDef[CIndex].isObsolete = true;
                                break;
                            case ComponentTypeTN.CIWS:
                                List.CIWSDef[CIndex].isObsolete = true;
                                break;
                            case ComponentTypeTN.Turret:
                                List.TurretDef[CIndex].isObsolete = true;
                                break;
                            case ComponentTypeTN.JumpEngine:
                                List.JumpEngineDef[CIndex].isObsolete = true;
                                break;
                        }
                        #endregion

                    }
                    else
                    {
                        m_oOptionsPanel.ComponentDataGrid.Rows[m_oOptionsPanel.ComponentDataGrid.CurrentCell.RowIndex].Cells[(int)ComponentCell.Obsolete].Value = "False";

                        #region CType false switch (Absorption listed here, but not implemented)
                        switch ((ComponentTypeTN)CType)
                        {
                            case ComponentTypeTN.Crew:
                                List.CrewQuarters[CIndex].isObsolete = false;
                                break;
                            case ComponentTypeTN.Fuel:
                                List.FuelStorage[CIndex].isObsolete = false;
                                break;
                            case ComponentTypeTN.Engineering:
                                List.EngineeringSpaces[CIndex].isObsolete = false;
                                break;
                            case ComponentTypeTN.Bridge:
                            case ComponentTypeTN.MaintenanceBay:
                            case ComponentTypeTN.FlagBridge:
                            case ComponentTypeTN.DamageControl:
                            case ComponentTypeTN.OrbitalHabitat:
                            case ComponentTypeTN.RecFacility:
                                List.OtherComponents[CIndex].isObsolete = false;
                                break;
                            case ComponentTypeTN.Engine:
                                List.Engines[CIndex].isObsolete = false;
                                break;
                            case ComponentTypeTN.PassiveSensor:
                                List.PassiveSensorDef[CIndex].isObsolete = false;
                                break;
                            case ComponentTypeTN.ActiveSensor:
                                List.ActiveSensorDef[CIndex].isObsolete = false;
                                break;
                            case ComponentTypeTN.CargoHold:
                                List.CargoHoldDef[CIndex].isObsolete = false;
                                break;
                            case ComponentTypeTN.CargoHandlingSystem:
                                List.CargoHandleSystemDef[CIndex].isObsolete = false;
                                break;
                            case ComponentTypeTN.CryoStorage:
                                List.ColonyBayDef[CIndex].isObsolete = false;
                                break;
                            case ComponentTypeTN.BeamFireControl:
                                List.BeamFireControlDef[CIndex].isObsolete = false;
                                break;
                            case ComponentTypeTN.Rail:
                            case ComponentTypeTN.Gauss:
                            case ComponentTypeTN.Plasma:
                            case ComponentTypeTN.Laser:
                            case ComponentTypeTN.Meson:
                            case ComponentTypeTN.Microwave:
                            case ComponentTypeTN.Particle:
                            case ComponentTypeTN.AdvRail:
                            case ComponentTypeTN.AdvLaser:
                            case ComponentTypeTN.AdvPlasma:
                            case ComponentTypeTN.AdvParticle:
                                List.BeamWeaponDef[CIndex].isObsolete = false;
                                break;
                            case ComponentTypeTN.Reactor:
                                List.ReactorDef[CIndex].isObsolete = false;
                                break;
                            case ComponentTypeTN.Shield:
                                List.ShieldDef[CIndex].isObsolete = false;
                                break;
                            case ComponentTypeTN.AbsorptionShield:
                                break;
                            case ComponentTypeTN.MissileLauncher:
                                List.MLauncherDef[CIndex].isObsolete = false;
                                break;
                            case ComponentTypeTN.Magazine:
                                List.MagazineDef[CIndex].isObsolete = false;
                                break;
                            case ComponentTypeTN.MissileFireControl:
                                List.MissileFireControlDef[CIndex].isObsolete = false;
                                break;
                            case ComponentTypeTN.CIWS:
                                List.CIWSDef[CIndex].isObsolete = false;
                                break;
                            case ComponentTypeTN.Turret:
                                List.TurretDef[CIndex].isObsolete = false;
                                break;
                            case ComponentTypeTN.JumpEngine:
                                List.JumpEngineDef[CIndex].isObsolete = false;
                                break;
                        }
                        #endregion
                    }
                }
            }
        }

        /// <summary>
        /// Adds ComponentAmt components to the current design.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddButton_Click(object sender, EventArgs e)
        {
            if (m_oOptionsPanel.ComponentDataGrid.CurrentCell.RowIndex != -1)
            {
                if (m_oOptionsPanel.ComponentDataGrid.Rows[m_oOptionsPanel.ComponentDataGrid.CurrentCell.RowIndex].Cells[(int)ComponentCell.CIndex].Value != null)
                {
                    int CType;
                    int CIndex = (int)m_oOptionsPanel.ComponentDataGrid.Rows[m_oOptionsPanel.ComponentDataGrid.CurrentCell.RowIndex].Cells[(int)ComponentCell.CIndex].Value;

                    Int32.TryParse((string)m_oOptionsPanel.ComponentDataGrid.Rows[m_oOptionsPanel.ComponentDataGrid.CurrentCell.RowIndex].Cells[(int)ComponentCell.CType].Value, out CType);

                    AddComponent(CType, CIndex, ComponentAmt);
                    UpdateDisplay();
                }
            }
        }


        #region Class Rename Panel
        /// <summary>
        /// Rename this class
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RenameButton_Click(object sender, EventArgs e)
        {
            if (_CurrnetShipClass != null)
            {
                m_oRenameClassPanel.RenameClassTextBox.Text = _CurrnetShipClass.Name;
                Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
                m_oRenameClassPanel.ShowDialog();
                m_oRenameClassPanel.RenameClassTextBox.Focus();
                Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
            }
        }

        /// <summary>
        /// Saves changes to the class notes text box back to the class.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveNotesButton_Click(object sender, EventArgs e)
        {
            if (_CurrnetShipClass != null)
            {
                _CurrnetShipClass.Notes = VM.CurrentShipClassNotes;
            }
        }

        /// <summary>
        /// Actually change the name
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OKButton_Click(object sender, EventArgs e)
        {
            _CurrnetShipClass.Name = m_oRenameClassPanel.RenameClassTextBox.Text;
            _CurrnetShipClass.BuildClassSummary();

            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oRenameClassPanel.Hide();
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;

            UpdateDisplay();
        }

        /// <summary>
        /// Same as above, only on enter pressed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RenameClassTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                _CurrnetShipClass.Name = m_oRenameClassPanel.RenameClassTextBox.Text;
                _CurrnetShipClass.BuildClassSummary();

                Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
                m_oRenameClassPanel.Hide();
                Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;

                //m_oOptionsPanel.ClassComboBox.Items[m_oOptionsPanel.ClassComboBox.SelectedIndex] = _CurrnetShipClass.Name;
                UpdateDisplay();

            }
        }

        /// <summary>
        /// Disregard the rename.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelRenameButton_Click(object sender, EventArgs e)
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oRenameClassPanel.Hide();
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }
        #endregion

        /// <summary>
        /// Adds componentAmt components to the current design, this time based on double clicking the CDG.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComponentDataGrid_DoubleClick(object sender, EventArgs e)
        {
            if (m_oOptionsPanel.ComponentDataGrid.CurrentCell.RowIndex != -1)
            {
                if (m_oOptionsPanel.ComponentDataGrid.Rows[m_oOptionsPanel.ComponentDataGrid.CurrentCell.RowIndex].Cells[(int)ComponentCell.CIndex].Value != null)
                {
                    int CType;
                    int CIndex = (int)m_oOptionsPanel.ComponentDataGrid.Rows[m_oOptionsPanel.ComponentDataGrid.CurrentCell.RowIndex].Cells[(int)ComponentCell.CIndex].Value;

                    Int32.TryParse((string)m_oOptionsPanel.ComponentDataGrid.Rows[m_oOptionsPanel.ComponentDataGrid.CurrentCell.RowIndex].Cells[(int)ComponentCell.CType].Value, out CType);

                    AddComponent(CType, CIndex, ComponentAmt);
                    UpdateDisplay();
                }
            }
        }

        /// <summary>
        /// handles component list box selection events. will remove components. This can be optimized by storing ShipClass component indices in ComponentDefTN.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComponentsListBox_DoubleClick(object sender, EventArgs e)
        {
            ComponentDefListTN List = _CurrnetFaction.ComponentList;
            int SelIndex = -1;
            int CT = -1;
            Guid CID;


            /// <summary>
            /// Armor takes slot 0, and Null selection is -1, so checking for selectedIndex > 0 covers both conditions.
            /// </summary>
            if (m_oOptionsPanel.GroupComponentsCheckBox.Checked == false && m_oOptionsPanel.ComponentsListBox.SelectedIndex > 0)
            {
                SelIndex = m_oOptionsPanel.ComponentsListBox.SelectedIndex - 1;

                CT = (int)CurrentShipClass.ListOfComponentDefs[SelIndex].componentType;
                CID = CurrentShipClass.ListOfComponentDefs[SelIndex].Id;
            }
            else
            {
                GetListBoxComponent(out CT, out CID);

            }
            int CAmt = -1 * ComponentAmt;

            FindAddListBoxComponent(CT, CID, CAmt);

            UpdateDisplay();
        }

        /// <summary>
        /// Removes components specified by either the Datagrid, or the list box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveButton_Click(object sender, EventArgs e)
        {
            int CAmt = -1 * ComponentAmt;

            if (m_oOptionsPanel.ComponentDataGrid.CurrentCell.RowIndex != -1)
            {
                if (m_oOptionsPanel.ComponentDataGrid.Rows[m_oOptionsPanel.ComponentDataGrid.CurrentCell.RowIndex].Cells[(int)ComponentCell.CIndex].Value != null)
                {
                    int CType;
                    int CIndex = (int)m_oOptionsPanel.ComponentDataGrid.Rows[m_oOptionsPanel.ComponentDataGrid.CurrentCell.RowIndex].Cells[(int)ComponentCell.CIndex].Value;

                    Int32.TryParse((string)m_oOptionsPanel.ComponentDataGrid.Rows[m_oOptionsPanel.ComponentDataGrid.CurrentCell.RowIndex].Cells[(int)ComponentCell.CType].Value, out CType);

                    AddComponent(CType, CIndex, CAmt);
                    UpdateDisplay();
                }
            }
            else if (m_oOptionsPanel.ComponentsListBox.SelectedIndex != -1)
            {
                int CT;
                Guid CID;
                int SelIndex;

                if (m_oOptionsPanel.GroupComponentsCheckBox.Checked == false)
                {
                    SelIndex = m_oOptionsPanel.ComponentsListBox.SelectedIndex - 1;

                    CT = (int)CurrentShipClass.ListOfComponentDefs[SelIndex].componentType;
                    CID = CurrentShipClass.ListOfComponentDefs[SelIndex].Id;
                }
                else
                {
                    GetListBoxComponent(out CT, out CID);

                }

                FindAddListBoxComponent(CT, CID, CAmt);
            }
        }

        /// <summary>
        /// Increases the armour level on this ship by 1, up to 65535.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ArmourUpButton_Click(object sender, EventArgs e)
        {
            if (_CurrnetShipClass != null)
            {
                if (_CurrnetShipClass.ShipArmorDef.depth < 65535 && _CurrnetShipClass.IsLocked == false)
                {
                    _CurrnetShipClass.NewArmor(_CurrnetShipClass.ShipArmorDef.Name, _CurrnetShipClass.ShipArmorDef.armorPerHS, (ushort)(_CurrnetShipClass.ShipArmorDef.depth + 1));

                    BuildMisc();
                    BuildPassiveDefences();
                    BuildDesignTab();
                }
            }
        }

        /// <summary>
        /// Decreases the armour level of the ship by 1, down to 1 layer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ArmourDownButton_Click(object sender, EventArgs e)
        {
            if (_CurrnetShipClass != null)
            {
                if (_CurrnetShipClass.ShipArmorDef.depth > 1 && _CurrnetShipClass.IsLocked == false)
                {
                    _CurrnetShipClass.NewArmor(_CurrnetShipClass.ShipArmorDef.Name, _CurrnetShipClass.ShipArmorDef.armorPerHS, (ushort)(_CurrnetShipClass.ShipArmorDef.depth - 1));

                    BuildMisc();
                    BuildPassiveDefences();
                    BuildDesignTab();
                }
            }
        }

        /// <summary>
        /// updates the design to the latest armor available.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewArmorButton_Click(object sender, EventArgs e)
        {
            if (_CurrnetFaction != null)
            {
                if (_CurrnetShipClass != null)
                {
                    if (_CurrnetShipClass.IsLocked == false)
                    {
                        int ArmorTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.ArmourProtection];

                        if (ArmorTech > 12)
                            ArmorTech = 12;

#warning move these to constants.UI at some point.
                        #region Armor Tech names
                        String Title = "N/A";
                        switch (ArmorTech)
                        {
                            case 0:
                                Title = "Conventional";
                                break;
                            case 1:
                                Title = "Duranium";
                                break;
                            case 2:
                                Title = "High Density Duranium";
                                break;
                            case 3:
                                Title = "Composite";
                                break;
                            case 4:
                                Title = "Ceramic Composite";
                                break;
                            case 5:
                                Title = "Laminate Composite";
                                break;
                            case 6:
                                Title = "Compressed Carbon";
                                break;
                            case 7:
                                Title = "Biphased Carbide";
                                break;
                            case 8:
                                Title = "Crystaline Composite";
                                break;
                            case 9:
                                Title = "Superdense";
                                break;
                            case 10:
                                Title = "Bonded Superdense";
                                break;
                            case 11:
                                Title = "Coherent Superdense";
                                break;
                            case 12:
                                Title = "Collapsium";
                                break;
                        }
                        #endregion

                        _CurrnetShipClass.NewArmor(Title, (ushort)Constants.MagazineTN.MagArmor[ArmorTech], _CurrnetShipClass.ShipArmorDef.depth);

                        BuildMisc();
                        BuildPassiveDefences();
                        BuildDesignTab();
                    }
                }
            }
        }

        /// <summary>
        /// brings up the tech research button from the class design page.
        /// </summary>
        /// <param name="sneder"></param>
        /// <param name="e"></param>
        private void DesignTechButton_Click(object sneder, EventArgs e)
        {
            Helpers.UIController.Instance.ComponentRP.Popup();
        }

        /// <summary>
        /// Get new deployment time for ship.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeploymentTimeTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_CurrnetShipClass != null)
            {
                if (_CurrnetShipClass.IsLocked == false)
                {
                    int newDepTime;

                    Int32.TryParse(m_oOptionsPanel.DeploymentTimeTextBox.Text, out newDepTime);

                    if (newDepTime > 0)
                    {
                        _CurrnetShipClass.SetDeploymentTime(newDepTime);

                        BuildMisc();
                        BuildCrewAccomPanel();
                        BuildDesignTab();
                    }
                }
            }
        }
        #endregion

        /// <summary>
        /// Handles current selection changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComponentDataGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (CLBSet == true)
            {
                CLBSet = false;
            }
            else
            {
                if (m_oOptionsPanel.ComponentsListBox.SelectedIndex != -1)
                {
                    CDGSet = true;
                    m_oOptionsPanel.ComponentsListBox.ClearSelected();
                }
            }
        }

        /// <summary>
        /// Handles current list box selection change events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComponentsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CDGSet == true)
            {
                CDGSet = false;
            }
            else
            {
                if (m_oOptionsPanel.ComponentDataGrid.SelectedCells.Count != 0)
                {
                    CLBSet = true;
                    m_oOptionsPanel.ComponentDataGrid.ClearSelection();
                }
            }
        }

        private void ClassComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (m_oOptionsPanel.ClassComboBox.SelectedIndex != -1 && m_oOptionsPanel.ClassComboBox.SelectedIndex < _CurrnetFaction.ShipDesigns.Count)
            {
                _CurrnetShipClass = _CurrnetFaction.ShipDesigns[m_oOptionsPanel.ClassComboBox.SelectedIndex];

                UpdateDisplay();
            }

            //if (VM.CurrentShipClass != null)
            //{
            //    m_oClassPropertiesPanel.ClassPropertyGrid.SelectedObject = VM.CurrentShipClass;
            //}
        }

        /// <summary>
        /// New ship class button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewButton_Click(object sender, EventArgs e)
        {
            ShipClassTN oNewShipClass = new ShipClassTN("New Class", VM.CurrentFaction);
            oNewShipClass.AddCrewQuarters(_CurrnetFaction.ComponentList.CrewQuarters[0], 1);
            oNewShipClass.AddFuelStorage(_CurrnetFaction.ComponentList.FuelStorage[0], 1);
            oNewShipClass.AddEngineeringSpaces(_CurrnetFaction.ComponentList.EngineeringSpaces[0], 1);
            oNewShipClass.AddOtherComponent(_CurrnetFaction.ComponentList.OtherComponents[0], 1);

            int ArmorTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.ArmourProtection];

            if (ArmorTech > 12)
                ArmorTech = 12;
#warning more armor names to move to constants.UI
            #region Armor Tech names
            String Title = "N/A";
            switch (ArmorTech)
            {
                case 0:
                    Title = "Conventional";
                    break;
                case 1:
                    Title = "Duranium";
                    break;
                case 2:
                    Title = "High Density Duranium";
                    break;
                case 3:
                    Title = "Composite";
                    break;
                case 4:
                    Title = "Ceramic Composite";
                    break;
                case 5:
                    Title = "Laminate Composite";
                    break;
                case 6:
                    Title = "Compressed Carbon";
                    break;
                case 7:
                    Title = "Biphased Carbide";
                    break;
                case 8:
                    Title = "Crystaline Composite";
                    break;
                case 9:
                    Title = "Superdense";
                    break;
                case 10:
                    Title = "Bonded Superdense";
                    break;
                case 11:
                    Title = "Coherent Superdense";
                    break;
                case 12:
                    Title = "Collapsium";
                    break;
            }
            #endregion

            oNewShipClass.NewArmor(Title, (ushort)Constants.MagazineTN.MagArmor[ArmorTech], oNewShipClass.ShipArmorDef.depth);

            _CurrnetFaction.ShipDesigns.Add(oNewShipClass);

            m_oOptionsPanel.ClassComboBox.SelectedIndex = m_oOptionsPanel.ClassComboBox.Items.Count - 1;
            _CurrnetShipClass = _CurrnetFaction.ShipDesigns[m_oOptionsPanel.ClassComboBox.SelectedIndex];

            UpdateDisplay();


        }

        /// <summary>
        /// Checkbox controlling for HS vs tonnage display
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SizeInTonsCheckBox_CheckedStateChanged(object sender, EventArgs e)
        {
            if (CurrentShipClass != null)
            {
                BuildPassiveDefences();
            }
        }

        /// <summary>
        /// Should components be grouped by type in the list box or just printed out as is?
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GroupComponentsCheckBox_CheckStateChanged(object sender, EventArgs e)
        {
            UpdateDisplay();
        }

        /// <summary>
        /// Handle how many components the user wants to add or subtract from a design.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AMTRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (m_oOptionsPanel.OneRadioButton.Checked == true)
                ComponentAmt = 1;
            else if (m_oOptionsPanel.FiveRadioButton.Checked == true)
                ComponentAmt = 5;
            else if (m_oOptionsPanel.TenRadioButton.Checked == true)
                ComponentAmt = 10;
            else if (m_oOptionsPanel.HundredRadioButton.Checked == true)
                ComponentAmt = 100;
        }
        #endregion


        #region PublicMethods

        public void ShowAllPanels(DockPanel a_oDockPanel)
        {
            //ShowPropertiesPanel(a_oDockPanel);
            //ShowDesignAndInfoPanel(a_oDockPanel);
            ShowOptionsPanel(a_oDockPanel);
        }

        public void ShowPropertiesPanel(DockPanel a_oDockPanel)
        {
            //Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            //m_oClassPropertiesPanel.Show(a_oDockPanel, DockState.DockRight);
            //Helpers.UIController.Instance.SuspendAutoPanelDisplay = false; 
        }

        public void ActivatePropertiesPanel()
        {
            //Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            //m_oClassPropertiesPanel.Activate();
            //Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        public void ShowDesignAndInfoPanel(DockPanel a_oDockPanel)
        {
            //Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            //m_oDesignAndInformationPanel.Show(a_oDockPanel, DockState.Document);
            //Helpers.UIController.Instance.SuspendAutoPanelDisplay = false; 
        }

        public void ActivateDesignAndInfoPanel()
        {
            //Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            //m_oDesignAndInformationPanel.Activate();
            //Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        public void ShowOptionsPanel(DockPanel a_oDockPanel)
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oOptionsPanel.Show(a_oDockPanel, DockState.Document);
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        public void ActivateOptionsPanel()
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oOptionsPanel.Activate();
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

        /// <summary>
        /// Updates the overall display for class design.
        /// </summary>
        public void UpdateDisplay()
        {
            if (CurrentShipClass != null)
            {
                BuildMisc();
                BuildPowerSystems();
                BuildPassiveDefences();
                BuildCrewAccomPanel();
                BuildOptionsPanel();
                if (CurrentShipClass.IsLocked == true)
                {
                    int TabId = m_oOptionsPanel.ClassDesignTabControl.TabPages.IndexOf(m_oOptionsPanel.DesignTabPage);
                    if (TabId != -1)
                    {
                        m_oOptionsPanel.ClassDesignTabControl.TabPages.RemoveAt(TabId);
                    }
                }
                else
                {
                    int TabId = m_oOptionsPanel.ClassDesignTabControl.TabPages.IndexOf(m_oOptionsPanel.DesignTabPage);
                    if (TabId == -1)
                    {
                        m_oOptionsPanel.ClassDesignTabControl.TabPages.Insert(1, m_oOptionsPanel.DesignTabPage);
                    }
                    BuildDesignTab();
                }

                BuildOrdnanceFighterTab();

                BuildErrorBox();

                UpdateDACRankInfoTab();
            }
        }

        #endregion


        #region Private methods
        /// <summary>
        /// print the current class summary to the appropriate window.
        /// </summary>
        private void BuildMisc()
        {
            m_oOptionsPanel.ClassSummaryTextBox.Text = CurrentShipClass.Summary;
            m_oOptionsPanel.BPCostTextBox.Text = Math.Round(CurrentShipClass.BuildPointCost).ToString();
        }

        /// <summary>
        /// Power systems panel.
        /// </summary>
        private void BuildPowerSystems()
        {
            m_oOptionsPanel.EnginePowerTextBox.Text = CurrentShipClass.MaxEnginePower.ToString();
            m_oOptionsPanel.MaxSpeedTextBox.Text = CurrentShipClass.MaxSpeed.ToString();
            m_oOptionsPanel.JumpRatingTextBox.Text = CurrentShipClass.SquadronSize.ToString();
            m_oOptionsPanel.JumpDistTextBox.Text = ((int)Math.Round((float)(CurrentShipClass.JumpRadius/1000))).ToString();
            m_oOptionsPanel.ReactorPowerTextBox.Text = CurrentShipClass.TotalPowerGeneration.ToString();
            m_oOptionsPanel.RequiredPowerTextBox.Text = CurrentShipClass.TotalPowerRequirement.ToString();
        }

        /// <summary>
        /// Passive defences panel
        /// </summary>
        private void BuildPassiveDefences()
        {
            m_oOptionsPanel.ArmorRatingTextBox.Text = CurrentShipClass.ShipArmorDef.depth.ToString();

            if (m_oOptionsPanel.SizeInTonsCheckBox.Checked == true)
                m_oOptionsPanel.ExactClassSizeTextBox.Text = Math.Round(CurrentShipClass.SizeTons).ToString();
            else
                m_oOptionsPanel.ExactClassSizeTextBox.Text = CurrentShipClass.SizeHS.ToString();

            m_oOptionsPanel.ArmorAreaTextBox.Text = CurrentShipClass.ShipArmorDef.area.ToString();
            m_oOptionsPanel.ArmorStrengthTextBox.Text = CurrentShipClass.ShipArmorDef.strength.ToString();
            m_oOptionsPanel.ArmorColumnsTextBox.Text = CurrentShipClass.ShipArmorDef.cNum.ToString();
            m_oOptionsPanel.ShieldStrengthTextBox.Text = CurrentShipClass.TotalShieldPool.ToString();

            if (CurrentShipClass.ShipShieldDef != null)
            {
                if (CurrentShipClass.ShipShieldDef.shieldGen == CurrentShipClass.ShipShieldDef.shieldPool)
                {
                    m_oOptionsPanel.ShieldRechargeTextBox.Text = "300";
                }
                else
                {
                    float shield = (float)Math.Floor((CurrentShipClass.ShipShieldDef.shieldPool / CurrentShipClass.ShipShieldDef.shieldGen) * 300.0f);
                    m_oOptionsPanel.ShieldRechargeTextBox.Text = shield.ToString();
                }
            }
            else
            {
                m_oOptionsPanel.ShieldRechargeTextBox.Text = "0";
            }

            m_oOptionsPanel.InternalHTKTextBox.Text = CurrentShipClass.TotalHTK.ToString();
        }

        /// <summary>
        /// Crew accomodations panel
        /// </summary>
        private void BuildCrewAccomPanel()
        {

            m_oOptionsPanel.DeploymentTimeTextBox.Text = CurrentShipClass.MaxDeploymentTime.ToString();

            if (_CurrnetShipClass.IsLocked == true)
                m_oOptionsPanel.DeploymentTimeTextBox.ReadOnly = true;
            else
                m_oOptionsPanel.DeploymentTimeTextBox.ReadOnly = false;

            m_oOptionsPanel.TonsPerManTextBox.Text = String.Format("{0:N3}", CurrentShipClass.TonsPerMan);
            m_oOptionsPanel.CapPerHSTextBox.Text = String.Format("{0:N2}", CurrentShipClass.CapPerHS);
            m_oOptionsPanel.AccomHSReqTextBox.Text = String.Format("{0:N4}", CurrentShipClass.AccomHSRequirement);
            m_oOptionsPanel.AccomHSAvailTextBox.Text = String.Format("{0:N1}", CurrentShipClass.AccomHSAvailable);
            m_oOptionsPanel.CrewBerthsTextBox.Text = CurrentShipClass.TotalRequiredCrew.ToString();
            m_oOptionsPanel.SpareBerthsTextBox.Text = CurrentShipClass.SpareCrewQuarters.ToString();
            m_oOptionsPanel.CryoBerthsTextBox.Text = CurrentShipClass.SpareCryoBerths.ToString();
        }


        /// <summary>
        /// checkboxes for tanker collier etc etc.
        /// </summary>
        private void BuildOptionsPanel()
        {
            m_oOptionsPanel.Tanker = CurrentShipClass.IsTanker;
            m_oOptionsPanel.Collier = CurrentShipClass.IsCollier;
            m_oOptionsPanel.SupplyShip = CurrentShipClass.IsSupply;
        }

        /// <summary>
        /// Builds the design tab. Really wishing I'd done these as a dictionary originally.
        /// Not implemented: ECCM, ECM,Cloak, Jump Engines, Maintenance Storage Bays,Hangar,Boat Bay,Troop Bay,Drop Pod,Orbital Hab,Rec Facilities,
        /// Geo Sensors,Grav Sensors, 
        /// </summary>
        private void BuildDesignTab()
        {
            String Entry;
            m_oOptionsPanel.BriefSummaryTextBox.Text = CurrentShipClass.Summary;

            m_oOptionsPanel.ComponentsListBox.Items.Clear();
            if (m_oOptionsPanel.GroupComponentsCheckBox.Checked == true)
            {

                /// <summary>
                /// Add anything new, like turrets here. likewise further down other component count checks need to be added to their respective groups.
                /// </summary>
                if (CurrentShipClass.ShipBFCDef.Count != 0 || CurrentShipClass.ShipBeamDef.Count != 0 || CurrentShipClass.ShipReactorDef.Count != 0 ||
                    CurrentShipClass.ShipMLaunchDef.Count != 0 || CurrentShipClass.ShipMagazineDef.Count != 0 || CurrentShipClass.ShipMFCDef.Count != 0 ||
                    CurrentShipClass.ShipCIWSDef.Count != 0 || CurrentShipClass.ShipTurretDef.Count != 0)
                {
                    Entry = "Weapons and Fire Control:";
                    m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);

                    for (int loop = 0; loop < CurrentShipClass.ShipBeamDef.Count; loop++)
                    {
                        Entry = String.Format("{0}x {1}", CurrentShipClass.ShipBeamCount[loop], CurrentShipClass.ShipBeamDef[loop].Name);
                        m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                    }

                    for (int loop = 0; loop < CurrentShipClass.ShipTurretDef.Count; loop++)
                    {
                        Entry = String.Format("{0}x {1}", CurrentShipClass.ShipTurretCount[loop], CurrentShipClass.ShipTurretDef[loop].Name);
                        m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                    }

                    for (int loop = 0; loop < CurrentShipClass.ShipMLaunchDef.Count; loop++)
                    {
                        Entry = String.Format("{0}x {1}", CurrentShipClass.ShipMLaunchCount[loop], CurrentShipClass.ShipMLaunchDef[loop].Name);
                        m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                    }

                    for (int loop = 0; loop < CurrentShipClass.ShipCIWSDef.Count; loop++)
                    {
                        Entry = String.Format("{0}x {1}", CurrentShipClass.ShipCIWSCount[loop], CurrentShipClass.ShipCIWSDef[loop].Name);
                        m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                    }

                    for (int loop = 0; loop < CurrentShipClass.ShipReactorDef.Count; loop++)
                    {
                        Entry = String.Format("{0}x {1}", CurrentShipClass.ShipReactorCount[loop], CurrentShipClass.ShipReactorDef[loop].Name);
                        m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                    }

                    for (int loop = 0; loop < CurrentShipClass.ShipMagazineDef.Count; loop++)
                    {
                        Entry = String.Format("{0}x {1}", CurrentShipClass.ShipMagazineCount[loop], CurrentShipClass.ShipMagazineDef[loop].Name);
                        m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                    }

                    for (int loop = 0; loop < CurrentShipClass.ShipBFCDef.Count; loop++)
                    {
                        Entry = String.Format("{0}x {1}", CurrentShipClass.ShipBFCCount[loop], CurrentShipClass.ShipBFCDef[loop].Name);
                        m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                    }

                    for (int loop = 0; loop < CurrentShipClass.ShipMFCDef.Count; loop++)
                    {
                        Entry = String.Format("{0}x {1}", CurrentShipClass.ShipMFCCount[loop], CurrentShipClass.ShipMFCDef[loop].Name);
                        m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                    }

                    Entry = "";
                    m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                }

                Entry = "Defences:";
                m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);

                Entry = String.Format("{0}x {1} Armour", CurrentShipClass.ShipArmorDef.size, CurrentShipClass.ShipArmorDef.Name);
                m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);

                if (CurrentShipClass.ShipShieldDef != null)
                {
                    Entry = String.Format("{0}x {1}", CurrentShipClass.ShipShieldCount, CurrentShipClass.ShipShieldDef.Name);
                    m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                }

                Entry = "";
                m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);

                if (CurrentShipClass.ShipEngineDef != null || CurrentShipClass.ShipJumpEngineDef.Count != 0)
                {
                    Entry = "Engines:";
                    m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);

                    if(CurrentShipClass.ShipEngineDef != null)
                    {
                        Entry = String.Format("{0}x {1}", CurrentShipClass.ShipEngineCount, CurrentShipClass.ShipEngineDef.Name);
                        m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                    }

                    for(int ComponentIterator = 0; ComponentIterator < CurrentShipClass.ShipJumpEngineDef.Count; ComponentIterator++)
                    {
                        Entry = String.Format("{0}x {1}", CurrentShipClass.ShipJumpEngineCount[ComponentIterator], CurrentShipClass.ShipJumpEngineDef[ComponentIterator].Name);
                        m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                    }

                    Entry = "";
                    m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                }


                if (CurrentShipClass.ShipCargoDef.Count != 0 || CurrentShipClass.ShipColonyDef.Count != 0 || CurrentShipClass.ShipCHSDef.Count != 0)
                {
                    Entry = "Special Functions:";
                    m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);

                    for (int loop = 0; loop < CurrentShipClass.ShipCargoDef.Count; loop++)
                    {
                        Entry = String.Format("{0}x {1}", CurrentShipClass.ShipCargoCount[loop], CurrentShipClass.ShipCargoDef[loop].Name);
                        m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                    }

                    for (int loop = 0; loop < CurrentShipClass.ShipColonyDef.Count; loop++)
                    {
                        Entry = String.Format("{0}x {1}", CurrentShipClass.ShipColonyCount[loop], CurrentShipClass.ShipColonyDef[loop].Name);
                        m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                    }

                    for (int loop = 0; loop < CurrentShipClass.ShipCHSDef.Count; loop++)
                    {
                        Entry = String.Format("{0}x {1}", CurrentShipClass.ShipCHSCount[loop], CurrentShipClass.ShipCHSDef[loop].Name);
                        m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                    }

                    Entry = "";
                    m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                }

                if (CurrentShipClass.ShipASensorDef.Count != 0 || CurrentShipClass.ShipPSensorDef.Count != 0)
                {
                    Entry = "Sensors:";
                    m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);

                    for (int loop = 0; loop < CurrentShipClass.ShipASensorDef.Count; loop++)
                    {
                        Entry = String.Format("{0}x {1}", CurrentShipClass.ShipASensorCount[loop], CurrentShipClass.ShipASensorDef[loop].Name);
                        m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                    }

                    for (int loop = 0; loop < CurrentShipClass.ShipPSensorDef.Count; loop++)
                    {
                        Entry = String.Format("{0}x {1}", CurrentShipClass.ShipPSensorCount[loop], CurrentShipClass.ShipPSensorDef[loop].Name);
                        m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                    }

                    Entry = "";
                    m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                }

                Entry = "General:";
                m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);

                for (int loop = 0; loop < CurrentShipClass.CrewQuarters.Count; loop++)
                {
                    Entry = String.Format("{0}x {1}", CurrentShipClass.CrewQuartersCount[loop], CurrentShipClass.CrewQuarters[loop].Name);
                    m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                }

                for (int loop = 0; loop < CurrentShipClass.FuelTanks.Count; loop++)
                {
                    Entry = String.Format("{0}x {1}", CurrentShipClass.FuelTanksCount[loop], CurrentShipClass.FuelTanks[loop].Name);
                    m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                }

                for (int loop = 0; loop < CurrentShipClass.EngineeringBays.Count; loop++)
                {
                    Entry = String.Format("{0}x {1}", CurrentShipClass.EngineeringBaysCount[loop], CurrentShipClass.EngineeringBays[loop].Name);
                    m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                }

                for (int loop = 0; loop < CurrentShipClass.OtherComponents.Count; loop++)
                {
                    Entry = String.Format("{0}x {1}", CurrentShipClass.OtherComponentsCount[loop], CurrentShipClass.OtherComponents[loop].Name);
                    m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                }
            }
            else
            {
                Entry = String.Format("{0}x {1} Armour", CurrentShipClass.ShipArmorDef.size, CurrentShipClass.ShipArmorDef.Name);
                m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                for (int loop = 0; loop < CurrentShipClass.ListOfComponentDefs.Count; loop++)
                {
                    Entry = String.Format("{0}x {1}", CurrentShipClass.ListOfComponentDefsCount[loop], CurrentShipClass.ListOfComponentDefs[loop].Name);
                    m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                }
            }
        }

        /// <summary>
        /// Almost exactly the same as the above design tab builder, but this one gets the component selected from the grouped list.
        /// maybe this should be commented. I'll have to get around to that if/when I refactor this.
        /// </summary>
        /// <param name="LineIndex">Line we want to find</param>
        /// <param name="CType">Component type to be "returned"</param>
        /// <param name="CIndex">Component index to be "returned"</param>
        private void GetListBoxComponent(out int CType, out Guid CIndex)
        {
            CType = -1;
            CIndex = Guid.Empty;
            int CurrentLine = 0;

            if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
            {
                return;
            }

            if (CurrentShipClass.ShipBFCDef.Count != 0 || CurrentShipClass.ShipBeamDef.Count != 0 || CurrentShipClass.ShipReactorDef.Count != 0 ||
                    CurrentShipClass.ShipMLaunchDef.Count != 0 || CurrentShipClass.ShipMagazineDef.Count != 0 || CurrentShipClass.ShipMFCDef.Count != 0 ||
                    CurrentShipClass.ShipCIWSDef.Count != 0 || CurrentShipClass.ShipTurretDef.Count != 0)
            {
                CurrentLine++;

                for (int loop = 0; loop < CurrentShipClass.ShipBeamDef.Count; loop++)
                {
                    if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                    {
                        CType = (int)CurrentShipClass.ShipBeamDef[loop].componentType;
                        CIndex = CurrentShipClass.ShipBeamDef[loop].Id;
                        return;
                    }
                    CurrentLine++;
                }

                for (int loop = 0; loop < CurrentShipClass.ShipTurretDef.Count; loop++)
                {
                    if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                    {
                        CType = (int)CurrentShipClass.ShipTurretDef[loop].componentType;
                        CIndex = CurrentShipClass.ShipTurretDef[loop].Id;
                        return;
                    }
                    CurrentLine++;
                }

                for (int loop = 0; loop < CurrentShipClass.ShipMLaunchDef.Count; loop++)
                {
                    if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                    {
                        CType = (int)CurrentShipClass.ShipMLaunchDef[loop].componentType;
                        CIndex = CurrentShipClass.ShipMLaunchDef[loop].Id;
                        return;
                    }
                    CurrentLine++;
                }

                for (int loop = 0; loop < CurrentShipClass.ShipCIWSDef.Count; loop++)
                {
                    if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                    {
                        CType = (int)CurrentShipClass.ShipCIWSDef[loop].componentType;
                        CIndex = CurrentShipClass.ShipCIWSDef[loop].Id;
                        return;
                    }
                    CurrentLine++;
                }

                for (int loop = 0; loop < CurrentShipClass.ShipReactorDef.Count; loop++)
                {
                    if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                    {
                        CType = (int)CurrentShipClass.ShipReactorDef[loop].componentType;
                        CIndex = CurrentShipClass.ShipReactorDef[loop].Id;
                        return;
                    }
                    CurrentLine++;
                }

                for (int loop = 0; loop < CurrentShipClass.ShipMagazineDef.Count; loop++)
                {
                    if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                    {
                        CType = (int)CurrentShipClass.ShipMagazineDef[loop].componentType;
                        CIndex = CurrentShipClass.ShipMagazineDef[loop].Id;
                        return;
                    }
                    CurrentLine++;
                }

                for (int loop = 0; loop < CurrentShipClass.ShipBFCDef.Count; loop++)
                {
                    if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                    {
                        CType = (int)CurrentShipClass.ShipBFCDef[loop].componentType;
                        CIndex = CurrentShipClass.ShipBFCDef[loop].Id;
                        return;
                    }
                    CurrentLine++;
                }

                for (int loop = 0; loop < CurrentShipClass.ShipMFCDef.Count; loop++)
                {
                    if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                    {
                        CType = (int)CurrentShipClass.ShipMFCDef[loop].componentType;
                        CIndex = CurrentShipClass.ShipMFCDef[loop].Id;
                        return;
                    }
                    CurrentLine++;
                }

                if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                {
                    return;
                }
                CurrentLine++;
            }

            if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
            {
                return;
            }
            CurrentLine++;

            if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
            {
                return;
            }
            CurrentLine++;

            if (CurrentShipClass.ShipShieldDef != null)
            {
                if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                {
                    CType = (int)CurrentShipClass.ShipShieldDef.componentType;
                    CIndex = CurrentShipClass.ShipShieldDef.Id;
                    return;
                }
                CurrentLine++;
            }

            if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
            {
                return;
            }
            CurrentLine++;

            if (CurrentShipClass.ShipEngineDef != null || CurrentShipClass.ShipJumpEngineDef.Count != 0)
            {
                if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                {
                    return;
                }
                CurrentLine++;

                if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                {
                    CType = (int)CurrentShipClass.ShipEngineDef.componentType;
                    CIndex = CurrentShipClass.ShipEngineDef.Id;
                    return;
                }
                CurrentLine++;

                for (int ComponentIterator = 0; ComponentIterator < CurrentShipClass.ShipJumpEngineDef.Count; ComponentIterator++)
                {
                    if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                    {
                        CType = (int)CurrentShipClass.ShipJumpEngineDef[ComponentIterator].componentType;
                        CIndex = CurrentShipClass.ShipJumpEngineDef[ComponentIterator].Id;
                        return;
                    }
                    CurrentLine++;
                }

                if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                {
                    return;
                }
                CurrentLine++;
            }

            if (CurrentShipClass.ShipCargoDef.Count != 0 || CurrentShipClass.ShipColonyDef.Count != 0 || CurrentShipClass.ShipCHSDef.Count != 0)
            {
                if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                {
                    return;
                }
                CurrentLine++;

                for (int loop = 0; loop < CurrentShipClass.ShipCargoDef.Count; loop++)
                {
                    if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                    {
                        CType = (int)CurrentShipClass.ShipCargoDef[loop].componentType;
                        CIndex = CurrentShipClass.ShipCargoDef[loop].Id;
                        return;
                    }
                    CurrentLine++;
                }

                for (int loop = 0; loop < CurrentShipClass.ShipColonyDef.Count; loop++)
                {
                    if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                    {
                        CType = (int)CurrentShipClass.ShipColonyDef[loop].componentType;
                        CIndex = CurrentShipClass.ShipColonyDef[loop].Id;
                        return;
                    }
                    CurrentLine++;
                }

                for (int loop = 0; loop < CurrentShipClass.ShipCHSDef.Count; loop++)
                {
                    if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                    {
                        CType = (int)CurrentShipClass.ShipCHSDef[loop].componentType;
                        CIndex = CurrentShipClass.ShipCHSDef[loop].Id;
                        return;
                    }
                    CurrentLine++;
                }

                if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                {
                    return;
                }
                CurrentLine++;
            }

            if (CurrentShipClass.ShipASensorDef.Count != 0 || CurrentShipClass.ShipPSensorDef.Count != 0)
            {
                if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                {
                    return;
                }
                CurrentLine++;

                for (int loop = 0; loop < CurrentShipClass.ShipASensorDef.Count; loop++)
                {
                    if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                    {
                        CType = (int)CurrentShipClass.ShipASensorDef[loop].componentType;
                        CIndex = CurrentShipClass.ShipASensorDef[loop].Id;
                        return;
                    }
                    CurrentLine++;
                }

                for (int loop = 0; loop < CurrentShipClass.ShipPSensorDef.Count; loop++)
                {
                    if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                    {
                        CType = (int)CurrentShipClass.ShipPSensorDef[loop].componentType;
                        CIndex = CurrentShipClass.ShipPSensorDef[loop].Id;
                        return;
                    }
                    CurrentLine++;
                }

                if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                {
                    return;
                }
                CurrentLine++;
            }

            if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
            {
                return;
            }
            CurrentLine++;

            for (int loop = 0; loop < CurrentShipClass.CrewQuarters.Count; loop++)
            {
                if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                {
                    CType = (int)CurrentShipClass.CrewQuarters[loop].componentType;
                    CIndex = CurrentShipClass.CrewQuarters[loop].Id;
                    return;
                }
                CurrentLine++;
            }

            for (int loop = 0; loop < CurrentShipClass.FuelTanks.Count; loop++)
            {
                if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                {
                    CType = (int)CurrentShipClass.FuelTanks[loop].componentType;
                    CIndex = CurrentShipClass.FuelTanks[loop].Id;
                    return;
                }
                CurrentLine++;
            }

            for (int loop = 0; loop < CurrentShipClass.EngineeringBays.Count; loop++)
            {
                if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                {
                    CType = (int)CurrentShipClass.EngineeringBays[loop].componentType;
                    CIndex = CurrentShipClass.EngineeringBays[loop].Id;
                    return;
                }
                CurrentLine++;
            }

            for (int loop = 0; loop < CurrentShipClass.OtherComponents.Count; loop++)
            {
                if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
                {
                    CType = (int)CurrentShipClass.OtherComponents[loop].componentType;
                    CIndex = CurrentShipClass.OtherComponents[loop].Id;
                    return;
                }
                CurrentLine++;
            }

            if (CurrentLine == m_oOptionsPanel.ComponentsListBox.SelectedIndex)
            {
                return;
            }
        }

        /// <summary>
        /// Creates the columns for the component data grid.
        /// </summary>
        private void SetupComponentDataGrid()
        {
            m_oOptionsPanel.ComponentDataGrid.Columns.Clear();
            try
            {
                Padding newPadding = new Padding(2, 0, 2, 0);
                AddColumn("Name", newPadding, m_oOptionsPanel.ComponentDataGrid);
                AddColumn("Rating Type", newPadding, m_oOptionsPanel.ComponentDataGrid);
                AddColumn("Rating", newPadding, m_oOptionsPanel.ComponentDataGrid);
                AddColumn("Cost", newPadding, m_oOptionsPanel.ComponentDataGrid);
                AddColumn("Size", newPadding, m_oOptionsPanel.ComponentDataGrid);
                AddColumn("Crew", newPadding, m_oOptionsPanel.ComponentDataGrid);
                AddColumn("Materials (exc Duranium)", newPadding, m_oOptionsPanel.ComponentDataGrid);

                AddColumn("CType", newPadding, m_oOptionsPanel.ComponentDataGrid);
                AddColumn("CIndex", newPadding, m_oOptionsPanel.ComponentDataGrid);
                AddColumn("Obsolete", newPadding, m_oOptionsPanel.ComponentDataGrid);

                m_oOptionsPanel.ComponentDataGrid.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

                foreach (DataGridViewColumn Column in m_oOptionsPanel.ComponentDataGrid.Columns)
                {
                    Column.SortMode = DataGridViewColumnSortMode.NotSortable;
                }

#warning These are needed for debug for right now.
                /*
                m_oOptionsPanel.ComponentDataGrid.Columns[m_oOptionsPanel.ComponentDataGrid.Columns.Count - 1].Visible = false;
                m_oOptionsPanel.ComponentDataGrid.Columns[m_oOptionsPanel.ComponentDataGrid.Columns.Count - 2].Visible = false;
                m_oOptionsPanel.ComponentDataGrid.Columns[m_oOptionsPanel.ComponentDataGrid.Columns.Count - 3].Visible = false;
                */

            }
            catch
            {
#if LOG4NET_ENABLED
                logger.Error("Something went wrong Creating Columns for Class Design ComponentGrid screen...");
#endif
            }
        }

        /// <summary>
        /// Just a space saver here to avoid copy pasting a lot.
        /// </summary>
        /// <param name="Header">Text of column header.</param>
        /// <param name="newPadding">Padding in use, not sure what this is or why its necessary. Cargo culting it is.</param>
        private void AddColumn(String Header, Padding newPadding, DataGridView DG)
        {
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.HeaderText = Header;
                col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                col.DefaultCellStyle.Padding = newPadding;
                if (col != null)
                {
                    DG.Columns.Add(col);
                }
            }
        }

        /// <summary>
        /// Listing of all components that can be added and statistics about them.
        /// </summary>
        private void BuildComponentDataGrid()
        {
            int row = 0;
            String Entry = "N/A";
            String Entry2 = "N/A";
            ComponentDefListTN List = _CurrnetFaction.ComponentList;

            if (TotalComponents == 0)
            {

                m_oOptionsPanel.ComponentDataGrid.Rows.Clear();

                try
                {

                    #region Basic Systems
                    using (DataGridViewRow NewRow = new DataGridViewRow())
                    {
                        /// <summary>
                        /// setup row height. note that by default they are 22 pixels in height!
                        /// </summary>
                        NewRow.Height = 18;
                        m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                        DataGridViewCellStyle style = new DataGridViewCellStyle();
                        style.Font = new System.Drawing.Font(m_oOptionsPanel.ComponentDataGrid.Font, System.Drawing.FontStyle.Bold);
                        m_oOptionsPanel.ComponentDataGrid.Rows[row].DefaultCellStyle = style;

                        m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = "Basic Systems";
                        CompLocation.Add(row);
                        row++;
                    }

                    for (int ComponentIterator = 0; ComponentIterator < List.CrewQuarters.Count; ComponentIterator++)
                    {
                        PopulateComponentRow(List.CrewQuarters[ComponentIterator], row, "Life Support", (List.CrewQuarters[ComponentIterator].size * 50.0f).ToString(), ComponentIterator);
                        row++;
                    }

                    for (int ComponentIterator = 0; ComponentIterator < List.FuelStorage.Count; ComponentIterator++)
                    {
                        PopulateComponentRow(List.FuelStorage[ComponentIterator], row, "Litres of Fuel", (List.FuelStorage[ComponentIterator].size * 50000.0f).ToString(), ComponentIterator);
                        row++;
                    }

                    for (int ComponentIterator = 0; ComponentIterator < List.EngineeringSpaces.Count; ComponentIterator++)
                    {
                        PopulateComponentRow(List.EngineeringSpaces[ComponentIterator], row, "Failure Rate", (List.EngineeringSpaces[ComponentIterator].size).ToString(), ComponentIterator);
                        row++;
                    }

                    for (int loop = 0; loop < List.OtherComponents.Count; loop++)
                    {
                        using (DataGridViewRow NewRow = new DataGridViewRow())
                        {
                            /// <summary>
                            /// setup row height. note that by default they are 22 pixels in height!
                            /// </summary>
                            NewRow.Height = 18;
                            m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                            switch (List.OtherComponents[loop].componentType)
                            {
                                case ComponentTypeTN.Bridge:
                                    Entry = "CommandControl";
                                    Entry2 = "1";
                                    break;
                                case ComponentTypeTN.MaintenanceBay:
                                    Entry = "MaintStorage";
                                    Entry2 = "1000";
                                    break;
                                case ComponentTypeTN.OrbitalHabitat:
                                    Entry = "Worker Capacity";
                                    Entry2 = "50000";
                                    break;
                                case ComponentTypeTN.RecFacility:
                                    Entry = "Crew Recreation";
                                    Entry2 = "0";
                                    break;
                                default:
                                    Entry = "UNK Component";
                                    Entry2 = "*********UNK*********";
                                    break;
                            }

                            PopulateComponentRow(List.OtherComponents[loop], row, Entry, Entry2, loop);
                            row++;
                        }
                    }
                    #endregion

                    #region Engines
                    using (DataGridViewRow NewRow = new DataGridViewRow())
                    {
                        /// <summary>
                        /// setup row height. note that by default they are 22 pixels in height!
                        /// </summary>
                        NewRow.Height = 18;
                        m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                        DataGridViewCellStyle style = new DataGridViewCellStyle();
                        style.Font = new System.Drawing.Font(m_oOptionsPanel.ComponentDataGrid.Font, System.Drawing.FontStyle.Bold);
                        m_oOptionsPanel.ComponentDataGrid.Rows[row].DefaultCellStyle = style;


                        m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = "Engines";
                        CompLocation.Add(row);

                        row++;
                    }

                    for (int ComponentIterator = 0; ComponentIterator < List.Engines.Count; ComponentIterator++)
                    {
                        PopulateComponentRow(List.Engines[ComponentIterator], row, "Engine Power", List.Engines[ComponentIterator].enginePower.ToString(), ComponentIterator);
                        row++;
                    }

                    for(int ComponentIterator = 0; ComponentIterator < List.JumpEngineDef.Count; ComponentIterator++)
                    {
                        PopulateComponentRow(List.JumpEngineDef[ComponentIterator], row, "Max Ship Size", 
                                             ((float)List.JumpEngineDef[ComponentIterator].maxJumpRating / Constants.ShipTN.TonsPerHS).ToString(), ComponentIterator);
                        row++;
                    }
                    #endregion

                    #region Fire Control
                    using (DataGridViewRow NewRow = new DataGridViewRow())
                    {
                        /// <summary>
                        /// setup row height. note that by default they are 22 pixels in height!
                        /// </summary>
                        NewRow.Height = 18;
                        m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                        DataGridViewCellStyle style = new DataGridViewCellStyle();
                        style.Font = new System.Drawing.Font(m_oOptionsPanel.ComponentDataGrid.Font, System.Drawing.FontStyle.Bold);
                        m_oOptionsPanel.ComponentDataGrid.Rows[row].DefaultCellStyle = style;


                        m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = "Fire Control";

                        CompLocation.Add(row);

                        row++;
                    }


                    for (int ComponentIterator = 0; ComponentIterator < List.BeamFireControlDef.Count; ComponentIterator++)
                    {
                        PopulateComponentRow(List.BeamFireControlDef[ComponentIterator], row, "50% Acc. Distance", List.BeamFireControlDef[ComponentIterator].range.ToString(), ComponentIterator);
                        row++;
                    }

                    for (int ComponentIterator = 0; ComponentIterator < List.MissileFireControlDef.Count; ComponentIterator++)
                    {
                        if (List.MissileFireControlDef[ComponentIterator].maxRange >= 100000)
                        {
                            float RNG = (float)(List.MissileFireControlDef[ComponentIterator].maxRange) / 100000.0f;
                            Entry = String.Format("{0:N1}B", RNG);
                        }
                        else if (List.MissileFireControlDef[ComponentIterator].maxRange >= 100)
                        {
                            float RNG = (float)(List.MissileFireControlDef[ComponentIterator].maxRange) / 100.0f;
                            Entry = String.Format("{0:N1}M", RNG);
                        }
                        else
                        {
                            float RNG = (float)List.MissileFireControlDef[ComponentIterator].maxRange;
                            Entry = String.Format("{0:N1}K", RNG);
                        }

                        PopulateComponentRow(List.MissileFireControlDef[ComponentIterator], row, "Range km", Entry, ComponentIterator);
                        row++;
                    }
                    #endregion

                    #region Energy Weapons / CIWS / Turrets
                    using (DataGridViewRow NewRow = new DataGridViewRow())
                    {
                        /// <summary>
                        /// setup row height. note that by default they are 22 pixels in height!
                        /// </summary>
                        NewRow.Height = 18;
                        m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                        DataGridViewCellStyle style = new DataGridViewCellStyle();
                        style.Font = new System.Drawing.Font(m_oOptionsPanel.ComponentDataGrid.Font, System.Drawing.FontStyle.Bold);
                        m_oOptionsPanel.ComponentDataGrid.Rows[row].DefaultCellStyle = style;


                        m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = "Energy Weapons";

                        CompLocation.Add(row);

                        row++;
                    }

                    for (int ComponentIterator = 0; ComponentIterator < List.BeamWeaponDef.Count; ComponentIterator++)
                    {
                        switch (List.BeamWeaponDef[ComponentIterator].componentType)
                        {
                            case ComponentTypeTN.Laser:
                            case ComponentTypeTN.AdvLaser:
                            case ComponentTypeTN.Plasma:
                            case ComponentTypeTN.AdvPlasma:
                            case ComponentTypeTN.Rail:
                            case ComponentTypeTN.AdvRail:
                            case ComponentTypeTN.Particle:
                            case ComponentTypeTN.AdvParticle:
                                Entry = "Damage";
                                Entry2 = List.BeamWeaponDef[ComponentIterator].damage[0].ToString();
                                break;

                            case ComponentTypeTN.Meson:
                            case ComponentTypeTN.Microwave:
                                Entry = "Range";
                                Entry2 = (List.BeamWeaponDef[ComponentIterator].range / 10000.0f).ToString();
                                break;

                            case ComponentTypeTN.Gauss:
                                Entry = "Rate of Fire";
                                Entry2 = List.BeamWeaponDef[ComponentIterator].shotCount.ToString();
                                break;
                        }

                        PopulateComponentRow(List.BeamWeaponDef[ComponentIterator], row, Entry, Entry2, ComponentIterator);
                        row++;
                    }

                    for (int ComponentIterator = 0; ComponentIterator < List.CIWSDef.Count; ComponentIterator++)
                    {
                        Entry = "Damage";
                        Entry2 = List.CIWSDef[ComponentIterator].rOF.ToString();
                        PopulateComponentRow(List.CIWSDef[ComponentIterator], row, Entry, Entry2, ComponentIterator);
                        row++;
                    }

                    for (int ComponentIterator = 0; ComponentIterator < List.TurretDef.Count; ComponentIterator++)
                    {
                        switch (List.TurretDef[ComponentIterator].baseBeamWeapon.componentType)
                        {
                            case ComponentTypeTN.Laser:
                            case ComponentTypeTN.AdvLaser:
                               Entry = "Damage";
                               Entry2 = List.TurretDef[ComponentIterator].baseBeamWeapon.damage[0].ToString();
                            break;

                            case ComponentTypeTN.Meson:
                                Entry = "Range";
                                Entry2 = (List.TurretDef[ComponentIterator].baseBeamWeapon.range / 10000.0f).ToString();
                            break;

                            case ComponentTypeTN.Gauss:
                               Entry = "Rate of Fire";
                               Entry2 = List.TurretDef[ComponentIterator].totalShotCount.ToString();
                            break;
                        }
                        PopulateComponentRow(List.TurretDef[ComponentIterator], row, Entry, Entry2, ComponentIterator);
                        row++;
                    }
                    #endregion

                    #region Missile/Torpedo Launchers (Plasma torpedos not yet implemented)
                    using (DataGridViewRow NewRow = new DataGridViewRow())
                    {
                        /// <summary>
                        /// setup row height. note that by default they are 22 pixels in height!
                        /// </summary>
                        NewRow.Height = 18;
                        m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                        DataGridViewCellStyle style = new DataGridViewCellStyle();
                        style.Font = new System.Drawing.Font(m_oOptionsPanel.ComponentDataGrid.Font, System.Drawing.FontStyle.Bold);
                        m_oOptionsPanel.ComponentDataGrid.Rows[row].DefaultCellStyle = style;


                        m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = "Missile/Torpedo Launchers";

                        CompLocation.Add(row);

                        row++;
                    }

                    for (int ComponentIterator = 0; ComponentIterator < List.MLauncherDef.Count; ComponentIterator++)
                    {
                        PopulateComponentRow(List.MLauncherDef[ComponentIterator], row, "Max Missile Size", List.MLauncherDef[ComponentIterator].launchMaxSize.ToString(), ComponentIterator);
                        row++;
                    }
                    #endregion

                    #region Magazines
                    using (DataGridViewRow NewRow = new DataGridViewRow())
                    {
                        /// <summary>
                        /// setup row height. note that by default they are 22 pixels in height!
                        /// </summary>
                        NewRow.Height = 18;
                        m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                        DataGridViewCellStyle style = new DataGridViewCellStyle();
                        style.Font = new System.Drawing.Font(m_oOptionsPanel.ComponentDataGrid.Font, System.Drawing.FontStyle.Bold);
                        m_oOptionsPanel.ComponentDataGrid.Rows[row].DefaultCellStyle = style;


                        m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = "Magazines";

                        CompLocation.Add(row);

                        row++;
                    }

                    for (int ComponentIterator = 0; ComponentIterator < List.MagazineDef.Count; ComponentIterator++)
                    {
                        PopulateComponentRow(List.MagazineDef[ComponentIterator], row, "Ordnance Storage", List.MagazineDef[ComponentIterator].capacity.ToString(), ComponentIterator);
                        row++;
                    }
                    #endregion

                    #region Planetary Combat (Troop bays Not yet implemented)

                    using (DataGridViewRow NewRow = new DataGridViewRow())
                    {
                        /// <summary>
                        /// setup row height. note that by default they are 22 pixels in height!
                        /// </summary>
                        NewRow.Height = 18;
                        m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                        DataGridViewCellStyle style = new DataGridViewCellStyle();
                        style.Font = new System.Drawing.Font(m_oOptionsPanel.ComponentDataGrid.Font, System.Drawing.FontStyle.Bold);
                        m_oOptionsPanel.ComponentDataGrid.Rows[row].DefaultCellStyle = style;


                        m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = "Planetary Combat";

                        CompLocation.Add(row);

                        row++;
                    }

                    #endregion

                    #region Power Plants
                    using (DataGridViewRow NewRow = new DataGridViewRow())
                    {
                        /// <summary>
                        /// setup row height. note that by default they are 22 pixels in height!
                        /// </summary>
                        NewRow.Height = 18;
                        m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                        DataGridViewCellStyle style = new DataGridViewCellStyle();
                        style.Font = new System.Drawing.Font(m_oOptionsPanel.ComponentDataGrid.Font, System.Drawing.FontStyle.Bold);
                        m_oOptionsPanel.ComponentDataGrid.Rows[row].DefaultCellStyle = style;


                        m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = "Power Plants";

                        CompLocation.Add(row);

                        row++;
                    }

                    for (int ComponentIterator = 0; ComponentIterator < List.ReactorDef.Count; ComponentIterator++)
                    {
                        PopulateComponentRow(List.ReactorDef[ComponentIterator], row, "Power Produced", List.ReactorDef[ComponentIterator].powerGen.ToString(), ComponentIterator);
                        row++;
                    }
                    #endregion

                    #region Active Sensors
                    using (DataGridViewRow NewRow = new DataGridViewRow())
                    {
                        /// <summary>
                        /// setup row height. note that by default they are 22 pixels in height!
                        /// </summary>
                        NewRow.Height = 18;
                        m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                        DataGridViewCellStyle style = new DataGridViewCellStyle();
                        style.Font = new System.Drawing.Font(m_oOptionsPanel.ComponentDataGrid.Font, System.Drawing.FontStyle.Bold);
                        m_oOptionsPanel.ComponentDataGrid.Rows[row].DefaultCellStyle = style;


                        m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = "Active Sensors";

                        CompLocation.Add(row);

                        row++;
                    }

                    for (int ComponentIterator = 0; ComponentIterator < List.ActiveSensorDef.Count; ComponentIterator++)
                    {
                        /// <summary>
                        /// 1 Billion in 10k km increments test here. 100000 = 1B
                        /// </summary>
                        if (List.ActiveSensorDef[ComponentIterator].maxRange >= 100000)
                        {
                            float RNG = (float)(List.ActiveSensorDef[ComponentIterator].maxRange) / 100000.0f;
                            Entry = String.Format("{0:N1}B", RNG);
                        }
                        else if (List.ActiveSensorDef[ComponentIterator].maxRange >= 100)
                        {
                            float RNG = (float)(List.ActiveSensorDef[ComponentIterator].maxRange) / 100.0f;
                            Entry = String.Format("{0:N1}M", RNG);
                        }
                        else
                        {
                            float RNG = (float)List.ActiveSensorDef[ComponentIterator].maxRange;
                            Entry = String.Format("{0:N1}K", RNG);
                        }

                        PopulateComponentRow(List.ActiveSensorDef[ComponentIterator], row, "Range km", Entry, ComponentIterator);
                        row++;
                    }
                    #endregion

                    #region Passive Sensors (Geo/Grav not yet implemented)
                    using (DataGridViewRow NewRow = new DataGridViewRow())
                    {
                        /// <summary>
                        /// setup row height. note that by default they are 22 pixels in height!
                        /// </summary>
                        NewRow.Height = 18;
                        m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                        DataGridViewCellStyle style = new DataGridViewCellStyle();
                        style.Font = new System.Drawing.Font(m_oOptionsPanel.ComponentDataGrid.Font, System.Drawing.FontStyle.Bold);
                        m_oOptionsPanel.ComponentDataGrid.Rows[row].DefaultCellStyle = style;


                        m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = "Passive Sensors";

                        CompLocation.Add(row);

                        row++;
                    }

                    for (int ComponentIterator = 0; ComponentIterator < List.PassiveSensorDef.Count; ComponentIterator++)
                    {
                        PopulateComponentRow(List.PassiveSensorDef[ComponentIterator], row, "Sensor Strength", List.PassiveSensorDef[ComponentIterator].rating.ToString(), ComponentIterator);
                        row++;
                    }
                    #endregion

                    #region Shields / Electronic Warfare (Cloak not implemented,ecm/eccm not implemented, absorption shield not implemented)
                    using (DataGridViewRow NewRow = new DataGridViewRow())
                    {
                        /// <summary>
                        /// setup row height. note that by default they are 22 pixels in height!
                        /// </summary>
                        NewRow.Height = 18;
                        m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                        DataGridViewCellStyle style = new DataGridViewCellStyle();
                        style.Font = new System.Drawing.Font(m_oOptionsPanel.ComponentDataGrid.Font, System.Drawing.FontStyle.Bold);
                        m_oOptionsPanel.ComponentDataGrid.Rows[row].DefaultCellStyle = style;


                        m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = "Shields / Electronic Warfare";

                        CompLocation.Add(row);

                        row++;
                    }

                    for (int ComponentIterator = 0; ComponentIterator < List.ShieldDef.Count; ComponentIterator++)
                    {
                        PopulateComponentRow(List.ShieldDef[ComponentIterator], row, "Shield Strength", List.ShieldDef[ComponentIterator].shieldPool.ToString(), ComponentIterator);
                        row++;
                    }
                    #endregion

                    #region Transportation and Industry (Hangar, Maint Module, Terraformer, Sorium Harvester, Tractor, Salvage, Luxury, construction module not yet implemented)
                    using (DataGridViewRow NewRow = new DataGridViewRow())
                    {
                        /// <summary>
                        /// setup row height. note that by default they are 22 pixels in height!
                        /// </summary>
                        NewRow.Height = 18;
                        m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                        DataGridViewCellStyle style = new DataGridViewCellStyle();
                        style.Font = new System.Drawing.Font(m_oOptionsPanel.ComponentDataGrid.Font, System.Drawing.FontStyle.Bold);
                        m_oOptionsPanel.ComponentDataGrid.Rows[row].DefaultCellStyle = style;


                        m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = "Transportation and Industry";

                        CompLocation.Add(row);

                        row++;
                    }

                    for (int ComponentIterator = 0; ComponentIterator < List.CargoHandleSystemDef.Count; ComponentIterator++)
                    {
                        PopulateComponentRow(List.CargoHandleSystemDef[ComponentIterator], row, "Cargo Handling Multiplier", 
                                             List.CargoHandleSystemDef[ComponentIterator].tractorMultiplier.ToString(), ComponentIterator);
                        row++;
                    }

                    for (int ComponentIterator = 0; ComponentIterator < List.CargoHoldDef.Count; ComponentIterator++)
                    {
                        PopulateComponentRow(List.CargoHoldDef[ComponentIterator], row, "Cargo Capacity",
                                             List.CargoHoldDef[ComponentIterator].cargoCapacity.ToString(), ComponentIterator);
                        row++;
                    }

                    for (int ComponentIterator = 0; ComponentIterator < List.ColonyBayDef.Count; ComponentIterator++)
                    {
                        PopulateComponentRow(List.ColonyBayDef[ComponentIterator], row, "Colonist Capacity",
                                             List.ColonyBayDef[ComponentIterator].cryoBerths.ToString(), ComponentIterator);
                        row++;
                    }

                    /// <summary>
                    /// Now for TypeCount. This is needed for behind the scenes reasons I believe.
                    /// </summary>
                    CompLocation.Add(row);
                    #endregion

                }
                catch
                {
#if LOG4NET_ENABLED
                    logger.Error("Something went wrong Creating Rows for Class Design ComponentGrid screen...");
#endif
                }
            }
            else if (TotalComponents != List.TotalComponents)
            {
                //rowCount = 1;
                //m_oOptionsPanel.ComponentDataGrid.Rows.Insert(int rowIndex, int rowCount);
                try
                {

                    #region Basic Component Addition
                    /// <summary>
                    /// A basic Component was added.
                    /// </summary>
                    if (CompLocation[(int)ComponentGroup.Engines] != (List.CrewQuarters.Count + List.FuelStorage.Count + List.EngineeringSpaces.Count + List.OtherComponents.Count + 1))
                    {
                        int CQCount = 0, FSCount = 0, ESCount = 0, OCCount = 0;
                        int rowLine = 1;

                        /// <summary>
                        /// Count the crew quarters rows:
                        /// </summary>
                        while ((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value == ((int)ComponentTypeTN.Crew).ToString())
                        {
                            rowLine++;
                        }

                        CQCount = rowLine - 1;

                        int AddedRows = List.CrewQuarters.Count - CQCount;

                        /// <summary>
                        /// Increment all the component locations past the current one(Engine) by added rows count.
                        /// </summary>
                        for (int loop = (int)ComponentGroup.Engines; loop <= (int)ComponentGroup.TypeCount; loop++)
                        {
                            CompLocation[loop] = CompLocation[loop] + AddedRows;
                        }

                        /// <summary>
                        /// insert and fill in the rows where appropriate.
                        /// </summary>
                        for (int ComponentIterator = CQCount; ComponentIterator <= (List.CrewQuarters.Count - 1); ComponentIterator++)
                        {
                            PopulateComponentRow(List.CrewQuarters[ComponentIterator], rowLine, "Life Support", (List.CrewQuarters[ComponentIterator].size * 50.0f).ToString(), ComponentIterator,true);
                            rowLine++;
                        }

                        /// <summary>
                        /// Fuel Storage Section.
                        /// </summary>
                        int CQEnd = rowLine;

                        while ((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value == ((int)ComponentTypeTN.Fuel).ToString())
                        {
                            rowLine++;
                        }

                        FSCount = rowLine - CQEnd;

                        AddedRows = List.FuelStorage.Count - FSCount;

                        /// <summary>
                        /// Increment all the component locations past the current one(Engine) by added rows count.
                        /// </summary>
                        for (int loop = (int)ComponentGroup.Engines; loop <= (int)ComponentGroup.TypeCount; loop++)
                        {
                            CompLocation[loop] = CompLocation[loop] + AddedRows;
                        }

                        /// <summary>
                        /// insert and fill in the rows where appropriate.
                        /// </summary>
                        for (int ComponentIterator = FSCount; ComponentIterator <= (List.FuelStorage.Count - 1); ComponentIterator++)
                        {
                            PopulateComponentRow(List.FuelStorage[ComponentIterator], rowLine, "Litres of Fuel", (List.FuelStorage[ComponentIterator].size * 50000.0f).ToString(), ComponentIterator,true);
                            rowLine++;
                        }

                        /// <summary>
                        /// Engineering spaces Section
                        /// </summary>
                        int FSEnd = rowLine;

                        while ((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value == ((int)ComponentTypeTN.Engineering).ToString())
                        {
                            rowLine++;
                        }

                        ESCount = rowLine - FSEnd;

                        AddedRows = List.EngineeringSpaces.Count - ESCount;

                        /// <summary>
                        /// Increment all the component locations past the current one(Engine) by added rows count.
                        /// </summary>
                        for (int loop = (int)ComponentGroup.Engines; loop <= (int)ComponentGroup.TypeCount; loop++)
                        {
                            CompLocation[loop] = CompLocation[loop] + AddedRows;
                        }

                        /// <summary>
                        /// insert and fill in the rows where appropriate.
                        /// </summary>
                        for (int ComponentIterator = ESCount; ComponentIterator <= (List.EngineeringSpaces.Count - 1); ComponentIterator++)
                        {
                            PopulateComponentRow(List.EngineeringSpaces[ComponentIterator], rowLine, "Failure Rate", List.EngineeringSpaces[ComponentIterator].size.ToString(), ComponentIterator,true);
                            rowLine++;
                        }

                        /// <summary>
                        /// Other Component Section.
                        /// </summary>
                        int ESEnd = rowLine;

                        int out1, out2;

                        int.TryParse((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value, out out1);
                        int.TryParse((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value, out out2);

                        while (out1 >= ((int)ComponentTypeTN.Bridge) && out2 <= ((int)ComponentTypeTN.RecFacility))
                        {
                            rowLine++;

                            int.TryParse((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value, out out1);
                            int.TryParse((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value, out out2);
                        }

                        OCCount = rowLine - ESEnd;

                        AddedRows = List.OtherComponents.Count - OCCount;

                        /// <summary>
                        /// Increment all the component locations past the current one(Engine) by added rows count.
                        /// </summary>
                        for (int loop = (int)ComponentGroup.Engines; loop <= (int)ComponentGroup.TypeCount; loop++)
                        {
                            CompLocation[loop] = CompLocation[loop] + AddedRows;
                        }

                        /// <summary>
                        /// insert and fill in the rows where appropriate.
                        /// </summary>
                        for (int ComponentIterator = OCCount; ComponentIterator <= (List.OtherComponents.Count - 1); ComponentIterator++)
                        {

                            switch (List.OtherComponents[ComponentIterator].componentType)
                            {
                                case ComponentTypeTN.Bridge:
                                    Entry = "CommandControl";
                                    Entry2 = "1";
                                    break;
                                case ComponentTypeTN.MaintenanceBay:
                                    Entry = "MaintStorage";
                                    Entry2 = "1000";
                                    break;
                                case ComponentTypeTN.OrbitalHabitat:
                                    Entry = "Worker Capacity";
                                    Entry2 = "50000";
                                    break;
                                case ComponentTypeTN.RecFacility:
                                    Entry = "Crew Recreation";
                                    Entry2 = "0";
                                    break;
                                default:
                                    Entry = "UNK Component";
                                    Entry2 = "*********UNK*********";
                                    break;
                            }

                            PopulateComponentRow(List.OtherComponents[ComponentIterator], rowLine, Entry, Entry2, ComponentIterator,true);
                            rowLine++;
                        }
                    }
                    #endregion

                    #region Engine Addition
                    /// <summary>
                    /// An Engine or JumpEngine was added to the component list.
                    /// </summary>
                    if (CompLocation[(int)ComponentGroup.FireControl] != (List.Engines.Count + List.JumpEngineDef.Count + CompLocation[(int)ComponentGroup.Engines] + 1))
                    {
                        int rowLine = CompLocation[(int)ComponentGroup.Engines] + 1;
                        int EngineCount = 0;

                        /// <summary>
                        /// Count the rows that are already filled with component type 10 or "Engine"
                        /// </summary>
                        while ((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value == ((int)ComponentTypeTN.Engine).ToString())
                        {
                            rowLine++;
                        }
                        EngineCount = (rowLine - CompLocation[(int)ComponentGroup.Engines] - 1);

                        int AddedRows = List.Engines.Count - EngineCount;

                        /// <summary>
                        /// Increment all the component locations past the current one(Engine) by added rows count.
                        /// </summary>
                        for (int loop = (int)ComponentGroup.FireControl; loop <= (int)ComponentGroup.TypeCount; loop++)
                        {
                            CompLocation[loop] = CompLocation[loop] + AddedRows;
                        }

                        /// <summary>
                        /// insert and fill in the rows where appropriate.
                        /// </summary>
                        for (int ComponentIterator = EngineCount; ComponentIterator <= (List.Engines.Count - 1); ComponentIterator++)
                        {
                            PopulateComponentRow(List.Engines[ComponentIterator], rowLine, "Engine Power", List.Engines[ComponentIterator].enginePower.ToString(), ComponentIterator, true);
                            rowLine++;
                        }

                        int EngEnd = rowLine;
                        int JECount = 0;

                        while ((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value == ((int)ComponentTypeTN.JumpEngine).ToString())
                        {
                            rowLine++;
                        }

                        JECount = (rowLine - EngEnd);

                        AddedRows = List.JumpEngineDef.Count - JECount;

                        /// <summary>
                        /// Increment all the component locations past the current one(Engine) by added rows count.
                        /// </summary>
                        for (int loop = (int)ComponentGroup.FireControl; loop <= (int)ComponentGroup.TypeCount; loop++)
                        {
                            CompLocation[loop] = CompLocation[loop] + AddedRows;
                        }

                        for (int ComponentIterator = JECount; ComponentIterator <= (List.JumpEngineDef.Count - 1); ComponentIterator++)
                        {
                            PopulateComponentRow(List.JumpEngineDef[ComponentIterator], rowLine, "Max Ship Size",
                                             ((float)List.JumpEngineDef[ComponentIterator].maxJumpRating / Constants.ShipTN.TonsPerHS).ToString(), ComponentIterator, true);
                            rowLine++;
                        }
                    }
                    #endregion

                    #region Fire Control Addition
                    if (CompLocation[(int)ComponentGroup.Beam] != (List.BeamFireControlDef.Count + List.MissileFireControlDef.Count + CompLocation[(int)ComponentGroup.FireControl] + 1))
                    {
                        int rowLine = CompLocation[(int)ComponentGroup.FireControl] + 1;
                        int BFCCount = 0;

                        /// <summary>
                        /// Count the rows that are already filled with component type 16 or "BFC"
                        /// </summary>
                        while ((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value == ((int)ComponentTypeTN.BeamFireControl).ToString())
                        {
                            rowLine++;
                        }
                        BFCCount = (rowLine - CompLocation[(int)ComponentGroup.FireControl] - 1);

                        int AddedRows = List.BeamFireControlDef.Count - BFCCount;

                        /// <summary>
                        /// Increment all the component locations past the current one(FireControl) by added rows count.
                        /// </summary>
                        for (int loop = (int)ComponentGroup.Beam; loop <= (int)ComponentGroup.TypeCount; loop++)
                        {
                            CompLocation[loop] = CompLocation[loop] + AddedRows;
                        }

                        /// <summary>
                        /// insert and fill in the rows where appropriate.
                        /// </summary>
                        for (int ComponentIterator = BFCCount; ComponentIterator <= (List.BeamFireControlDef.Count - 1); ComponentIterator++)
                        {
                            PopulateComponentRow(List.BeamFireControlDef[ComponentIterator], rowLine, "50% Acc. Distance", 
                                                 List.BeamFireControlDef[ComponentIterator].range.ToString(), ComponentIterator, true);
                            rowLine++;
                        }


                        /// <summary>
                        /// Missile fire control Section.
                        /// </summary>
                        int BFCEnd = rowLine;
                        int MFCCount = 0;

                        /// <summary>
                        /// Count the rows that are already filled with component type 33 or "MFC"
                        /// </summary>
                        while ((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value == ((int)ComponentTypeTN.MissileFireControl).ToString())
                        {
                            rowLine++;
                        }
                        MFCCount = (rowLine - BFCEnd);

                        AddedRows = List.MissileFireControlDef.Count - MFCCount;

                        /// <summary>
                        /// Increment all the component locations past the current one(FireControl) by added rows count.
                        /// </summary>
                        for (int loop = (int)ComponentGroup.Beam; loop <= (int)ComponentGroup.TypeCount; loop++)
                        {
                            CompLocation[loop] = CompLocation[loop] + AddedRows;
                        }

                        /// <summary>
                        /// insert and fill in the rows where appropriate.
                        /// </summary>
                        for (int ComponentIterator = MFCCount; ComponentIterator <= (List.MissileFireControlDef.Count - 1); ComponentIterator++)
                        {
                            if (List.MissileFireControlDef[ComponentIterator].maxRange >= 100000)
                            {
                                float RNG = (float)(List.MissileFireControlDef[ComponentIterator].maxRange) / 100000.0f;
                                Entry = String.Format("{0:N1}B", RNG);
                            }
                            else if (List.MissileFireControlDef[ComponentIterator].maxRange >= 100)
                            {
                                float RNG = (float)(List.MissileFireControlDef[ComponentIterator].maxRange) / 100.0f;
                                Entry = String.Format("{0:N1}M", RNG);
                            }
                            else
                            {
                                float RNG = (float)List.MissileFireControlDef[ComponentIterator].maxRange;
                                Entry = String.Format("{0:N1}K", RNG);
                            }

                            PopulateComponentRow(List.MissileFireControlDef[ComponentIterator], rowLine, "Range km", Entry, ComponentIterator, true);
                            rowLine++;
                        }
                    }
                    #endregion

                    #region Energy Weapon Addition / CIWS / Turrets
                    /// <summary>
                    /// A Beam weapon/CIWS/Turret was added.
                    /// </summary>
                    if (CompLocation[(int)ComponentGroup.Missiles] != (List.BeamWeaponDef.Count + List.CIWSDef.Count + List.TurretDef.Count + CompLocation[(int)ComponentGroup.Beam] + 1))
                    {
                        int rowLine = CompLocation[(int)ComponentGroup.Beam] + 1;
                        int BeamCount = 0;

                        /// <summary>
                        /// Count the rows that are already filled with component types corresponding to beam weapons.
                        /// </summary>
                        int out1, out2;

                        int.TryParse((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value, out out1);
                        int.TryParse((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value, out out2);

                        while (out1 >= ((int)ComponentTypeTN.Rail) && out2 <= ((int)ComponentTypeTN.AdvParticle))
                        {
                            rowLine++;

                            int.TryParse((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value, out out1);
                            int.TryParse((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value, out out2);
                        }

                        BeamCount = (rowLine - CompLocation[(int)ComponentGroup.Beam] - 1);

                        int AddedRows = List.BeamWeaponDef.Count - BeamCount;

                        /// <summary>
                        /// Increment all the component locations past the current one(Beam) by added rows count.
                        /// </summary>
                        for (int loop = (int)ComponentGroup.Missiles; loop <= (int)ComponentGroup.TypeCount; loop++)
                        {
                            CompLocation[loop] = CompLocation[loop] + AddedRows;
                        }

                        /// <summary>
                        /// insert and fill in the rows where appropriate.
                        /// </summary>
                        for (int ComponentIterator = BeamCount; ComponentIterator <= (List.BeamWeaponDef.Count - 1); ComponentIterator++)
                        {
                            switch (List.BeamWeaponDef[ComponentIterator].componentType)
                            {
                                case ComponentTypeTN.Laser:
                                case ComponentTypeTN.AdvLaser:
                                case ComponentTypeTN.Plasma:
                                case ComponentTypeTN.AdvPlasma:
                                case ComponentTypeTN.Rail:
                                case ComponentTypeTN.AdvRail:
                                case ComponentTypeTN.Particle:
                                case ComponentTypeTN.AdvParticle:
                                    Entry = "Damage";
                                    Entry2 = List.BeamWeaponDef[ComponentIterator].damage[0].ToString();
                                    break;

                                case ComponentTypeTN.Meson:
                                case ComponentTypeTN.Microwave:
                                    Entry = "Range";
                                    Entry2 = (List.BeamWeaponDef[ComponentIterator].range / 10000.0f).ToString();
                                    break;

                                case ComponentTypeTN.Gauss:
                                    Entry = "Rate of Fire";
                                    Entry2 = List.BeamWeaponDef[ComponentIterator].shotCount.ToString();
                                    break;
                            }

                            PopulateComponentRow(List.BeamWeaponDef[ComponentIterator], rowLine, Entry, Entry2, ComponentIterator, true);
                            rowLine++;
                        }

                        int BWEnd = rowLine;

                        /// <summary>
                        /// Advance through all the CIWS
                        /// </summary>
                        while ((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value == ((int)ComponentTypeTN.CIWS).ToString())
                        {
                            rowLine++;
                        }

                        int CIWSCount = rowLine - BWEnd;

                        AddedRows = List.CIWSDef.Count - CIWSCount;

                        /// <summary>
                        /// Increment all the component locations past the current one(Missiles) by added rows count.
                        /// </summary>
                        for (int loop = (int)ComponentGroup.Missiles; loop <= (int)ComponentGroup.TypeCount; loop++)
                        {
                            CompLocation[loop] = CompLocation[loop] + AddedRows;
                        }

                        /// <summary>
                        /// insert and fill in the rows where appropriate.
                        /// </summary>
                        for (int ComponentIterator = CIWSCount; ComponentIterator < List.CIWSDef.Count; ComponentIterator++)
                        {
                            Entry = "Damage";
                            Entry2 = List.CIWSDef[ComponentIterator].rOF.ToString();
                            PopulateComponentRow(List.CIWSDef[ComponentIterator], rowLine, Entry, Entry2, ComponentIterator, true);
                            rowLine++;
                        }


                        int CIWSEnd = rowLine;

                        /// <summary>
                        /// Advance through all the Turrets
                        /// </summary>
                        while ((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value == ((int)ComponentTypeTN.Turret).ToString())
                        {
                            rowLine++;
                        }

                        int TurretCount = rowLine - CIWSEnd;

                        AddedRows = List.TurretDef.Count - TurretCount;

                        /// <summary>
                        /// Increment all the component locations past the current one(Missiles) by added rows count.
                        /// </summary>
                        for (int loop = (int)ComponentGroup.Missiles; loop <= (int)ComponentGroup.TypeCount; loop++)
                        {
                            CompLocation[loop] = CompLocation[loop] + AddedRows;
                        }

                        /// <summary>
                        /// insert and fill in the rows where appropriate.
                        /// </summary>
                        for (int ComponentIterator = TurretCount; ComponentIterator < List.TurretDef.Count; ComponentIterator++)
                        {
                            switch (List.TurretDef[ComponentIterator].baseBeamWeapon.componentType)
                            {
                                case ComponentTypeTN.Laser:
                                case ComponentTypeTN.AdvLaser:
                                    Entry = "Damage";
                                    Entry2 = List.TurretDef[ComponentIterator].baseBeamWeapon.damage[0].ToString();
                                    break;

                                case ComponentTypeTN.Meson:
                                    Entry = "Range";
                                    Entry2 = (List.TurretDef[ComponentIterator].baseBeamWeapon.range / 10000.0f).ToString();
                                    break;

                                case ComponentTypeTN.Gauss:
                                    Entry = "Rate of Fire";
                                    Entry2 = List.TurretDef[ComponentIterator].totalShotCount.ToString();
                                    break;
                            }

                            PopulateComponentRow(List.TurretDef[ComponentIterator], rowLine, Entry, Entry2, ComponentIterator, true);
                            rowLine++;
                        }
                    }
                    #endregion

                    #region Missile/Torpedo Launchers(Plasma torps not implemented)
                    /// <summary>
                    /// An engine was added to the component list.
                    /// </summary>
                    if (CompLocation[(int)ComponentGroup.Magazines] != (List.MLauncherDef.Count + CompLocation[(int)ComponentGroup.Missiles] + 1))
                    {
                        int rowLine = CompLocation[(int)ComponentGroup.Missiles] + 1;
                        int MLauncherCount = 0;

                        /// <summary>
                        /// Count the rows that are already filled with component type 31 or "Missile Launcher"
                        /// </summary>
                        while ((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value == ((int)ComponentTypeTN.MissileLauncher).ToString())
                        {
                            rowLine++;
                        }
                        MLauncherCount = (rowLine - CompLocation[(int)ComponentGroup.Missiles] - 1);

                        int AddedRows = List.MLauncherDef.Count - MLauncherCount;

                        /// <summary>
                        /// Increment all the component locations past the current one(Magazine) by added rows count.
                        /// </summary>
                        for (int loop = (int)ComponentGroup.Magazines; loop <= (int)ComponentGroup.TypeCount; loop++)
                        {
                            CompLocation[loop] = CompLocation[loop] + AddedRows;
                        }

                        /// <summary>
                        /// insert and fill in the rows where appropriate.
                        /// </summary>
                        for (int ComponentIterator = MLauncherCount; ComponentIterator <= (List.MLauncherDef.Count - 1); ComponentIterator++)
                        {
                            PopulateComponentRow(List.MLauncherDef[ComponentIterator], rowLine, "Max Missile Size", List.MLauncherDef[ComponentIterator].launchMaxSize.ToString(), ComponentIterator, true);
                            rowLine++; 
                        }
                    }
                    #endregion

                    #region Magazines
                    /// <summary>
                    /// An engine was added to the component list.
                    /// </summary>
                    if (CompLocation[(int)ComponentGroup.Ground] != (List.MagazineDef.Count + CompLocation[(int)ComponentGroup.Magazines] + 1))
                    {
                        int rowLine = CompLocation[(int)ComponentGroup.Magazines] + 1;
                        int MagazineCount = 0;

                        /// <summary>
                        /// Count the rows that are already filled with component type 32 or "Magazines"
                        /// </summary>
                        while ((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value == ((int)ComponentTypeTN.Magazine).ToString())
                        {
                            rowLine++;
                        }
                        MagazineCount = (rowLine - CompLocation[(int)ComponentGroup.Magazines] - 1);

                        int AddedRows = List.MagazineDef.Count - MagazineCount;

                        /// <summary>
                        /// Increment all the component locations past the current one(Engine) by added rows count.
                        /// </summary>
                        for (int loop = (int)ComponentGroup.Ground; loop <= (int)ComponentGroup.TypeCount; loop++)
                        {
                            CompLocation[loop] = CompLocation[loop] + AddedRows;
                        }

                        /// <summary>
                        /// insert and fill in the rows where appropriate.
                        /// </summary>
                        for (int ComponentIterator = MagazineCount; ComponentIterator <= (List.MagazineDef.Count - 1); ComponentIterator++)
                        {
                            PopulateComponentRow(List.MagazineDef[ComponentIterator], rowLine, "Ordnance Storage", List.MagazineDef[ComponentIterator].capacity.ToString(), ComponentIterator, true);
                            rowLine++;
                        }
                    }
                    #endregion

                    #region Planetary Combat (Troop bays et all not yet implemented)
                    #endregion

                    #region Reactor
                    /// <summary>
                    /// An engine was added to the component list.
                    /// </summary>
                    if (CompLocation[(int)ComponentGroup.Actives] != (List.ReactorDef.Count + CompLocation[(int)ComponentGroup.Reactors] + 1))
                    {
                        int rowLine = CompLocation[(int)ComponentGroup.Reactors] + 1;
                        int ReactorCount = 0;

                        /// <summary>
                        /// Count the rows that are already filled with component type 28 or "Reactor"
                        /// </summary>
                        while ((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value == ((int)ComponentTypeTN.Reactor).ToString())
                        {
                            rowLine++;
                        }
                        ReactorCount = (rowLine - CompLocation[(int)ComponentGroup.Reactors] - 1);

                        int AddedRows = List.ReactorDef.Count - ReactorCount;

                        /// <summary>
                        /// Increment all the component locations past the current one(Engine) by added rows count.
                        /// </summary>
                        for (int loop = (int)ComponentGroup.Actives; loop <= (int)ComponentGroup.TypeCount; loop++)
                        {
                            CompLocation[loop] = CompLocation[loop] + AddedRows;
                        }

                        /// <summary>
                        /// insert and fill in the rows where appropriate.
                        /// </summary>
                        for (int ComponentIterator = ReactorCount; ComponentIterator <= (List.ReactorDef.Count - 1); ComponentIterator++)
                        {
                            PopulateComponentRow(List.ReactorDef[ComponentIterator], rowLine, "Power Produced", List.ReactorDef[ComponentIterator].powerGen.ToString(), ComponentIterator, true);
                            rowLine++;
                        }
                    }
                    #endregion

                    #region Actives
                    /// <summary>
                    /// An engine was added to the component list.
                    /// </summary>
                    if (CompLocation[(int)ComponentGroup.Passives] != (List.ActiveSensorDef.Count + CompLocation[(int)ComponentGroup.Actives] + 1))
                    {
                        int rowLine = CompLocation[(int)ComponentGroup.Actives] + 1;
                        int ActiveCount = 0;

                        /// <summary>
                        /// Count the rows that are already filled with component type 12 or "Active"
                        /// </summary>
                        while ((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value == ((int)ComponentTypeTN.ActiveSensor).ToString())
                        {
                            rowLine++;
                        }
                        ActiveCount = (rowLine - CompLocation[(int)ComponentGroup.Actives] - 1);

                        int AddedRows = List.ActiveSensorDef.Count - ActiveCount;

                        /// <summary>
                        /// Increment all the component locations past the current one(Engine) by added rows count.
                        /// </summary>
                        for (int loop = (int)ComponentGroup.Passives; loop <= (int)ComponentGroup.TypeCount; loop++)
                        {
                            CompLocation[loop] = CompLocation[loop] + AddedRows;
                        }

                        /// <summary>
                        /// insert and fill in the rows where appropriate.
                        /// </summary>
                        for (int ComponentIterator = ActiveCount; ComponentIterator <= (List.ActiveSensorDef.Count - 1); ComponentIterator++)
                        {
                            if (List.ActiveSensorDef[ComponentIterator].maxRange >= 100000)
                            {
                                float RNG = (float)(List.ActiveSensorDef[ComponentIterator].maxRange) / 100000.0f;
                                Entry = String.Format("{0:N1}B", RNG);
                            }
                            else if (List.ActiveSensorDef[ComponentIterator].maxRange >= 100)
                            {
                                float RNG = (float)(List.ActiveSensorDef[ComponentIterator].maxRange) / 100.0f;
                                Entry = String.Format("{0:N1}M", RNG);
                            }
                            else
                            {
                                float RNG = (float)List.ActiveSensorDef[ComponentIterator].maxRange;
                                Entry = String.Format("{0:N1}K", RNG);
                            }

                            PopulateComponentRow(List.ActiveSensorDef[ComponentIterator], rowLine, "Range km", Entry, ComponentIterator, true);
                            rowLine++;
                        }
                    }
                    #endregion

                    #region Passives
                    /// <summary>
                    /// An engine was added to the component list.
                    /// </summary>
                    if (CompLocation[(int)ComponentGroup.Shields] != (List.PassiveSensorDef.Count + CompLocation[(int)ComponentGroup.Passives] + 1))
                    {
                        int rowLine = CompLocation[(int)ComponentGroup.Passives] + 1;
                        int PassiveCount = 0;

                        /// <summary>
                        /// Count the rows that are already filled with component type 11 or "Passive"
                        /// </summary>
                        while ((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value == ((int)ComponentTypeTN.PassiveSensor).ToString())
                        {
                            rowLine++;
                        }
                        PassiveCount = (rowLine - CompLocation[(int)ComponentGroup.Passives] - 1);

                        int AddedRows = List.PassiveSensorDef.Count - PassiveCount;

                        /// <summary>
                        /// Increment all the component locations past the current one(Engine) by added rows count.
                        /// </summary>
                        for (int loop = (int)ComponentGroup.Shields; loop <= (int)ComponentGroup.TypeCount; loop++)
                        {
                            CompLocation[loop] = CompLocation[loop] + AddedRows;
                        }

                        /// <summary>
                        /// insert and fill in the rows where appropriate.
                        /// </summary>
                        for (int ComponentIterator = PassiveCount; ComponentIterator <= (List.PassiveSensorDef.Count - 1); ComponentIterator++)
                        {
                            PopulateComponentRow(List.PassiveSensorDef[ComponentIterator], rowLine, "Sensor Strength", List.PassiveSensorDef[ComponentIterator].rating.ToString(), ComponentIterator, true);
                            rowLine++;
                        }
                    }
                    #endregion

                    #region Shields (ECM,ECCM,Cloaks,Absorption shields not yet implemented)
                    /// <summary>
                    /// An engine was added to the component list.
                    /// </summary>
                    if (CompLocation[(int)ComponentGroup.Industry] != (List.PassiveSensorDef.Count + CompLocation[(int)ComponentGroup.Shields] + 1))
                    {
                        int rowLine = CompLocation[(int)ComponentGroup.Shields] + 1;
                        int ShieldCount = 0;

                        /// <summary>
                        /// Count the rows that are already filled with component type 29 or "Shield"
                        /// </summary>
                        while ((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value == ((int)ComponentTypeTN.Shield).ToString())
                        {
                            rowLine++;
                        }
                        ShieldCount = (rowLine - CompLocation[(int)ComponentGroup.Shields] - 1);

                        int AddedRows = List.ShieldDef.Count - ShieldCount;

                        /// <summary>
                        /// Increment all the component locations past the current one(Engine) by added rows count.
                        /// </summary>
                        for (int loop = (int)ComponentGroup.Industry; loop <= (int)ComponentGroup.TypeCount; loop++)
                        {
                            CompLocation[loop] = CompLocation[loop] + AddedRows;
                        }

                        /// <summary>
                        /// insert and fill in the rows where appropriate.
                        /// </summary>
                        for (int ComponentIterator = ShieldCount; ComponentIterator <= (List.ShieldDef.Count - 1); ComponentIterator++)
                        {
                            PopulateComponentRow(List.ShieldDef[ComponentIterator], rowLine, "Shield Strength", List.ShieldDef[ComponentIterator].shieldPool.ToString(), ComponentIterator, true);
                            rowLine++;
                        }
                    }
                    #endregion

                    #region Transportation and Industry (Hangar, Maint Module, Terraformer, Sorium Harvester, Tractor, Salvage, Luxury, construction module not yet implemented)
                    /// <summary>
                    /// An engine was added to the component list.
                    /// </summary>
                    if (CompLocation[(int)ComponentGroup.TypeCount] != (List.CargoHandleSystemDef.Count + List.CargoHoldDef.Count + List.ColonyBayDef.Count + CompLocation[(int)ComponentGroup.Industry] + 1))
                    {
                        int rowLine = CompLocation[(int)ComponentGroup.Industry] + 1;
                        int CHSCount = 0;

                        /// <summary>
                        /// Count the rows that are already filled with component type 14 or "Cargo Handling System"
                        /// </summary>
                        while ((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value == ((int)ComponentTypeTN.CargoHandlingSystem).ToString())
                        {
                            rowLine++;
                        }
                        CHSCount = (rowLine - CompLocation[(int)ComponentGroup.Industry] - 1);

                        int AddedRows = List.CargoHandleSystemDef.Count - CHSCount;

                        /// <summary>
                        /// Increment all the component locations past the current one(Engine) by added rows count.
                        /// </summary>
                        for (int loop = (int)ComponentGroup.TypeCount; loop <= (int)ComponentGroup.TypeCount; loop++)
                        {
                            CompLocation[loop] = CompLocation[loop] + AddedRows;
                        }

                        /// <summary>
                        /// insert and fill in the rows where appropriate.
                        /// </summary>
                        for (int ComponentIterator = CHSCount; ComponentIterator <= (List.CargoHandleSystemDef.Count - 1); ComponentIterator++)
                        {
                            PopulateComponentRow(List.CargoHandleSystemDef[ComponentIterator], rowLine, "Cargo Handling Multiplier",
                                                 List.CargoHandleSystemDef[ComponentIterator].tractorMultiplier.ToString(), ComponentIterator, true);
                            rowLine++;
                        }

                        /// <summary>
                        /// Cargo Hold Section
                        /// </summary>
                        int CHSEnd = rowLine;
                        int CHCount = 0;

                        /// <summary>
                        /// Count the rows that are already filled with component type 13 or "Cargo Hold"
                        /// </summary>
                        while ((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value == ((int)ComponentTypeTN.CargoHold).ToString())
                        {
                            rowLine++;
                        }

                        CHCount = rowLine - CHSEnd;

                        AddedRows = List.CargoHoldDef.Count - CHCount;

                        /// <summary>
                        /// Increment all the component locations past the current one(Engine) by added rows count.
                        /// </summary>
                        for (int loop = (int)ComponentGroup.TypeCount; loop <= (int)ComponentGroup.TypeCount; loop++)
                        {
                            CompLocation[loop] = CompLocation[loop] + AddedRows;
                        }

                        /// <summary>
                        /// insert and fill in the rows where appropriate.
                        /// </summary>
                        for (int ComponentIterator = CHCount; ComponentIterator <= (List.CargoHoldDef.Count - 1); ComponentIterator++)
                        {
                            PopulateComponentRow(List.CargoHoldDef[ComponentIterator], rowLine, "Cargo Capacity",
                                                 List.CargoHoldDef[ComponentIterator].cargoCapacity.ToString(), ComponentIterator, true);
                            rowLine++;
                        }

                        /// <summary>
                        ///Colony Bay Section
                        /// </summary>
                        int CHEnd = rowLine;
                        int CBCount = 0;

                        /// <summary>
                        /// Count the rows that are already filled with component type 15 or "Cryobay"
                        /// </summary>
                        while ((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value == ((int)ComponentTypeTN.CryoStorage).ToString())
                        {
                            rowLine++;
                        }

                        CBCount = rowLine - CHEnd;

                        AddedRows = List.ColonyBayDef.Count - CBCount;

                        /// <summary>
                        /// Increment all the component locations past the current one(Engine) by added rows count.
                        /// </summary>
                        for (int loop = (int)ComponentGroup.TypeCount; loop <= (int)ComponentGroup.TypeCount; loop++)
                        {
                            CompLocation[loop] = CompLocation[loop] + AddedRows;
                        }

                        /// <summary>
                        /// insert and fill in the rows where appropriate.
                        /// </summary>
                        for (int ComponentIterator = CBCount; ComponentIterator <= (List.ColonyBayDef.Count - 1); ComponentIterator++)
                        {
                            PopulateComponentRow(List.ColonyBayDef[ComponentIterator], rowLine, "Colonist Capacity",
                                                 List.ColonyBayDef[ComponentIterator].cryoBerths.ToString(), ComponentIterator, true);
                            rowLine++; 
                        }
                    }
                    #endregion

                }
                catch
                {
#if LOG4NET_ENABLED
                    logger.Error("Something went wrong with updating rows for class design componentGrid screen...");
#endif
                }
            }
        }

        /// <summary>
        /// Fill in the information about this row given a component, row number, rating strings, and the component iterator
        /// </summary>
        /// <param name="Comp">Component to add.</param>
        /// <param name="row">Row number.</param>
        /// <param name="RateType">Rating Type string.</param>
        /// <param name="RateValue">Rating Value String.</param>
        /// <param name="ComponentIterator">Component iterator, its place in its respective list in ComponentListTN.cs</param>
        private void PopulateComponentRow(ComponentDefTN Comp, int row, String RateType, String RateValue, int ComponentIterator, bool Insert = false)
        {
            using (DataGridViewRow NewRow = new DataGridViewRow())
            {
                /// <summary>
                /// setup row height. note that by default they are 22 pixels in height!
                /// </summary>
                NewRow.Height = 18;
                if(Insert == false)
                    m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);
                else
                    m_oOptionsPanel.ComponentDataGrid.Rows.Insert(row, NewRow);

                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = Comp.Name;
                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.RatingType].Value = RateType;
                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Rating].Value = RateValue;
                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Cost].Value = Comp.cost.ToString();
                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Size].Value = (Comp.size * Constants.ShipTN.TonsPerHS).ToString();
                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Crew].Value = Comp.crew;

                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Materials].Value = BuildMineralCost(Comp);

                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CType].Value = ((int)Comp.componentType).ToString();
                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CIndex].Value = ComponentIterator;
                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Obsolete].Value = Comp.isObsolete.ToString();

                TotalComponents = TotalComponents + 1;
            }
        }

        /// <summary>
        /// Builds the mineral cost for AuroraTN components.
        /// </summary>
        /// <param name="Comp">Component whos cost I want.</param>
        /// <returns>Mineral cost string.</returns>
        private String BuildMineralCost(ComponentDefTN Comp)
        {
            String MineralString = "";
            for (int mineralIterator = 1; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
            {
                if (Comp.minerialsCost[mineralIterator] != 0)
                {
                    MineralString = String.Format("{0} {1:N2}x {2}", MineralString, Comp.minerialsCost[mineralIterator], (Constants.Minerals.MinerialNames)mineralIterator);
                }
            }
            return MineralString;
        }


        /// <summary>
        /// Adds or subtracts a component to/from the design.
        /// </summary>
        /// <param name="CType">Type of component</param>
        /// <param name="CIndex">Index in faction component list of component(of type)</param>
        /// <param name="CompAmt">Number to add/subtract</param>
        private void AddComponent(int CType, int CIndex, int CompAmt)
        {
            ComponentDefListTN List = _CurrnetFaction.ComponentList;
            #region Add Component Switch(Absorption shield listed but not implemented
            switch ((ComponentTypeTN)CType)
            {
                case ComponentTypeTN.Crew:

                    /// <summary>
                    /// Check to see if a subtraction is happening.
                    /// </summary>
                    if (CompAmt <= -1)
                    {
                        /// <summary>
                        /// Get the Index of the component in question on the ship.
                        /// </summary>
                        int Index = CurrentShipClass.CrewQuarters.IndexOf(List.CrewQuarters[CIndex]);

                        ///<summary>
                        ///if present then proceed
                        ///</summary>
                        if (Index != -1)
                        {
                            /// <summary>
                            /// get absolute value of CompAmt and compare it to crew quarters
                            /// </summary>
                            int Cabs = CompAmt * -1;

                            if (Cabs > CurrentShipClass.CrewQuartersCount[Index])
                            {
                                CompAmt = CurrentShipClass.CrewQuartersCount[Index] * -1;
                            }

                            /// <summary>
                            /// Subtract the appropriate component amount from the ship. This is the only place in the UI where this happens, so hopefully this error check will be sufficient.
                            /// </summary>
                            CurrentShipClass.AddCrewQuarters(List.CrewQuarters[CIndex], (short)CompAmt);
                        }
                    }
                    else
                    {
                        /// <summary>
                        /// Add the component as normal.
                        /// </summary>
                        CurrentShipClass.AddCrewQuarters(List.CrewQuarters[CIndex], (short)CompAmt);
                    }
                    break;
                case ComponentTypeTN.Fuel:
                    if (CompAmt <= -1)
                    {
                        int Index = CurrentShipClass.FuelTanks.IndexOf(List.FuelStorage[CIndex]);

                        if (Index != -1)
                        {
                            int Cabs = CompAmt * -1;

                            if (Cabs > CurrentShipClass.FuelTanksCount[Index])
                            {
                                CompAmt = CurrentShipClass.FuelTanksCount[Index] * -1;
                            }

                            CurrentShipClass.AddFuelStorage(List.FuelStorage[CIndex], (short)CompAmt);
                        }
                    }
                    else
                        CurrentShipClass.AddFuelStorage(List.FuelStorage[CIndex], (short)CompAmt);
                    break;
                case ComponentTypeTN.Engineering:
                    if (CompAmt <= -1)
                    {
                        int Index = CurrentShipClass.EngineeringBays.IndexOf(List.EngineeringSpaces[CIndex]);

                        if (Index != -1)
                        {
                            int Cabs = CompAmt * -1;

                            if (Cabs > CurrentShipClass.EngineeringBaysCount[Index])
                            {
                                CompAmt = CurrentShipClass.EngineeringBaysCount[Index] * -1;
                            }

                            CurrentShipClass.AddEngineeringSpaces(List.EngineeringSpaces[CIndex], (short)CompAmt);
                        }
                    }
                    else
                        CurrentShipClass.AddEngineeringSpaces(List.EngineeringSpaces[CIndex], (short)CompAmt);
                    break;
                case ComponentTypeTN.Bridge:
                case ComponentTypeTN.MaintenanceBay:
                case ComponentTypeTN.FlagBridge:
                case ComponentTypeTN.DamageControl:
                case ComponentTypeTN.OrbitalHabitat:
                case ComponentTypeTN.RecFacility:
                    if (CompAmt <= -1)
                    {
                        int Index = CurrentShipClass.OtherComponents.IndexOf(List.OtherComponents[CIndex]);

                        if (Index != -1)
                        {
                            int Cabs = CompAmt * -1;

                            if (Cabs > CurrentShipClass.OtherComponentsCount[Index])
                            {
                                CompAmt = CurrentShipClass.OtherComponentsCount[Index] * -1;
                            }

                            CurrentShipClass.AddOtherComponent(List.OtherComponents[CIndex], (short)CompAmt);
                        }
                    }
                    else
                        CurrentShipClass.AddOtherComponent(List.OtherComponents[CIndex], (short)CompAmt);
                    break;
                case ComponentTypeTN.Engine:
                    if (CompAmt <= -1)
                    {
                        if (CurrentShipClass.ShipEngineDef != null)
                        {
                            int Cabs = CompAmt * -1;

                            if (Cabs > CurrentShipClass.ShipEngineCount)
                            {
                                CompAmt = CurrentShipClass.ShipEngineCount * -1;
                            }
                            CurrentShipClass.AddEngine(List.Engines[CIndex], (short)CompAmt);
                        }
                    }
                    else
                        CurrentShipClass.AddEngine(List.Engines[CIndex], (short)CompAmt);
                    break;
                case ComponentTypeTN.PassiveSensor:
                    if (CompAmt <= -1)
                    {
                        int Index = CurrentShipClass.ShipPSensorDef.IndexOf(List.PassiveSensorDef[CIndex]);

                        if (Index != -1)
                        {
                            int Cabs = CompAmt * -1;

                            if (Cabs > CurrentShipClass.ShipPSensorCount[Index])
                            {
                                CompAmt = CurrentShipClass.ShipPSensorCount[Index] * -1;
                            }

                            CurrentShipClass.AddPassiveSensor(List.PassiveSensorDef[CIndex], (short)CompAmt);
                        }
                    }
                    else
                        CurrentShipClass.AddPassiveSensor(List.PassiveSensorDef[CIndex], (short)CompAmt);
                    break;
                case ComponentTypeTN.ActiveSensor:
                    if (CompAmt <= -1)
                    {
                        int Index = CurrentShipClass.ShipASensorDef.IndexOf(List.ActiveSensorDef[CIndex]);

                        if (Index != -1)
                        {
                            int Cabs = CompAmt * -1;

                            if (Cabs > CurrentShipClass.ShipASensorCount[Index])
                            {
                                CompAmt = CurrentShipClass.ShipASensorCount[Index] * -1;
                            }

                            CurrentShipClass.AddActiveSensor(List.ActiveSensorDef[CIndex], (short)CompAmt);
                        }
                    }
                    else
                        CurrentShipClass.AddActiveSensor(List.ActiveSensorDef[CIndex], (short)CompAmt);
                    break;
                case ComponentTypeTN.CargoHold:
                    if (CompAmt <= -1)
                    {
                        int Index = CurrentShipClass.ShipCargoDef.IndexOf(List.CargoHoldDef[CIndex]);

                        if (Index != -1)
                        {
                            int Cabs = CompAmt * -1;

                            if (Cabs > CurrentShipClass.ShipCargoCount[Index])
                            {
                                CompAmt = CurrentShipClass.ShipCargoCount[Index] * -1;
                            }

                            CurrentShipClass.AddCargoHold(List.CargoHoldDef[CIndex], (short)CompAmt);
                        }
                    }
                    else
                        CurrentShipClass.AddCargoHold(List.CargoHoldDef[CIndex], (short)CompAmt);
                    break;
                case ComponentTypeTN.CargoHandlingSystem:
                    if (CompAmt <= -1)
                    {
                        int Index = CurrentShipClass.ShipCHSDef.IndexOf(List.CargoHandleSystemDef[CIndex]);

                        if (Index != -1)
                        {
                            int Cabs = CompAmt * -1;

                            if (Cabs > CurrentShipClass.ShipCHSCount[Index])
                            {
                                CompAmt = CurrentShipClass.ShipCHSCount[Index] * -1;
                            }

                            CurrentShipClass.AddCargoHandlingSystem(List.CargoHandleSystemDef[CIndex], (short)CompAmt);
                        }
                    }
                    else
                        CurrentShipClass.AddCargoHandlingSystem(List.CargoHandleSystemDef[CIndex], (short)CompAmt);
                    break;
                case ComponentTypeTN.CryoStorage:
                    if (CompAmt <= -1)
                    {
                        int Index = CurrentShipClass.ShipColonyDef.IndexOf(List.ColonyBayDef[CIndex]);

                        if (Index != -1)
                        {
                            int Cabs = CompAmt * -1;

                            if (Cabs > CurrentShipClass.ShipColonyCount[Index])
                            {
                                CompAmt = CurrentShipClass.ShipColonyCount[Index] * -1;
                            }

                            CurrentShipClass.AddColonyBay(List.ColonyBayDef[CIndex], (short)CompAmt);
                        }
                    }
                    else
                        CurrentShipClass.AddColonyBay(List.ColonyBayDef[CIndex], (short)CompAmt);
                    break;
                case ComponentTypeTN.BeamFireControl:
                    if (CompAmt <= -1)
                    {
                        int Index = CurrentShipClass.ShipBFCDef.IndexOf(List.BeamFireControlDef[CIndex]);

                        if (Index != -1)
                        {
                            int Cabs = CompAmt * -1;

                            if (Cabs > CurrentShipClass.ShipBFCCount[Index])
                            {
                                CompAmt = CurrentShipClass.ShipBFCCount[Index] * -1;
                            }

                            CurrentShipClass.AddBeamFireControl(List.BeamFireControlDef[CIndex], (short)CompAmt);
                        }
                    }
                    else
                        CurrentShipClass.AddBeamFireControl(List.BeamFireControlDef[CIndex], (short)CompAmt);
                    break;
                case ComponentTypeTN.Rail:
                case ComponentTypeTN.Gauss:
                case ComponentTypeTN.Plasma:
                case ComponentTypeTN.Laser:
                case ComponentTypeTN.Meson:
                case ComponentTypeTN.Microwave:
                case ComponentTypeTN.Particle:
                case ComponentTypeTN.AdvRail:
                case ComponentTypeTN.AdvLaser:
                case ComponentTypeTN.AdvPlasma:
                case ComponentTypeTN.AdvParticle:
                    if (CompAmt <= -1)
                    {
                        int Index = CurrentShipClass.ShipBeamDef.IndexOf(List.BeamWeaponDef[CIndex]);

                        if (Index != -1)
                        {
                            int Cabs = CompAmt * -1;

                            if (Cabs > CurrentShipClass.ShipBeamCount[Index])
                            {
                                CompAmt = CurrentShipClass.ShipBeamCount[Index] * -1;
                            }

                            CurrentShipClass.AddBeamWeapon(List.BeamWeaponDef[CIndex], (short)CompAmt);
                        }
                    }
                    else
                        CurrentShipClass.AddBeamWeapon(List.BeamWeaponDef[CIndex], (short)CompAmt);

                    break;
                case ComponentTypeTN.Reactor:
                    if (CompAmt <= -1)
                    {
                        int Index = CurrentShipClass.ShipReactorDef.IndexOf(List.ReactorDef[CIndex]);

                        if (Index != -1)
                        {
                            int Cabs = CompAmt * -1;

                            if (Cabs > CurrentShipClass.ShipReactorCount[Index])
                            {
                                CompAmt = CurrentShipClass.ShipReactorCount[Index] * -1;
                            }

                            CurrentShipClass.AddReactor(List.ReactorDef[CIndex], (short)CompAmt);
                        }
                    }
                    else
                        CurrentShipClass.AddReactor(List.ReactorDef[CIndex], (short)CompAmt);
                    break;
                case ComponentTypeTN.Shield:
                    if (CompAmt <= -1)
                    {
                        if (CurrentShipClass.ShipShieldDef != null)
                        {
                            int Cabs = CompAmt * -1;

                            if (Cabs > CurrentShipClass.ShipShieldCount)
                            {
                                CompAmt = CurrentShipClass.ShipShieldCount * -1;
                            }
                            CurrentShipClass.AddShield(List.ShieldDef[CIndex], (short)CompAmt);
                        }
                    }
                    else
                        CurrentShipClass.AddShield(List.ShieldDef[CIndex], (short)CompAmt);
                    break;
                case ComponentTypeTN.AbsorptionShield:
                    break;
                case ComponentTypeTN.MissileLauncher:
                    if (CompAmt <= -1)
                    {
                        int Index = CurrentShipClass.ShipMLaunchDef.IndexOf(List.MLauncherDef[CIndex]);

                        if (Index != -1)
                        {
                            int Cabs = CompAmt * -1;

                            if (Cabs > CurrentShipClass.ShipMLaunchCount[Index])
                            {
                                CompAmt = CurrentShipClass.ShipMLaunchCount[Index] * -1;
                            }

                            CurrentShipClass.AddLauncher(List.MLauncherDef[CIndex], (short)CompAmt);
                        }
                    }
                    else
                        CurrentShipClass.AddLauncher(List.MLauncherDef[CIndex], (short)CompAmt);

                    break;
                case ComponentTypeTN.Magazine:
                    if (CompAmt <= -1)
                    {
                        int Index = CurrentShipClass.ShipMagazineDef.IndexOf(List.MagazineDef[CIndex]);

                        if (Index != -1)
                        {
                            int Cabs = CompAmt * -1;

                            if (Cabs > CurrentShipClass.ShipMagazineCount[Index])
                            {
                                CompAmt = CurrentShipClass.ShipMagazineCount[Index] * -1;
                            }

                            CurrentShipClass.AddMagazine(List.MagazineDef[CIndex], (short)CompAmt);
                        }
                    }
                    else
                        CurrentShipClass.AddMagazine(List.MagazineDef[CIndex], (short)CompAmt);

                    break;
                case ComponentTypeTN.MissileFireControl:
                    if (CompAmt <= -1)
                    {
                        int Index = CurrentShipClass.ShipMFCDef.IndexOf(List.MissileFireControlDef[CIndex]);

                        if (Index != -1)
                        {
                            int Cabs = CompAmt * -1;

                            if (Cabs > CurrentShipClass.ShipMFCCount[Index])
                            {
                                CompAmt = CurrentShipClass.ShipMFCCount[Index] * -1;
                            }

                            CurrentShipClass.AddMFC(List.MissileFireControlDef[CIndex], (short)CompAmt);
                        }
                    }
                    else
                        CurrentShipClass.AddMFC(List.MissileFireControlDef[CIndex], (short)CompAmt);

                    break;
                case ComponentTypeTN.CIWS:
                    if (CompAmt <= -1)
                    {
                        int Index = CurrentShipClass.ShipCIWSDef.IndexOf(List.CIWSDef[CIndex]);

                        if (Index != -1)
                        {
                            int Cabs = CompAmt * -1;

                            if (Cabs > CurrentShipClass.ShipCIWSCount[Index])
                            {
                                CompAmt = CurrentShipClass.ShipCIWSCount[Index] * -1;
                            }

                            CurrentShipClass.AddCIWS(List.CIWSDef[CIndex], (short)CompAmt);
                        }
                    }
                    else
                        CurrentShipClass.AddCIWS(List.CIWSDef[CIndex], (short)CompAmt);
                    break;
                case ComponentTypeTN.Turret:
                    if (CompAmt <= -1)
                    {
                        int Index = CurrentShipClass.ShipTurretDef.IndexOf(List.TurretDef[CIndex]);

                        if (Index != -1)
                        {
                            int Cabs = CompAmt * -1;

                            if (Cabs > CurrentShipClass.ShipTurretCount[Index])
                            {
                                CompAmt = CurrentShipClass.ShipTurretCount[Index] * -1;
                            }

                            CurrentShipClass.AddTurret(List.TurretDef[CIndex], (short)CompAmt);
                        }
                    }
                    else
                        CurrentShipClass.AddTurret(List.TurretDef[CIndex], (short)CompAmt);
                    break;
                case ComponentTypeTN.JumpEngine:
                    if (CompAmt <= -1)
                    {
                        int Index = CurrentShipClass.ShipJumpEngineDef.IndexOf(List.JumpEngineDef[CIndex]);

                        if (Index != -1)
                        {
                            int Cabs = CompAmt * -1;

                            if (Cabs > CurrentShipClass.ShipJumpEngineCount[Index])
                            {
                                CompAmt = CurrentShipClass.ShipJumpEngineCount[Index] * -1;
                            }

                            CurrentShipClass.AddJumpEngine(List.JumpEngineDef[CIndex], (short)CompAmt);
                        }
                    }
                    else
                        CurrentShipClass.AddJumpEngine(List.JumpEngineDef[CIndex], (short)CompAmt);
                    break;
            }
            #endregion

        }

        /// <summary>
        /// Rather than block copy this, it is its own function. The list box has some funky logic for finding out what component is added/subtracted, so here it is.
        /// </summary>
        /// <param name="CT">ComponentType, what type of component I am looking for.</param>
        /// <param name="CID">ComponentID, what global unique Identifier is associated with this component definition.</param>
        /// <param name="CAmt">ComponentAmount, number of components to add(or more probably subtract)</param>
        private void FindAddListBoxComponent(int CT, Guid CID, int CAmt)
        {
            ComponentDefListTN List = _CurrnetFaction.ComponentList;
            #region ComponentListBox double click switch(absorption shield listed but not implemented)
            switch ((ComponentTypeTN)CT)
            {
                case ComponentTypeTN.Crew:
                    for (int loop = 0; loop < List.CrewQuarters.Count; loop++)
                    {
                        if (List.CrewQuarters[loop].Id == CID)
                        {
                            AddComponent(CT, loop, CAmt);
                            break;
                        }
                    }
                    break;
                case ComponentTypeTN.Fuel:
                    for (int loop = 0; loop < List.FuelStorage.Count; loop++)
                    {
                        if (List.FuelStorage[loop].Id == CID)
                        {
                            AddComponent(CT, loop, CAmt);
                            break;
                        }
                    }
                    break;
                case ComponentTypeTN.Engineering:
                    for (int loop = 0; loop < List.EngineeringSpaces.Count; loop++)
                    {
                        if (List.EngineeringSpaces[loop].Id == CID)
                        {
                            AddComponent(CT, loop, CAmt);
                            break;
                        }
                    }
                    break;
                case ComponentTypeTN.Bridge:
                case ComponentTypeTN.MaintenanceBay:
                case ComponentTypeTN.FlagBridge:
                case ComponentTypeTN.DamageControl:
                case ComponentTypeTN.OrbitalHabitat:
                case ComponentTypeTN.RecFacility:
                    for (int loop = 0; loop < List.OtherComponents.Count; loop++)
                    {
                        if (List.OtherComponents[loop].Id == CID)
                        {
                            AddComponent(CT, loop, CAmt);
                            break;
                        }
                    }
                    break;
                case ComponentTypeTN.Engine:
                    for (int loop = 0; loop < List.Engines.Count; loop++)
                    {
                        if (List.Engines[loop].Id == CID)
                        {
                            AddComponent(CT, loop, CAmt);
                            break;
                        }
                    }
                    break;
                case ComponentTypeTN.PassiveSensor:
                    for (int loop = 0; loop < List.PassiveSensorDef.Count; loop++)
                    {
                        if (List.PassiveSensorDef[loop].Id == CID)
                        {
                            AddComponent(CT, loop, CAmt);
                            break;
                        }
                    }
                    break;
                case ComponentTypeTN.ActiveSensor:
                    for (int loop = 0; loop < List.ActiveSensorDef.Count; loop++)
                    {
                        if (List.ActiveSensorDef[loop].Id == CID)
                        {
                            AddComponent(CT, loop, CAmt);
                            break;
                        }
                    }
                    break;
                case ComponentTypeTN.CargoHold:
                    for (int loop = 0; loop < List.CargoHoldDef.Count; loop++)
                    {
                        if (List.CargoHoldDef[loop].Id == CID)
                        {
                            AddComponent(CT, loop, CAmt);
                            break;
                        }
                    }
                    break;
                case ComponentTypeTN.CargoHandlingSystem:
                    for (int loop = 0; loop < List.CargoHandleSystemDef.Count; loop++)
                    {
                        if (List.CargoHandleSystemDef[loop].Id == CID)
                        {
                            AddComponent(CT, loop, CAmt);
                            break;
                        }
                    }
                    break;
                case ComponentTypeTN.CryoStorage:
                    for (int loop = 0; loop < List.ColonyBayDef.Count; loop++)
                    {
                        if (List.ColonyBayDef[loop].Id == CID)
                        {
                            AddComponent(CT, loop, CAmt);
                            break;
                        }
                    }
                    break;
                case ComponentTypeTN.BeamFireControl:
                    for (int loop = 0; loop < List.BeamFireControlDef.Count; loop++)
                    {
                        if (List.BeamFireControlDef[loop].Id == CID)
                        {
                            AddComponent(CT, loop, CAmt);
                            break;
                        }
                    }
                    break;
                case ComponentTypeTN.Rail:
                case ComponentTypeTN.Gauss:
                case ComponentTypeTN.Plasma:
                case ComponentTypeTN.Laser:
                case ComponentTypeTN.Meson:
                case ComponentTypeTN.Microwave:
                case ComponentTypeTN.Particle:
                case ComponentTypeTN.AdvRail:
                case ComponentTypeTN.AdvLaser:
                case ComponentTypeTN.AdvPlasma:
                case ComponentTypeTN.AdvParticle:
                    for (int loop = 0; loop < List.BeamWeaponDef.Count; loop++)
                    {
                        if (List.BeamWeaponDef[loop].Id == CID)
                        {
                            AddComponent(CT, loop, CAmt);
                            break;
                        }
                    }
                    break;
                case ComponentTypeTN.Reactor:
                    for (int loop = 0; loop < List.ReactorDef.Count; loop++)
                    {
                        if (List.ReactorDef[loop].Id == CID)
                        {
                            AddComponent(CT, loop, CAmt);
                            break;
                        }
                    }
                    break;
                case ComponentTypeTN.Shield:
                    for (int loop = 0; loop < List.ShieldDef.Count; loop++)
                    {
                        if (List.ShieldDef[loop].Id == CID)
                        {
                            AddComponent(CT, loop, CAmt);
                            break;
                        }
                    }
                    break;
                case ComponentTypeTN.AbsorptionShield:
                    break;
                case ComponentTypeTN.MissileLauncher:
                    for (int loop = 0; loop < List.MLauncherDef.Count; loop++)
                    {
                        if (List.MLauncherDef[loop].Id == CID)
                        {
                            AddComponent(CT, loop, CAmt);
                            break;
                        }
                    }
                    break;
                case ComponentTypeTN.Magazine:
                    for (int loop = 0; loop < List.MagazineDef.Count; loop++)
                    {
                        if (List.MagazineDef[loop].Id == CID)
                        {
                            AddComponent(CT, loop, CAmt);
                            break;
                        }
                    }
                    break;
                case ComponentTypeTN.MissileFireControl:
                    for (int loop = 0; loop < List.MissileFireControlDef.Count; loop++)
                    {
                        if (List.MissileFireControlDef[loop].Id == CID)
                        {
                            AddComponent(CT, loop, CAmt);
                            break;
                        }
                    }
                    break;
                case ComponentTypeTN.CIWS:
                    for (int loop = 0; loop < List.CIWSDef.Count; loop++)
                    {
                        if (List.CIWSDef[loop].Id == CID)
                        {
                            AddComponent(CT, loop, CAmt);
                            break;
                        }
                    }
                    break;
                case ComponentTypeTN.Turret:
                    for (int loop = 0; loop < List.TurretDef.Count; loop++)
                    {
                        if (List.TurretDef[loop].Id == CID)
                        {
                            AddComponent(CT, loop, CAmt);
                            break;
                        }
                    }
                    break;
                case ComponentTypeTN.JumpEngine:
                    for (int loop = 0; loop < List.JumpEngineDef.Count; loop++)
                    {
                        if (List.JumpEngineDef[loop].Id == CID)
                        {
                            AddComponent(CT, loop, CAmt);
                            break;
                        }
                    }
                    break;
            }
            #endregion
        }


        /// <summary>
        /// BuildErrorBox checks the design for errors and prints them:
        /// Only 1 spinal mount laser(wMType = MountType.Spinal or MountType.AdvSpinal)
        /// BFC needs beam weapons
        /// Beam weapons need BFC
        /// Power Generation must exceed power requirement
        /// Crew quarters required(if not automatic)
        /// bridge for ships over 1K(if not done automatically)
        /// Engines/shields need fuel
        /// atleast 1HS of ebay for commercial craft(not an error if none present, will just flag as military and print warning)
        /// MFC needs launch tubs
        /// launch tubes need MFC
        /// ECCM needs FC of some type
        /// Cloak must be rated for size
        /// Jump engine must be rated for size and type
        /// ship must not be 0HS total.
        /// not enough MSP for largest component to repair(warning not error)
        /// </summary>
        private void BuildErrorBox()
        {
            IsDesignGood = true;
        }


        private void TankerCheckbox_Changed(object sender, EventArgs e)
        {
            CurrentShipClass.IsTanker = m_oOptionsPanel.Tanker;
        }
        private void SupplyCheckbox_Changed(object sender, EventArgs e)
        {
            CurrentShipClass.IsSupply = m_oOptionsPanel.SupplyShip;
        }
        private void CollierCheckbox_Changed(object sender, EventArgs e)
        {
            CurrentShipClass.IsCollier = m_oOptionsPanel.Collier;
        }
        #region Ordnance / Fighter Tab Build functions

        /// <summary>
        /// radio buttons,Missile listbox and datagrid, fighter listbox and datagrid, 2 Checkboxes, 3 buttons
        /// </summary>


        #region Ordnance / Fighter Tab Event handlers
        /// <summary>
        /// Handle how many components the user wants to add or subtract from a design.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MslAMTRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (m_oOptionsPanel.OF1xRadioButton.Checked == true)
                MissileAmt = 1;
            else if (m_oOptionsPanel.OF10xRadioButton.Checked == true)
                MissileAmt = 10;
            else if (m_oOptionsPanel.OF100xRadioButton.Checked == true)
                MissileAmt = 100;
            else if (m_oOptionsPanel.OF1000xRadioButton.Checked == true)
                MissileAmt = 1000;
        }

        /// <summary>
        /// Adds MissileAmt missiles to the current designs preferred ordnance loadout, this time based on double clicking the MDG.
        /// SetPreferredOrdnance handleds completely all conditions, so no need for any error checking here.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MissileDataGrid_DoubleClick(object sender, EventArgs e)
        {
            if (m_oOptionsPanel.MissileDataGrid.CurrentCell.RowIndex != -1)
            {
                _CurrnetShipClass.SetPreferredOrdnance(_CurrnetFaction.ComponentList.MissileDef[m_oOptionsPanel.MissileDataGrid.CurrentCell.RowIndex], MissileAmt);


                _CurrnetShipClass.BuildClassSummary();
                BuildMisc();
                BuildDesignTab();
            }

            BuildMissileListBox();
        }

        /// <summary>
        /// Subtracts MissileAmt missiles from the current designs preferred ordnance loadout.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PreferredOrdnanceListBox_DoubleClick(object sender, EventArgs e)
        {
            int index = m_oOptionsPanel.PreferredOrdnanceListBox.SelectedIndex;
            int count = 0;
            if (index != -1 && index < m_oOptionsPanel.PreferredOrdnanceListBox.Items.Count - 4)
            {
                foreach (KeyValuePair<OrdnanceDefTN, int> pair in _CurrnetShipClass.ShipClassOrdnance)
                {
                    if (count == index)
                    {
                        _CurrnetShipClass.SetPreferredOrdnance(pair.Key, (MissileAmt * -1));

                        _CurrnetShipClass.BuildClassSummary();
                        BuildMisc();
                        BuildDesignTab();


                        break;
                    }

                    count++;
                }

                BuildMissileListBox();
            }
        }

        /// <summary>
        /// Toggle this missile to obsolete or not.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MslObsButton_Click(object sender, EventArgs e)
        {
            /// <summary>
            /// This happens if there are no entries in the list.
            /// </summary>
            if (m_oOptionsPanel.MissileDataGrid.CurrentCell != null)
            {
                if (m_oOptionsPanel.MissileDataGrid.CurrentCell.RowIndex != -1)
                {
                    if (_CurrnetFaction.ComponentList.MissileDef[m_oOptionsPanel.MissileDataGrid.CurrentCell.RowIndex].isObsolete == false)
                        _CurrnetFaction.ComponentList.MissileDef[m_oOptionsPanel.MissileDataGrid.CurrentCell.RowIndex].isObsolete = true;
                    else
                        _CurrnetFaction.ComponentList.MissileDef[m_oOptionsPanel.MissileDataGrid.CurrentCell.RowIndex].isObsolete = false;

                    BuildMissileDataGrid();
                }
            }

        }

        /// <summary>
        /// show all the obsolete missiles if checked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowObsMslCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            BuildMissileDataGrid();
        }

        /// <summary>
        /// Ignore the largest launcher size requirement for displaying missiles.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IgnoreMslSizeCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            BuildMissileDataGrid();
        }

        #endregion

        #region Ordnance / Fighter tab display functions

        /// <summary>
        /// Builds the overall ordnance and fighters tab
        /// </summary>
        private void BuildOrdnanceFighterTab()
        {
            BuildMissileListBox();
            BuildMissileDataGrid();
        }

        /// <summary>
        /// Updates the display on the DAC / Rank / Info tab
        /// </summary>
        private void UpdateDACRankInfoTab()
        {
            m_oOptionsPanel.Notes.Text = _CurrnetShipClass.Notes;
        }

        /// <summary>
        /// Build the preferred ordnance listbox
        /// </summary>
        private void BuildMissileListBox()
        {
            String Entry = "N/A";
            decimal TotalCost = 0.0m;

            m_oOptionsPanel.PreferredOrdnanceListBox.Items.Clear();
            foreach (KeyValuePair<OrdnanceDefTN, int> pair in _CurrnetShipClass.ShipClassOrdnance)
            {
                Entry = String.Format("{0}x {1}", pair.Value, pair.Key.Name);
                m_oOptionsPanel.PreferredOrdnanceListBox.Items.Add(Entry);

                TotalCost = TotalCost + (pair.Key.cost * pair.Value);
            }

            Entry = "----------------------------------------";
            m_oOptionsPanel.PreferredOrdnanceListBox.Items.Add(Entry);

            Entry = String.Format("Cost: {0} BP", _CurrnetShipClass.PreferredOrdnanceCost);
            m_oOptionsPanel.PreferredOrdnanceListBox.Items.Add(Entry);

            Entry = String.Format("Capacity Required: {0}", _CurrnetShipClass.PreferredOrdnanceSize);
            m_oOptionsPanel.PreferredOrdnanceListBox.Items.Add(Entry);

            Entry = String.Format("Capacity Available {0}", (_CurrnetShipClass.TotalMagazineCapacity - _CurrnetShipClass.PreferredOrdnanceSize));
            m_oOptionsPanel.PreferredOrdnanceListBox.Items.Add(Entry);

        }

        /// <summary>
        /// Creates the columns for the component data grid.
        /// </summary>
        private void SetupMissileDataGrid()
        {
            m_oOptionsPanel.MissileDataGrid.Columns.Clear();
            try
            {
                Padding newPadding = new Padding(2, 0, 2, 0);
                AddColumn("Name", newPadding, m_oOptionsPanel.MissileDataGrid);
                AddColumn("Size", newPadding, m_oOptionsPanel.MissileDataGrid);
                AddColumn("Cost", newPadding, m_oOptionsPanel.MissileDataGrid);
                AddColumn("Speed", newPadding, m_oOptionsPanel.MissileDataGrid);
                AddColumn("Endurance", newPadding, m_oOptionsPanel.MissileDataGrid);
                AddColumn("Range", newPadding, m_oOptionsPanel.MissileDataGrid);
                AddColumn("WH", newPadding, m_oOptionsPanel.MissileDataGrid);
                AddColumn("Man Rating", newPadding, m_oOptionsPanel.MissileDataGrid);
                AddColumn("ECM", newPadding, m_oOptionsPanel.MissileDataGrid);
                AddColumn("Armour", newPadding, m_oOptionsPanel.MissileDataGrid);
                AddColumn("Radiation", newPadding, m_oOptionsPanel.MissileDataGrid);
                AddColumn("Sensors", newPadding, m_oOptionsPanel.MissileDataGrid);
                AddColumn("Second Stage", newPadding, m_oOptionsPanel.MissileDataGrid);
                AddColumn("IsObsolete", newPadding, m_oOptionsPanel.MissileDataGrid);

                m_oOptionsPanel.MissileDataGrid.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

                foreach (DataGridViewColumn Column in m_oOptionsPanel.MissileDataGrid.Columns)
                {
                    Column.SortMode = DataGridViewColumnSortMode.NotSortable;
                }

                m_oOptionsPanel.MissileDataGrid.Columns[m_oOptionsPanel.MissileDataGrid.Columns.Count - 1].Visible = false;


            }
            catch
            {
#if LOG4NET_ENABLED
                logger.Error("Something went wrong Creating Columns for Class Design MissileGrid screen...");
#endif
            }
        }

        /// <summary>
        /// builds and populates the rows to the missile datagrid.
        /// Size Restriction and IsObs need to be checked here?
        /// </summary>
        private void BuildMissileDataGrid()
        {
            if (m_oOptionsPanel.MissileDataGrid.Columns.Count == 0)
                return;

            m_oOptionsPanel.MissileDataGrid.Rows.Clear();

            if (_CurrnetFaction == null)
                return;

            try
            {
                for (int loop = 0; loop < _CurrnetFaction.ComponentList.MissileDef.Count; loop++)
                {
                    using (DataGridViewRow NewRow = new DataGridViewRow())
                    {

                        /// <summary>
                        /// setup row height. note that by default they are 22 pixels in height!
                        /// </summary>
                        NewRow.Height = 18;
                        m_oOptionsPanel.MissileDataGrid.Rows.Add(NewRow);

                        DataGridViewCellStyle style = new DataGridViewCellStyle();
                        m_oOptionsPanel.MissileDataGrid.Rows[loop].DefaultCellStyle = style;



                        m_oOptionsPanel.MissileDataGrid.Rows[loop].Cells[(int)MissileCell.Name].Value = _CurrnetFaction.ComponentList.MissileDef[loop].Name;
                        m_oOptionsPanel.MissileDataGrid.Rows[loop].Cells[(int)MissileCell.Size].Value = _CurrnetFaction.ComponentList.MissileDef[loop].size.ToString();
                        m_oOptionsPanel.MissileDataGrid.Rows[loop].Cells[(int)MissileCell.Cost].Value = _CurrnetFaction.ComponentList.MissileDef[loop].cost.ToString();
                        m_oOptionsPanel.MissileDataGrid.Rows[loop].Cells[(int)MissileCell.Speed].Value = _CurrnetFaction.ComponentList.MissileDef[loop].maxSpeed.ToString();


                        float Endurance;
                        String EndString = "N/A";
                        String Range = "N/A";
                        if (_CurrnetFaction.ComponentList.MissileDef[loop].fuel != 0.0f && _CurrnetFaction.ComponentList.MissileDef[loop].totalFuelConsumption != 0.0f)
                        {
                            Endurance = (_CurrnetFaction.ComponentList.MissileDef[loop].fuel / _CurrnetFaction.ComponentList.MissileDef[loop].totalFuelConsumption);
                        }
                        else
                            Endurance = 0.0f;

                        if (Endurance >= 8640.0f)
                        {
                            float YE = Endurance / 8640.0f;
                            EndString = String.Format("{0:N1} Years", YE);
                        }
                        else if (Endurance >= 720.0f)
                        {
                            float ME = Endurance / 720.0f;
                            EndString = String.Format("{0:N1} Months", ME);
                        }
                        else if (Endurance >= 24.0f)
                        {
                            float DE = Endurance / 24.0f;
                            EndString = String.Format("{0:N1} Days", DE);
                        }
                        else if (Endurance >= 1.0f)
                        {
                            EndString = String.Format("{0:N1} hours", Endurance);
                        }
                        else if ((Endurance * 60.0f) >= 1.0f)
                        {
                            EndString = String.Format("{0:N1} minutes", (Endurance * 60.0f));
                        }
                        else
                        {
                            EndString = String.Format("0 minutes");
                        }

                        m_oOptionsPanel.MissileDataGrid.Rows[loop].Cells[(int)MissileCell.Endurance].Value = EndString;

                        if (Endurance != 0.0f)
                        {
                            float TimeOneBillionKM = (1000000000.0f / _CurrnetFaction.ComponentList.MissileDef[loop].maxSpeed) / 3600.0f;

                            float test = Endurance / TimeOneBillionKM;


                            if (test >= 1.0f)
                            {
                                Range = String.Format("{0:N1}B km", test);
                            }
                            else
                            {
                                float range = (Endurance * (_CurrnetFaction.ComponentList.MissileDef[loop].maxSpeed * 3600.0f)) / 1000000.0f;
                                Range = String.Format("{0:N1}M km", range);
                            }
                        }
                        else
                            Range = "0 km";
                        m_oOptionsPanel.MissileDataGrid.Rows[loop].Cells[(int)MissileCell.Range].Value = Range;

                        m_oOptionsPanel.MissileDataGrid.Rows[loop].Cells[(int)MissileCell.WH].Value = _CurrnetFaction.ComponentList.MissileDef[loop].warhead.ToString();
                        m_oOptionsPanel.MissileDataGrid.Rows[loop].Cells[(int)MissileCell.ManRating].Value = _CurrnetFaction.ComponentList.MissileDef[loop].manuever.ToString();
                        m_oOptionsPanel.MissileDataGrid.Rows[loop].Cells[(int)MissileCell.ECM].Value = _CurrnetFaction.ComponentList.MissileDef[loop].eCMValue;
                        m_oOptionsPanel.MissileDataGrid.Rows[loop].Cells[(int)MissileCell.Armour].Value = _CurrnetFaction.ComponentList.MissileDef[loop].armor;
                        m_oOptionsPanel.MissileDataGrid.Rows[loop].Cells[(int)MissileCell.Radiation].Value = _CurrnetFaction.ComponentList.MissileDef[loop].radValue;

                        String SensorString = "";
                        if (_CurrnetFaction.ComponentList.MissileDef[loop].thermalStr != 0.0f)
                        {
                            SensorString = String.Format("TH: {0:N3} ", _CurrnetFaction.ComponentList.MissileDef[loop].thermalStr);
                        }
                        if (_CurrnetFaction.ComponentList.MissileDef[loop].eMStr != 0.0f)
                        {
                            SensorString = String.Format("{0}EM: {1:N3} ", SensorString, _CurrnetFaction.ComponentList.MissileDef[loop].eMStr);
                        }
                        if (_CurrnetFaction.ComponentList.MissileDef[loop].activeStr != 0.0f)
                        {
                            SensorString = String.Format("{0}Active: {1:N3} ", SensorString, _CurrnetFaction.ComponentList.MissileDef[loop].activeStr);
                        }
                        if (_CurrnetFaction.ComponentList.MissileDef[loop].geoStr != 0.0f)
                        {
                            SensorString = String.Format("{0}Geo: {1:N3}", SensorString, _CurrnetFaction.ComponentList.MissileDef[loop].geoStr);
                        }

                        m_oOptionsPanel.MissileDataGrid.Rows[loop].Cells[(int)MissileCell.Sensor].Value = SensorString;



                        String StageTwo = "N/A";
                        if (_CurrnetFaction.ComponentList.MissileDef[loop].subRelease != null && _CurrnetFaction.ComponentList.MissileDef[loop].subReleaseCount != 0)
                            StageTwo = String.Format("{0}x{1}", _CurrnetFaction.ComponentList.MissileDef[loop].subReleaseCount, _CurrnetFaction.ComponentList.MissileDef[loop].subRelease.Name);
                        m_oOptionsPanel.MissileDataGrid.Rows[loop].Cells[(int)MissileCell.StageTwo].Value = StageTwo;


                        int MissileSizeAdjust = (int)Math.Ceiling(_CurrnetFaction.ComponentList.MissileDef[loop].size);
                        if ((m_oOptionsPanel.IgnoreMslSizeCheckBox.Checked == false && MissileSizeAdjust > _CurrnetShipClass.LargestLauncher) ||
                             (m_oOptionsPanel.ShowObsMslCheckBox.Checked == false && _CurrnetFaction.ComponentList.MissileDef[loop].isObsolete == true))
                        {
                            m_oOptionsPanel.MissileDataGrid.Rows[loop].Visible = false;
                        }
                    }

                }
            }
            catch
            {
#if LOG4NET_ENABLED
                logger.Error("Something went wrong Creating Rows for Class Design MissileGrid screen..");
#endif
            }
        }
        #endregion

        #endregion

        #endregion
    }
}
