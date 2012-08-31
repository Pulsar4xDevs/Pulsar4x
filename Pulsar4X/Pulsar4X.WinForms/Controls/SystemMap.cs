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

namespace Pulsar4X.WinForms.Controls
{
    public partial class SystemMap : UserControl
    {

        public static readonly ILog logger = LogManager.GetLogger(typeof(SystemMap));


        /// <summary> The gl canvas </summary>
        GLCanvas m_GLCanvas;
        
        public SystemMap()
        {
            InitializeComponent();
        }

        private void SystemMap_Load(object sender, EventArgs e)
        {
            try
            {
                m_GLCanvas = OpenTKUtilities.Instance.CreateGLCanvas(); //new GLCanvas30();
                m_GLCanvas.Size = this.Size;
                this.Dock = DockStyle.Fill;
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
    }
}
