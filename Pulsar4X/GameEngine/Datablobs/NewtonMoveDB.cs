using System;
using Newtonsoft.Json;
using Pulsar4X.Orbital;
using Pulsar4X.Engine;

namespace Pulsar4X.Datablobs
{
    /// <summary>
    /// This gets added to an entity when it's doing a newton thrust manuver.
    /// </summary>
    public class NewtonMoveDB : BaseDataBlob
    {
        internal DateTime LastProcessDateTime = new DateTime();


        /// <summary>
        /// This is the parent ralitive manuver deltaV (ie within SOI not StarSystem Global)
        /// </summary>
        /// <value></value>
        public Vector3 ManuverDeltaV {get; internal set;}

        /// <summary>
        /// Just returns the lengths of the manuver deltaV
        /// </summary>
        /// <returns></returns>
        public double ManuverDeltaVLen {get{return ManuverDeltaV.Length();}}
        /// <summary>
        /// Orbital Frame Of Reference: Y is prograde
        /// </summary>
        //public Vector3 DeltaVForManuver_FoRO_m { get; private set; }

        public DateTime ActionOnDateTime { get; internal set; }

        /// <summary>
        /// Parent relative velocity vector.
        /// </summary>
        public Vector3 CurrentVector_ms { get; internal set; }

        public Entity SOIParent { get; internal set; }
        public double ParentMass { get; internal set; }

        private KeplerElements _ke;

        internal void UpdateKeplerElements(KeplerElements ke)
        {
            _ke = ke;
        }

        internal void UpdateKeplerElements()
        {
            double myMass = OwningEntity.GetDataBlob<MassVolumeDB>().MassTotal;
            var sgp = GeneralMath.StandardGravitationalParameter(myMass + ParentMass);
            var pos = OwningEntity.GetDataBlob<PositionDB>().RelativePosition;
            var dateTime = OwningEntity.StarSysDateTime;
            _ke = OrbitMath.KeplerFromPositionAndVelocity(sgp, pos, CurrentVector_ms, dateTime);
        }

        [JsonConstructor]
        private NewtonMoveDB() { }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sphereOfInfluenceParent"></param>
        /// <param name="velocity_ms">Parentrelative Velocity</param>
        /// <param name="manuverDeltaV">Parentrelative Manuver</param>
        public NewtonMoveDB(Entity sphereOfInfluenceParent, Vector3 velocity_ms, Vector3 manuverDeltaV)
        {
            CurrentVector_ms = velocity_ms;
            SOIParent = sphereOfInfluenceParent;
            ManuverDeltaV = manuverDeltaV;
            ParentMass = SOIParent.GetDataBlob<MassVolumeDB>().MassDry;
            LastProcessDateTime = sphereOfInfluenceParent.Manager.ManagerSubpulses.StarSysDateTime;

        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sphereOfInfluenceParent"></param>
        /// <param name="velocity_ms">Parentrelative Velocity</param>
        public NewtonMoveDB(Entity sphereOfInfluenceParent, Vector3 velocity_ms)
        {
            CurrentVector_ms = velocity_ms;
            SOIParent = sphereOfInfluenceParent;
            ParentMass = SOIParent.GetDataBlob<MassVolumeDB>().MassDry;
            LastProcessDateTime = sphereOfInfluenceParent.Manager.ManagerSubpulses.StarSysDateTime;

        }

        public NewtonMoveDB(NewtonMoveDB db)
        {
            LastProcessDateTime = db.LastProcessDateTime;
            CurrentVector_ms = db.CurrentVector_ms;
            SOIParent = db.SOIParent;
            ParentMass = db.ParentMass;

        }
        public override object Clone()
        {
            return new NewtonMoveDB(this);
        }

        internal override void OnSetToEntity()
        {
            if (OwningEntity.HasDataBlob<OrbitDB>())
            {
                OwningEntity.RemoveDataBlob<OrbitDB>();
            }
            if (OwningEntity.HasDataBlob<OrbitUpdateOftenDB>())
            {
                OwningEntity.RemoveDataBlob<OrbitUpdateOftenDB>();
            }
            if (OwningEntity.HasDataBlob<WarpMovingDB>())
            {
                OwningEntity.RemoveDataBlob<WarpMovingDB>();
            }
            if(OwningEntity.HasDataBlob<MassVolumeDB>())
            {
                UpdateKeplerElements();
            }
        }

        public KeplerElements GetElements()
        {
            return _ke;
        }
    }
}