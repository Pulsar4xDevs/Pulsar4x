using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Pulsar4X.Forms;

namespace Pulsar4X
{
    /// <summary> This the Primary Form for the program, all other UI elements hang of this in some way.</summary>
    public partial class MainForm : Form
    {
        /// <summary> The main tab control </summary>
        Pulsar4X.WinForms.Controls.DraggableTabControl m_MainTabControl = new Pulsar4X.WinForms.Controls.DraggableTabControl();


        /// <summary>   Default constructor. </summary>
        /// <remarks>   Gregory.nott, 9/7/2012. </remarks>
        public MainForm()
        {
            InitializeComponent();
            m_MainTabControl.Name = "DraggableTabControl";
            m_MainTabControl.Dock = DockStyle.Fill;
        }


        /// <summary>   Gets the main panel. used For the Draggable Tabs </summary>
        /// <returns>   The main panel. </returns>
        public Panel GetMainPanel()
        {
            return MainPanel;
        }


        /// <summary>   Event handler. Called by MainForm for load events. </summary>
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Event information. </param>
        private void MainForm_Load(object sender, EventArgs e)
        {
            // Load in the dragable tabs here, setup ref. list to all tabs?
            //m_MainTabControl.Size = MainPanel.Size;
            m_MainTabControl.MinimumSize = new System.Drawing.Size(1008, 706);
#if OPENGL
            m_MainTabControl.TabPages.Add(Pulsar4X.WinForms.Controls.UIController.g_aTabPages[3]);
#endif
            //m_MainTabControl.TabPages.Add(Pulsar4X.WinForms.Controls.UIController.g_aTabPages[0]);
            m_MainTabControl.TabPages.Add(Pulsar4X.WinForms.Controls.UIController.g_aTabPages[9]);

            MainPanel.Controls.Add(m_MainTabControl);
            
            // close splash screen:
#if SPLASHSCREEN
            Pulsar4X.WinForms.Forms.StartupSplashScreen.SetStatus("Starting...");
            Pulsar4X.WinForms.Forms.StartupSplashScreen.Progress = 0.9;
            Pulsar4X.WinForms.Forms.StartupSplashScreen.CloseForm();
#endif
        }


        /// <summary>   Event handler. Called by exitToolStripMenuItem for click events. </summary>
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Event information. </param>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        
        /// <summary>   Event handler. Called by systemMapToolStripMenuItem for click events. </summary>
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Event information. </param>
        private void systemMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Show System Map.
#if OPENGL
            WinForms.Controls.UIController.ShowTab(WinForms.UIConstants.UITabs.SYSTEM_MAP_INDEX);
#endif
        }


        /// <summary>   Event handler. Called by aboutToolStripMenuItem for click events. </summary>
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Event information. </param>
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Pulsar4X.WinForms.Forms.AboutBox AboutBox = new WinForms.Forms.AboutBox();
            AboutBox.Show();
        }


        /// <summary>
        /// Event handler. Called by systemInformationToolStripMenuItem for click events.
        /// </summary>
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Event information. </param>
        private void systemInformationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Show System information tab.
            WinForms.Controls.UIController.ShowTab(WinForms.UIConstants.UITabs.SYSTEM_GENERATION_AND_DISPLAY_INDEX);
        }

        private void commanderNameThemesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new CommanderNameThemesDialog();
            dialog.ShowDialog();
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Pulsar4X.WinForms.Forms.Options oOpt = new WinForms.Forms.Options();
            oOpt.ShowDialog();
        }


    }
}
