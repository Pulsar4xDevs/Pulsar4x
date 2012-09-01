using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using Pulsar4X;

namespace Pulsar4X.WinForms.Forms
{
    public partial class StartupSplashScreen : Form
    {
        /// <summary>
        /// Splash Scrren Instance
        /// </summary>
        static StartupSplashScreen m_oSplashScreen = null;

        /// <summary>
        /// Splash Screen Thread
        /// </summary>
        static Thread m_oThread = null;

        // Used to control the fading in and out of our splash screen:
        private double m_dOpacityIncrement = 0.05;
        private double m_dOpacityDecrement = 0.1;
        private const int TIMER_INTERVAL = 50;

        /// <summary>
        /// Holds the Current Status text.
        /// </summary>
        private string m_szStatus = "Loading...";

        // Used to update progress bar:
        private double m_dCompletionFraction = 0;
        private Rectangle m_rProgress;
        private double m_dLastCompletionFraction = 0;
        private double m_dPBIncrementPerTimerInterval = 0.0015;

        /// <summary>
        /// Tracks the number of Ticks through the timer!
        /// </summary>
        private int m_iActualTicks = 0;

        

        /// <summary>
        /// Constructor
        /// </summary>
        private StartupSplashScreen()
        {
            InitializeComponent();

            this.Opacity = 0.0;
            m_oTimer.Interval = TIMER_INTERVAL;
            m_oTimer.Start();
        }


        /// <summary>
        /// Shows the Startup Splash Screen on it's own thread.
        /// </summary>
        static public void ShowSplashScreen()
        {
            // Make sure there is only one splahs screen.
            if (m_oSplashScreen != null)
            {
                return;
            }

            m_oThread = new Thread(new ThreadStart(StartupSplashScreen.ShowForm));
            m_oThread.IsBackground = true;
            m_oThread.ApartmentState = ApartmentState.STA;
            m_oThread.Start();
        }

        static private void ShowForm()
        {
            m_oSplashScreen = new StartupSplashScreen();
            Application.Run(m_oSplashScreen);
            //m_oSplashScreen.Show();
        }

        /// <summary>
        /// Used to Close the Splash Screen and it thread.
        /// </summary>
        static public void CloseForm()
        {
            if (m_oSplashScreen != null)
            {
                // make this start to go away:
                m_oSplashScreen.m_dOpacityIncrement = -m_oSplashScreen.m_dOpacityDecrement;
            }

            // Do not need these anymore.
            m_oThread = null;
            m_oSplashScreen = null;

            //m_oSplashScreen.Close();
        }

        /// <summary>
        /// Set the status Text.
        /// </summary>
        /// <param name="a_szNewStatus">The New Status Text</param>
        static public void SetStatus(string a_szNewStatus)
        {
            if (m_oSplashScreen == null)
            {
                return;
            }

            m_oSplashScreen.m_szStatus = a_szNewStatus;
        }

        /// <summary>
        /// Gets or Sets Progress bar Percentage.
        /// </summary>
        static public double Progress
        {
            get
            {
                if (m_oSplashScreen != null)
                {
                    return m_oSplashScreen.m_dCompletionFraction;
                }

                return 100.0;
            }
            set
            {
                if (m_oSplashScreen != null)
                {
                    m_oSplashScreen.m_dLastCompletionFraction = m_oSplashScreen.m_dCompletionFraction;
                    m_oSplashScreen.m_dCompletionFraction = value;
                }
            }
        }


        
        private void Timer_Tick(object sender, System.EventArgs e)
        {
            // Fade in or out:
            if (m_dOpacityIncrement > 0)
            {
                m_iActualTicks++;           // Increm,ent the number of ticks.
                if (this.Opacity < 1)
                {
                    this.Opacity += m_dOpacityIncrement;
                }
            }
            else
            {
                if (this.Opacity > 0)
                {
                    this.Opacity += m_dOpacityIncrement;
                }
                else
                {
                    this.Close();
                }
            }

            // Update status:
            m_oStatusLabel.Text = m_szStatus;

            // Update progress bar:
            if (m_dLastCompletionFraction < m_dCompletionFraction)
            {
                m_dLastCompletionFraction += m_dPBIncrementPerTimerInterval;
                int iWidth = (int)Math.Floor(m_oStatusPanel.ClientRectangle.Width * m_dCompletionFraction);
                int iHeight = m_oStatusPanel.ClientRectangle.Height;
                int iX = m_oStatusPanel.ClientRectangle.X;
                int iY = m_oStatusPanel.ClientRectangle.Y;
                if (iWidth > 0 && iHeight > 0)
                {
                    m_rProgress = new Rectangle(iX, iY, iWidth, iHeight);
                    m_oStatusPanel.Invalidate(m_rProgress);
                }
            }

        }

        private void m_oStatusPanel_Paint(object sender, PaintEventArgs e)
        {
            if (e.ClipRectangle.Width > 0 && m_iActualTicks > 1)
            {
                System.Drawing.Drawing2D.LinearGradientBrush brBackground = new System.Drawing.Drawing2D.LinearGradientBrush(m_rProgress,
                                                                                                                            Color.DarkSlateBlue,
                                                                                                                            Color.DodgerBlue,
                                                                                                                            System.Drawing.Drawing2D.LinearGradientMode.Horizontal);
                e.Graphics.FillRectangle(brBackground, m_rProgress);
            }
        }

    }
}
