using Newtonsoft.Json;
using Pulsar4X.Orbital;
using Pulsar4X.Engine.Sensors;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Datablobs
{
    public class SensorPositionDB : BaseDataBlob, IPosition
    {
        [JsonProperty]
        internal DataFrom GetDataFrom = DataFrom.Parent;

        [JsonProperty]
        public PositionDB ActualEntityPositionDB; //the detected actual entity

        [JsonProperty]
        public PositionDB? ParentPositionDB; //detected actual entity positional parent for relative positions.

        [JsonProperty]
        public Vector3 MemoryrelativePosition_m;

        [JsonProperty]
        internal Vector3 AcuracyOffset = new Vector3();

        public Vector3 AbsolutePosition
        {
            get
            {
                if (GetDataFrom == DataFrom.Parent)
                    return ActualEntityPositionDB.AbsolutePosition;
                if (GetDataFrom == DataFrom.Sensors)
                    return ActualEntityPositionDB.AbsolutePosition + AcuracyOffset;
                else
                    return ParentPositionDB.AbsolutePosition + MemoryrelativePosition_m;
            }
        }

        public Vector3 RelativePosition_AU
        {
            get { return Distance.MToAU(RelativePosition); }
        }

        public Vector3 RelativePosition
        {
            get
            {
                if (GetDataFrom == DataFrom.Parent)
                    return ActualEntityPositionDB.RelativePosition;
                if (GetDataFrom == DataFrom.Sensors)
                    return ActualEntityPositionDB.RelativePosition + AcuracyOffset;
                else
                    return MemoryrelativePosition_m;
            }
        }


        [JsonConstructor]
        private SensorPositionDB()
        { }

        public SensorPositionDB(PositionDB actualEntityPosition, DataFrom dataFrom = DataFrom.Parent)
        {
            ActualEntityPositionDB = actualEntityPosition;
            //if(actualEntityPosition.ParentDB != null)
            ParentPositionDB = (PositionDB?)actualEntityPosition.ParentDB;
            GetDataFrom = DataFrom.Parent;
        }

        public SensorPositionDB(SensorPositionDB toClone)
        {
            GetDataFrom = toClone.GetDataFrom;
            MemoryrelativePosition_m = toClone.MemoryrelativePosition_m;
        }

        public override object Clone()
        {
            return new SensorPositionDB(this);
        }
    }
}
