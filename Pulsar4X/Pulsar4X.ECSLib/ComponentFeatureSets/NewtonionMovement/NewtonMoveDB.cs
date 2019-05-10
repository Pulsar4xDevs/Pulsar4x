using System;

namespace Pulsar4X.ECSLib
{
    public class NewtonMoveDB : BaseDataBlob
    {
        //internal DateTime TimeSinceLastRun { get; set; }
        public Vector4 ThrustVector { get; internal set; } = Vector4.Zero;
        public Vector4 CurrentVector_kms { get; internal set; }

        public Entity SOIParent { get; internal set; }
        public double ParentMass { get; internal set; }

        public NewtonMoveDB() { }

        public NewtonMoveDB(Entity sphereOfInfluenceParent)
        {
            SOIParent = sphereOfInfluenceParent;
            ParentMass = SOIParent.GetDataBlob<MassVolumeDB>().Mass;
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
}
