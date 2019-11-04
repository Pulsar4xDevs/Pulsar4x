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
                OrbitDB newOrbit = OrbitDB.FromVector(parentEntity, EntityCommanding, newVector3d, _db.ActionOnDateTime);
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

    public class TransitToOrbitCommand : EntityCommand
    {
        internal override int ActionLanes => 1;
        internal override bool IsBlocking => true;

        Entity _factionEntity;
        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        public Guid TargetEntityGuid { get; set; }

        private Entity _targetEntity;
        public Vector3 TargetOffsetPosition;
        public DateTime TransitStartDateTime;
        public Vector3 ExpendDeltaV;

        WarpMovingDB _db;


        /// <summary>
        /// Creates the transit cmd.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="faction">Faction.</param>
        /// <param name="orderEntity">Order entity.</param>
        /// <param name="targetEntity">Target entity.</param>
        /// <param name="targetOffsetPos_m">Target offset position in au.</param>
        /// <param name="transitStartDatetime">Transit start datetime.</param>
        /// <param name="expendDeltaV_AU">Amount of DV to expend to change the orbit in AU/s</param>
        public static void CreateTransitCmd(Game game, Entity faction, Entity orderEntity, Entity targetEntity, Vector3 targetOffsetPos_m, DateTime transitStartDatetime, Vector3 expendDeltaV)
        {
            var cmd = new TransitToOrbitCommand()
            {
                RequestingFactionGuid = faction.Guid,
                EntityCommandingGuid = orderEntity.Guid,
                CreatedDate = orderEntity.Manager.ManagerSubpulses.StarSysDateTime,
                TargetEntityGuid = targetEntity.Guid,
                TargetOffsetPosition = targetOffsetPos_m,
                TransitStartDateTime = transitStartDatetime,
                ExpendDeltaV = expendDeltaV,
            };
            game.OrderHandler.HandleOrder(cmd);
        }



        internal override void ActionCommand(Game game)
        {
            
            if (!IsRunning)
            {
                
                var warpDB = _entityCommanding.GetDataBlob<WarpAbilityDB>();
                var powerDB = _entityCommanding.GetDataBlob<EnergyGenAbilityDB>();
                Guid eType = warpDB.EnergyType;
                double estored = powerDB.EnergyStored[eType];
                double creationCost = warpDB.BubbleCreationCost;
                if (creationCost <= estored)
                {
                    (Vector3 pos, DateTime eti) targetIntercept = InterceptCalcs.GetInterceptPosition_m(_entityCommanding, _targetEntity.GetDataBlob<OrbitDB>(), TransitStartDateTime, TargetOffsetPosition);
                    OrbitDB orbitDB = _entityCommanding.GetDataBlob<OrbitDB>();
                    Vector3 currentPos_m = Entity.GetPosition_m(_entityCommanding, TransitStartDateTime);
                    //var ralPos = OrbitProcessor.GetPosition_AU(orbitDB, TransitStartDateTime);
                    //var masses = _entityCommanding.GetDataBlob<MassVolumeDB>().Mass + orbitDB.Parent.GetDataBlob<MassVolumeDB>().Mass;
                    //var sgp = GameConstants.Science.GravitationalConstant * masses / 3.347928976e33;

                    //Vector4 currentVec = OrbitProcessor.PreciseOrbitalVector(sgp, ralPos, orbitDB.SemiMajorAxis);
                    Vector3 currentVec_m = OrbitProcessor.GetOrbitalVector_m(orbitDB, TransitStartDateTime);
                    _db = new WarpMovingDB(targetIntercept.pos);
                    _db.TranslateRelitiveExit = TargetOffsetPosition;
                    _db.EntryDateTime = TransitStartDateTime;
                    _db.PredictedExitTime = targetIntercept.eti;
                    _db.TranslateEntryPoint = currentPos_m;
                    _db.SavedNewtonionVector = currentVec_m;

                    _db.ExpendDeltaV = ExpendDeltaV;
                    if (_targetEntity.HasDataBlob<SensorInfoDB>())
                    {
                        _db.TargetEntity = _targetEntity.GetDataBlob<SensorInfoDB>().DetectedEntity;
                    }
                    else
                        _db.TargetEntity = _targetEntity;

                    if (EntityCommanding.HasDataBlob<OrbitDB>())
                        EntityCommanding.RemoveDataBlob<OrbitDB>();
                    EntityCommanding.SetDataBlob(_db);
                    WarpMoveProcessor.StartNonNewtTranslation(EntityCommanding);


                    IsRunning = true;
                    
                    //some debug code:
                    /*
                    var distance = (currentPos - targetIntercept.Item1).Length();
                    var distancekm = Distance.AuToKm(distance);
                    var time = targetIntercept.Item2 - TransitStartDateTime;
                    double spd = _entityCommanding.GetDataBlob<PropulsionAbilityDB>().MaximumSpeed_MS;
                    spd = Distance.MToAU(spd);
                    var distb = spd * time.TotalSeconds;
                    var distbKM = Distance.AuToKm(distb);
                    var dif = distancekm - distbKM;
                    //Assert.AreEqual(distancekm, distbKM);
                    */
                }


            }

        }

        internal override bool IsFinished()
        {
            if (_db != null && _db.IsAtTarget)
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