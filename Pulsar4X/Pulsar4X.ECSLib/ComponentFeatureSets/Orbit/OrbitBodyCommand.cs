using System;
using Pulsar4X.Vectors;

namespace Pulsar4X.ECSLib
{

    public class ChangeCurrentOrbitCommand : EntityCommand
    {
        internal override int ActionLanes => 1;
        internal override bool IsBlocking => true;

        Entity _factionEntity;
        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        NewtonMoveDB _db;

        public static void CreateCommand(Game game, Entity faction, Entity orderEntity, DateTime actionDateTime, Vector3 expendDeltaV_AU)
        {
            var cmd = new ChangeCurrentOrbitCommand()
            {
                RequestingFactionGuid = faction.Guid,
                EntityCommandingGuid = orderEntity.Guid,
                CreatedDate = orderEntity.Manager.ManagerSubpulses.StarSysDateTime,

            };

            var parent = orderEntity.GetDataBlob<OrbitDB>().Parent;
            cmd._db = new NewtonMoveDB(parent, Vector3.Zero);
            cmd._db.ActionOnDateTime = actionDateTime;
            cmd._db.DeltaVToExpend_AU = expendDeltaV_AU;
            


            game.OrderHandler.HandleOrder(cmd);
        }

        internal override void ActionCommand(Game game)
        {
            if (!IsRunning)
            {
                Entity parentEntity = EntityCommanding.GetDataBlob<OrbitDB>().Parent;
                Vector3 newVector = OrbitProcessor.InstantaneousOrbitalVelocityVector_AU(EntityCommanding.GetDataBlob<OrbitDB>(), _db.ActionOnDateTime);
                newVector += _db.DeltaVToExpend_AU;
                var spdmps = Distance.AuToMt( newVector.Length());
                Vector3 newVector3d = new Vector3(newVector.X, newVector.Y,0);
                OrbitDB newOrbit = OrbitDB.FromVelocity_AU(parentEntity, EntityCommanding, newVector3d, _db.ActionOnDateTime);
                /*
                if (newOrbit.Periapsis > targetSOI)
                {
                    //TODO: find who's SOI we're currently in and create an orbit for that;
                }
                if (newOrbit.Apoapsis > targetSOI)
                {
                    //TODO: change orbit to new parent at SOI change
                }
                */


                EntityCommanding.SetDataBlob(newOrbit);
                newOrbit.SetParent(parentEntity);

            }
        }

        internal override bool IsFinished()
        {
            if (IsRunning)
                return true;
            else
                return false;
        }

        internal override bool IsValidCommand(Game game)
        {
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
                return true;
            else
                return false;
        }
    }


}