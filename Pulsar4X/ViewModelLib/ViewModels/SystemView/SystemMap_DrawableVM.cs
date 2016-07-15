using System;
using System.Collections.Generic;
using Pulsar4X.ECSLib;
using System.Windows.Threading;

namespace Pulsar4X.ViewModel.SystemView
{
    public class SystemMap_DrawableVM : ViewModelBase
    {
        

        public List<Entity> IconableEntitys { get; } = new List<Entity>();

        public void Initialise(GameVM gameVM, StarSystem starSys, List<float> scale_data)
        {


            IconableEntitys.Clear();
            IconableEntitys.AddRange(starSys.SystemManager.GetAllEntitiesWithDataBlob<PositionDB>(gameVM.CurrentAuthToken));
       
        }        
    }




}
