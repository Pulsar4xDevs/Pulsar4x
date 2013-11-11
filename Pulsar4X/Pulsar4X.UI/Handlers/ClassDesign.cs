using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using Pulsar4X.UI.ViewModels;
using Pulsar4X.Entities;
using Pulsar4X.Stargen;
using Newtonsoft.Json;


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
                _CurrnetFaction = value;

                if (_CurrnetFaction != null)
                {
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
                _CurrnetShipClass = value;

                if (_CurrnetShipClass != null)
                {
                    UpdateDisplay();
                }

            }
        }

        //Panels.ClassDes_DesignAndInfo m_oDesignAndInformationPanel;
        Panels.ClassDes_Options m_oOptionsPanel;
        //Panels.ClassDes_Properties m_oClassPropertiesPanel;

        ClassDesignViewModel VM;


        public ClassDesign()
        {
            // create panels:
            //m_oClassPropertiesPanel = new Panels.ClassDes_Properties();
            //m_oDesignAndInformationPanel = new Panels.ClassDes_DesignAndInfo();
            m_oOptionsPanel = new Panels.ClassDes_Options();

            //m_oClassPropertiesPanel.ClassPropertyGrid.PropertySort = PropertySort.CategorizedAlphabetical;

            // creat ViewModel.
            VM = new ClassDesignViewModel();

            // setup bindings:
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

            //if (VM.CurrentShipClass != null)
            //{
            //    m_oClassPropertiesPanel.ClassPropertyGrid.SelectedObject = VM.CurrentShipClass;
            //}


            // Setup Events:
            m_oOptionsPanel.NewButton.Click += new EventHandler(NewButton_Click);
            

            UpdateDisplay();

        }

        #region event handlers
        private void FactionComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private  void ClassComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            
            //if (VM.CurrentShipClass != null)
            //{
            //    m_oClassPropertiesPanel.ClassPropertyGrid.SelectedObject = VM.CurrentShipClass;
            //}
        }

        private void NewButton_Click(object sender, EventArgs e)
        {
            ShipClassTN oNewShipClass = new ShipClassTN("New Class",VM.CurrentFaction);
            VM.ShipDesigns.Add(oNewShipClass);
            m_oOptionsPanel.ClassComboBox.SelectedItem = oNewShipClass;
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
                BuildClassSummary();
                BuildPowerSystems();
                BuildPassiveDefenses();
            }
        }

        #endregion


        #region Private methods
        /// <summary>
        /// print the current class summary to the appropriate window.
        /// </summary>
        private void BuildClassSummary()
        {
            m_oOptionsPanel.ClassSummaryTextBox.Text = CurrentShipClass.Summary;
        }

        private void BuildPowerSystems()
        {
            m_oOptionsPanel.EnginePowerTextBox.Text = CurrentShipClass.MaxEnginePower.ToString();
            m_oOptionsPanel.MaxSpeedTextBox.Text = CurrentShipClass.MaxSpeed.ToString();
            m_oOptionsPanel.ReactorPowerTextBox.Text = CurrentShipClass.TotalPowerGeneration.ToString();
            m_oOptionsPanel.RequiredPowerTextBox.Text = CurrentShipClass.TotalPowerRequirement.ToString();
        }

        private void BuildPassiveDefenses()
        {
            m_oOptionsPanel.ArmorRatingTextBox.Text = CurrentShipClass.ShipArmorDef.depth.ToString();
            m_oOptionsPanel.ExactClassSizeTextBox.Text = CurrentShipClass.SizeTons.ToString();
            m_oOptionsPanel.ArmorAreaTextBox.Text = (CurrentShipClass.ShipArmorDef.area / 4.0f).ToString();
            m_oOptionsPanel.ArmorStrengthTextBox.Text = Math.Round((CurrentShipClass.ShipArmorDef.area / 16.0) * (double)CurrentShipClass.ShipArmorDef.depth).ToString();
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
        #endregion
    }
}
