using Newtonsoft.Json;
namespace Pulsar4X.ECSLib
{
    public class SensorPositionDB : BaseDataBlob, IPosition
    {
        internal DataFrom GetDataFrom = DataFrom.Parent;
        public PositionDB ActualEntityPositionDB; //the detected actual entity
        public PositionDB ParentPositionDB; //detected actual entity positional parent for ralitive positions.
        public Vector3 MemoryRalitivePosition_m;
        internal Vector3 AcuracyOffset = new Vector3();

        public Vector3 AbsolutePosition_AU
        {
            get { return Distance.MToAU(AbsolutePosition_m); }
        }

        public Vector3 AbsolutePosition_m 
        {             
            get
            {
                if (GetDataFrom == DataFrom.Parent)
                    return ActualEntityPositionDB.AbsolutePosition_m;
                if (GetDataFrom == DataFrom.Sensors)
                    return ActualEntityPositionDB.AbsolutePosition_m + AcuracyOffset;
                else
                    return ParentPositionDB.AbsolutePosition_m + MemoryRalitivePosition_m; 
            } 
        }

        public Vector3 RelativePosition_AU
        {
            get { return Distance.MToAU(RelativePosition_m); }
        }

        public Vector3 RelativePosition_m
        {
            get
            {
                if (GetDataFrom == DataFrom.Parent)
                    return ActualEntityPositionDB.RelativePosition_m;
                if (GetDataFrom == DataFrom.Sensors)
                    return ActualEntityPositionDB.RelativePosition_m + AcuracyOffset;
                else
                    return MemoryRalitivePosition_m;
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
            MemoryRalitivePosition_m = toClone.MemoryRalitivePosition_m;
        }

        public override object Clone()
        {
            return new SensorPositionDB(this);
        }
    }
}
