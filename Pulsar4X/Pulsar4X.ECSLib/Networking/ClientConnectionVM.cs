using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Pulsar4X.Networking;

namespace Pulsar4X.ECSLib
{
    public class ClientConnectionVM : ViewModelBase
    {
        public string FactionName { get; set; }
        public string FactionPassword { get; set; }

        public string ServerAddress { get; set; } = "127.0.0.1";
        public int PortNum { get; set; } = 4888;
        public string ConnectionStatus
        {
            get
            {
                return _netClient.NetPeerObject.Status.ToString();
            }
        }

        public ObservableCollection<string> NetMessages { get; set; }

        NetworkClient _netClient;

        public ClientConnectionVM(GameVM gameVM)
        {
            _netClient = new NetworkClient(ServerAddress, PortNum);
            NetMessages = _netClient.Messages;
            gameVM.NetMessages = _netClient.Messages;
        }

        private ICommand _connectCMD;
        public ICommand ConnectCMD
        {
            get
            {
                return _connectCMD ?? (_connectCMD = new CommandHandler(OnConnect, true));
            }
        }

        private void OnConnect()
        {
            _netClient.HostAddress = ServerAddress;
            _netClient.PortNum = PortNum;
            _netClient.ClientConnect();
        }
    }
}
