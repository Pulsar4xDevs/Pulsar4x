using Newtonsoft.Json;
namespace Pulsar4X.ECSLib
{
    public class SensorPositionDB : BaseDataBlob, IPosition
    {
        internal DataFrom GetDataFrom = DataFrom.Parent;
        public PositionDB ActualEntityPositionDB; //the detected actual entity
        public PositionDB ParentPositionDB; //detected actual entity positional parent for ralitive positions.
        public Vector3 MemoryRalitivePosition;
        internal Vector3 AcuracyOffset = new Vector3();

        public Vector3 AbsolutePosition_AU
        {
            get
            {
                if (GetDataFrom == DataFrom.Parent)
                    return ActualEntityPositionDB.AbsolutePosition_AU;
                if (GetDataFrom == DataFrom.Sensors)
                    return ActualEntityPositionDB.AbsolutePosition_AU + AcuracyOffset;
                else
                    return ParentPositionDB.AbsolutePosition_AU + MemoryRalitivePosition; 
            }
        }

        public Vector3 RelativePosition_AU
        {
            get
            {
                if (GetDataFrom == DataFrom.Parent)
                    return ActualEntityPositionDB.RelativePosition_AU;
                if (GetDataFrom == DataFrom.Sensors)
                    return ActualEntityPositionDB.RelativePosition_AU + AcuracyOffset;
                else
                    return MemoryRalitivePosition;
            }
        }


        [JsonConstructor]
        private SensorPositionDB()
        { }

        public SensorPositionDB(PositionDB actualEntityPosition, DataFrom dataFrom = DataFrom.Parent)
        {
            ActualEntityPositionDB = actualEntityPosition;
            //if(actualEntityPosition.ParentDB != null)
            ParentPositionDB = (PositionDB)actualEntityPosition.ParentDB;
            GetDataFrom = DataFrom.Parent; 
        }

        public SensorPositionDB(SensorPositionDB toClone)
        {
            GetDataFrom = toClone.GetDataFrom;
            MemoryRalitivePosition = toClone.MemoryRalitivePosition;
        }

        public override object Clone()
        {
            return new SensorPositionDB(this);
        }
    }
}
