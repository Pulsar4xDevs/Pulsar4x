using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ECSLib
{
    public class StarSystemSelectionVM : ViewModelBase
    {
        public bool Enable { get { return StarSystems.Count > 0; } }
        public DictionaryVM<StarSystem, string> StarSystems { get; } = new DictionaryVM<StarSystem, string>();
        public SystemMap_DrawableVM SelectedSystemVM { get; } = new SystemMap_DrawableVM();
        private GameVM _gameVM;
        //private int viewport_width;
        //private int viewport_height;

        public StarSystemSelectionVM(GameVM gameVM, Game game, Entity factionEntity)
        {
            _gameVM = gameVM;
            foreach (var item in game.GetSystems(gameVM.CurrentAuthToken))
            {
                StarSystems.Add(item, item.NameDB.GetName(factionEntity));
                
            }
            StarSystems.SelectedIndex = 0;
            StarSystems.SelectionChangedEvent += StarSystems_SelectionChangedEvent;
            StarSystems.DisplayList.CollectionChanged += DisplayList_CollectionChanged;
            //_gameVM.StarSystems.CollectionChanged += StarSystems_CollectionChanged;
        }

        public void Initialise()
        {
            if (Enable) 
                StarSystems.SelectedIndex = 0;
        }

        void DisplayList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (SelectedSystemVM.SystemSubpulse == null)
            { 
                StarSystems.SelectedIndex = 0; 
            }
        }

        /*
        private void StarSystems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (StarSystem starSystem in e.NewItems)
                    {
                        StarSystems.Add(starSystem, starSystem.NameDB.GetName(_gameVM.CurrentFaction));
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (StarSystem starSystem in e.OldItems)
                    {
                        StarSystems.Remove(starSystem);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    StarSystems.Clear();
                    break;
                default:
                    throw new NotSupportedException("Unsupported change of underlying viewmodel.");
            }
        }
*/
        private void StarSystems_SelectionChangedEvent(int oldSelection, int newSelection)
        {

            var selectedSystem = StarSystems.SelectedKey;
            if (selectedSystem != null)
            {
                SelectedSystemVM.Initialise(_gameVM, StarSystems.SelectedKey, _gameVM.CurrentFaction);
            }
        }
    }
}
