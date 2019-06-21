using System;
using Newtonsoft.Json;
namespace Pulsar4X.ECSLib
{
    public class NewtonMoveDB : BaseDataBlob
    {
        internal DateTime LastProcessDateTime = new DateTime();
        public Vector3 ThrustVector { get; internal set; } = Vector3.Zero;
        public Vector3 CurrentVector_kms { get; internal set; }

        public Entity SOIParent { get; internal set; }
        public double ParentMass { get; internal set; }

        [JsonConstructor]
        private NewtonMoveDB() { }

        public NewtonMoveDB(Entity sphereOfInfluenceParent)
        {
            SOIParent = sphereOfInfluenceParent;
            ParentMass = SOIParent.GetDataBlob<MassVolumeDB>().Mass;
            LastProcessDateTime = sphereOfInfluenceParent.Manager.ManagerSubpulses.StarSysDateTime;
        }
        public NewtonMoveDB(NewtonMoveDB db)
        {
            LastProcessDateTime = db.LastProcessDateTime;
            ThrustVector = db.ThrustVector;
            CurrentVector_kms = db.CurrentVector_kms;
            SOIParent = db.SOIParent;
            ParentMass = db.ParentMass; 
        }
        public override object Clone()
        {
            return new NewtonMoveDB(this);
        }
    }
}
