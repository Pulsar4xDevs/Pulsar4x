using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Json;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI
{
    public class MainForm : Form
    {
        internal GameVM GameVM { get; set; }
        public MainForm()
        {
            ClientSize = new Size(600, 400);
            GameVM = new GameVM();
            Content = new Views.SystemView(GameVM);
            CreateMenuToolBar();
            Title = "Pulsar4X";
        }

        void CreateMenuToolBar()
        {
            var newgame = new Commands.NewGame(NewGameOptionsVM.Create(GameVM));
            var savegame = new Commands.SaveGame(GameVM);
            var loadgame = new Commands.LoadGame(GameVM);
            var quit = new Commands.Quit();

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
                          },
                };
            }
            if (Platform.Supports<ToolBar>())
            {
                // create and set the toolbar
                //ToolBar = new ToolBar();

                //ToolBar.Items.Add(newgame);

            }

        }
    }
}