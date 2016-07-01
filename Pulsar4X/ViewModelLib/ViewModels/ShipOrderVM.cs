using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Pulsar4X.ViewModel
{
    public class ShipOrderVM : IViewModel
    {
        public ShipOrderVM(Entity factionEntity)
        {
        }

        public static ShipOrderVM Create(GameVM game)
        {
            return new ShipOrderVM(game.CurrentFaction);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Refresh(bool partialRefresh = false)
        {
            throw new NotImplementedException();
        }
    }
}
