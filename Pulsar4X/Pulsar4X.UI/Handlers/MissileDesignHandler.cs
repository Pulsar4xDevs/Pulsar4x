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
using Newtonsoft.Json;

#if LOG4NET_ENABLED
using log4net.Config;
using log4net;
#endif

using Pulsar4X.Entities.Components;
using System.Runtime.InteropServices;

namespace Pulsar4X.UI.Handlers
{
    public class MissileDesignHandler
    {
#if LOG4NET_ENABLED
        public static readonly ILog logger = LogManager.GetLogger(typeof(MissileDesignHandler));
#endif
        /// <summary>
        /// Display panel for missile design.
        /// </summary>
        Panels.MissileDesign m_oMissileDesignPanel { get; set; }
        Panels.ClassDes_RenameClass m_oNameSeriesPanel;

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

                /// <summary>
                /// I don't need all these returns, but am putting them in anyways just in case I do something here later. 
                /// And as an aside, the reason for all these summarys everywhere is that they look nice.
                /// </summary>
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
        /// Currently selected missile series.
        /// </summary>
        private OrdnanceSeriesTN _CurrnetMissileSeries;
        public OrdnanceSeriesTN CurrentMissileSeries
        {
            get { return _CurrnetMissileSeries; }
            set
            {
                _CurrnetMissileSeries = value;
                if (_CurrnetMissileSeries == null)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Currently selected missile from the previous designs.
        /// This is supposed to set the MSP allocation to that of the previous design.
        /// </summary>
        private OrdnanceDefTN _CurrnetMissile;
        public OrdnanceDefTN CurrentMissile
        {
            get { return _CurrnetMissile; }
            set
            {
                _CurrnetMissile = value;
                if (_CurrnetMissile == null)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Missile creation values.
        /// </summary>
        private int EngineCount = 1;
        private float WarheadMSP = 0.0f;
        private float WarheadValue = 0.0f;
        private float FuelMSP = 0.0f;
        private float FuelValue = 0.0f;
        private float AgilityMSP = 0.0f;
        private float AgilityValue = 0.0f;
        private float ReactorMSP = 0.0f;
        private float ReactorValue = 0.0f;
        private float ActiveMSP = 0.0f;
        private float ActiveValue = 0.0f;
        private float ThermalMSP = 0.0f;
        private float ThermalValue = 0.0f;
        private float EMMSP = 0.0f;
        private float EMValue = 0.0f;
        private float GeoMSP = 0.0f;
        private float GeoValue = 0.0f;
        private ushort Resolution = 1;
        private float ArmourMSP = 0.0f;
        private float ArmourValue = 0.0f;
        private float ECMMSP = 0.0f;
        private float ECMValue = 0.0f;
        private bool Laser = false;
        private bool Enhanced = false;
        private OrdnanceDefTN SubMunition = null;
        private int SepRange = 150;
        private int SubNumber = 0;

        /// <summary>
        /// Missile project itself.
        /// </summary>
        OrdnanceDefTN OrdnanceProject = null;

        /// <summary>
        /// Should information be displayed, or should tech data be displayed. True = Info, False = tech. Totally arbitrary.
        /// </summary>
        private bool InfoToggle { get; set; }


        /// <summary>
        /// Constructor for missile design handler.
        /// </summary>
        public MissileDesignHandler()
        {
            m_oMissileDesignPanel = new Panels.MissileDesign();
            m_oNameSeriesPanel = new Panels.ClassDes_RenameClass();


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
            /// </summary>
            m_oMissileDesignPanel.MissileEngineComboBox.Bind(c => c.DataSource, VM, d => d.MissileEngines);
            m_oMissileDesignPanel.MissileEngineComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentMissileEngine, DataSourceUpdateMode.OnPropertyChanged);
            m_oMissileDesignPanel.MissileEngineComboBox.DisplayMember = "Name";
            VM.MissileEngineChanged += (s, args) => _CurrnetMissileEngine = VM.CurrentMissileEngine;
            _CurrnetMissileEngine = VM.CurrentMissileEngine;
            m_oMissileDesignPanel.MissileEngineComboBox.SelectedIndexChanged += (s, args) => m_oMissileDesignPanel.MissileEngineComboBox.DataBindings["SelectedItem"].WriteValue();
            m_oMissileDesignPanel.MissileEngineComboBox.SelectedIndexChanged += new EventHandler(MissileEngineComboBox_SelectedIndexChanged);

            m_oMissileDesignPanel.MSeriesComboBox.Bind(c => c.DataSource, VM, d => d.MissileSeries);
            m_oMissileDesignPanel.MSeriesComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentMissileSeries, DataSourceUpdateMode.OnPropertyChanged);
            m_oMissileDesignPanel.MSeriesComboBox.DisplayMember = "Name";
            VM.MissileSeriesChanged += (s, args) => _CurrnetMissileSeries = VM.CurrentMissileSeries;
            _CurrnetMissileSeries = VM.CurrentMissileSeries;
            m_oMissileDesignPanel.MSeriesComboBox.SelectedIndexChanged += (s, args) => m_oMissileDesignPanel.MSeriesComboBox.DataBindings["SelectedItem"].WriteValue();
            m_oMissileDesignPanel.MSeriesComboBox.SelectedIndexChanged += new EventHandler(MSeriesComboBox_SelectedIndexChanged);

            m_oMissileDesignPanel.PreviousOrdnanceComboBox.Bind(c => c.DataSource, VM, d => d.Missiles);
            m_oMissileDesignPanel.PreviousOrdnanceComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentMissile, DataSourceUpdateMode.OnPropertyChanged);
            m_oMissileDesignPanel.PreviousOrdnanceComboBox.DisplayMember = "Name";
            VM.MissileChanged += (s, args) => _CurrnetMissile = VM.CurrentMissile;
            _CurrnetMissile = VM.CurrentMissile;
            m_oMissileDesignPanel.PreviousOrdnanceComboBox.SelectedIndexChanged += (s, args) => m_oMissileDesignPanel.PreviousOrdnanceComboBox.DataBindings["SelectedItem"].WriteValue();
            m_oMissileDesignPanel.PreviousOrdnanceComboBox.SelectedIndexChanged += new EventHandler(PreviousOrdnanceComboBox_SelectedIndexChanged);

            m_oMissileDesignPanel.SubMunitionComboBox.SelectedIndexChanged += new EventHandler(SubMunitionComboBox_SelectedIndexChanged);

            m_oMissileDesignPanel.CloseMDButton.Click += new EventHandler(CloseMDButton_Click);

            m_oMissileDesignPanel.NumEnginesTextBox.TextChanged += new EventHandler(AnyTextBox_TextChanged);
            m_oMissileDesignPanel.WHMSPTextBox.TextChanged += new EventHandler(AnyTextBox_TextChanged);
            m_oMissileDesignPanel.FuelMSPTextBox.TextChanged += new EventHandler(AnyTextBox_TextChanged);
            m_oMissileDesignPanel.AgilityMSPTextBox.TextChanged += new EventHandler(AnyTextBox_TextChanged);

            m_oMissileDesignPanel.ActiveMSPTextBox.TextChanged += new EventHandler(AnyTextBox_TextChanged);
            m_oMissileDesignPanel.ThermalMSPTextBox.TextChanged += new EventHandler(AnyTextBox_TextChanged);
            m_oMissileDesignPanel.EMMSPTextBox.TextChanged += new EventHandler(AnyTextBox_TextChanged);
            m_oMissileDesignPanel.GeoMSPTextBox.TextChanged += new EventHandler(AnyTextBox_TextChanged);
            m_oMissileDesignPanel.ResolutionTextBox.TextChanged += new EventHandler(AnyTextBox_TextChanged);

            m_oMissileDesignPanel.ArmourMSPTextBox.TextChanged += new EventHandler(AnyTextBox_TextChanged);
            m_oMissileDesignPanel.ECMMSPTextBox.TextChanged += new EventHandler(AnyTextBox_TextChanged);

            m_oMissileDesignPanel.LaserWCheckBox.CheckedChanged += new EventHandler(LWCheckBox_CheckedChanged);
            m_oMissileDesignPanel.ERCheckBox.CheckedChanged += new EventHandler(ERCheckBox_CheckedChanged);

            m_oMissileDesignPanel.CreateSeriesButton.Click += new EventHandler(CreateSeriesButton_Click);
            m_oMissileDesignPanel.DeleteSeriesButton.Click += new EventHandler(DeleteSeriesButton_Click);
            m_oMissileDesignPanel.InstantButton.Click += new EventHandler(InstantButton_Click);
            m_oMissileDesignPanel.ClearDesignButton.Click += new EventHandler(ClearDesignButton_Click);
            m_oMissileDesignPanel.SetSeriesButton.Click += new EventHandler(SetSeriesButton_Click);
            m_oMissileDesignPanel.ReplaceAllButton.Click += new EventHandler(ReplaceAllButton_Click);
            m_oMissileDesignPanel.CreateMissileButton.Click += new EventHandler(CreateMissileButton_Click);
            m_oMissileDesignPanel.ToggleInfoButton.Click += new EventHandler(ToggleInfoButton_Click);

            m_oMissileDesignPanel.SepRangeTextBox.TextChanged += new EventHandler(AnyTextBox_TextChanged);
            m_oMissileDesignPanel.SubNumberTextBox.TextChanged += new EventHandler(AnyTextBox_TextChanged);


            m_oNameSeriesPanel.NewClassNameLabel.Text = "Please enter the name of the new Missile Series";
            m_oNameSeriesPanel.OKButton.Click += new EventHandler(OKButton_Click);
            m_oNameSeriesPanel.CancelRenameButton.Click += new EventHandler(CancelRenameButton_Click);
            m_oNameSeriesPanel.RenameClassTextBox.KeyPress += new KeyPressEventHandler(RenameClassTextBox_KeyPress);

            InfoToggle = false;

            m_oMissileDesignPanel.TechDataGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            m_oMissileDesignPanel.TechDataGrid.RowHeadersVisible = false;
            m_oMissileDesignPanel.TechDataGrid.AutoGenerateColumns = false;
            SetupTechDataGrid();
        }


        #region Public methods
        /// <summary>
        /// Opens as a popup the missile design page
        /// </summary>
        public void Popup()
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            BuildMissileDesignPage();
            m_oMissileDesignPanel.ShowDialog();
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        /// <summary>
        /// Space master on, bring in the instant button.
        /// </summary>
        public void SMOn()
        {
            m_oMissileDesignPanel.InstantButton.Visible = true;
            m_oMissileDesignPanel.InstantButton.Enabled = true;
        }

        /// <summary>
        /// Space master off, get rid of the instant button.
        /// </summary>
        public void SMOff()
        {
            m_oMissileDesignPanel.InstantButton.Visible = false;
            m_oMissileDesignPanel.InstantButton.Enabled = false;
        }
        #endregion


        #region Private methods


        #region Series Naming
        /// <summary>
        /// Actually enters the name
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OKButton_Click(object sender, EventArgs e)
        {
            CreateSeries();

            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oNameSeriesPanel.Hide();
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        /// <summary>
        /// Same as above, only on enter pressed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RenameClassTextBox_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                CreateSeries();


                Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
                m_oNameSeriesPanel.Hide();
                Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
            }
        }

        /// <summary>
        /// Disregard the series creation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelRenameButton_Click(object sender, EventArgs e)
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oNameSeriesPanel.Hide();
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        /// <summary>
        /// creates the series with the appropriate name. this is only called from two places, the okbutton, and keypress functions above.
        /// </summary>
        private void CreateSeries()
        {
            OrdnanceSeriesTN NewSeries = new OrdnanceSeriesTN(m_oNameSeriesPanel.RenameClassTextBox.Text);
            _CurrnetFaction.OrdnanceSeries.Add(NewSeries);

            m_oNameSeriesPanel.RenameClassTextBox.Text = "";

            m_oMissileDesignPanel.MSeriesComboBox.SelectedIndex = (_CurrnetFaction.OrdnanceSeries.Count - 1);
        }
        #endregion
        /// <summary>
        /// When a new empire/faction is selected this will be run
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EmpireComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_CurrnetFaction.ComponentList.MissileEngineDef.Count != 0)
            {
                _CurrnetMissileEngine = _CurrnetFaction.ComponentList.MissileEngineDef[0];
            }
            else
            {
                m_oMissileDesignPanel.TotalEngineCostTextBox.Text = "";
                m_oMissileDesignPanel.TotalEngineSizeTextBox.Text = "";
                m_oMissileDesignPanel.TotalEPTextBox.Text = "";
                m_oMissileDesignPanel.MissileEngineComboBox.Text = "";
                _CurrnetMissileEngine = null;
            }

            if (_CurrnetFaction.OrdnanceSeries.Count != 0)
            {
                _CurrnetMissileSeries = _CurrnetFaction.OrdnanceSeries[0];
                m_oMissileDesignPanel.MSeriesComboBox.SelectedIndex = 0;
            }
            else
            {
                String Error = String.Format("Faction {0} somehow has no default missile series \"No Series Selected\".", _CurrnetFaction.Name);
                MessageEntry MessageEntry = new MessageEntry(MessageEntry.MessageType.Error, null, null,
                                                      GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, Error);
                _CurrnetFaction.MessageLog.Add(MessageEntry);
                _CurrnetMissileSeries = null;
            }

            if (_CurrnetFaction.ComponentList.MissileDef.Count == 0)
            {
                m_oMissileDesignPanel.WHMSPTextBox.Text = "0";
                m_oMissileDesignPanel.FuelMSPTextBox.Text = "0";
                m_oMissileDesignPanel.AgilityMSPTextBox.Text = "0";
                m_oMissileDesignPanel.ActiveMSPTextBox.Text = "0";
                m_oMissileDesignPanel.ThermalMSPTextBox.Text = "0";
                m_oMissileDesignPanel.EMMSPTextBox.Text = "0";
                m_oMissileDesignPanel.GeoMSPTextBox.Text = "0";
                m_oMissileDesignPanel.ResolutionTextBox.Text = "1";
                m_oMissileDesignPanel.ArmourMSPTextBox.Text = "0";
                m_oMissileDesignPanel.ECMMSPTextBox.Text = "0";
                m_oMissileDesignPanel.LaserWCheckBox.Checked = false;
                m_oMissileDesignPanel.ERCheckBox.Checked = false;
                m_oMissileDesignPanel.NumEnginesTextBox.Text = "1";
                m_oMissileDesignPanel.SubNumberTextBox.Text = "0";
                m_oMissileDesignPanel.SepRangeTextBox.Text = "150";
                m_oMissileDesignPanel.SubSizeTextBox.Text = "0";
                m_oMissileDesignPanel.SubCostTextBox.Text = "0";
                m_oMissileDesignPanel.SubTotalSizeTextBox.Text = "0";
                m_oMissileDesignPanel.SubTotalCostTextBox.Text = "0";
                SubMunition = null;

                m_oMissileDesignPanel.PreviousOrdnanceComboBox.Text = "";
            }

            BuildSubMunitionComboBox();

            BuildMissileDesignPage();
        }

        /// <summary>
        /// When a new missile engine is selected this will be run
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MissileEngineComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            BuildMissileDesignPage();
        }

        /// <summary>
        /// Upon new missile series selected rebuild the series box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MSeriesComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            BuildMissileSeriesBox();
        }

        /// <summary>
        /// sets all fields to the value of the previous ordnance
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PreviousOrdnanceComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            String Entry;

            Entry = String.Format("{0:N4}", CurrentMissile.wMSP);

            if (Entry == "0.0000")
                Entry = "0";

            m_oMissileDesignPanel.WHMSPTextBox.Text = Entry;

            Entry = String.Format("{0:N4}", (CurrentMissile.fuel / 2500.0f));

            if (Entry == "0.0000")
                Entry = "0";

            m_oMissileDesignPanel.FuelMSPTextBox.Text = Entry;

            Entry = String.Format("{0:N4}", CurrentMissile.agMSP);

            if (Entry == "0.0000")
                Entry = "0";

            m_oMissileDesignPanel.AgilityMSPTextBox.Text = Entry;

            Entry = String.Format("{0:N4}", CurrentMissile.acMSP);

            if (Entry == "0.0000")
                Entry = "0";

            m_oMissileDesignPanel.ActiveMSPTextBox.Text = Entry;

            Entry = String.Format("{0:N4}", CurrentMissile.tMSP);

            if (Entry == "0.0000")
                Entry = "0";

            m_oMissileDesignPanel.ThermalMSPTextBox.Text = Entry;

            Entry = String.Format("{0:N4}", CurrentMissile.eMSP);

            if (Entry == "0.0000")
                Entry = "0";

            m_oMissileDesignPanel.EMMSPTextBox.Text = Entry;

            Entry = String.Format("{0:N4}", CurrentMissile.gMSP);

            if (Entry == "0.0000")
                Entry = "0";

            m_oMissileDesignPanel.GeoMSPTextBox.Text = Entry;

            if (CurrentMissile.activeStr != 0)
                m_oMissileDesignPanel.ResolutionTextBox.Text = CurrentMissile.aSD.resolution.ToString();
            else
                m_oMissileDesignPanel.ResolutionTextBox.Text = "1";

            m_oMissileDesignPanel.ArmourMSPTextBox.Text = CurrentMissile.armor.ToString();

            if (CurrentMissile.eCMValue != 0)
                m_oMissileDesignPanel.ECMMSPTextBox.Text = "1";
            else
                m_oMissileDesignPanel.ECMMSPTextBox.Text = "0";

            /// <summary>
            /// radValue is not equal to warhead if the warhead is a bomb pumped laser so handle that one first.
            /// </summary>
            if (CurrentMissile.isLaser == true)
                m_oMissileDesignPanel.LaserWCheckBox.Checked = true;
            else if (CurrentMissile.radValue != CurrentMissile.warhead)
                m_oMissileDesignPanel.ERCheckBox.Checked = true;
            else
            {
                m_oMissileDesignPanel.LaserWCheckBox.Checked = false;
                m_oMissileDesignPanel.ERCheckBox.Checked = false;
            }

            CurrentMissileSeries = CurrentMissile.ordSeries;
            int index = _CurrnetFaction.OrdnanceSeries.IndexOf(CurrentMissileSeries);
            m_oMissileDesignPanel.MSeriesComboBox.SelectedIndex = index;

            if (CurrentMissile.ordnanceEngine != null)
            {
                index = _CurrnetFaction.ComponentList.MissileEngineDef.IndexOf(CurrentMissile.ordnanceEngine);
                m_oMissileDesignPanel.MissileEngineComboBox.SelectedIndex = index;
                CurrentMissileEngine = CurrentMissile.ordnanceEngine;
            }

            m_oMissileDesignPanel.NumEnginesTextBox.Text = CurrentMissile.engineCount.ToString();

            if (CurrentMissile.subRelease != null)
            {
                index = _CurrnetFaction.ComponentList.MissileDef.IndexOf(CurrentMissile.subRelease);

                BuildSubMunitionComboBox();

                m_oMissileDesignPanel.SubMunitionComboBox.SelectedIndex = index;
                SubMunition = CurrentMissile.subRelease;
                m_oMissileDesignPanel.SubNumberTextBox.Text = CurrentMissile.subReleaseCount.ToString();
                m_oMissileDesignPanel.SepRangeTextBox.Text = CurrentMissile.subReleaseDistance.ToString();
            }
            else
            {
                m_oMissileDesignPanel.SubNumberTextBox.Text = "0";
                m_oMissileDesignPanel.SepRangeTextBox.Text = "150";
            }

            BuildMissileDesignPage();
        }

        /// <summary>
        /// On submunition selection change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SubMunitionComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = m_oMissileDesignPanel.SubMunitionComboBox.SelectedIndex;

            SubMunition = _CurrnetFaction.ComponentList.MissileDef[index];
            BuildMissileDesignPage();
        }

        /// <summary>
        /// Closes the dialogbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseMDButton_Click(object sender, EventArgs e)
        {
            m_oMissileDesignPanel.MissileSummaryTextBox.Clear();
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oMissileDesignPanel.Hide();
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }


        /// <summary>
        /// On player input, rebuild the missile design page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnyTextBox_TextChanged(object sender, EventArgs e)
        {
            BuildMissileDesignPage();
        }

        /// <summary>
        /// The ER checkbox is checked or unchecked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ERCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (m_oMissileDesignPanel.ERCheckBox.Checked == true)
            {
                m_oMissileDesignPanel.LaserWCheckBox.Checked = false;

                Enhanced = true;
                Laser = false;
            }
            else
            {
                Enhanced = false;
            }

            BuildMissileDesignPage();
        }

        /// <summary>
        /// The Laser warhead checkbox is checked or unchecked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LWCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (m_oMissileDesignPanel.LaserWCheckBox.Checked == true)
            {
                m_oMissileDesignPanel.ERCheckBox.Checked = false;

                Laser = true;
                Enhanced = false;
            }
            else
            {
                Laser = false;
            }

            BuildMissileDesignPage();
        }


        /// <summary>
        /// A new missile Series has to be created here.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateSeriesButton_Click(object sender, EventArgs e)
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oNameSeriesPanel.Show();
            m_oNameSeriesPanel.RenameClassTextBox.Focus();
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        /// <summary>
        /// Deletes an existing Series. The "No Series Selected" Series cannot be deleted however.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteSeriesButton_Click(object sender, EventArgs e)
        {
            int index = m_oMissileDesignPanel.MSeriesComboBox.SelectedIndex;

            if (index > 0 && index < _CurrnetFaction.OrdnanceSeries.Count)
            {
                foreach (OrdnanceDefTN def in _CurrnetFaction.OrdnanceSeries[index].missilesInSeries)
                {
                    def.ordSeries = _CurrnetFaction.OrdnanceSeries[0];
                }

                _CurrnetFaction.OrdnanceSeries.RemoveAt(index);
            }

            BuildMissileDesignPage();
        }

        /// <summary>
        /// Instantly adds the current Ordnance Project to the missile def list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InstantButton_Click(object sender, EventArgs e)
        {

            OrdnanceProject.ordSeries = _CurrnetMissileSeries;
            _CurrnetMissileSeries.AddMissileToSeries(OrdnanceProject);

            if (OrdnanceProject.Name != m_oMissileDesignPanel.MissileNameTextBox.Text)
                OrdnanceProject.Name = m_oMissileDesignPanel.MissileNameTextBox.Text;
            _CurrnetFaction.ComponentList.MissileDef.Add(OrdnanceProject);

            m_oMissileDesignPanel.PreviousOrdnanceComboBox.SelectedIndex = m_oMissileDesignPanel.PreviousOrdnanceComboBox.Items.Count - 1;
            CurrentMissile = OrdnanceProject;

            BuildSubMunitionComboBox();

            BuildMissileDesignPage();
        }

        /// <summary>
        /// Clears the design window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearDesignButton_Click(object sender, EventArgs e)
        {
            String Entry = "0";

            m_oMissileDesignPanel.WHMSPTextBox.Text = Entry;
            m_oMissileDesignPanel.FuelMSPTextBox.Text = Entry;
            m_oMissileDesignPanel.AgilityMSPTextBox.Text = Entry;
            m_oMissileDesignPanel.ActiveMSPTextBox.Text = Entry;
            m_oMissileDesignPanel.ThermalMSPTextBox.Text = Entry;
            m_oMissileDesignPanel.EMMSPTextBox.Text = Entry;
            m_oMissileDesignPanel.GeoMSPTextBox.Text = Entry;
            m_oMissileDesignPanel.ResolutionTextBox.Text = "1";
            m_oMissileDesignPanel.ArmourMSPTextBox.Text = Entry;
            m_oMissileDesignPanel.ECMMSPTextBox.Text = "0";
            m_oMissileDesignPanel.LaserWCheckBox.Checked = false;
            m_oMissileDesignPanel.ERCheckBox.Checked = false;
            m_oMissileDesignPanel.NumEnginesTextBox.Text = "0";
            m_oMissileDesignPanel.SubNumberTextBox.Text = "0";
            m_oMissileDesignPanel.SepRangeTextBox.Text = "150";

            BuildMissileDesignPage();
        }

        /// <summary>
        /// alters the previous missile selections series to the current series.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetSeriesButton_Click(object sender, EventArgs e)
        {
            if (CurrentMissile != null)
            {
                OrdnanceSeriesTN LastSeries = CurrentMissile.ordSeries;
                LastSeries.missilesInSeries.Remove(CurrentMissile);

                CurrentMissile.ordSeries = CurrentMissileSeries;
                CurrentMissileSeries.AddMissileToSeries(CurrentMissile);

                BuildMissileDesignPage();
            }
        }

        /// <summary>
        /// Selects whether the tech page should be displayed or whether the missile info label should be displayed.
        /// True = Info, False = tech. Totally arbitrary.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToggleInfoButton_Click(object sender, EventArgs e)
        {
            if (InfoToggle == false)
            {
                InfoToggle = true;
                m_oMissileDesignPanel.InfoLabel.Visible = true;
                m_oMissileDesignPanel.TechDataGrid.Visible = false;
                m_oMissileDesignPanel.InfoGroupBox.Text = "";
            }
            else
            {
                InfoToggle = false;
                m_oMissileDesignPanel.InfoLabel.Visible = false;
                m_oMissileDesignPanel.TechDataGrid.Visible = true;
                m_oMissileDesignPanel.InfoGroupBox.Text = "Current Missile Technology";
            }
        }

        /// <summary>
        /// This needs to update all class designs to use the current missile instead of older missiles.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReplaceAllButton_Click(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// This needs to create a research project for this faction at some point.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateMissileButton_Click(object sender, EventArgs e)
        {
        }

        #region Build / Calculate info

        /// <summary>
        /// Displays all information based on player input.
        /// </summary>
        private void BuildMissileDesignPage()
        {
            BuildBasicMissileParametersBox();
            BuildSensorParametersBox();
            BuildDefenceParametersBox();
            BuildMissileSeriesBox();

            if (_CurrnetMissileEngine != null)
            {
                BuildMissileEngineBox();
            }

            BuildSubmunitionBox();
            BuildInfoTechPage();

            BuildMissileSummary();
        }

        /// <summary>
        /// Builds the missile engine group box
        /// </summary>
        private void BuildMissileEngineBox()
        {

            int number;

            bool result = Int32.TryParse(m_oMissileDesignPanel.NumEnginesTextBox.Text, out number);

            if (result == true)
            {
                if (number < 0)
                    number = 0;

                float EP = _CurrnetMissileEngine.enginePower * number;
                float size = _CurrnetMissileEngine.size * number * 20.0f;
                decimal cost = _CurrnetMissileEngine.cost * number;
                m_oMissileDesignPanel.TotalEngineCostTextBox.Text = String.Format("{0:N4}", cost);
                m_oMissileDesignPanel.TotalEngineSizeTextBox.Text = String.Format("{0:N2}", size);
                m_oMissileDesignPanel.TotalEPTextBox.Text = String.Format("{0:N3}", EP);
                EngineCount = number;
            }
        }

        /// <summary>
        /// Builds Warhead,fuel,agility and later reactor values from player entries.
        /// </summary>
        private void BuildBasicMissileParametersBox()
        {
            float WarH;
            float Fuel;
            float Agil;

            bool res = float.TryParse(m_oMissileDesignPanel.WHMSPTextBox.Text, out WarH);

            if (res && WarH >= 0.0f)
            {
                int WHTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.WarheadStrength];
                if (WHTech > 11)
                    WHTech = 11;

                int warhead = (int)Math.Floor(WarH * Constants.OrdnanceTN.warheadTech[WHTech]);
                WarheadMSP = WarH;
                WarheadValue = warhead;
                m_oMissileDesignPanel.WHValueTextBox.Text = WarheadValue.ToString();
            }
            else if (WarH < 0.0f)
            {
                WarheadMSP = 0.0f;
                WarheadValue = 0.0f;

                m_oMissileDesignPanel.WHMSPTextBox.Text = "0";
                m_oMissileDesignPanel.WHValueTextBox.Text = "0";
            }
            else
            {
                WarheadMSP = 0.0f;
                WarheadValue = 0.0f;
            }

            res = float.TryParse(m_oMissileDesignPanel.FuelMSPTextBox.Text, out Fuel);

            if (res && Fuel >= 0.0f)
            {
                float FuelVal = Fuel * 2500.0f;
                FuelMSP = Fuel;
                FuelValue = FuelVal;
                m_oMissileDesignPanel.FuelValueTextBox.Text = FuelValue.ToString();
            }
            else if (Fuel < 0.0f)
            {
                FuelMSP = 0.0f;
                FuelValue = 0.0f;

                m_oMissileDesignPanel.FuelMSPTextBox.Text = "0";
                m_oMissileDesignPanel.FuelValueTextBox.Text = "0";
            }
            else
            {
                FuelMSP = 0.0f;
                FuelValue = 0.0f;
            }

            res = float.TryParse(m_oMissileDesignPanel.AgilityMSPTextBox.Text, out Agil);

            if (res && Agil >= 0.0f)
            {
                int AgilTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.MissileAgility];
                if (AgilTech > 11)
                    AgilTech = 11;

                int agility = (int)Math.Floor(Agil * Constants.OrdnanceTN.agilityTech[AgilTech]);
                AgilityMSP = Agil;
                AgilityValue = agility;
                m_oMissileDesignPanel.AgilityValueTextBox.Text = AgilityValue.ToString();
            }
            else if (Agil < 0.0f)
            {
                AgilityMSP = 0.0f;
                AgilityValue = 0.0f;

                m_oMissileDesignPanel.AgilityMSPTextBox.Text = "0";
                m_oMissileDesignPanel.AgilityValueTextBox.Text = "0";
            }
            else
            {
                AgilityMSP = 0.0f;
                AgilityValue = 0.0f;
            }
        }

        /// <summary>
        /// Builds active, thermal, em, and geo sensor additions to the missile. reactor is modified by these.
        /// </summary>
        private void BuildSensorParametersBox()
        {
            float Active;
            float Thermal;
            float EM;
            float Geo;
            ushort Resol;

            bool res = float.TryParse(m_oMissileDesignPanel.ActiveMSPTextBox.Text, out Active);

            if (res && Active >= 0.0f)
            {
                int ActTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.ActiveSensorStrength];
                if (ActTech > 11)
                    ActTech = 11;

                ActiveMSP = Active;
                ActiveValue = Constants.OrdnanceTN.activeTech[ActTech] * ActiveMSP;
                m_oMissileDesignPanel.ActiveValueTextBox.Text = ActiveValue.ToString();
            }
            else if (Active < 0.0f)
            {
                ActiveMSP = 0.0f;
                ActiveValue = 0.0f;

                m_oMissileDesignPanel.ActiveMSPTextBox.Text = "0";
                m_oMissileDesignPanel.ActiveValueTextBox.Text = "0";
            }
            else
            {
                ActiveMSP = 0.0f;
                ActiveValue = 0.0f;
            }

            res = float.TryParse(m_oMissileDesignPanel.ThermalMSPTextBox.Text, out Thermal);

            if (res && Thermal >= 0.0f)
            {
                int THTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.ThermalSensorSensitivity];
                if (THTech > 11)
                    THTech = 11;

                ThermalMSP = Thermal;
                ThermalValue = Constants.OrdnanceTN.passiveTech[THTech] * ThermalMSP;
                m_oMissileDesignPanel.ThermalValueTextBox.Text = ThermalValue.ToString();
            }
            else if (Thermal < 0.0f)
            {
                ThermalMSP = 0.0f;
                ThermalValue = 0.0f;

                m_oMissileDesignPanel.ThermalMSPTextBox.Text = "0";
                m_oMissileDesignPanel.ThermalValueTextBox.Text = "0";
            }
            else
            {
                ThermalMSP = 0.0f;
                ThermalValue = 0.0f;
            }

            res = float.TryParse(m_oMissileDesignPanel.EMMSPTextBox.Text, out EM);

            if (res && EM >= 0.0f)
            {
                int EMTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.EMSensorSensitivity];
                if (EMTech > 11)
                    EMTech = 11;

                EMMSP = EM;
                EMValue = Constants.OrdnanceTN.passiveTech[EMTech] * EMMSP;
                m_oMissileDesignPanel.EMValueTextBox.Text = EMValue.ToString();
            }
            else if (EM < 0.0f)
            {
                EMMSP = 0.0f;
                EMValue = 0.0f;

                m_oMissileDesignPanel.EMMSPTextBox.Text = "0";
                m_oMissileDesignPanel.EMValueTextBox.Text = "0";
            }
            else
            {
                EMMSP = 0.0f;
                EMValue = 0.0f;
            }

            res = float.TryParse(m_oMissileDesignPanel.GeoMSPTextBox.Text, out Geo);

            if (res && Geo >= 0.0f)
            {
                int GeoTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.GeoSensor];
                if (GeoTech > 3)
                    GeoTech = 3;

                GeoMSP = Geo;
                GeoValue = Constants.OrdnanceTN.geoTech[GeoTech] * GeoMSP;
                m_oMissileDesignPanel.GeoValueTextBox.Text = GeoValue.ToString();
            }
            else if (Geo < 0.0f)
            {
                GeoMSP = 0.0f;
                GeoValue = 0.0f;

                m_oMissileDesignPanel.GeoMSPTextBox.Text = "0";
                m_oMissileDesignPanel.GeoValueTextBox.Text = "0";
            }
            else
            {
                GeoMSP = 0.0f;
                GeoValue = 0.0f;
            }

            res = UInt16.TryParse(m_oMissileDesignPanel.ResolutionTextBox.Text, out Resol);

            if (res)
            {
                if (Resol <= 0)
                {
                    Resol = 1;
                    m_oMissileDesignPanel.ResolutionTextBox.Text = Resolution.ToString();
                }
                else if (Resol > 500)
                {
                    Resol = 500;
                    m_oMissileDesignPanel.ResolutionTextBox.Text = Resolution.ToString();
                }

                Resolution = Resol;

            }

            ReactorValue = ((ActiveValue + ThermalValue + EMValue + GeoValue) / 5.0f);

            if (ReactorValue > 0.0f)
            {
                int ReactorTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.ReactorBaseTech];
                if (ReactorTech > 11)
                    ReactorTech = 11;

                ReactorMSP = ReactorValue / Constants.OrdnanceTN.reactorTech[ReactorTech];

                m_oMissileDesignPanel.ReactorMSPTextBox.Text = ReactorMSP.ToString();
                m_oMissileDesignPanel.ReactorValueTextBox.Text = ReactorValue.ToString();
            }
            else
            {
                ReactorMSP = 0.0f;
                ReactorValue = 0.0f;
                m_oMissileDesignPanel.ReactorMSPTextBox.Text = "0";
                m_oMissileDesignPanel.ReactorValueTextBox.Text = "0";
            }
        }

        /// <summary>
        /// Builds the armour and ECM text boxes
        /// </summary>
        private void BuildDefenceParametersBox()
        {
            float Armour;
            float ECM;

            bool res = float.TryParse(m_oMissileDesignPanel.ArmourMSPTextBox.Text, out Armour);

            if (res && Armour >= 0.0f)
            {

                ArmourMSP = Armour;
                /// <summary>
                /// Should Missile Armour be based on armour tech?
                /// </summary>
                ArmourValue = ArmourMSP;
                m_oMissileDesignPanel.ArmourValueTextBox.Text = ArmourValue.ToString();
            }
            else if (Armour < 0.0f)
            {
                ArmourMSP = 0.0f;
                ArmourValue = 0.0f;

                m_oMissileDesignPanel.ArmourMSPTextBox.Text = "0";
                m_oMissileDesignPanel.ArmourValueTextBox.Text = "0";
            }
            else
            {
                ArmourMSP = 0.0f;
                ArmourValue = 0.0f;
            }

            res = float.TryParse(m_oMissileDesignPanel.ECMMSPTextBox.Text, out ECM);

            if (res && ECM > 0.0f)
            {
                int ECMTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.MissileECM];
                if (ECMTech > 9)
                    ECMTech = 9;

                if (ECM > 1.0f)
                    ECMMSP = 1.0f;
                else
                    ECMMSP = ECM;

                ECMValue = (float)((ECMTech + 1) * 10) * ECMMSP;


                m_oMissileDesignPanel.ECMMSPTextBox.Text = ECMMSP.ToString();
                m_oMissileDesignPanel.ECMValueTextBox.Text = ECMValue.ToString();
            }
            else if (res && ECM < 0.0f)
            {
                ECMMSP = 0.0f;
                ECMValue = 0.0f;

                m_oMissileDesignPanel.ECMMSPTextBox.Text = "0";
                m_oMissileDesignPanel.ECMValueTextBox.Text = "0";
            }
            else if (res && ECM == 0.0f)
            {
                ECMMSP = 0.0f;
                ECMValue = 0.0f;
            }
            else if (res == false)
            {
                ECMMSP = 0.0f;
                ECMValue = 0.0f;
            }

        }

        /// <summary>
        /// Populate this particular list
        /// </summary>
        private void BuildMissileSeriesBox()
        {
            m_oMissileDesignPanel.MSeriesListBox.Items.Clear();

            if (m_oMissileDesignPanel.MSeriesComboBox.SelectedIndex != 0 && CurrentMissileSeries != null)
            {
                foreach (OrdnanceDefTN Def in CurrentMissileSeries.missilesInSeries)
                {
                    m_oMissileDesignPanel.MSeriesListBox.Items.Add(Def.Name);
                }
            }
        }

        /// <summary>
        /// Submuntion display handler
        /// </summary>
        private void BuildSubmunitionBox()
        {
            int SNumber;
            int SRange;

            bool res = Int32.TryParse(m_oMissileDesignPanel.SepRangeTextBox.Text, out SRange);

            if (res)
            {
                if (SRange >= 0)
                {
                    SepRange = SRange;
                }
                else
                {
                    m_oMissileDesignPanel.SepRangeTextBox.Text = "0";
                    SepRange = 0;
                }
            }
            else
                SepRange = 0;

            res = Int32.TryParse(m_oMissileDesignPanel.SubNumberTextBox.Text, out SNumber);

            if (res)
            {
                if (SNumber >= 0)
                {
                    SubNumber = SNumber;
                }
                else
                {
                    m_oMissileDesignPanel.SubNumberTextBox.Text = "0";
                    SubNumber = 0;
                }
            }
            else
                SubNumber = 0;

            if (SubMunition != null)
            {
                m_oMissileDesignPanel.SubSizeTextBox.Text = (SubMunition.size).ToString();
                m_oMissileDesignPanel.SubCostTextBox.Text = SubMunition.cost.ToString();

                String Entry = String.Format("{0:N4}", (SubMunition.size * SubNumber));
                m_oMissileDesignPanel.SubTotalSizeTextBox.Text = Entry;

                Entry = String.Format("{0:N4}", (SubMunition.cost * SubNumber));

                m_oMissileDesignPanel.SubTotalCostTextBox.Text = Entry;
            }
        }

        /// <summary>
        /// Build the tech list if toggled: True = Info, False = tech. Totally arbitrary.
        /// </summary>
        private void BuildInfoTechPage()
        {
            if (InfoToggle == false)
            {
                PrintTechDataGridInfo();
            }
        }

        /// <summary>
        /// Builds the summary textbox and name.
        /// </summary>
        private void BuildMissileSummary()
        {
            m_oMissileDesignPanel.MissileNameTextBox.Clear();
            m_oMissileDesignPanel.MissileSummaryTextBox.Clear();

            int ECMTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.MissileECM];
            if (ECMTech == -1)
                m_oMissileDesignPanel.ECMMSPTextBox.ReadOnly = true;
            else
                m_oMissileDesignPanel.ECMMSPTextBox.ReadOnly = false;

            int ERTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.EnhancedRadiationWarhead];
            if (ERTech == -1)
                m_oMissileDesignPanel.ERCheckBox.Enabled = false;
            else
                m_oMissileDesignPanel.ERCheckBox.Enabled = true;

            int LWTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.LaserWarhead];
            if (LWTech == -1)
                m_oMissileDesignPanel.LaserWCheckBox.Enabled = false;
            else
                m_oMissileDesignPanel.LaserWCheckBox.Enabled = true;

            double TotalSize = WarheadMSP + FuelMSP + AgilityMSP + ActiveMSP + ThermalMSP + EMMSP + GeoMSP + ReactorMSP + ECMMSP + ArmourMSP;
            if (_CurrnetMissileEngine != null && EngineCount != 0)
            {
                TotalSize = TotalSize + (EngineCount * _CurrnetMissileEngine.size * 20.0f);
            }

            if (SubMunition != null && SubNumber != 0)
            {
                TotalSize = TotalSize + (SubMunition.size * SubNumber);
            }

            if (TotalSize != 0.0 && TotalSize <= Constants.OrdnanceTN.MaxSize)
            {
                #region Tech
                int WHTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.WarheadStrength];
                if (WHTech > 11)
                    WHTech = 11;

                int AgilTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.MissileAgility];
                if (AgilTech > 11)
                    AgilTech = 11;

                int ActTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.ActiveSensorStrength];
                if (ActTech > 11)
                    ActTech = 11;

                int THTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.ThermalSensorSensitivity];
                if (THTech > 11)
                    THTech = 11;

                int EMTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.EMSensorSensitivity];
                if (EMTech > 11)
                    EMTech = 11;

                int GeoTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.GeoSensor];
                if (GeoTech > 3)
                    GeoTech = 3;

                int ReactorTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.ReactorBaseTech];
                if (ReactorTech > 11)
                    ReactorTech = 11;

                if (ECMTech > 9)
                    ECMTech = 9;

                if (ERTech > 3)
                    ERTech = 3;

                if (LWTech > 3)
                    LWTech = 3;

                #endregion

                m_oMissileDesignPanel.MissileNameTextBox.Text = String.Format("Size {0:N4} Missile", TotalSize);

                OrdnanceProject = new OrdnanceDefTN(m_oMissileDesignPanel.MissileNameTextBox.Text, null, WarheadMSP, WHTech, FuelMSP, AgilityMSP, AgilTech, ActiveMSP, ActTech, ThermalMSP, THTech, EMMSP, EMTech, GeoMSP, GeoTech, Resolution,
                                                    ReactorTech, ArmourValue, ECMMSP, ECMTech, Enhanced, ERTech, Laser, LWTech, _CurrnetMissileEngine, EngineCount, SubMunition, SubNumber, SepRange);

                String Entry = String.Format("Missile Size: {0:N4} MSP  ({1} HS)     Warhead: {2}    Armour: {3}     Manoeuvre Rating: {4}\n",
                                              OrdnanceProject.size, OrdnanceProject.size / 20.0f, OrdnanceProject.warhead, OrdnanceProject.armor, OrdnanceProject.manuever);
                m_oMissileDesignPanel.MissileSummaryTextBox.AppendText(Entry);

                String FormattedSpeed = OrdnanceProject.maxSpeed.ToString("#,##0");

                String EndString = "N/A";
                String RangeString = "N/A";

                if (OrdnanceProject.fuel == 0.0f || OrdnanceProject.totalFuelConsumption == 0.0f)
                {
                    RangeString = "0.0M km";
                    EndString = "0 Minutes";
                }
                else
                {
                    float Endurance = (OrdnanceProject.fuel / OrdnanceProject.totalFuelConsumption);

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

                    if (Endurance != 0.0f)
                    {
                        float TimeOneBillionKM = (1000000000.0f / OrdnanceProject.maxSpeed) / 3600.0f;
                        float test = Endurance / TimeOneBillionKM;

                        if (test >= 1.0f)
                        {
                            RangeString = String.Format("{0:N1}B km", test);
                        }
                        else
                        {
                            float range = (Endurance * (OrdnanceProject.maxSpeed * 3600.0f)) / 1000000.0f;
                            RangeString = String.Format("{0:N1}M km", range);
                        }
                    }
                }

                Entry = String.Format("Speed: {0} km/s    Engine Endurance: {1}   Range: {2}\n", FormattedSpeed, EndString, RangeString);
                m_oMissileDesignPanel.MissileSummaryTextBox.AppendText(Entry);

                if (OrdnanceProject.activeStr != 0.0f)
                {
                    Entry = String.Format("Active Sensor Strength: {0}   Sensitivity Modifier: {1}%\n", OrdnanceProject.activeStr, (_CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.EMSensorSensitivity] * 10.0f));
                    m_oMissileDesignPanel.MissileSummaryTextBox.AppendText(Entry);

                    String FormattedSRange = (OrdnanceProject.aSD.maxRange * 10000).ToString("#,##0");
                    Entry = String.Format("Resolution: {0}    Maximum Range vs {1} ton object (or larger): {2} km\n", OrdnanceProject.aSD.resolution, ((float)OrdnanceProject.aSD.resolution * 50.0f), FormattedSRange);
                    m_oMissileDesignPanel.MissileSummaryTextBox.AppendText(Entry);
                }

                if (OrdnanceProject.thermalStr != 0.0f)
                {
                    String FormattedSRange = (OrdnanceProject.tHD.range * 10000).ToString("#,##0");
                    Entry = String.Format("Thermal Sensor Strength: {0}    Detect Sig Strength 1000:  {1} km\n", OrdnanceProject.thermalStr, FormattedSRange);
                    m_oMissileDesignPanel.MissileSummaryTextBox.AppendText(Entry);
                }

                if (OrdnanceProject.eMStr != 0.0f)
                {
                    String FormattedSRange = (OrdnanceProject.eMD.range * 10000).ToString("#,##0");
                    Entry = String.Format("EM Sensor Strength: {0}    Detect Sig Strength 1000:  {1} km\n", OrdnanceProject.eMStr, FormattedSRange);
                    m_oMissileDesignPanel.MissileSummaryTextBox.AppendText(Entry);
                }

                if (OrdnanceProject.geoStr != 0.0f)
                {
                    float maxPoints = 0.0f;

                    if (OrdnanceProject.engineCount > 0 && OrdnanceProject.fuel > 0.0f)
                    {
                        float Endurance = (OrdnanceProject.fuel / OrdnanceProject.totalFuelConsumption);
                        maxPoints = OrdnanceProject.geoStr * Endurance;
                    }

                    Entry = String.Format("Geo Sensor Strength: {0}    Maximum points: {1:N4}\n", OrdnanceProject.geoStr, maxPoints);
                    m_oMissileDesignPanel.MissileSummaryTextBox.AppendText(Entry);
                }

                if (OrdnanceProject.isLaser == true)
                {
                    Entry = String.Format("Laser Warhead\n");
                    m_oMissileDesignPanel.MissileSummaryTextBox.AppendText(Entry);
                }
                else if (OrdnanceProject.radValue != OrdnanceProject.warhead)
                {
                    Entry = String.Format("Radiation Damage: {0}\n", OrdnanceProject.radValue);
                    m_oMissileDesignPanel.MissileSummaryTextBox.AppendText(Entry);
                }

                if (OrdnanceProject.eCMValue != 0)
                {
                    Entry = String.Format("ECM Level: {0}\n", ((float)OrdnanceProject.eCMValue / 10.0f));
                    m_oMissileDesignPanel.MissileSummaryTextBox.AppendText(Entry);
                }

                Entry = String.Format("Cost Per Missile: {0}\n", OrdnanceProject.cost);
                m_oMissileDesignPanel.MissileSummaryTextBox.AppendText(Entry);


                if (OrdnanceProject.subRelease != null && OrdnanceProject.subReleaseCount != 0)
                {
                    Entry = String.Format("Second Stage: {0} x{1}\n", OrdnanceProject.subRelease, OrdnanceProject.subReleaseCount);
                    m_oMissileDesignPanel.MissileSummaryTextBox.AppendText(Entry);

                    if (OrdnanceProject.subReleaseDistance <= 1000000)
                    {
                        String FormattedSubRange = (OrdnanceProject.subReleaseDistance * 1000).ToString("#,##0");
                        Entry = String.Format("Second Stage Separation Range: {0} km\n", FormattedSubRange);
                    }
                    else
                    {
                        float SD = (OrdnanceProject.subReleaseDistance / 1000000.0f);
                        Entry = String.Format("Second Stage Separation Range: {0} B km\n", SD);
                    }
                    m_oMissileDesignPanel.MissileSummaryTextBox.AppendText(Entry);

                    float Endurance = (OrdnanceProject.fuel / OrdnanceProject.totalFuelConsumption);
                    float SubEndurance = (OrdnanceProject.subRelease.fuel / OrdnanceProject.subRelease.totalFuelConsumption);

                    float TotalEndurance = Endurance + SubEndurance;

                    if (TotalEndurance >= 8640.0f)
                    {
                        float YE = TotalEndurance / 8640.0f;
                        EndString = String.Format("{0:N1} Years", YE);
                    }
                    else if (TotalEndurance >= 720.0f)
                    {
                        float ME = TotalEndurance / 720.0f;
                        EndString = String.Format("{0:N1} Months", ME);
                    }
                    else if (TotalEndurance >= 24.0f)
                    {
                        float DE = TotalEndurance / 24.0f;
                        EndString = String.Format("{0:N1} Days", DE);
                    }
                    else if (TotalEndurance >= 1.0f)
                    {
                        EndString = String.Format("{0:N1} hours", TotalEndurance);
                    }
                    else if ((TotalEndurance * 60.0f) >= 1.0f)
                    {
                        EndString = String.Format("{0:N1} minutes", (TotalEndurance * 60.0f));
                    }
                    else
                    {
                        EndString = String.Format("0 minutes");
                    }

                    if (TotalEndurance != 0.0f)
                    {
                        float TimeOneBillionKM = (1000000000.0f / OrdnanceProject.maxSpeed) / 3600.0f;
                        float TimeOneBillionKMSub = (1000000000.0f / OrdnanceProject.subRelease.maxSpeed) / 3600.0f;

                        float test1 = Endurance / TimeOneBillionKM;
                        float test2 = SubEndurance / TimeOneBillionKMSub;

                        float test = test1 + test2;

                        if (test >= 1.0f)
                        {
                            RangeString = String.Format("{0:N1}B km", test);
                        }
                        else
                        {
                            float range = (Endurance * (OrdnanceProject.maxSpeed * 3600.0f)) / 1000000.0f + (SubEndurance * (OrdnanceProject.subRelease.maxSpeed * 3600.0f)) / 1000000.0f;
                            RangeString = String.Format("{0:N1}M km", range);
                        }
                    }
                    else
                        RangeString = "0.0M";

                    Entry = String.Format("Overall Endurance: {0}   Overall Range: {1} km\n", EndString, RangeString);
                    m_oMissileDesignPanel.MissileSummaryTextBox.AppendText(Entry);
                }

                Entry = String.Format("Chance to Hit: 1k km/s {0}%   3k km/s {1}%   5k km/s {2}%   10k km/s {3}%\n", OrdnanceProject.ToHit(1000.0f), OrdnanceProject.ToHit(3000.0f),
                                                                                                                   OrdnanceProject.ToHit(5000.0f), OrdnanceProject.ToHit(10000.0f));
                m_oMissileDesignPanel.MissileSummaryTextBox.AppendText(Entry);

                Entry = "Materials Required: ";
                for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
                {
                    if (OrdnanceProject.minerialsCost[mineralIterator] != 0.0m)
                    {
                        Entry = String.Format("{0}   {1:N3}x {2}", Entry, OrdnanceProject.minerialsCost[mineralIterator], (Constants.Minerals.MinerialNames)mineralIterator);
                    }
                }
                if (OrdnanceProject.fuel != 0.0f)
                {
                    Entry = String.Format("{0}   Fuel x{1}\n", Entry, Math.Floor(OrdnanceProject.fuelCost));
                }

                m_oMissileDesignPanel.MissileSummaryTextBox.AppendText(Entry);

                Entry = String.Format("\nDevelopment Cost for Project: {0}RP\n", (OrdnanceProject.cost * 100));
                m_oMissileDesignPanel.MissileSummaryTextBox.AppendText(Entry);

            }
            else if (TotalSize > Constants.OrdnanceTN.MaxSize)
            {
                String Entry = String.Format("Missile Exceeds max size.\n");
                m_oMissileDesignPanel.MissileSummaryTextBox.AppendText(Entry);
            }
        }
        #endregion

        #region Data Grid handling functions
        /// <summary>
        /// Just a space saver here to avoid copy pasting a lot.
        /// </summary>
        /// <param name="Header">Text of column header.</param>
        /// <param name="newPadding">Padding in use, not sure what this is or why its necessary. Cargo culting it is.</param>
        private void AddColumn(String Header, Padding newPadding)
        {
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.HeaderText = Header;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                col.DefaultCellStyle.Padding = newPadding;
                if (col != null)
                {
                    m_oMissileDesignPanel.TechDataGrid.Columns.Add(col);
                }
            }
        }

        /// <summary>
        /// creates the columns and populates the 1st column.
        /// </summary>
        private void SetupTechDataGrid()
        {
            try
            {
                /// <summary>
                /// Add columns:
                /// </summary>
                Padding newPadding = new Padding(2, 0, 2, 0);
                AddColumn("Technology Type", newPadding);
                AddColumn("Status", newPadding);


                for (int loop = 0; loop < 13; loop++)
                {
                    using (DataGridViewRow row = new DataGridViewRow())
                    {
                        /// <summary>
                        /// setup row height. note that by default they are 22 pixels in height!
                        /// </summary>
                        row.Height = 18;
                        m_oMissileDesignPanel.TechDataGrid.Rows.Add(row);
                    }
                }

                m_oMissileDesignPanel.TechDataGrid.Rows[0].Cells[0].Value = "Warhead Strength per MSP";
                m_oMissileDesignPanel.TechDataGrid.Rows[1].Cells[0].Value = "Missile Agility per MSP";
                m_oMissileDesignPanel.TechDataGrid.Rows[2].Cells[0].Value = "Fuel Consumption per EPH";
                m_oMissileDesignPanel.TechDataGrid.Rows[3].Cells[0].Value = "ECM Strength per MSP";
                m_oMissileDesignPanel.TechDataGrid.Rows[4].Cells[0].Value = "Active Sensor Strength per MSP";
                m_oMissileDesignPanel.TechDataGrid.Rows[5].Cells[0].Value = "Thermal Sensor Strength per MSP";
                m_oMissileDesignPanel.TechDataGrid.Rows[6].Cells[0].Value = "EM Sensor Strength per MSP";
                m_oMissileDesignPanel.TechDataGrid.Rows[7].Cells[0].Value = "Geo Sensor Strength per MSP";
                m_oMissileDesignPanel.TechDataGrid.Rows[8].Cells[0].Value = "Reactor Power per MSP";
                m_oMissileDesignPanel.TechDataGrid.Rows[9].Cells[0].Value = "Laser Warhead Strength / AR";
                m_oMissileDesignPanel.TechDataGrid.Rows[10].Cells[0].Value = "Enhanced Radiation Modifier";

            }
            catch
            {
#if LOG4NET_ENABLED
                logger.Error("Something went wrong Creating Columns for Missile design screen...");
#endif
            }
        }

        /// <summary>
        /// Builds the tech data grid with the latest tech values.
        /// </summary>
        private void PrintTechDataGridInfo()
        {

            #region Tech
            int WHTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.WarheadStrength];
            if (WHTech > 11)
                WHTech = 11;

            int AgilTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.MissileAgility];
            if (AgilTech > 11)
                AgilTech = 11;

            int FuelTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.FuelConsumption];
            if (FuelTech > 12)
                FuelTech = 12;

            int ActTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.ActiveSensorStrength];
            if (ActTech > 11)
                ActTech = 11;

            int THTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.ThermalSensorSensitivity];
            if (THTech > 11)
                THTech = 11;

            int EMTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.EMSensorSensitivity];
            if (EMTech > 11)
                EMTech = 11;

            int GeoTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.GeoSensor];
            if (GeoTech > 3)
                GeoTech = 3;

            int ReactorTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.ReactorBaseTech];
            if (ReactorTech > 11)
                ReactorTech = 11;

            int ECMTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.MissileECM];
            if (ECMTech > 9)
                ECMTech = 9;

            int ERTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.EnhancedRadiationWarhead];
            if (ERTech > 3)
                ERTech = 3;

            int LWTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.LaserWarhead];
            if (LWTech > 3)
                LWTech = 3;

            #endregion

            m_oMissileDesignPanel.TechDataGrid.Rows[0].Cells[1].Value = Constants.OrdnanceTN.warheadTech[WHTech];
            m_oMissileDesignPanel.TechDataGrid.Rows[1].Cells[1].Value = Constants.OrdnanceTN.agilityTech[AgilTech];
            m_oMissileDesignPanel.TechDataGrid.Rows[2].Cells[1].Value = Constants.EngineTN.FuelConsumption[FuelTech];
            m_oMissileDesignPanel.TechDataGrid.Rows[3].Cells[1].Value = (ECMTech + 1);
            m_oMissileDesignPanel.TechDataGrid.Rows[4].Cells[1].Value = Constants.OrdnanceTN.activeTech[ActTech];
            m_oMissileDesignPanel.TechDataGrid.Rows[5].Cells[1].Value = Constants.OrdnanceTN.passiveTech[THTech];
            m_oMissileDesignPanel.TechDataGrid.Rows[6].Cells[1].Value = Constants.OrdnanceTN.passiveTech[EMTech];
            m_oMissileDesignPanel.TechDataGrid.Rows[7].Cells[1].Value = Constants.OrdnanceTN.geoTech[GeoTech];
            m_oMissileDesignPanel.TechDataGrid.Rows[8].Cells[1].Value = Constants.OrdnanceTN.reactorTech[ReactorTech];
            m_oMissileDesignPanel.TechDataGrid.Rows[9].Cells[1].Value = Constants.OrdnanceTN.laserTech[LWTech];
            m_oMissileDesignPanel.TechDataGrid.Rows[10].Cells[1].Value = Constants.OrdnanceTN.radTech[ERTech];
        }

        #endregion

        #region SubMunition kludge

        /// <summary>
        /// Build the combo box appropriately.
        /// </summary>
        private void BuildSubMunitionComboBox()
        {
            m_oMissileDesignPanel.SubMunitionComboBox.Items.Clear();
            foreach (OrdnanceDefTN Def in _CurrnetFaction.ComponentList.MissileDef)
            {
                m_oMissileDesignPanel.SubMunitionComboBox.Items.Add(Def.Name);
            }

            if (m_oMissileDesignPanel.SubMunitionComboBox.Items.Count != 0)
            {
                m_oMissileDesignPanel.SubMunitionComboBox.SelectedIndex = 0;
            }
            else
            {
                m_oMissileDesignPanel.SubMunitionComboBox.Text = "";
            }
        }
        #endregion

        #endregion
    }
}
