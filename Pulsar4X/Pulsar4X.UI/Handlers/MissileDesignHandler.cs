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
        /// Missile creation values.
        /// </summary>
        private int EngineCount;
        private float WarheadMSP;
        private float WarheadValue;
        private float FuelMSP;
        private float FuelValue;
        private float AgilityMSP;
        private float AgilityValue;

        OrdnanceDefTN OrdnanceProject;

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

            m_oMissileDesignPanel.CloseMDButton.Click += new EventHandler(CloseMDButton_Click);

            m_oMissileDesignPanel.NumEnginesTextBox.TextChanged += new EventHandler(AnyTextBox_TextChanged);
            m_oMissileDesignPanel.WHMSPTextBox.TextChanged += new EventHandler(AnyTextBox_TextChanged);
            m_oMissileDesignPanel.FuelMSPTextBox.TextChanged += new EventHandler(AnyTextBox_TextChanged);
            m_oMissileDesignPanel.AgilityMSPTextBox.TextChanged += new EventHandler(AnyTextBox_TextChanged);

            EngineCount = 0;
            WarheadMSP = 0.0f;
            FuelMSP = 0.0f;
            AgilityMSP = 0.0f;
            OrdnanceProject = null;

            

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
        #endregion


        #region Private methods
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
        /// Closes the dialogbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseMDButton_Click(object sender, EventArgs e)
        {
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
        /// Displays all information based on player input.
        /// </summary>
        private void BuildMissileDesignPage()
        {
            BuildBasicMissileParametersBox();

            if (_CurrnetMissileEngine != null)
            {
                BuildMissileEngineBox();
            }

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
                float EP = _CurrnetMissileEngine.enginePower * number;
                float size = _CurrnetMissileEngine.size * number * 20.0f;
                decimal cost = _CurrnetMissileEngine.cost * number;
                m_oMissileDesignPanel.TotalEngineCostTextBox.Text = String.Format("{0:N4}",cost);
                m_oMissileDesignPanel.TotalEngineSizeTextBox.Text = String.Format("{0:N2}",size);
                m_oMissileDesignPanel.TotalEPTextBox.Text = String.Format("{0:N3}",EP);
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

            if (res)
            {
                int WHTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.WarheadStrength];
                if(WHTech > 11)
                    WHTech = 11;

                int warhead = (int)Math.Floor(WarH * Constants.OrdnanceTN.warheadTech[WHTech]);
                WarheadMSP = WarH;
                WarheadValue = warhead;
                m_oMissileDesignPanel.WHValueTextBox.Text = WarheadValue.ToString();
            }

            res = float.TryParse(m_oMissileDesignPanel.FuelMSPTextBox.Text, out Fuel);

            if (res)
            {
                float FuelVal = Fuel * 2500.0f;
                FuelMSP = Fuel;
                FuelValue = FuelVal;
                m_oMissileDesignPanel.FuelValueTextBox.Text = FuelValue.ToString();
            }

            res = float.TryParse(m_oMissileDesignPanel.AgilityMSPTextBox.Text, out Agil);

            if (res)
            {
                int AgilTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.MissileAgility];
                if (AgilTech > 11)
                    AgilTech = 11;

                int agility = (int)Math.Floor(Agil * Constants.OrdnanceTN.agilityTech[AgilTech]);
                AgilityMSP = Agil;
                AgilityValue = agility;
                m_oMissileDesignPanel.AgilityValueTextBox.Text = AgilityValue.ToString();
            }
            
        }

        /// <summary>
        /// Builds the summary textbox and name.
        /// </summary>
        private void BuildMissileSummary()
        {
            m_oMissileDesignPanel.MissileNameTextBox.Clear();
            m_oMissileDesignPanel.MissileSummaryTextBox.Clear();

            double TotalSize = WarheadMSP + FuelMSP + AgilityMSP;
            if(_CurrnetMissileEngine != null && EngineCount != 0)
            {
                TotalSize = TotalSize + (EngineCount * _CurrnetMissileEngine.size * 20.0f);
            }

            if (TotalSize != 0.0)
            {
                OrdnanceSeries OS_StandIn = new OrdnanceSeries();

                #region Tech
                int WHTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.WarheadStrength];
                if(WHTech > 11)
                    WHTech = 11;

                int AgilTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.MissileAgility];
                if (AgilTech > 11)
                    AgilTech = 11;
                #endregion

                OrdnanceProject = new OrdnanceDefTN(m_oMissileDesignPanel.MissileNameTextBox.Text, OS_StandIn, WarheadMSP, WHTech, FuelMSP, AgilityMSP, AgilTech, 0.0f, 0, 0.0f, 0, 0.0f, 0, 0.0f, 0, 1,
                                                    0, 0, false, 0, false, 0, false, 0, _CurrnetMissileEngine, EngineCount);

                m_oMissileDesignPanel.MissileNameTextBox.Text = String.Format("Size {0:N4} Missile", OrdnanceProject.size);
                String Entry = String.Format("Missile Size: {0:N4} MSP  ({1} HS)     Warhead: {2}    Armour: {3}     Manoeuvre Rating: {4}\n", 
                                              OrdnanceProject.size, OrdnanceProject.size / 20.0f, OrdnanceProject.warhead, OrdnanceProject.armor, OrdnanceProject.manuever);
                m_oMissileDesignPanel.MissileSummaryTextBox.AppendText(Entry);

                String FormattedSpeed = OrdnanceProject.maxSpeed.ToString("#,##0");

                float Endurance = (OrdnanceProject.fuel / OrdnanceProject.totalFuelConsumption);
                String EndString = "N/A";

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
                String RangeString = "0 km";

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
                        float range = (Endurance * (OrdnanceProject.maxSpeed * 3600.0f))/1000000.0f;
                        RangeString = String.Format("{0:N1}M km", range);
                    }
                }

                Entry = String.Format("Speed: {0} km/s    Engine Endurance: {1}   Range: {2}\n",FormattedSpeed,EndString,RangeString);
                m_oMissileDesignPanel.MissileSummaryTextBox.AppendText(Entry);

                Entry = String.Format("Cost Per Missile: {0}\n", OrdnanceProject.cost);
                m_oMissileDesignPanel.MissileSummaryTextBox.AppendText(Entry);

                Entry = String.Format("Chance to Hit: 1k km/s {0}%   3k km/s {1}%   5k km/s {2}%   10k km/s {3}%\n", OrdnanceProject.ToHit(1000.0f), OrdnanceProject.ToHit(3000.0f),
                                                                                                                   OrdnanceProject.ToHit(5000.0f), OrdnanceProject.ToHit(10000.0f));
                m_oMissileDesignPanel.MissileSummaryTextBox.AppendText(Entry);

                Entry = String.Format("Materials Required:    Not Yet Implemented\n");
                m_oMissileDesignPanel.MissileSummaryTextBox.AppendText(Entry);

                Entry = String.Format("\nDevelopment Cost for Project: {0}RP\n",(OrdnanceProject.cost*100));
                m_oMissileDesignPanel.MissileSummaryTextBox.AppendText(Entry);

            }
        }
        #endregion
    }
}
