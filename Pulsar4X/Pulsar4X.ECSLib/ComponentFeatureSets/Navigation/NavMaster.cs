using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;

namespace Pulsar4X.ECSLib
{

    public struct Manuver
    {
        public enum ManuverType
        {
            Drift,
            NewtonSimple,
            NewtonThrust,
            Warp,
            Wormhole
        }
        public ManuverType TypeOfManuver;
        public DateTime StartDateTime;
        public DateTime EndDateTime;
        public KeplerElements StartKepler;
        public KeplerElements EndKepler;
        public Entity StartSOIParent;
        public Entity EndSOIParent;
    }
    public class NavSequenceDB : BaseDataBlob
    {
        public string CurrentActivity { get; internal set; }
        internal List<Manuver> ManuverNodes = new List<Manuver>();

        internal void AddManuver(Manuver.ManuverType type, DateTime startDate, Entity StartParent, KeplerElements startKE, DateTime endDate, Entity EndParent, KeplerElements endKE)
        {
            var node = new Manuver()
            {
                TypeOfManuver = type,
                StartDateTime = startDate,
                StartSOIParent = StartParent,
                StartKepler = startKE,
                EndDateTime = endDate,
                EndSOIParent = EndParent,
                EndKepler = endKE,
            };
            ManuverNodes.Add(node);
            StartParent.Manager.ManagerSubpulses.AddEntityInterupt(startDate, nameof(NavSequenceProcessor), OwningEntity);
            EndParent.Manager.ManagerSubpulses.AddEntityInterupt(endDate, nameof(NavSequenceProcessor), OwningEntity);
        }
        
        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }

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
                    
                    
                    NewtonMoveDB newDB = new NewtonMoveDB(manuver.StartSOIParent, currentVel, (Vector3)manuverDV);
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
                    break;
                }
            }
        }

        void EndManuver(NavSequenceDB db)
        {
            db.ManuverNodes.RemoveAt(0);
        }
    }
    
    
    
    
    public class NavMaster : EntityCommand
    {
        public override string Name { get; } = "";

        public override string Details { get; } = "";

        public bool CycleCommand = false;
        List<EntityCommand> Orders = new List<EntityCommand>();
        List<EntityCommand> OrdersForOthers = new List<EntityCommand>();
        public override ActionLaneTypes ActionLanes { get; }
        public override bool IsBlocking { get; } = false;
        internal override Entity EntityCommanding { get; }
        internal override bool IsValidCommand(Game game)
        {
            return true;
        }

        internal override void Execute(DateTime atDateTime)
        {
            throw new NotImplementedException();
        }

        public override bool IsFinished()
        {
            throw new NotImplementedException();
        }

        public override EntityCommand Clone()
        {
            throw new NotImplementedException();
        }
    }



}