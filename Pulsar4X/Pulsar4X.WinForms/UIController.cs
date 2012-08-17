using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

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
            DraggableTabControl FromtabControl = GetDraggableTabControl(ref FromForm);
            DraggableTabControl ToTabControl = GetDraggableTabControl(ref ToForm);


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
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public static void PopOutTab(ref Form FromForm, ref TabPage Tab)
        {
            Forms.SubForm SubForm = new Forms.SubForm();                                // Create the new subform.
            Panel SubPanel = SubForm.GetMainPanel();                                    // get the MainPanel
            DraggableTabControl tempDraggableTabControl = new DraggableTabControl();    // Create new DraggabletabControl
            tempDraggableTabControl.Name = "DraggableTabControl";                       // name the control
            tempDraggableTabControl.Size = SubPanel.Size;                               // Set size to take up the full MainPanel
            tempDraggableTabControl.TabPages.Add(Tab);                                  // Add the poped out tab to thre controll
            DraggableTabControl FromTabControl = GetDraggableTabControl(ref FromForm);  // get the DraggableTabControl from the source (from) form
            SubPanel.Controls.Add(tempDraggableTabControl);                             // Remove the tab from the source panel.
            // the Tab has been moved.

            g_lSubForms.Add(SubForm);               // Add new SubForm to the SubForms list.
            SubForm.Show();                         // Show the new form.
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
        public static DraggableTabControl GetDraggableTabControl(ref Form Form)
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
            // Cleans up the SubForms List b
        }
    }
}
