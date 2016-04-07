using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ViewModel.SystemView
{
    public class StarSystemVM : ViewModelBase
    {
        public DictionaryVM<StarSystem, string> StarSystems { get; } = new DictionaryVM<StarSystem, string>();
        public SystemMap_DrawableVM SelectedSystemVM { get; } = new SystemMap_DrawableVM();
        private GameVM _gameVM;
        private int viewport_width;
        private int viewport_height;

        public StarSystemVM(GameVM gameVM, Game game, Entity factionEntity)
        {
            _gameVM = gameVM;
            foreach (var item in game.GetSystems(gameVM.CurrentAuthToken))
            {
                StarSystems.Add(item, item.NameDB.GetName(factionEntity));
                
            }
            
            StarSystems.SelectionChangedEvent += StarSystems_SelectionChangedEvent;
            StarSystems.SelectedIndex = 0;
            _gameVM.StarSystems.CollectionChanged += StarSystems_CollectionChanged;
        }

        private void StarSystems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (SystemVM systemVM in e.NewItems)
                    {
                        StarSystems.Add(systemVM.StarSystem, systemVM.StarSystem.NameDB.GetName(_gameVM.CurrentFaction));
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (SystemVM systemVM in e.OldItems)
                    {
                        StarSystems.Remove(systemVM.StarSystem);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    StarSystems.Clear();
                    break;
                default:
                    throw new NotSupportedException("Unsupported change of underlying viewmodel.");
            }
        }

        private void StarSystems_SelectionChangedEvent(int oldSelection, int newSelection)
        {
            var scale_data = new List<float>();
            var cam = new Camera(viewport_width, viewport_height);
            cam.Position = new OpenTK.Vector3(0,0,
                            //(float)ActiveSystem.ParentStar.Position.X,
                            //(float)ActiveSystem.ParentStar.Position.Y,
                            -1f
                           );

            var selectedSystem = StarSystems.SelectedKey;
            if (selectedSystem != null)
            { 
                SelectedSystemVM.Initialise(_gameVM, StarSystems.SelectedKey, scale_data);
            }
        }
    }
}
