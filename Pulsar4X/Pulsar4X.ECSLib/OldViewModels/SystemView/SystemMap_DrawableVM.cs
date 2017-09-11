using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class SystemMap_DrawableVM : ViewModelBase
    {
        

        public List<Entity> IconableEntitys { get; } = new List<Entity>();
        public ManagerSubPulse SystemSubpulse { get; private set; }

        public void Initialise(GameVM gameVM, StarSystem starSys)
        {

            IconableEntitys.Clear();
            IconableEntitys.AddRange(starSys.SystemManager.GetAllEntitiesWithDataBlob<PositionDB>(gameVM.CurrentAuthToken));
            SystemSubpulse = starSys.SystemManager.ManagerSubpulses;
            starSys.SystemManager.GetAllEntitiesWithDataBlob<NewtonBalisticDB>(gameVM.CurrentAuthToken);

            OnPropertyChanged(nameof(IconableEntitys));
        }

    }




}
