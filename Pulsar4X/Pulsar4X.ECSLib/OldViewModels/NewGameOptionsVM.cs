using Pulsar4X.ECSLib;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Pulsar4X.ECSLib
{
    public class NewGameOptionsVM : IViewModel
    {
        private GameVM _gameVM;

        public string GmPassword { get; set; }

        public bool CreatePlayerFaction { get; set; }
        public string FactionName { get; set; }
        public string FactionPassword { get; set; }
        public bool DefaultStart { get; set; }

        public int NumberOfSystems { get; set; }
        public ObservableCollection<DataVersionInfo> AvailableModList { get; set; }
        public ObservableCollection<DataVersionInfo> SelectedModList { get; set; }

        private bool _createServer = true;
        public bool CreateServer
        {
            get
            {
                return _createServer;
            }
            set
            {
                _createServer = value;
                OnPropertyChanged();
            }
        }

        public int PortNumber { get; set; } = 4888;

        public NewGameOptionsVM()
        {
            CreatePlayerFaction = true;
            DefaultStart = true;
            FactionName = "United Earth Federation";
            FactionPassword = "";
            GmPassword = "";
            NumberOfSystems = 50;
            AvailableModList = new ObservableCollection<DataVersionInfo>(StaticDataManager.AvailableData());
            SelectedModList = new ObservableCollection<DataVersionInfo>();
        }


        public static NewGameOptionsVM Create(GameVM gameVM)
        {
            NewGameOptionsVM optionsVM = new NewGameOptionsVM();
            optionsVM._gameVM = gameVM;

            return optionsVM;
        }

        public void CreateGame()
        {
            _gameVM.CreateGame(this);
            if (CreateServer)
            {
                ServerOrderHandler handler = new ServerOrderHandler(_gameVM.Game, PortNumber);
                _gameVM.NetMessages = handler.NetHost.Messages;

            }
        }



        public event PropertyChangedEventHandler PropertyChanged;
        public void Refresh(bool partialRefresh = false)
        {
            //throw new NotImplementedException();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
