using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    class AtmoDrawData : Icon
    {

        public AtmoDrawData(Entity entity): base(entity.GetDataBlob<PositionDB>())
        {
            _positionDB = entity.GetDataBlob<PositionDB>();
        }
    }
}
