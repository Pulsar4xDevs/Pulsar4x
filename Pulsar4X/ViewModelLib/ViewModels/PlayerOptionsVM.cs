using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ViewModel;
using Pulsar4X.ECSLib;
using System.Windows.Input;

namespace Pulsar4X.ViewModel
{
    public class PlayerOptionsVM : ViewModelBase
    {
        private GameVM _gameVM;
        public DictionaryVM<Player, string> Players { get; } = new DictionaryVM<Player, string>();

        public string PassWord
        {
            get { return _password; }
            set { _password = value; OnPropertyChanged(); }
        }
        private string _password = "";

        public ICommand SwitchToPlayerCMD { get { return new RelayCommand<object>(obj => ChangeToPlayer(PassWord)); } }


        public PlayerOptionsVM(GameVM gameVM)
        {
            _gameVM = gameVM;
            Players.Add(_gameVM.Game.SpaceMaster, _gameVM.Game.SpaceMaster.Name);
            foreach (var player in gameVM.Game.Players)
            {
                Players.Add(player, player.Name);
            }
            Players.SelectedIndex = 0;
            Players.SelectionChangedEvent += Factions_SelectionChangedEvent;
        }

        private void Factions_SelectionChangedEvent(int oldSelection, int newSelection)
        {
            
        }


        private void ChangeToPlayer(string password)
        {
            _gameVM.CurrentPlayer = Players.SelectedKey;
            _gameVM.CurrentAuthToken = new AuthenticationToken(_gameVM.CurrentPlayer, password);
        }
    }
}
