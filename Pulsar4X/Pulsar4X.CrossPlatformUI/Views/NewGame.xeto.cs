using System;
using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ECSLib;


namespace Pulsar4X.CrossPlatformUI.Views {
	public class NewGame : Dialog {
		#region Controls
		protected CheckBox CreatePlayerFaction;
		protected CheckBox DefaultStart;
		protected TextBox FactionName;
		protected PasswordBox FactionPassword;
		protected PasswordBox GmPassword;
		protected NumericUpDown NumberOfSystems;
		protected ListBox AvailableModsList { get; set; }
		protected ListBox SelectedModsList { get; set; }
		#endregion

		#region ViewModels
		private NewGameOptionsVM new_game_options;
		private GameVM _gameVM;
		#endregion

		public NewGame(GameVM gameVM) {
			XamlReader.Load(this);
			_gameVM = gameVM;
			new_game_options = new NewGameOptionsVM();
			DataContext = new_game_options;
			BindControls();
		}

		private void BindControls() {
			CreatePlayerFaction.CheckedBinding.BindDataContext((NewGameOptionsVM n) => n.CreatePlayerFaction);
			DefaultStart.CheckedBinding.BindDataContext((NewGameOptionsVM n) => n.DefaultStart);
			FactionName.TextBinding.BindDataContext((NewGameOptionsVM n) => n.FactionName);
			FactionPassword.TextBinding.BindDataContext((NewGameOptionsVM n) => n.FactionPassword);
			GmPassword.TextBinding.BindDataContext((NewGameOptionsVM n) => n.GmPassword);
			var NumberOfSystemsBinding = (new BindableBinding<NumericUpDown, double>(NumberOfSystems, c => c.Value, (c, v) => c.Value = v));
			NumberOfSystemsBinding.BindDataContext((NewGameOptionsVM n) => n.NumberOfSystems);
			//NumberOfSystems.BindDataContext<int>("NumberOfSystems", "NumberOfSystems");

			AvailableModsList.ItemTextBinding = Binding.Property((DataVersionInfo n) => n.FullVersion);
			SelectedModsList.ItemTextBinding = Binding.Property((DataVersionInfo n) => n.FullVersion);

			AvailableModsList.DataStore = new_game_options.AvailableModList;
			SelectedModsList.DataStore = new_game_options.SelectedModList;
		}

		protected void AddModButton_Click(object sender, EventArgs e) {
			if (AvailableModsList.SelectedValue != null) {
				new_game_options.SelectedModList.Add((DataVersionInfo)AvailableModsList.SelectedValue);
				new_game_options.AvailableModList.RemoveAt(AvailableModsList.SelectedIndex);
			}
		}
		protected void RemoveModButton_Click(object sender, EventArgs e) {
			if (SelectedModsList.SelectedValue != null) {
				new_game_options.AvailableModList.Add((DataVersionInfo)SelectedModsList.SelectedValue);
				new_game_options.SelectedModList.RemoveAt(SelectedModsList.SelectedIndex);
			}
		}

		protected void MoveModUpButton_Click(object sender, EventArgs e) {
			if (SelectedModsList.SelectedValue != null && SelectedModsList.SelectedIndex != 0) {
				new_game_options.SelectedModList.Move(SelectedModsList.SelectedIndex, SelectedModsList.SelectedIndex - 1);
			}
		}
		protected void MoveModDownButton_Click(object sender, EventArgs e) {
			if (SelectedModsList.SelectedValue != null && SelectedModsList.SelectedIndex != new_game_options.SelectedModList.Count - 1) {
				new_game_options.SelectedModList.Move(SelectedModsList.SelectedIndex, SelectedModsList.SelectedIndex + 1);
			}
		}

		protected void OkButton_Click(object sender, EventArgs e) {
			_gameVM.CreateGame(new_game_options);
			Close();
		}
		protected void CancelButton_Click(object sender, EventArgs e) {
			Close();
		}
	}
}
