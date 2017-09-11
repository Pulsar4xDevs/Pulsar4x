using Eto.Drawing;
using Eto.Forms;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;
using GameVM = Pulsar4X.ECSLib.GameVM;

namespace Pulsar4X.CrossPlatformUI.Commands
{
    sealed class SaveGame : Command
    {
        private readonly GameVM _gameVM;

        public SaveGame(GameVM gameVM)
        {
            _gameVM = gameVM;
            ID = "savegame";
            Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.NewGame.ico");
            MenuText = "Save Game";
            ToolBarText = "Save Game";
            Shortcut = Keys.F6;
            Enabled = _gameVM.HasGame;
            _gameVM.PropertyChanged += GameVMPropertyChanged;
        }

        private void GameVMPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "HasGame")
            {
                Enabled = _gameVM.HasGame;
            }
        }

        protected override void OnExecuted(EventArgs e)
        {
            base.OnExecuted(e);

            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.Filters.Add(new FileDialogFilter("Pulsar4x Json Save File", ".json"));

            if (fileDialog.ShowDialog(Application.Instance.MainForm) == DialogResult.Ok)
            {
                string pathToFile = fileDialog.FileName;
                try
                {
                    _gameVM.SaveGame(pathToFile);
                }
                catch (Exception exception)
                {
                    ((MainForm)Application.Instance.MainForm).DisplayException("Saving Game", exception);
                }
            }
        }
    }
}
