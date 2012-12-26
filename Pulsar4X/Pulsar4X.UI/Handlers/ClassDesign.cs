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

        Panels.ClassDes_DesignAndInfo m_oDesignAndInformationPanel;
        Panels.ClassDes_Options m_oOptionsPanel;
        Panels.ClassDes_Properties m_oClassPropertiesPanel;

        ClassDesignViewModel VM;


        public ClassDesign()
        {
            // create panels:
            m_oClassPropertiesPanel = new Panels.ClassDes_Properties();
            m_oDesignAndInformationPanel = new Panels.ClassDes_DesignAndInfo();
            m_oOptionsPanel = new Panels.ClassDes_Options();

            m_oClassPropertiesPanel.ClassPropertyGrid.PropertySort = PropertySort.CategorizedAlphabetical;

            // creat ViewModel.
            VM = new ClassDesignViewModel();

            // setup bindings:
            m_oOptionsPanel.FactionComboBox.Bind(c => c.DataSource, VM, d => d.Factions);
            m_oOptionsPanel.FactionComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentFaction, DataSourceUpdateMode.OnPropertyChanged);
            m_oOptionsPanel.FactionComboBox.DisplayMember = "Name";
            m_oOptionsPanel.FactionComboBox.SelectedIndexChanged += (s, args) => m_oOptionsPanel.FactionComboBox.DataBindings["SelectedItem"].WriteValue();

            m_oOptionsPanel.ClassComboBox.Bind(c => c.DataSource, VM, d => d.ShipDesigns);
            m_oOptionsPanel.ClassComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentShipClass, DataSourceUpdateMode.OnPropertyChanged);
            m_oOptionsPanel.ClassComboBox.DisplayMember = "Name";
            m_oOptionsPanel.ClassComboBox.SelectedIndexChanged += (s, args) => m_oOptionsPanel.ClassComboBox.DataBindings["SelectedItem"].WriteValue();

            if (VM.CurrentShipClass != null)
            {
                m_oClassPropertiesPanel.ClassPropertyGrid.SelectedObject = VM.CurrentShipClass;
            }


            // Setup Events:
            m_oOptionsPanel.NewButton.Click += new EventHandler(NewButton_Click);
            m_oOptionsPanel.ClassComboBox.SelectedIndexChanged += new EventHandler(ClassComboBox_SelectedIndexChanged);
        }

        void ClassComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (VM.CurrentShipClass != null)
            {
                m_oClassPropertiesPanel.ClassPropertyGrid.SelectedObject = VM.CurrentShipClass;
            }
        }

        void NewButton_Click(object sender, EventArgs e)
        {
            ShipClassTN oNewShipClass = new ShipClassTN("New Class");
            VM.ShipDesigns.Add(oNewShipClass);
            m_oOptionsPanel.ClassComboBox.SelectedItem = oNewShipClass;
        }


        #region PublicMethods

        public void ShowAllPanels(DockPanel a_oDockPanel)
        {
            ShowPropertiesPanel(a_oDockPanel);
            ShowDesignAndInfoPanel(a_oDockPanel);
            ShowOptionsPanel(a_oDockPanel);
        }

        public void ShowPropertiesPanel(DockPanel a_oDockPanel)
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oClassPropertiesPanel.Show(a_oDockPanel, DockState.DockRight);
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false; 
        }

        public void ActivatePropertiesPanel()
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oClassPropertiesPanel.Activate();
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        public void ShowDesignAndInfoPanel(DockPanel a_oDockPanel)
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oDesignAndInformationPanel.Show(a_oDockPanel, DockState.Document);
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false; 
        }

        public void ActivateDesignAndInfoPanel()
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oDesignAndInformationPanel.Activate();
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        public void ShowOptionsPanel(DockPanel a_oDockPanel)
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oOptionsPanel.Show(a_oDockPanel, DockState.DockLeft);
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

        #endregion
    }
}
