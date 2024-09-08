using System;
using Pulsar4X.Datablobs;
using Pulsar4X.Interfaces;
using Pulsar4X.Extensions;
using Pulsar4X.Orbital;

namespace Pulsar4X.Engine
{
    public class NavSequenceProcessor : IInstanceProcessor
    {
        internal override void ProcessEntity(Entity entity, DateTime atDateTime)
        {
            var db = entity.GetDataBlob<NavSequenceDB>();
            var manuver = db.ManuverNodes[0];
            if(manuver.StartDateTime == atDateTime)
                StartManuver(entity, db, manuver, atDateTime);
            if(manuver.EndDateTime < atDateTime)
                EndManuver(db);
            
            db.ManuverNodes.RemoveAt(0);
            var nextManuverDate = db.ManuverNodes[0].StartDateTime;
        }

        void StartManuver(Entity entity, NavSequenceDB db, Manuver manuver, DateTime atDateTime)
        {
            switch (manuver.TypeOfManuver)
            {
                case Manuver.ManuverType.Drift:
                {
                    string orbitingName = entity.GetSOIParentEntity().GetDataBlob<NameDB>().GetName(entity.GetFactionOwner);
                    db.CurrentActivity = "Orbiting " + orbitingName;
                    break;
                }
                case Manuver.ManuverType.NewtonThrust:
                {
                    var currentVel = entity.GetRelativeFutureVelocity(atDateTime);
                    
                    var parentMass = entity.GetSOIParentEntity().GetDataBlob<MassVolumeDB>().MassTotal;
                    var myMass = entity.GetDataBlob<MassVolumeDB>().MassTotal;
                    //var sgp = GeneralMath.StandardGravitationalParameter(myMass + parentMass);
                    
                    //This is going to be very wrong at long manuvers. 
                    var startVector = OrbitalMath.GetStateVectors(manuver.StartKepler, atDateTime);
                    var endVector = OrbitalMath.GetStateVectors(manuver.EndKepler, manuver.EndDateTime);
                    var manuverDV = endVector.velocity - startVector.velocity;
                    
                    
                    NewtonSimDB newDB = new NewtonSimDB(manuver.StartSOIParent, currentVel, (Vector3)manuverDV);
                    entity.SetDataBlob(newDB);
                    db.CurrentActivity = "Maneuvering";
                    break;
                }
                case Manuver.ManuverType.NewtonSimple:
                {
                    NewtonSimpleMoveDB newDB = new NewtonSimpleMoveDB(manuver.StartSOIParent, manuver.StartKepler, manuver.EndKepler, manuver.StartDateTime);
                    entity.SetDataBlob(newDB);
                    db.CurrentActivity = "Maneuvering";
                    break;
                }
                case Manuver.ManuverType.Warp:
                {
                    var endVector = OrbitalMath.GetStateVectors(manuver.EndKepler, manuver.EndDateTime);
                    WarpMovingDB newDB = new WarpMovingDB(entity, manuver.EndSOIParent, endVector.position);
                    entity.SetDataBlob(newDB);
                    db.CurrentActivity = "Warping";
                    break;
                }
                case Manuver.ManuverType.Wormhole:
                {
                    throw new NotImplementedException();
                }
            }
        }

        void EndManuver(NavSequenceDB db)
        {
            db.ManuverNodes.RemoveAt(0);
        }
    }
}