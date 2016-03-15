using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ViewModel.SystemView
{
    public class StarSystemVM : ViewModelBase
    {
        public DictionaryVM<StarSystem, string> StarSystems { get; } = new DictionaryVM<StarSystem, string>();
        public SystemMap_DrawableVM SelectedSystemVM { get; private set; }
        private AuthenticationToken _authToken;

        private int viewport_width;
        private int viewport_height;

        public StarSystemVM(Game game, Entity factionEntity, AuthenticationToken authToken)
        {
            _authToken = authToken;
            foreach (var item in game.GetSystems(authToken))
            {
                StarSystems.Add(item, item.NameDB.GetName(factionEntity));
                
            }
            
            StarSystems.SelectionChangedEvent += StarSystems_SelectionChangedEvent;
            StarSystems.SelectedIndex = 0;
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

            SelectedSystemVM = new SystemMap_DrawableVM(StarSystems.SelectedKey, _authToken, scale_data, cam);
        }
    }
}
