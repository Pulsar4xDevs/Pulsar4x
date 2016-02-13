using Eto.Drawing;
using Eto.Forms;
using Pulsar4X.ViewModel;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

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
            _gameVM.PropertyChanged += _gameVM_PropertyChanged;
        }

        private void _gameVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "HasGame")
            {
                Enabled = _gameVM.HasGame;
            }
        }

        protected override async void OnExecuted(EventArgs e)
        {
            base.OnExecuted(e);
            
            await Save();
        }

        public async Task Save()
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.Filters.Add(new FileDialogFilter("Pulsar4x Json Save File", ".json"));

            if (fileDialog.ShowDialog(Application.Instance.MainForm) == DialogResult.Ok)
            {
                string pathToFile = fileDialog.FileName;
                try
                {
                    await _gameVM.SaveGame(pathToFile);
                }
                catch (Exception exception)
                {
                    ((MainForm)Application.Instance.MainForm).DisplayException("Saving Game", exception);
                }
            }
        }
    }
}
