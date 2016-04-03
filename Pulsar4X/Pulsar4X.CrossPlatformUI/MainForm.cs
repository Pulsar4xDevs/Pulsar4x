using System;
using System.ComponentModel;
using Eto.Drawing;
using Eto.Forms;
using Pulsar4X.CrossPlatformUI.Commands;
using Pulsar4X.CrossPlatformUI.Views;
using Pulsar4X.ViewModel;
using NewGame = Pulsar4X.CrossPlatformUI.Commands.NewGame;

namespace Pulsar4X.CrossPlatformUI
{
    public class MainForm : Form
    {
        private readonly GameVM _gameVM;

        private static Command savegame;

        private static Command sysMap;
        private static Command colView;
        private static Command componentDesign;
        private static Command shipDesign;
        private static Command missileDesign;
        private static Command componentTemplateDesign;

        public MainForm()
        {
            _gameVM = new GameVM();
            ClientSize = new Size(600, 400);
            Content = new MainWindow(_gameVM);
            CreateMenuToolBar();
            Title = "Pulsar4X";
        }

        void CreateMenuToolBar()
        {
            var newgame = new NewGame(_gameVM);
            var loadgame = new LoadGame(_gameVM);
            var quit = new Quit(_gameVM);
            savegame = new SaveGame(_gameVM);
            sysMap = new SystemMap(_gameVM);
            colView = new ColonyView(_gameVM);
            componentDesign = new ComponentDesignViewCMD(_gameVM);
            shipDesign = new ShipDesignViewCMD(_gameVM);
            missileDesign = new MissileDesignCMD(_gameVM);
            componentTemplateDesign = new ComponentTemplateViewCMD(_gameVM);
			

            if (Platform.Supports<MenuBar>())
            {
                Menu = new MenuBar
                {
                    ApplicationItems =
                          {
                              newgame,
                              new SeparatorMenuItem(),
                              savegame,
                              loadgame,
                              new SeparatorMenuItem(),
                              quit
                          },
                    HelpItems =
                          {
                              new Command { MenuText = "Help Command" }
                          }
                };
            }
            if (Platform.Supports<ToolBar>())
            {
                // create and set the toolbar
                ToolBar = new ToolBar();

                ToolBar.Items.Add(sysMap);
                ToolBar.Items.Add(colView);
                ToolBar.Items.Add(componentDesign);
                ToolBar.Items.Add(shipDesign);
                ToolBar.Items.Add(missileDesign);
                ToolBar.Items.Add(componentTemplateDesign);
            }
        }

        public void DisplayException(string activity, Exception exception)
        {
            MessageBox.Show("Exception thrown while " + activity + ":\n\n" +
                    exception.GetType() + "\n" +
                    exception.Message +
                    "\n\nThrown in function:\n" +
                    exception.TargetSite +
                    "\n\nStack Trace:\n" +
                    exception.StackTrace,
                    "Exception", MessageBoxButtons.OK, MessageBoxType.Error);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (_gameVM.HasGame)
            {
                var result = MessageBox.Show("Would you like to save the game before exiting?", MessageBoxButtons.YesNoCancel, MessageBoxType.Question, MessageBoxDefaultButton.Yes);

                switch (result)
                {
                    case DialogResult.Yes:
                        var saveGame = new SaveGame(_gameVM);
                        saveGame.Execute();
                        break;

                    case DialogResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }
        }
    }
}