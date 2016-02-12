using Eto.Forms;
using Eto.Serialization.Json;
using Pulsar4X.ECSLib;
using Pulsar4X.ViewModel;
using System;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class NewGame : Dialog
    {
        protected CheckBox CreatePlayerFaction;
        protected CheckBox DefaultStart;
        protected TextBox FactionName;
        protected PasswordBox FactionPassword;
        protected PasswordBox GmPassword;
        protected NumericUpDown NumberOfSystems;
        protected ListBox AvailableModList { get; set; }
        protected ListBox SelectedModList { get; set; }

        private NewGameOptionsVM new_game_options;
        private GameVM Game;

        public NewGame(GameVM Game)
        {
            JsonReader.Load(this);
            this.Game = Game;
            new_game_options = new NewGameOptionsVM();
            DataContext = new_game_options;
            CreatePlayerFaction.CheckedBinding.BindDataContext((NewGameOptionsVM n) => n.CreatePlayerFaction);
            DefaultStart.CheckedBinding.BindDataContext((NewGameOptionsVM n) => n.DefaultStart);
            FactionName.TextBinding.BindDataContext((NewGameOptionsVM n) => n.FactionName);
            FactionPassword.TextBinding.BindDataContext((NewGameOptionsVM n) => n.FactionPassword);
            GmPassword.TextBinding.BindDataContext((NewGameOptionsVM n) => n.GmPassword);
            var NumberOfSystemsBinding = (new BindableBinding<NumericUpDown, double>(NumberOfSystems, c => c.Value, (c, v) => c.Value = v));
            NumberOfSystemsBinding.BindDataContext((NewGameOptionsVM n) => n.NumberOfSystems);
            //NumberOfSystems.BindDataContext<int>("NumberOfSystems", "NumberOfSystems");

            AvailableModList.ItemTextBinding = Binding.Property((DataVersionInfo n) => n.FullVersion);
            SelectedModList.ItemTextBinding = Binding.Property((DataVersionInfo n) => n.FullVersion);

            AvailableModList.DataStore = new_game_options.AvailableModList;
            SelectedModList.DataStore = new_game_options.SelectedModList;
        }

        protected void AddModButton_Click(object sender, EventArgs e)
        {
            if (AvailableModList.SelectedValue != null)
            {
                new_game_options.SelectedModList.Add((DataVersionInfo)AvailableModList.SelectedValue);
                new_game_options.AvailableModList.RemoveAt(AvailableModList.SelectedIndex);
            }
        }

        protected void RemoveModButton_Click(object sender, EventArgs e)
        {
            if (SelectedModList.SelectedValue != null)
            {
                new_game_options.AvailableModList.Add((DataVersionInfo)SelectedModList.SelectedValue);
                new_game_options.SelectedModList.RemoveAt(SelectedModList.SelectedIndex);
            }
        }

        protected void MoveUpModButton_Click(object sender, EventArgs e)
        {
            if (SelectedModList.SelectedValue != null && SelectedModList.SelectedIndex != 0)
            {
                new_game_options.SelectedModList.Move(SelectedModList.SelectedIndex, SelectedModList.SelectedIndex - 1);
            }
        }

        protected void MoveDownModButton_Click(object sender, EventArgs e)
        {
            if (SelectedModList.SelectedValue != null && SelectedModList.SelectedIndex != new_game_options.SelectedModList.Count-1)
            {
                new_game_options.SelectedModList.Move(SelectedModList.SelectedIndex, SelectedModList.SelectedIndex + 1);
            }
        }

        protected void DefaultButton_Click(object sender, EventArgs e)
        {
            Game.CreateGame(new_game_options);
            MessageBox.Show("New Game Created", "New Game", MessageBoxType.Information);
            MainForm.toggleToolbar(true);
            MainForm.toggleSaveGame(true);
            Close();
        }

        protected void AbortButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
