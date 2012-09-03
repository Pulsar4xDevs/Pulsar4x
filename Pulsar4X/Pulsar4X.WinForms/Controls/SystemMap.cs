using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Pulsar4X.WinForms;
using Pulsar4X.WinForms.Controls;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using log4net.Config;
using log4net;
using Pulsar4X.ViewModels;

namespace Pulsar4X.WinForms.Controls
{
    public partial class SystemMap : UserControl
    {

        public static readonly ILog logger = LogManager.GetLogger(typeof(SystemMap));

        private Pulsar4X.Entities.StarSystem m_oCurrnetSystem;

        public GLStarSystemViewModel VM { get; set; }

        /// <summary> The gl canvas </summary>
        GLCanvas m_GLCanvas;
        
        public SystemMap()
        {
            InitializeComponent();
            this.MouseWheel += new MouseEventHandler(SystemMap_MouseWheel);

            VM = new GLStarSystemViewModel();
            this.Bind(c => c.CurrentStarSystem, VM, d => d.CurrentStarSystem);
        }

        private void SystemMap_Load(object sender, EventArgs e)
        {
            try
            {
                m_GLCanvas = OpenTKUtilities.Instance.CreateGLCanvas(); //new GLCanvas30();
                m_GLCanvas.Size = this.Size;
                //this.Dock = DockStyle.Fill;
                m_GLCanvas.Dock = DockStyle.Fill;
                this.Controls.Add(m_GLCanvas);
            }
            catch (System.NotSupportedException ex)
            {
                logger.Fatal("Error Occured when trying to Load a GLCanvas.", ex);
            }
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //m_GLCanvas.TestFunc(int.Parse(textBox1.Text));
            FPSLabel.Text = m_GLCanvas.FPS.ToString();
        }

        private void SystemMap_SizeChanged(object sender, EventArgs e)
        {
            this.Size = Parent.Size;
        }

        private void SystemMap_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            // See here for MSDN Ref: http://msdn.microsoft.com/en-us/library/system.windows.forms.control.mousewheel(v=vs.71).aspx
            if (e.Delta <= -120)
            {
                // then we have scrolled down, so zoom out!!
                m_GLCanvas.DecreaseZoomScaler();
                
            }
            else if (e.Delta >= 120)
            {
                // the we have scrolled up, so zoom in.
                m_GLCanvas.IncreaseZoomScaler();
            }
        }


        /// <summary>
        /// Gets or sets the currently viewed star system.
        /// </summary>
        Entities.StarSystem CurrentStarSystem
        {
            get
            {
                return m_oCurrnetSystem;
            }
            set
            {
                if (value != m_oCurrnetSystem)
                {
                    m_oCurrnetSystem = value;
                    RefreshStarSystem();
                }
            }
        }

        /// <summary>
        /// Refreshes and redraws the StarSystem
        /// </summary>
        public void RefreshStarSystem()
        {
        }
        
    }
}
