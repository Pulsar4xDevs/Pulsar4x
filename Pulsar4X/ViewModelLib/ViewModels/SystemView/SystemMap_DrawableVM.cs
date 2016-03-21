using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;
using System.Windows;

namespace Pulsar4X.ViewModel.SystemView
{
    public class SystemMap_DrawableVM : ViewModelBase
    {

        public List<SystemObjectGraphicsInfo> SystemBodies { get; set; } = new List<SystemObjectGraphicsInfo>();

        public Camera camera;
        public List<float> scale_data;

        public SystemMap_DrawableVM(StarSystem starSys, AuthenticationToken authToken, List<float> scale_data, Camera camera)
        {
            this.scale_data = scale_data;
            this.camera = camera;
            foreach (var item in starSys.SystemManager.GetAllEntitiesWithDataBlob<StarInfoDB>(authToken))
            {
                SystemBodies.Add(new SystemObjectGraphicsInfo(item));
            }
            foreach (var item in starSys.SystemManager.GetAllEntitiesWithDataBlob<SystemBodyDB>(authToken))
            {
                SystemBodies.Add(new SystemObjectGraphicsInfo(item));
            }
            OnPropertyChanged();
        }        
    }

    public class SystemObjectGraphicsInfo
    {
        private Entity item;
        public IconData Icon { get; set; }
        public OrbitEllipseFading OrbitEllipse { get; set; }
        public SystemObjectGraphicsInfo(Entity item)
        {
            Icon = new IconData(item.GetDataBlob<PositionDB>());
            if(item.HasDataBlob<OrbitDB>() && !item.GetDataBlob<OrbitDB>().IsStationary)
                OrbitEllipse = new OrbitEllipseFading(item.GetDataBlob<OrbitDB>());
        }
    }


}
