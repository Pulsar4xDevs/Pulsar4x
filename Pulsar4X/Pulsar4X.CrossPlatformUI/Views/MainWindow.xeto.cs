using Eto.Drawing;
using Eto.Forms;
using Eto.Serialization.Xaml;

using Pulsar4X.ViewModel;
using System;

namespace Pulsar4X.CrossPlatformUI.Views {
    public class MainWindow : Panel {
        #region Form Controls
        protected StackLayout TopButtonBar;

        protected StackLayout adv_buttons;
        #region Advance Time Buttons
        protected Button btn_time_5sec;
        protected Button btn_time_30sec;
        protected Button btn_time_2min;
        protected Button btn_time_5min;
        protected Button btn_time_20min;
        protected Button btn_time_1hr;
        protected Button btn_time_3hrs;
        protected Button btn_time_8hrs;
        protected Button btn_time_1day;
        protected Button btn_time_5day;
        protected Button btn_time_30day;
        #endregion

        protected DropDown dd_subpulse;
        protected TabControl view_tabs;
        #endregion

        private GameVM _game;
        public MainWindow(GameVM game) {
            _game = game;
            DataContext = _game;
            XamlReader.Load(this);
        }
        
        public void AddOrSelectTabPanel(string title, Container c, bool activate = true, bool force_new = false) {
            if (!force_new) {
                foreach (TabPage p in view_tabs.Pages) {
                    if (p.Text == title) {
                        if (activate) { view_tabs.SelectedPage = p; }
                        return;
                    }
                }
            }
            TabPage tp = new TabPage();
            tp.Content = c;
            tp.Text = title;
            view_tabs.Pages.Add(tp);
            if (activate) { view_tabs.SelectedPage = tp; }
        }
    }
}
