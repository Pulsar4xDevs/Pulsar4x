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

        private NewGameOptionsVM new_game_options;

        public NewGame(NewGameOptionsVM context)
        {
            JsonReader.Load(this);
            DataContext = context;
            new_game_options = context;
            CreatePlayerFaction.CheckedBinding.BindDataContext((NewGameOptionsVM n) => n.CreatePlayerFaction);
            DefaultStart.CheckedBinding.BindDataContext((NewGameOptionsVM n) => n.DefaultStart);
            FactionName.TextBinding.BindDataContext((NewGameOptionsVM n) => n.FactionName);
            FactionPassword.TextBinding.BindDataContext((NewGameOptionsVM n) => n.FactionPassword);
            GmPassword.TextBinding.BindDataContext((NewGameOptionsVM n) => n.GmPassword);
            var NumberOfSystemsBinding = (new BindableBinding<NumericUpDown, double>(NumberOfSystems, c => c.Value, (c, v) => c.Value = v));
            NumberOfSystemsBinding.BindDataContext((NewGameOptionsVM n) => n.NumberOfSystems);
            //NumberOfSystems.BindDataContext<int>("NumberOfSystems", "NumberOfSystems");
        }

        protected void DefaultButton_Click(object sender, EventArgs e)
        {
            new_game_options.CreateGame();
            Close();
        }

        protected void AbortButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
