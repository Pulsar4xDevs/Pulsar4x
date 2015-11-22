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
        public MainForm(GameVM Game)
        {
            ClientSize = new Size(600, 400);
            Content = new Views.SystemView(Game);
            CreateMenuToolBar(Game);
            Title = "Pulsar4X";
        }

        void CreateMenuToolBar(GameVM Game)
        {
            var newgame = new Commands.NewGame(Game);
            var savegame = new Commands.SaveGame(Game);
            var loadgame = new Commands.LoadGame(Game);
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