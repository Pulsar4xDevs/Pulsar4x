using System;
using System.Collections.Generic;
using Pulsar4X.ECSLib.ComponentFeatureSets.Missiles;
using Pulsar4X.Orbital;

namespace Pulsar4X.ECSLib
{

    public class NewtonThrustCommand : EntityCommand
    {
        public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement;
        public override bool IsBlocking => true;
        public override string Name { get {return _name;}}

        string _name = "Newtonion thrust";

        public override string Details
        {
            get
            {
                return _details;
            }
        }
        string _details = "";

        Entity _factionEntity;
        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        public Vector3 OrbitrelativeDeltaV;
        //private Vector3 _parentRalitiveDeltaV;
        NewtonMoveDB _db;

        DateTime _vectorDateTime;

        public List<(string item, double value)> DebugDetails = new List<(string, double)>(); 

        public static void CreateCommand(Guid faction, Entity orderEntity, DateTime manuverNodeTime, Vector3 expendDeltaV_m, double burnTime, string name="Newtonion thrust")
        {



            var cmd = new NewtonThrustCommand()
            {
                RequestingFactionGuid = faction,
                EntityCommandingGuid = orderEntity.Guid,
                CreatedDate = orderEntity.Manager.ManagerSubpulses.StarSysDateTime,
                OrbitrelativeDeltaV = expendDeltaV_m,

                
                //var sgp = OrbitalMath.CalculateStandardGravityParameterInM3S2()

                //_parentRalitiveDeltaV = pralitiveDV,
                _vectorDateTime = manuverNodeTime,
                ActionOnDate = manuverNodeTime - TimeSpan.FromSeconds(burnTime * 0.5),
                _name = name,

            };
            
            StaticRefLib.Game.OrderHandler.HandleOrder(cmd);
            cmd.UpdateDetailString();
        }

        public static void CreateCommands(Entity ship, (Vector3 dv, double t)[] manuvers)
        {
            var fuelTypeID = ship.GetDataBlob<NewtonThrustAbilityDB>().FuelType;
            var fuelType = StaticRefLib.StaticData.CargoGoods.GetAny(fuelTypeID);
            var burnRate = ship.GetDataBlob<NewtonThrustAbilityDB>().FuelBurnRate;
            var exhaustVelocity = ship.GetDataBlob<NewtonThrustAbilityDB>().ExhaustVelocity;
            var mass = ship.GetDataBlob<MassVolumeDB>().MassTotal;
            var tnow = ship.StarSysDateTime;
            

            foreach (var manuver in manuvers)
            {
                var tmanuver = tnow + TimeSpan.FromSeconds(manuver.t);
                double fuelBurned = OrbitMath.TsiolkovskyFuelUse(mass, exhaustVelocity, manuver.dv.Length());
                double tburn = fuelBurned / burnRate;
                mass -= fuelBurned;

                var cmd = new NewtonThrustCommand()
                {
                    RequestingFactionGuid = ship.FactionOwner,
                    EntityCommandingGuid = ship.Guid,
                    CreatedDate = ship.Manager.ManagerSubpulses.StarSysDateTime,
                    OrbitrelativeDeltaV = manuver.dv,

                    _vectorDateTime = tmanuver,
                    ActionOnDate = tmanuver - TimeSpan.FromSeconds(tburn * 0.5),
                };

                StaticRefLib.Game.OrderHandler.HandleOrder(cmd);
                cmd.UpdateDetailString();
            }
        }

        public static NewtonThrustCommand CreateCommand(Entity ship, (Vector3 dv, double t) manuver)
        {
            var fuelTypeID = ship.GetDataBlob<NewtonThrustAbilityDB>().FuelType;
            var fuelType = StaticRefLib.StaticData.CargoGoods.GetAny(fuelTypeID);
            var burnRate = ship.GetDataBlob<NewtonThrustAbilityDB>().FuelBurnRate;
            var exhaustVelocity = ship.GetDataBlob<NewtonThrustAbilityDB>().ExhaustVelocity;
            var mass = ship.GetDataBlob<MassVolumeDB>().MassTotal;
            var tnow = ship.StarSysDateTime;

                var tmanuver = tnow + TimeSpan.FromSeconds(manuver.t);
                double fuelBurned = OrbitMath.TsiolkovskyFuelUse(mass, exhaustVelocity, manuver.dv.Length());
                double tburn = fuelBurned / burnRate;
                mass -= fuelBurned;

                var cmd = new NewtonThrustCommand()
                {
                    RequestingFactionGuid = ship.FactionOwner,
                    EntityCommandingGuid = ship.Guid,
                    CreatedDate = ship.Manager.ManagerSubpulses.StarSysDateTime,
                    OrbitrelativeDeltaV = manuver.dv,

                    _vectorDateTime = tmanuver,
                    ActionOnDate = tmanuver - TimeSpan.FromSeconds(tburn * 0.5),
                };

                StaticRefLib.Game.OrderHandler.HandleOrder(cmd);
                cmd.UpdateDetailString();

                return cmd;

        }
        
        public static NewtonThrustCommand CreateCommand(Entity ship, Vector3 dv, DateTime tmanuver)
        {
            var fuelTypeID = ship.GetDataBlob<NewtonThrustAbilityDB>().FuelType;
            var fuelType = StaticRefLib.StaticData.CargoGoods.GetAny(fuelTypeID);
            var burnRate = ship.GetDataBlob<NewtonThrustAbilityDB>().FuelBurnRate;
            var exhaustVelocity = ship.GetDataBlob<NewtonThrustAbilityDB>().ExhaustVelocity;
            var mass = ship.GetDataBlob<MassVolumeDB>().MassTotal;
            var tnow = ship.StarSysDateTime;

            
            double fuelBurned = OrbitMath.TsiolkovskyFuelUse(mass, exhaustVelocity, dv.Length());
            double tburn = fuelBurned / burnRate;
            mass -= fuelBurned;

            var cmd = new NewtonThrustCommand()
            {
                RequestingFactionGuid = ship.FactionOwner,
                EntityCommandingGuid = ship.Guid,
                CreatedDate = ship.Manager.ManagerSubpulses.StarSysDateTime,
                OrbitrelativeDeltaV = dv,

                _vectorDateTime = tmanuver,
                ActionOnDate = tmanuver - TimeSpan.FromSeconds(tburn * 0.5),
            };

            StaticRefLib.Game.OrderHandler.HandleOrder(cmd);
            cmd.UpdateDetailString();

            return cmd;

        }

        internal override void ActionCommand(DateTime atDateTime)
        {
            if (!IsRunning && atDateTime >= ActionOnDate)
            {
                 var parent = _entityCommanding.GetSOIParentEntity();
                 var currentVel = _entityCommanding.GetRelativeFutureVelocity(_vectorDateTime);

                var parentMass = _entityCommanding.GetSOIParentEntity().GetDataBlob<MassVolumeDB>().MassTotal;
                var myMass = _entityCommanding.GetDataBlob<MassVolumeDB>().MassTotal;
                var sgp = OrbitalMath.CalculateStandardGravityParameterInM3S2(myMass, parentMass);

                var futurePosition = _entityCommanding.GetRelativeFuturePosition(_vectorDateTime);
                var futureVector = _entityCommanding.GetRelativeFutureVelocity(_vectorDateTime);
                var pralitiveDV = OrbitalMath.ProgradeToParentVector(sgp, OrbitrelativeDeltaV, futurePosition, futureVector);
                



                _db = new NewtonMoveDB(parent, currentVel);
                _db.ActionOnDateTime = ActionOnDate;
                _db.ManuverDeltaV = pralitiveDV;
                _entityCommanding.SetDataBlob(_db);

                UpdateDetailString();
                IsRunning = true;
            }
        }

        public override void UpdateDetailString()
        {
            
                
            if(ActionOnDate > _entityCommanding.StarSysDateTime)
                _details = "Waiting " + (ActionOnDate - _entityCommanding.StarSysDateTime).ToString("d'd 'h'h 'm'm 's's'") + "\n" 
                + "   to expend  " + Stringify.Velocity(OrbitrelativeDeltaV.Length()) + " Δv";
            else if(IsRunning)
                _details = "Expending " + Stringify.Velocity(_db.ManuverDeltaVLen) + " Δv";
                
            
               
        }

        public override bool IsFinished()
        {
            if (IsRunning && _db.ManuverDeltaV.Length() == 0)
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

        public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement;
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
                var soiParentEntity = _entityCommanding.GetSOIParentEntity();
                _soiParentMass = soiParentEntity.GetDataBlob<MassVolumeDB>().MassDry;
                var currentVel = _entityCommanding.GetRelativeFutureVelocity(atDateTime);
                if (_entityCommanding.HasDataBlob<NewtonMoveDB>())
                {
                    _newtonMovedb = _entityCommanding.GetDataBlob<NewtonMoveDB>();
                }
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
                (Vector3 Position, Vector3 Velocity) curOurRalState = _entityCommanding.GetRelativeState();
                (Vector3 Position, Vector3 Velocity) curTgtRalState = _targetEntity.GetRelativeState();
                var dvRemaining = _newtonAbilityDB.DeltaV;
                
                var tgtVelocity = _targetEntity.GetAbsoluteFutureVelocity(atDateTime);
                //calculate the differencecs in velocity vectors.
                Vector3 leadToTgt = (curTgtRalState.Velocity - curOurRalState.Velocity);
                 
                //convert the lead to an orbit relative (prograde Y) vector. 
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

                _newtonMovedb.ManuverDeltaV = manuverVector; //TODO: this is going to be even more broken now. it used to be using the prograde vector reference and now is using parent/
                _entityCommanding.Manager.ManagerSubpulses.AddEntityInterupt(atDateTime + TimeSpan.FromSeconds(5), nameof(OrderableProcessor), _entityCommanding);
                
            }
            else
            {
                _newtonMovedb.ManuverDeltaV = new Vector3();
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
                var futurePosition = _targetEntity.GetRelativeFuturePosition(futureDate);
                    
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
            if (IsRunning && _newtonMovedb.ManuverDeltaV.Length() <= 0)
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

        public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement;
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
                var soiParentEntity = _entityCommanding.GetSOIParentEntity();
                _soiParentMass = soiParentEntity.GetDataBlob<MassVolumeDB>().MassDry;
                var currentVel = _entityCommanding.GetRelativeFutureVelocity(atDateTime);               
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
                (Vector3 pos, Vector3 Velocity) curOurRalState = _entityCommanding.GetRelativeState();
                (Vector3 pos, Vector3 Velocity) curTgtRalState = _targetEntity.GetRelativeState();
                var dvRemaining = _newtonAbilityDB.DeltaV;


                var myMass = _entityCommanding.GetDataBlob<MassVolumeDB>().MassTotal;
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
                _newtonMovedb.ManuverDeltaV = new Vector3();
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
                var futurePosition = _targetEntity.GetRelativeFuturePosition(futureDate);
                    
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
            if (IsRunning && _newtonMovedb.ManuverDeltaV.Length() <= 0)
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