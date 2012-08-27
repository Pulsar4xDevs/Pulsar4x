using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OpenTK.Graphics;
using Pulsar4X.WinForms;
using Pulsar4X.WinForms.Controls;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace Pulsar4X.WinForms.Controls
{
    public partial class SystemMap : UserControl
    {

        GLCanvas m_GLCanvas;
        
        public SystemMap()
        {
            InitializeComponent();
        }

        private void SystemMap_Load(object sender, EventArgs e)
        {
            m_GLCanvas = new GLCanvas30();
            m_GLCanvas.Size = this.Size;
            this.Dock = DockStyle.Fill;
            m_GLCanvas.Dock = DockStyle.Fill;
            this.Controls.Add(m_GLCanvas);
            //m_GLCanvas.PreRenderPlanet(5.0f);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //m_GLCanvas.TestFunc(int.Parse(textBox1.Text));
            FPSLabel.Text = m_GLCanvas.m_fps.ToString();
        }
    }
}
