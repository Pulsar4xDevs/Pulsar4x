using Eto.Drawing;
using Eto.Forms;
using Pulsar4X.ViewModel;
using System;
using System.Threading.Tasks;

namespace Pulsar4X.CrossPlatformUI.Commands
{
    class LoadGame : Command
    {
        private GameVM _gameVM;
        public LoadGame(GameVM gameVM)
        {
            ID = "Loadgame";
            Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.NewGame.ico");
            MenuText = "Load Game";
            ToolBarText = "Load Game";
            Shortcut = Keys.F5;
            _gameVM = gameVM;
        }

        protected override void OnExecuted(EventArgs e)
        {
            base.OnExecuted(e);

            if (_gameVM.HasGame)
            {
                // Check to see if we want to save current game.
                DialogResult result = MessageBox.Show("Would you like to save the current game?", MessageBoxButtons.YesNo, MessageBoxType.Question, MessageBoxDefaultButton.Yes);
                if (result == DialogResult.Yes)
                {
                    var saveGame = new SaveGame(_gameVM);
                    saveGame.Execute();
                }
            }

            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filters.Add(new FileDialogFilter("Pulsar4x Json save file", ".json"));
            if (fileDialog.ShowDialog(Application.Instance.MainForm) == DialogResult.Ok)
            {
                string pathToFile = fileDialog.FileName;
                try
                {
                    _gameVM.LoadGame(pathToFile);
                }
                catch (Exception exception)
                {
                    ((MainForm)Application.Instance.MainForm).DisplayException("Loading Game", exception);
                }
            }
        }
    }
}
