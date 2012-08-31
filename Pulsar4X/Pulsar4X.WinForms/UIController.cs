using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using Pulsar4X.WinForms;

namespace Pulsar4X.WinForms.Controls
{
    public class UIController
    {
        /// <summary>
        /// This is a List of all Tab pages used by the UI.
        /// All Tab Controls access this list when trying to access 		  
        /// </summary>
        public static TabPage[] g_aTabPages = new TabPage[UIConstants.NO_OF_UI_TAB_PAGES];

        /// <summary> This is a list of All Pop out SubForms active in the UI. </summary>
        public static List<Forms.SubForm> g_lSubForms = new List<Forms.SubForm>();


        /// <summary> The Main UI Form. </summary>
        public static MainForm g_aMainForm;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Default constructor. </summary>
        ///
        /// <remarks>   Gregory.nott, 17/08/2012. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public UIController()
        {
            g_aMainForm = new MainForm();

            for (int i = 0; i < UIConstants.NO_OF_UI_TAB_PAGES; ++i)
            {
                string temp = "test " + i.ToString();
                g_aTabPages[i] = new TabPage(temp);
                g_aTabPages[i].AutoScroll = true;                                               // turn on autoscoll.

                if (i == UIConstants.UITabs.GAME_START_SCREEN_INDEX)
                {
                    g_aTabPages[i].Name = UIConstants.UITabs.GAME_START_SCREEN_NAME;            // Set Tab Name
                    g_aTabPages[i].Text = UIConstants.UITabs.GAME_START_SCREEN_NAME;            // Set Tab Title
                    Controls.GameStartScreen GameStartScr = new GameStartScreen();              // Creat Game Start Screen.
                    g_aTabPages[i].Controls.Add(GameStartScr);                                  // Add Control to tab.
                }

                if (i == UIConstants.UITabs.SYSTEM_MAP_INDEX)
                {
                    g_aTabPages[i].Name = UIConstants.UITabs.SYSTEM_MAP_NAME;                   // Set Tab Name
                    g_aTabPages[i].Text = UIConstants.UITabs.SYSTEM_MAP_NAME;                   // Set Tab Title
                    Controls.SystemMap SystemMap = new SystemMap();                             // Creat Game Start Screen.
                    SystemMap.Dock = DockStyle.Fill;                                            // Set the system map to fill the whole tab.
                    g_aTabPages[i].Controls.Add(SystemMap);                                     // Add Control to tab.
                }

                if (i == UIConstants.UITabs.SYSTEM_GENERATION_AND_DISPLAY_INDEX)
                {
                    g_aTabPages[i].Name = UIConstants.UITabs.SYSTEM_GENERATION_AND_DISPLAY_NAME; // Set tab Name.
                    g_aTabPages[i].Text = UIConstants.UITabs.SYSTEM_GENERATION_AND_DISPLAY_NAME; // Set Tab Title
                    Controls.SystemGenAndDisplay SystemGenAndDis = new SystemGenAndDisplay();    // Create System Gen and Display Control.
                    g_aTabPages[i].Controls.Add(SystemGenAndDis);                                // Add Control to tab.
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Move tab a tab from one form to another. </summary>
        ///
        /// <remarks>   Gregory.nott, 17/08/2012. </remarks>
        ///
        /// <param name="FromForm"> [in,out] from form. </param>
        /// <param name="ToForm">   [in,out] to form. </param>
        /// <param name="Tab">      [in,out] The tab. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public static void MoveTab(ref Form FromForm, ref Form ToForm, ref TabPage Tab)
        {
            // get the From TabControl:
            DraggableTabControl FromtabControl = GetDraggableTabControl(FromForm);
            DraggableTabControl ToTabControl = GetDraggableTabControl(ToForm);


            ToTabControl.TabPages.Add(Tab);
            FromtabControl.TabPages.Remove(Tab);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Pops the tab out into a new window. </summary>
        ///
        /// <remarks>   Gregory.nott, 17/08/2012. </remarks>
        ///
        /// <param name="FromForm"> [in,out] from form. </param>
        /// <param name="Tab">      [in,out] The tab. </param>
        /// <param name="Location"> [in,out] The Location to position the new form. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public static void PopOutTab(ref Form FromForm, ref TabPage Tab, ref Point Location)
        {
            Forms.SubForm SubForm = new Forms.SubForm();                                // Create the new subform.
            Panel SubPanel = SubForm.GetMainPanel();                                    // get the MainPanel
            DraggableTabControl tempDraggableTabControl = new DraggableTabControl();    // Create new DraggabletabControl
            tempDraggableTabControl.Name = "DraggableTabControl";                       // name the control
            tempDraggableTabControl.Size = SubPanel.Size;                               // Set size to take up the full MainPanel
            tempDraggableTabControl.TabPages.Add(Tab);                                  // Add the poped out tab to thre controll
            DraggableTabControl FromTabControl = GetDraggableTabControl(FromForm);  // get the DraggableTabControl from the source (from) form
            SubPanel.Controls.Add(tempDraggableTabControl);                             // Remove the tab from the source panel.
            // the Tab has been moved.

            g_lSubForms.Add(SubForm);               // Add new SubForm to the SubForms list.
            SubForm.Show();                         // Show the new form.
            SubForm.Location = Location;            // Position the new form, must be set after Show() is called or it mwon't work.
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the Draggable Tab Control child of a Form. </summary>
        ///
        /// <remarks>   Gregory.nott, 17/08/2012. </remarks>
        ///
        /// <param name="Form"> [in,out] The form. </param>
        ///
        /// <returns>   The draggable tab control. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public static DraggableTabControl GetDraggableTabControl(Form Form)
        {
            Control[] SubControls = new Control[1];                 // Array of Controls for storage. 
            SubControls = Form.Controls.Find("MainPanel", true);    // note that the tab Controls are allways containd in the "MainPanel" 
            Panel SubPanel = SubControls[0] as Panel;               // there should only be one "MainPanel" so will will make an assumption that it is at index 0.
            ///< @todo Add error checking for Finding SubControls here.
            Control[] SubPanelControls = SubPanel.Controls.Find("DraggableTabControl", true);   // Finds the DraggableTabControl.
            DraggableTabControl returnTabControl = SubPanelControls[0] as DraggableTabControl;  // Cast To DraggableTabControl from control ready for return.
            ///< @todo Add error checking on DraggableTabControl being returned.

            return returnTabControl;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Cleans up the SubForms list by removing any form that has no List. </summary>
        ///
        /// <remarks>   Gregory.nott, 17/08/2012. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public static void SubFormsCleanup()
        {
            if (g_lSubForms.Count > 0)
            {
                foreach (Forms.SubForm subForm in g_lSubForms)
                {
                    DraggableTabControl tabControl = GetDraggableTabControl(subForm); // get the tab control
                    if (tabControl != null)
                    {
                        if (tabControl.TabPages.Count < 1)
                        {
                            subForm.Hide();                             // Hide the form.
                            UIController.g_lSubForms.Remove(subForm);   // remove it from our list.
                            subForm.Close();                            // Close it.
                            return;                                     // Only remove one form per cleanup call.
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Will Find the Specified tab and make it the active tab/window.
        /// </summary>
        public static void ShowTab(int a_iTabIndex)
        {
            // first check that the tab is in a valid window:
            DraggableTabControl parentControl = g_aTabPages[a_iTabIndex].Parent as DraggableTabControl;
            if (parentControl != null)
            {
                // we have a valid parent control...
                g_aTabPages[a_iTabIndex].BringToFront();                   // Bring Tab Page to fron.
                parentControl.SelectedTab = g_aTabPages[a_iTabIndex];       // Make tab bage the selected one for it's Parent controller.
                Form parentForm = parentControl.Parent.Parent as Form;    // two parents to go up to Panel, then form.
                if (parentForm != null)
                {
                    parentForm.Activate();                                  // Make the Parent form active
                    parentForm.BringToFront();                              // and bring it to the front.
                }
            }
            else
            {
                // it has no parent controller, add it to main form:
                Form MainForm = g_aMainForm as Form;
                DraggableTabControl TabControl = GetDraggableTabControl(MainForm);
                TabControl.TabPages.Add(g_aTabPages[a_iTabIndex]);
                TabControl.SelectedTab = g_aTabPages[a_iTabIndex];
            }
        }
    }
}
