using System;
using System.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ViewModel
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


        public NewGameOptionsVM()
        {
            CreatePlayerFaction = true;
            DefaultStart = true;
            FactionName = "United Earth Federation";
            FactionPassword = "FPnotImplemented";
            GmPassword = "GMPWnotImplemented";
            NumberOfSystems = 50;
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
        }



        public event PropertyChangedEventHandler PropertyChanged;
        public void Refresh(bool partialRefresh = false)
        {
            //throw new NotImplementedException();
        }
    }
}
