using System;
using Pulsar4X.ECSLib.ComponentFeatureSets.Missiles;
using Pulsar4X.Vectors;

namespace Pulsar4X.ECSLib
{

    public class NewtonThrustCommand : EntityCommand
    {
        public override int ActionLanes => 1;
        public override bool IsBlocking => true;
        public override string Name { get; } = "Nav: Thrust";

        public override string Details
        {
            get
            {
                return "Expend + " + Stringify.Velocity(_orbitRalitiveDeltaV.Length()) + "Δv";
            }
        }

        Entity _factionEntity;
        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        private Vector3 _orbitRalitiveDeltaV;
        NewtonMoveDB _db;

        public static void CreateCommand(Guid faction, Entity orderEntity, DateTime actionDateTime, Vector3 expendDeltaV_m)
        {
            var cmd = new NewtonThrustCommand()
            {
                RequestingFactionGuid = faction,
                EntityCommandingGuid = orderEntity.Guid,
                CreatedDate = orderEntity.Manager.ManagerSubpulses.StarSysDateTime,
                _orbitRalitiveDeltaV = expendDeltaV_m,
                ActionOnDate = actionDateTime

            };
            StaticRefLib.Game.OrderHandler.HandleOrder(cmd);
        }

        internal override void ActionCommand(DateTime atDateTime)
        {
            if (!IsRunning)
            {
                 var parent = Entity.GetSOIParentEntity(_entityCommanding);
                 var currentVel = Entity.GetRalitiveFutureVelocity(_entityCommanding, ActionOnDate);               
                if(_entityCommanding.HasDataBlob<OrbitDB>())
                    _entityCommanding.RemoveDataBlob<OrbitDB>();
                _db = new NewtonMoveDB(parent, currentVel);
                _db.ActionOnDateTime = ActionOnDate;
                _db.DeltaVForManuver_FoRO_m = _orbitRalitiveDeltaV;
                _entityCommanding.SetDataBlob(_db);
                IsRunning = true;
            }
        }

        public override bool IsFinished()
        {
            if (IsRunning && _db.DeltaVForManuver_FoRO_m.Length() <= 0)
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

    public class ThrustToTargetCmd : EntityCommand
    {
        
        public override string Name { get; } = "Nav: Intercept/Collide with target";

        public override string Details
        {
            get
            {
                string targetName = _targetEntity.GetDataBlob<NameDB>().GetName(_factionEntity);
                return "Attempting intercept on + " + targetName + ", with " + Stringify.Velocity(_newtonAbilityDB.DeltaV) + "Δv remaining.";
            }
        }
        
        public override int ActionLanes => 1;
        public override bool IsBlocking => true;

        Entity _factionEntity;
        Entity _entityCommanding;
        private OrdnanceDesign _missileDesign;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        private Entity _targetEntity;
        NewtonMoveDB _newtonMovedb;
        NewtonThrustAbilityDB _newtonAbilityDB;
        private double _startDV;
        private double _startBurnTime;
        private double _fuelBurnRate;
        private double _totalFuel;

        private double _soiParentMass;
        
        public static void CreateCommand(Guid faction, Entity orderEntity, DateTime actionDateTime, Entity targetEntity)
        {
            var cmd = new ThrustToTargetCmd()
            {
                RequestingFactionGuid = faction,
                EntityCommandingGuid = orderEntity.Guid,
                CreatedDate = orderEntity.StarSysDateTime,
                _targetEntity = targetEntity,
                ActionOnDate = actionDateTime,
            };
            StaticRefLib.Game.OrderHandler.HandleOrder(cmd);
        }

        internal override void ActionCommand(DateTime atDateTime)
        {
            if(atDateTime < ActionOnDate)
                return;
            if (!IsRunning)
            {
                IsRunning = true;
                _newtonAbilityDB = _entityCommanding.GetDataBlob<NewtonThrustAbilityDB>();
                _startDV = _newtonAbilityDB.DeltaV;
                _fuelBurnRate = _newtonAbilityDB.FuelBurnRate;
                _totalFuel = _newtonAbilityDB.TotalFuel_kg;
                var soiParentEntity = Entity.GetSOIParentEntity(_entityCommanding);
                _soiParentMass = soiParentEntity.GetDataBlob<MassVolumeDB>().MassDry;
                var currentVel = Entity.GetRalitiveFutureVelocity(_entityCommanding, atDateTime);               
                if(_entityCommanding.HasDataBlob<OrbitDB>())
                _entityCommanding.RemoveDataBlob<OrbitDB>();
                if(_entityCommanding.HasDataBlob<OrbitUpdateOftenDB>())
                _entityCommanding.RemoveDataBlob<OrbitUpdateOftenDB>();
                if (_entityCommanding.HasDataBlob<NewtonMoveDB>())
                    _newtonMovedb = _entityCommanding.GetDataBlob<NewtonMoveDB>();
                else
                {
                    _newtonMovedb = new NewtonMoveDB(soiParentEntity, currentVel); 
                }
                
                _entityCommanding.SetDataBlob(_newtonMovedb);
            }
            var halfDV = _startDV * 0.5; //lets burn half the dv getting into a good intercept. 
            var dvUsed = _startDV - _newtonAbilityDB.DeltaV;
            var dvToUse = halfDV - dvUsed;
            if(dvToUse > 0)
            {
                (Vector3 Position, Vector3 Velocity) curOurRalState = Entity.GetRalitiveState(_entityCommanding);
                (Vector3 Position, Vector3 Velocity) curTgtRalState = Entity.GetRalitiveState(_targetEntity);
                var dvRemaining = _newtonAbilityDB.DeltaV;
                
                var tgtVelocity = Entity.GetAbsoluteFutureVelocity(_targetEntity, atDateTime);
                //calculate the differencecs in velocity vectors.
                Vector3 leadToTgt = (curTgtRalState.Velocity - curOurRalState.Velocity);
                 
                //convert the lead to an orbit ralitive (prograde Y) vector. 
                //var manuverVector = OrbitMath.GlobalToOrbitVector(leadToTgt, curOurRalState.Position, curOurRalState.Velocity);


                var burnRate = _newtonAbilityDB.FuelBurnRate;
                //var foo = OrbitMath.TsiolkovskyFuelUse(_totalFuel, )
                var fuelUse = OrbitMath.TsiolkovskyFuelCost(
                    _newtonAbilityDB.TotalFuel_kg, 
                    _newtonAbilityDB.ExhaustVelocity, 
                    dvToUse//pretty sure this should be dvToUse, but that's giving me a silent crash. 
                    );
                var burnTime = fuelUse / burnRate;
                
                var manuverVector = ManuverVector(dvToUse, burnTime, curOurRalState, curTgtRalState, atDateTime);

                _newtonMovedb.DeltaVForManuver_FoRO_m = manuverVector;
                _entityCommanding.Manager.ManagerSubpulses.AddEntityInterupt(atDateTime + TimeSpan.FromSeconds(5), nameof(OrderableProcessor), _entityCommanding);
                
            }
            else
            {
                _newtonMovedb.DeltaVForManuver_FoRO_m = new Vector3();
            }
            
        }

        Vector3 ManuverVector(
            double dvToUse, 
            double burnTime, 
            (Vector3 Position, Vector3 Velocity) ourState, 
            (Vector3 Position, Vector3 Velocity) tgtState, 
            DateTime atDateTime )
        {
            var distanceToTgt = (ourState.Position - tgtState.Position).Length();
            var tgtBearing = tgtState.Position - ourState.Position;

            double newttt = TimeToTarget(dvToUse, burnTime, distanceToTgt, ourState.Velocity, tgtState.Velocity);
            int itterations = 0;
            double oldttt = double.PositiveInfinity;
            while (oldttt > newttt && Math.Abs(newttt - oldttt) > 1) //itterate till we get a solution that's less than a second difference from last.
            {
                oldttt = newttt;

                TimeSpan timespanToIntercept = TimeSpan.MaxValue;
                if (newttt * 10000000 <= long.MaxValue)
                {
                    timespanToIntercept = TimeSpan.FromSeconds(newttt);
                }
                DateTime futureDate = atDateTime + timespanToIntercept;
                var futurePosition = Entity.GetRalitiveFuturePosition(_targetEntity, futureDate);
                    
                tgtBearing = futurePosition - ourState.Position;
                distanceToTgt = (tgtBearing).Length();

                newttt = TimeToTarget(dvToUse, burnTime, distanceToTgt, ourState.Velocity, tgtState.Velocity);
                itterations++;

            }
            
            var vectorToTgt = Vector3.Normalise(tgtBearing);
            var deltaVVector = vectorToTgt * dvToUse;
            
            /*
            Vector3 manuverVector = OrbitMath.GlobalToOrbitVector(
                deltaVVector, 
                ourState.Position, 
                ourState.Velocity);
            
            
            
            var myMass = _newtonAbilityDB.DryMass_kg + _newtonAbilityDB.TotalFuel_kg;
            var sgp = OrbitMath.CalculateStandardGravityParameterInM3S2(myMass, _soiParentMass);
            
            var manuverVector = OrbitMath.ParentToProgradeVector(
                sgp, 
                deltaVVector, 
                ourState.Position, 
                ourState.Velocity);
             */
            
            //So now I'm thrusting in the direction of the target's future position,
            //not thrusting in a direction that'll get me to that position.
            return vectorToTgt * dvToUse;//manuverVector; 
        }


        double TimeToTarget(double dvToUse, double burnTime, double distanceToTgt, Vector3 ourVelocity, Vector3 targetVelocity)
        {
            
            double acceleration = dvToUse / burnTime;
            //not fully accurate since we're not calculating for jerk.
            var distanceWhileAcclerating = 1.5 * acceleration * burnTime * burnTime;
            double timeToTarget;
            if(distanceWhileAcclerating >  distanceToTgt)
            {
                distanceWhileAcclerating = distanceToTgt;
                timeToTarget = Math.Sqrt(distanceToTgt / (1.5 * acceleration));
            }
            else
            {
                Vector3 leadToTgt = targetVelocity - ourVelocity;
                var closingSpeed = leadToTgt.Length() + dvToUse;
                var timeAtFullVelocity = ((distanceToTgt - distanceWhileAcclerating) / closingSpeed);
                timeToTarget = timeAtFullVelocity + burnTime ;
            }

            return timeToTarget;
        }

        public override bool IsFinished()
        {
            if (IsRunning && _newtonMovedb.DeltaVForManuver_FoRO_m.Length() <= 0)
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


    
    /// <summary>
    /// This was an alternate attempt to intecept by aplying thrust 90 degrees to the current direction of travel...
    /// or something. never fully completed. Delete?
    /// </summary>
    public class Thrust90ToTargetCmd : EntityCommand
    {
        public override string Name { get; } = "Nav: Intercept/Collide with target";

        public override string Details
        {
            get
            {
                string targetName = _targetEntity.GetDataBlob<NameDB>().GetName(_factionEntity);
                return "Attempting intercept on + " + targetName + ", with " + Stringify.Velocity(_newtonAbilityDB.DeltaV) + "Δv remaining.";
            }
        }
        
        public override int ActionLanes => 1;
        public override bool IsBlocking => true;

        Entity _factionEntity;
        Entity _entityCommanding;
        private OrdnanceDesign _missileDesign;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        private Entity _targetEntity;
        NewtonMoveDB _newtonMovedb;
        NewtonThrustAbilityDB _newtonAbilityDB;
        private double _startDV;
        private double _startBurnTime;
        private double _fuelBurnRate;
        private double _totalFuel;
        private double _soiParentMass;
        
        public static void CreateCommand(Guid faction, Entity orderEntity, DateTime actionDateTime, Entity targetEntity)
        {
            var cmd = new Thrust90ToTargetCmd()
            {
                RequestingFactionGuid = faction,
                EntityCommandingGuid = orderEntity.Guid,
                CreatedDate = orderEntity.StarSysDateTime,
                _targetEntity = targetEntity,
                ActionOnDate = actionDateTime,
            };
            StaticRefLib.Game.OrderHandler.HandleOrder(cmd);
        }

        internal override void ActionCommand(DateTime atDateTime)
        {
            if(atDateTime < ActionOnDate)
                return;
            if (!IsRunning)
            {
                IsRunning = true;
                _newtonAbilityDB = _entityCommanding.GetDataBlob<NewtonThrustAbilityDB>();
                _startDV = _newtonAbilityDB.DeltaV;
                _fuelBurnRate = _newtonAbilityDB.FuelBurnRate;
                _totalFuel = _newtonAbilityDB.TotalFuel_kg;
                var soiParentEntity = Entity.GetSOIParentEntity(_entityCommanding);
                _soiParentMass = soiParentEntity.GetDataBlob<MassVolumeDB>().MassDry;
                var currentVel = Entity.GetRalitiveFutureVelocity(_entityCommanding, atDateTime);               
                if(_entityCommanding.HasDataBlob<OrbitDB>())
                _entityCommanding.RemoveDataBlob<OrbitDB>();
                if(_entityCommanding.HasDataBlob<OrbitUpdateOftenDB>())
                _entityCommanding.RemoveDataBlob<OrbitUpdateOftenDB>();
                if (_entityCommanding.HasDataBlob<NewtonMoveDB>())
                    _newtonMovedb = _entityCommanding.GetDataBlob<NewtonMoveDB>();
                else
                {
                    _newtonMovedb = new NewtonMoveDB(soiParentEntity, currentVel); 
                }
                
                _entityCommanding.SetDataBlob(_newtonMovedb);
            }
            var halfDV = _startDV * 0.5; //lets burn half the dv getting into a good intercept. 
            var dvUsed = _startDV - _newtonAbilityDB.DeltaV;
            var dvToUse = halfDV - dvUsed;
            if(dvToUse > 0)
            {
                (Vector3 pos, Vector3 Velocity) curOurRalState = Entity.GetRalitiveState(_entityCommanding);
                (Vector3 pos, Vector3 Velocity) curTgtRalState = Entity.GetRalitiveState(_targetEntity);
                var dvRemaining = _newtonAbilityDB.DeltaV;


                var myMass = _newtonAbilityDB.DryMass_kg + _newtonAbilityDB.TotalFuel_kg;
                var sgp = OrbitMath.CalculateStandardGravityParameterInM3S2(myMass, _soiParentMass);
            
                var vectorToTgtFromPrograde = OrbitMath.ParentToProgradeVector(
                    sgp, 
                    curTgtRalState.pos, 
                    curOurRalState.pos, 
                    curOurRalState.Velocity);
                
                
                var vttnorm = Vector3.Normalise(vectorToTgtFromPrograde);


            }
            else
            {
                _newtonMovedb.DeltaVForManuver_FoRO_m = new Vector3();
            }
            
        }

        Vector3 ManuverVector(
            double dvToUse, 
            double burnTime, 
            (Vector3 Position, Vector3 Velocity) ourState, 
            (Vector3 Position, Vector3 Velocity) tgtState, 
            DateTime atDateTime )
        {
            var distanceToTgt = (ourState.Position - tgtState.Position).Length();
            var tgtBearing = tgtState.Position - ourState.Position;
            var timeToIntecept = TimeToTarget(dvToUse, burnTime, distanceToTgt, ourState.Velocity, tgtState.Velocity);
            double newttt = 0;
            int itterations = 0;
            var oldttt = timeToIntecept;
            while (Math.Abs(newttt - oldttt) > 1) //itterate till we get a solution that's less than a second difference from last.
            {
                oldttt = newttt;

                TimeSpan timespanToIntercept = TimeSpan.MaxValue;
                if (timeToIntecept * 10000000 <= long.MaxValue)
                {
                    timespanToIntercept = TimeSpan.FromSeconds(timeToIntecept);
                }
                DateTime futureDate = atDateTime + timespanToIntercept;
                var futurePosition = Entity.GetRalitiveFuturePosition(_targetEntity, futureDate);
                    
                tgtBearing = futurePosition - ourState.Position;
                distanceToTgt = (tgtBearing).Length();

                newttt = TimeToTarget(dvToUse, burnTime, distanceToTgt, ourState.Velocity, tgtState.Velocity);
                itterations++;

            }
            
            var vectorToTgt = Vector3.Normalise(tgtBearing);
            var deltaVVector = vectorToTgt * dvToUse;
            
            /*
            
            var myMass = _newtonAbilityDB.DryMass_kg + _newtonAbilityDB.TotalFuel_kg;
            var sgp = OrbitMath.CalculateStandardGravityParameterInM3S2(myMass, _soiParentMass);
            
            var manuverVector = OrbitMath.ParentToProgradeVector(
                sgp, 
                deltaVVector, 
                ourState.Position, 
                ourState.Velocity);
            */
            //So now I'm thrusting in the direction of the target's future position,
            //not thrusting in a direction that'll get me to that position.
            return vectorToTgt * dvToUse;//manuverVector; 
        }


        double TimeToTarget(double dvToUse, double burnTime, double distanceToTgt, Vector3 ourVelocity, Vector3 targetVelocity)
        {
            
            double acceleration = dvToUse / burnTime;
            //not fully accurate since we're not calculating for jerk.
            var distanceWhileAcclerating = 1.5 * acceleration * burnTime * burnTime;
            double timeToTarget;
            if(distanceWhileAcclerating >  distanceToTgt)
            {
                distanceWhileAcclerating = distanceToTgt;
                timeToTarget = Math.Sqrt(distanceToTgt / (1.5 * acceleration));
            }
            else
            {
                Vector3 leadToTgt = targetVelocity - ourVelocity;
                var closingSpeed = leadToTgt.Length() + dvToUse;
                var timeAtFullVelocity = ((distanceToTgt - distanceWhileAcclerating) / closingSpeed);
                timeToTarget = timeAtFullVelocity + burnTime ;
            }

            return timeToTarget;
        }

        public override bool IsFinished()
        {
            if (IsRunning && _newtonMovedb.DeltaVForManuver_FoRO_m.Length() <= 0)
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