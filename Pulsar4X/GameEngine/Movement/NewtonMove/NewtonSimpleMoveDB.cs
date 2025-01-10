using System;
using Newtonsoft.Json;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Galaxy;
using Pulsar4X.Orbital;

namespace Pulsar4X.Movement
{
    public class NewtonSimpleMoveDB : BaseDataBlob
    {
        [JsonProperty]
        internal DateTime LastProcessDateTime = new DateTime();
        [JsonProperty]
        public DateTime ActionOnDateTime { get; internal set; }
        [JsonProperty]
        public KeplerElements CurrentTrajectory { get; internal set; }
        [JsonProperty]
        public KeplerElements TargetTrajectory { get; internal set; }
        [JsonProperty]

        public bool IsComplete = false;
        [JsonProperty]
        public Entity SOIParent { get; internal set; }
        [JsonProperty]
        public double ParentMass { get; internal set; }

        [JsonConstructor]
        private NewtonSimpleMoveDB() { }

        public NewtonSimpleMoveDB(Entity soiParent, KeplerElements start, KeplerElements end, DateTime onDateTime)
        {
            LastProcessDateTime = onDateTime;
            ActionOnDateTime = onDateTime;
            CurrentTrajectory = start;
            TargetTrajectory = end;
            SOIParent = soiParent;
            ParentMass = SOIParent.GetDataBlob<MassVolumeDB>().MassTotal;
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
}