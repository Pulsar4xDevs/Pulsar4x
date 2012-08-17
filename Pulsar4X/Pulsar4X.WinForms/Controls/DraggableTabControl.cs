using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;


namespace Pulsar4X.WinForms.Controls
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   Draggable tab control. </summary>
    ///
    /// <remarks>   Gregory.nott, 17/08/2012. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public class DraggableTabControl : TabControl
    {

        /// <summary> The dragged tab </summary>
        private TabPage m_DraggedTab;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Default constructor. </summary>
        ///
        /// <remarks>   Gregory.nott, 17/08/2012. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public DraggableTabControl()
        {
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the mouse down action. i.e. Sets the dragged tab </summary>
        ///
        /// <remarks>   Gregory.nott, 17/08/2012. </remarks>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Event information to send to registered event handlers. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            m_DraggedTab = TabAt(e.Location);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the mouse move action. i.e. Adjusts the position of the Tab in the tab Control </summary>
        ///
        /// <remarks>   Gregory.nott, 17/08/2012. </remarks>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Event information to send to registered event handlers. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || m_DraggedTab == null)
            {
                return;
            }

            TabPage tab = TabAt(e.Location);

            if (tab == null || tab == m_DraggedTab)
            {
                return; // we are on ourself, return!
            }

            Swap(m_DraggedTab, tab);
            SelectedTab = m_DraggedTab;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the mouse up action. Pops out the Tab into a new window or moves it between windows. </summary>
        ///
        /// <remarks>   Gregory.nott, 17/08/2012. </remarks>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Event information to send to registered event handlers. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || m_DraggedTab == null)
            {
                return;
            }

            TabPage tab = TabAt(e.Location);

            if (tab == null)
            {
                // Test to see if we were droped outside any window.
                Form parentForm = this.Parent.Parent as Form;
                Form DropedForm = FormAt(e.Location);
                if (DropedForm == null)
                {
                    // if yes creat new Subform, add to subform list, creat new tab control, add to tab control list, add tab to new tab control, reomve from this one.
                    UIController.PopOutTab(ref parentForm, ref m_DraggedTab);
                }
                else
                {
                    // else check if on a different tab control (i.e. not this) using tab control list.
                    // if yes add to the other tab control and remove from this.
                    UIController.MoveTab(ref parentForm, ref DropedForm, ref m_DraggedTab);
                }

                // close old window, if a sub form!:
                Forms.SubForm Temp = parentForm as Forms.SubForm;
                if (Temp != null)
                {
                    if (this.TabPages.Count < 1)
                    {
                        Temp.Hide();
                        UIController.g_lSubForms.Remove(Temp);
                        Temp.Close();
                    }
                }

                return;
            }

            if (tab == m_DraggedTab)
            {
                return; // we are on ourself, return!
            }

            Swap(m_DraggedTab, tab);
            SelectedTab = m_DraggedTab;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Works out which tab we are at </summary>
        ///
        /// <remarks>   Gregory.nott, 17/08/2012. </remarks>
        ///
        /// <param name="position"> The position. </param>
        ///
        /// <returns>   The Tab we are at </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private TabPage TabAt(Point position)
        {
            int count = TabCount;

            for (int i = 0; i < count; i++)
            {
                if (GetTabRect(i).Contains(position))
                {
                    return TabPages[i];
                }
            }

            return null;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Swaps Tabs. </summary>
        ///
        /// <remarks>   Gregory.nott, 17/08/2012. </remarks>
        ///
        /// <param name="a">   First TabPage </param>
        /// <param name="b">   Second TabPage </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private void Swap(TabPage a, TabPage b)
        {
            int i = TabPages.IndexOf(a);
            int j = TabPages.IndexOf(b);
            TabPages[i] = b;
            TabPages[j] = a;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Works out which For we are at. </summary>
        ///
        /// <remarks>   Gregory.nott, 17/08/2012. </remarks>
        ///
        /// <param name="position"> The position. </param>
        ///
        /// <returns>   The Form we are at. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private Form FormAt(Point position)
        {
            // first check the MainForm
            if (UIController.g_aMainForm.Bounds.Contains(position))
            {
                return UIController.g_aMainForm;
            }

            // now loop though each form in the subform list:
            foreach (Forms.SubForm SubForm in UIController.g_lSubForms)
            {
                if (SubForm.Bounds.Contains(position))
                {
                    return SubForm;
                }
            }

            // if we can't find a form then return null.
            return null;
        }
    }
}
