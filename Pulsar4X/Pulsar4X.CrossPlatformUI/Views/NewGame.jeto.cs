using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Json;
using Pulsar4X.ViewModel;

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

            foreach (var item in new_game_options.AvailableModList)
            {
                AvailableModList.Items.Add(item.Name);
            }
        }

        protected void AddModButton_Click(object sender, EventArgs e)
        {
            if (AvailableModList.SelectedKey != null)
            {
                var item = AvailableModList.SelectedKey;
                AvailableModList.Items.RemoveAt(AvailableModList.SelectedIndex);
                SelectedModList.Items.Add(item);
            }
        }

        protected void RemoveModButton_Click(object sender, EventArgs e)
        {
            if (SelectedModList.SelectedKey != null)
            {
                var item = SelectedModList.SelectedKey;
                SelectedModList.Items.RemoveAt(SelectedModList.SelectedIndex);
                AvailableModList.Items.Add(item);
            }
        }

        protected void MoveUpModButton_Click(object sender, EventArgs e)
        {
            if (SelectedModList.SelectedKey != null && SelectedModList.SelectedIndex != 0)
            {
                SelectedModList.Items.Move(SelectedModList.SelectedIndex, SelectedModList.SelectedIndex - 1);
            }
        }

        protected void MoveDownModButton_Click(object sender, EventArgs e)
        {
            if (SelectedModList.SelectedKey != null && SelectedModList.SelectedIndex != SelectedModList.Items.Count-1)
            {
                SelectedModList.Items.Move(SelectedModList.SelectedIndex, SelectedModList.SelectedIndex + 1);
            }
        }

        protected void DefaultButton_Click(object sender, EventArgs e)
        {
            foreach (var item in new_game_options.SelectedModList)
            {
                SelectedModList.Items.Add(item.Name);
            }

            Game.CreateGame(new_game_options);
            Close();
        }

        protected void AbortButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
