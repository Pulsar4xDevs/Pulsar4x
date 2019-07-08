using System;
using Newtonsoft.Json;
using Pulsar4X.Vectors;

namespace Pulsar4X.ECSLib
{
    public class NewtonMoveDB : BaseDataBlob
    {
        internal DateTime LastProcessDateTime = new DateTime();
        public Vector3 ThrustVector { get; internal set; } = Vector3.Zero;
        public Vector3 CurrentVector_ms { get; internal set; }

        public Entity SOIParent { get; internal set; }
        public double ParentMass { get; internal set; }

        [JsonConstructor]
        private NewtonMoveDB() { }

        public NewtonMoveDB(Entity sphereOfInfluenceParent, Vector3 velocity_ms)
        {
            CurrentVector_ms = velocity_ms;
            SOIParent = sphereOfInfluenceParent;
            ParentMass = SOIParent.GetDataBlob<MassVolumeDB>().Mass;
            LastProcessDateTime = sphereOfInfluenceParent.Manager.ManagerSubpulses.StarSysDateTime;
        }
        public NewtonMoveDB(NewtonMoveDB db)
        {
            LastProcessDateTime = db.LastProcessDateTime;
            ThrustVector = db.ThrustVector;
            CurrentVector_ms = db.CurrentVector_ms;
            SOIParent = db.SOIParent;
            ParentMass = db.ParentMass; 
        }
        public override object Clone()
        {
            return new NewtonMoveDB(this);
        }
    }
    
    //TODO: merge this with the above NewtonMoveDB. 
    public class NewtonionMoveDB : BaseDataBlob
    {

        public DateTime LastProcessDateTime { get; internal set; }

        public Vector2 DeltaVToExpend_AU  { get; internal set; }

        public DateTime ActionOnDateTime { get; internal set; }

        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }

}
