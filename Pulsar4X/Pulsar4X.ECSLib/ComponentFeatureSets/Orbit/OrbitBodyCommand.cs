using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Pulsar4X.ECSLib
{

    public class TransitToOrbitCommand : EntityCommand
    {
        internal override int ActionLanes => 1;
        internal override bool IsBlocking => true;

        Entity _factionEntity;
        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        public Guid TargetEntityGuid { get; set; }

        private Entity _targetEntity;
        public Vector4 TargetOffsetPosition_AU;
        public DateTime TransitStartDateTime;
        public Vector4 DeltaVExpendAtExit;

        TranslateMoveDB _db;


        public static void CreateTransitCmd(Game game, Entity faction, Entity orderEntity, Entity targetEntity, Vector4 targetOffsetPos_AU, DateTime transitStartDatetime)
        {
            var cmd = new TransitToOrbitCommand()
            {
                RequestingFactionGuid = faction.Guid,
                EntityCommandingGuid = orderEntity.Guid,
                CreatedDate = orderEntity.Manager.ManagerSubpulses.SystemLocalDateTime,
                TargetEntityGuid = targetEntity.Guid,
                TargetOffsetPosition_AU = targetOffsetPos_AU,
                TransitStartDateTime = transitStartDatetime
            };
            game.OrderHandler.HandleOrder(cmd);
        }



        internal override void ActionCommand(Game game)
        {
            
            if (!IsRunning)
            {
                

                (Vector4, DateTime) targetIntercept = InterceptCalcs.GetInterceptPosition(_entityCommanding, _targetEntity.GetDataBlob<OrbitDB>(), TransitStartDateTime, TargetOffsetPosition_AU);
                OrbitDB orbitDB = _entityCommanding.GetDataBlob<OrbitDB>();
                Vector4 currentPos = OrbitProcessor.GetAbsolutePosition_AU(orbitDB, TransitStartDateTime);
                var ralPos = OrbitProcessor.GetPosition_AU(orbitDB, TransitStartDateTime);
                var masses = _entityCommanding.GetDataBlob<MassVolumeDB>().Mass + orbitDB.Parent.GetDataBlob<MassVolumeDB>().Mass;
                var sgp = GameConstants.Science.GravitationalConstant * masses / 3.347928976e33;

                //Vector4 currentVec = OrbitProcessor.PreciseOrbitalVector(sgp, ralPos, orbitDB.SemiMajorAxis);
                Vector4 currentVec = OrbitProcessor.GetOrbitalVector(orbitDB, TransitStartDateTime);
                _db = new TranslateMoveDB(targetIntercept.Item1);
                _db.EntryDateTime = TransitStartDateTime;
                _db.PredictedExitTime = targetIntercept.Item2;
                _db.TranslateEntryPoint_AU = currentPos;
                _db.SavedNewtonionVector_AU = currentVec;
                if (_targetEntity.HasDataBlob<SensorInfoDB>())
                {
                    _db.TargetEntity = _targetEntity.GetDataBlob<SensorInfoDB>().DetectedEntity;
                }
                else
                    _db.TargetEntity = _targetEntity;
                if (EntityCommanding.HasDataBlob<OrbitDB>())
                    EntityCommanding.RemoveDataBlob<OrbitDB>();
                EntityCommanding.SetDataBlob(_db);
                TranslateMoveProcessor.StartNonNewtTranslation(EntityCommanding);
                IsRunning = true;


                var distance = (currentPos - targetIntercept.Item1).Length();
                var distancekm = Distance.AuToKm(distance);

                var time = targetIntercept.Item2 - TransitStartDateTime;

                double spd = _entityCommanding.GetDataBlob<PropulsionAbilityDB>().MaximumSpeed_MS;
                spd = Distance.MToAU(spd);
                var distb = spd * time.TotalSeconds;
                var distbKM = Distance.AuToKm(distb);

                var dif = distancekm - distbKM;
                //Assert.AreEqual(distancekm, distbKM);
            }

        }

        internal override bool IsFinished()
        {
            if (_db.IsAtTarget)
                return true;
            return false;
        }

        internal override bool IsValidCommand(Game game)
        {
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
            {
                if (game.GlobalManager.FindEntityByGuid(TargetEntityGuid, out _targetEntity))
                {
                    return true;
                }
            }
            return false;
        }
    }

    /*

    public class OrbitBodyCommand : EntityCommand
    {

        internal override int ActionLanes => 1;
        internal override bool IsBlocking => true;

        public Guid TargetEntityGuid { get; set; }
        public Vector4 TargetPosition { get; set; }

        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        private Entity _targetEntity;

        public double ApoapsisInKM { get; set; }
        public double PeriapsisInKM { get; set; }
        internal List<EntityCommand> NestedCommands { get; } = new List<EntityCommand>();

        [JsonIgnore] Entity _factionEntity;



        public static void CreateOrbitBodyCommand(Game game, DateTime starSysDate, Guid factionGuid, Guid orderEntity, Guid targetEntity, double ApihelionKm, double PerhelionKm)
        {
            var cmd = new OrbitBodyCommand()
            {
                RequestingFactionGuid = factionGuid,
                EntityCommandingGuid = orderEntity,
                CreatedDate = starSysDate,
                TargetEntityGuid = targetEntity,
                ApoapsisInKM = ApihelionKm,
                PeriapsisInKM = PerhelionKm,
            };
            game.OrderHandler.HandleOrder(cmd);
        }

        public static void CreateOrbitBodyCommand(Game game, Entity faction, Entity orderEntity, Entity targetEntity, double apoapsisKm, double periapsisKm)
        {
            var cmd = new OrbitBodyCommand()
            {
                RequestingFactionGuid = faction.Guid,
                EntityCommandingGuid = orderEntity.Guid,
                CreatedDate = orderEntity.Manager.ManagerSubpulses.SystemLocalDateTime,
                TargetEntityGuid = targetEntity.Guid,
                ApoapsisInKM = apoapsisKm,
                PeriapsisInKM = periapsisKm,
            };
            game.OrderHandler.HandleOrder(cmd);
        }


        internal override bool IsValidCommand(Game game)
        {
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
            {
                if (game.GlobalManager.FindEntityByGuid(TargetEntityGuid, out _targetEntity))
                {
                    return true;
                }
            }
            return false;
        }

        internal override void ActionCommand(Game game)
        {
            OrderableProcessor.ProcessOrderList(game, NestedCommands);
            if (NestedCommands.Count == 0)
            {
                if (!IsRunning)
                {
                    var entPos = _entityCommanding.GetDataBlob<PositionDB>().PositionInKm;
                    var tarPos = _targetEntity.GetDataBlob<PositionDB>().PositionInKm;
                    double distanceAU = PositionDB.GetDistanceBetween(_entityCommanding.GetDataBlob<PositionDB>(), _targetEntity.GetDataBlob<PositionDB>());
                    var rangeAU = ApoapsisInKM / GameConstants.Units.KmPerAu;
                    if (Math.Abs(rangeAU - distanceAU) <= 500 / GameConstants.Units.MetersPerAu) //distance within 500m 
                    {
                        DateTime datenow = _entityCommanding.Manager.ManagerSubpulses.SystemLocalDateTime;
                        var newOrbit = ShipMovementProcessor.CreateOrbitHereWithPerihelion(_entityCommanding, _targetEntity, PeriapsisInKM, datenow);
                        _entityCommanding.SetDataBlob(newOrbit);
                        IsRunning = true;
                    }
                    else //insert new translate move
                    {
                        var cmd = new TranslateMoveCommand() 
                        { 
                            RequestingFactionGuid = this.RequestingFactionGuid, 
                            EntityCommandingGuid = this.EntityCommandingGuid, 
                            CreatedDate = this.CreatedDate, 
                            TargetEntityGuid = this.TargetEntityGuid, 
                            RangeInKM = this.ApoapsisInKM 
                        };
                        NestedCommands.Insert(0, cmd);
                        cmd.IsValidCommand(game);
                        cmd.ActionCommand(game);
                    }
                }
            }
        }

        internal override bool IsFinished()
        {
            if (_entityCommanding.HasDataBlob<OrbitDB>())
                return true;
            return false;
        }
    }*/
}