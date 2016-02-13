using Eto.Drawing;
using Eto.Forms;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI
{
    public class MainForm : Form
    {
        private static Command savegame;

        private static Command sysMap;
        private static Command colView;
        private static Command componentDesign;
        private static Command shipDesign;
        private static Command componentTemplateDesign;

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
            var loadgame = new Commands.LoadGame(Game);
            var quit = new Commands.Quit();
            savegame = new Commands.SaveGame(Game);
            sysMap = new Commands.SystemMap(Game);
            colView = new Commands.ColonyView(Game);
            shipDesign = new Commands.ShipDesignViewCMD(Game);
            componentTemplateDesign = new Commands.ComponentTemplateViewCMD(Game);
			

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
                ToolBar = new ToolBar();

                ToolBar.Items.Add(sysMap);
                ToolBar.Items.Add(colView);
                ToolBar.Items.Add(componentDesign);
                ToolBar.Items.Add(shipDesign);
                ToolBar.Items.Add(componentTemplateDesign);
                //at start, toolbar and savegame are disabled
                toggleToolbar(false);
                toggleSaveGame(false);
            }
        }

        public static void toggleToolbar(bool toggle)
        {
            sysMap.Enabled = toggle;
            colView.Enabled = toggle;
            componentDesign.Enabled = toggle;
            shipDesign.Enabled = toggle;
            componentTemplateDesign.Enabled = toggle;
        }

        public static void toggleSaveGame(bool toggle)
        {
            savegame.Enabled = toggle;
        }
    }
}