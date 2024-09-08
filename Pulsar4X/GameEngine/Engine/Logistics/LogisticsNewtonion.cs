using System;
using System.Collections.Generic;
using GameEngine.WarpMove;
using Pulsar4X.Orbital;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Extensions;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Engine.Logistics
{

    public static class LogisticsNewtonion
    {
        public static double TravelTimeToSource(Entity shippingEntity, LogiBaseDB tbase, OrbitDB odb, DateTime currentDateTime)
        {
            if(tbase.OwningEntity == null) throw new ArgumentNullException("LogiBaseDB cannot be null");

            double travelTimeToSource = 0;
            (Vector3 position, DateTime atDateTime) sourceIntercept;
            if (shippingEntity.HasDataBlob<WarpAbilityDB>())
            {
                sourceIntercept = WarpMath.GetInterceptPosition
                (
                    shippingEntity,
                    odb,
                    currentDateTime
                );
                travelTimeToSource = (shippingEntity.StarSysDateTime - sourceIntercept.atDateTime).TotalSeconds;
            }
            else
            {
                List<Entity> shipparents = new List<Entity>();
                Entity? soiParent = shippingEntity.GetSOIParentEntity();
                Entity soiroot = shippingEntity.GetDataBlob<PositionDB>().Root;

                if(soiParent == null) throw new NullReferenceException("soiParent cannot be null");
                shipparents.Add(soiParent);
                while (soiParent != soiroot)
                {
                    soiParent = soiParent.GetSOIParentEntity();
                    if(soiParent == null) throw new NullReferenceException("soiParent cannot be null");
                    shipparents.Add(soiParent);
                }


                Entity? soiTargetParent = tbase.OwningEntity.GetSOIParentEntity();
                Entity soiTargetRoot = tbase.OwningEntity.GetDataBlob<PositionDB>().Root;

                if(soiTargetParent == null) throw new NullReferenceException("soiTargetParent cannot be null");

                if (soiroot != soiTargetRoot)
                    throw new Exception("Impossibru!");//this should only happen if we're in different systems, need to eventualy handle that. else the tree has gotten fucked up
                List<Entity> soiTargetParents = new List<Entity>();
                soiTargetParents.Add(soiTargetParent);
                while (soiTargetParent != soiroot)
                {
                    soiTargetParent = soiTargetParent.GetSOIParentEntity();
                    if(soiTargetParent == null) throw new NullReferenceException("soiTargetParent cannot be null");
                    soiTargetParents.Add(soiTargetParent);

                }


                //we cycle through both lists from the top till the soi body doesn't match, the last one will be the shared parent.
                Entity sharedSOIBody = soiroot;
                int i = shipparents.Count - 1;
                int j = soiTargetParents.Count - 1;
                while (shipparents[i] == soiTargetParents[j])
                {
                    sharedSOIBody = shipparents[i];
                    i--;
                    j--;
                }
                double shipMass = shippingEntity.GetDataBlob<MassVolumeDB>().MassTotal;
                // double totalDeltaV = 0;
                // double TotalSeconds = 0;
                var pos = shippingEntity.GetRalitivePosition();
                var time = shippingEntity.StarSysDateTime;
                List<(double deltav, double secTillNextManuver)> dvandTimes = new List<(double deltav, double secTillNextManuver)>();
                double totalTimeInSeconds = 0;
                for (int k = 0; k < i; k++)
                {
                    var nextParent = shipparents[k];
                    var bodyMass = nextParent.GetDataBlob<MassVolumeDB>().MassTotal;
                    var sgp = GeneralMath.StandardGravitationalParameter(shipMass + bodyMass);
                    //var ke = OrbitMath.FromPosition(pos, sgp, time);
                    //var r1 = ke.SemiMajorAxis; //is this right? feel like it's not, but then is current position right
                    var r1 = pos.Length(); //or is this one right?
                    var r2 = nextParent.GetSOI_m();
                    var wca1 = Math.Sqrt(sgp / r1);
                    var wca2 = Math.Sqrt((2 * r2) / (r1 + r2)) - 1;
                    var dva = wca1 * wca2;
                    var timeToPeriaps = Math.PI * Math.Sqrt((Math.Pow(r1 + r2, 3)) / (8 * sgp));
                    dvandTimes.Add((dva, timeToPeriaps));
                    totalTimeInSeconds += timeToPeriaps;
                }
                for (int k = soiTargetParents.Count; k < 0; k--)
                {
                    var nextParent = soiTargetParents[k];
                    var bodyMass = nextParent.GetDataBlob<MassVolumeDB>().MassTotal;
                    var sgp = GeneralMath.StandardGravitationalParameter(shipMass + bodyMass);
                    //var ke = OrbitMath.FromPosition(pos, sgp, time);
                    //var r1 = ke.SemiMajorAxis; //is this right? feel like it's not, but then is current position right
                    var r1 = pos.Length(); //or is this one right?
                    var r2 = nextParent.GetSOI_m();
                    var wca1 = Math.Sqrt(sgp / r1);
                    var wca2 = Math.Sqrt((2 * r2) / (r1 + r2)) - 1;
                    var dva = wca1 * wca2;
                    var timeToPeriaps = Math.PI * Math.Sqrt((Math.Pow(r1 + r2, 3)) / (8 * sgp));
                    dvandTimes.Add((dva, timeToPeriaps));
                    totalTimeInSeconds += timeToPeriaps;
                }

                travelTimeToSource = totalTimeInSeconds;

            }

            return travelTimeToSource;

        }

        public static (ManuverState endState, double fuelBurned) ManuverToParentColony(Entity ship, Entity cur, Entity target, ManuverState startState)
        {


            var shipMass = startState.Mass;
            double tsec = 0;
            DateTime dateTime = startState.At;
            double fuelUse = 0;
            Vector3 pos = startState.Position;
            Vector3 vel = startState.Velocity;
            var targetBody = target.GetSOIParentEntity();
            if(targetBody == null) throw new NullReferenceException("targetBody cannot be null");

            //var myMass = ship.GetDataBlob<MassVolumeDB>().MassTotal;
            var tgtBdyMass = target.GetSOIParentEntity().GetDataBlob<MassVolumeDB>().MassTotal;
            var sgpTgtBdy = GeneralMath.StandardGravitationalParameter(shipMass + tgtBdyMass);
            var curBdyMass = cur.GetSOIParentEntity().GetDataBlob<MassVolumeDB>().MassTotal;
            var sgpCurBdy = GeneralMath.StandardGravitationalParameter(shipMass + curBdyMass);
            var ke = OrbitalMath.KeplerFromPositionAndVelocity(sgpCurBdy, startState.Position, startState.Velocity, startState.At);

            double mySMA = 0;
            if (ship.HasDataBlob<OrbitDB>())
                mySMA = ship.GetDataBlob<OrbitDB>().SemiMajorAxis;
            if (ship.HasDataBlob<OrbitUpdateOftenDB>())
                mySMA = ship.GetDataBlob<OrbitUpdateOftenDB>().SemiMajorAxis;
            if (ship.HasDataBlob<NewtonSimDB>())
                mySMA = ship.GetDataBlob<NewtonSimDB>().GetElements().SemiMajorAxis;

            double targetSMA = OrbitMath.LowOrbitRadius(targetBody);




            var manuvers = OrbitalMath.Hohmann2(sgpTgtBdy, mySMA, targetSMA);

            var cargoLibary = ship.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoGoods;
            NewtonSimCommand.CreateCommands(cargoLibary, ship, manuvers);

            foreach (var manvr in manuvers)
            {

                double vexhaust = ship.GetDataBlob<NewtonionThrustAbilityDB>().ExhaustVelocity;
                double fuelBurned = OrbitalMath.TsiolkovskyFuelUse(shipMass, vexhaust, manvr.deltaV.Length());
                tsec += manvr.timeInSeconds;
                dateTime = dateTime + TimeSpan.FromSeconds(tsec);
                fuelUse += fuelBurned;
                shipMass -= fuelBurned;

                var preManuverState = OrbitMath.GetStateVectors(ke, dateTime);
                pos = preManuverState.position;
                vel = new Vector3(preManuverState.velocity.X, preManuverState.velocity.Y, 0);
                vel += manvr.deltaV;

                ke = OrbitalMath.KeplerFromPositionAndVelocity(sgpCurBdy, pos, vel, dateTime);
                var postManuverState = OrbitMath.GetStateVectors(ke, dateTime);
                pos = postManuverState.position;
                vel = new Vector3(postManuverState.velocity.X, postManuverState.velocity.Y, 0);

            }

            ManuverState mstate = new ManuverState()
            {
                At = startState.At + TimeSpan.FromSeconds(tsec),
                Mass = shipMass,
                Position = pos,
                Velocity = vel

            };

            return (mstate, fuelUse);
        }

        public static (ManuverState endState, double fuelBurned) ManuverToSiblingObject(Entity ship, Entity cur, Entity target, ManuverState startState)
        {
            var shipMass = startState.Mass;
            double tsec = 0;
            DateTime dateTime = startState.At;
            double fuelUse = 0;
            Vector3 pos = startState.Position;
            Vector3 vel = startState.Velocity;

            //var myMass = ship.GetDataBlob<MassVolumeDB>().MassTotal;
            var tgtBdyMass = target.GetSOIParentEntity().GetDataBlob<MassVolumeDB>().MassTotal;
            var sgpTgtBdy = GeneralMath.StandardGravitationalParameter(shipMass + tgtBdyMass);
            var curBdyMass = cur.GetSOIParentEntity().GetDataBlob<MassVolumeDB>().MassTotal;
            var sgpCurBdy = GeneralMath.StandardGravitationalParameter(shipMass + curBdyMass);
            var ke = OrbitalMath.KeplerFromPositionAndVelocity(sgpCurBdy, startState.Position, startState.Velocity, startState.At);

            double mySMA = 0;
            if (ship.HasDataBlob<OrbitDB>())
                mySMA = ship.GetDataBlob<OrbitDB>().SemiMajorAxis;
            if (ship.HasDataBlob<OrbitUpdateOftenDB>())
                mySMA = ship.GetDataBlob<OrbitUpdateOftenDB>().SemiMajorAxis;
            if (ship.HasDataBlob<NewtonSimDB>())
                mySMA = ship.GetDataBlob<NewtonSimDB>().GetElements().SemiMajorAxis;


            double targetSMA = 0;
            if (target.HasDataBlob<OrbitDB>())
                targetSMA = target.GetDataBlob<OrbitDB>().SemiMajorAxis;
            if (target.HasDataBlob<OrbitUpdateOftenDB>())
                targetSMA = target.GetDataBlob<OrbitUpdateOftenDB>().SemiMajorAxis;
            if (target.HasDataBlob<NewtonSimDB>())
                targetSMA = target.GetDataBlob<NewtonSimDB>().GetElements().SemiMajorAxis;

            var manuvers = OrbitalMath.Hohmann2(sgpTgtBdy, mySMA, targetSMA);

            var cargoLibary = ship.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoGoods;
            NewtonSimCommand.CreateCommands(cargoLibary, ship, manuvers);

            foreach (var manvr in manuvers)
            {

                double ve = ship.GetDataBlob<NewtonionThrustAbilityDB>().ExhaustVelocity;
                double fuelBurned = OrbitalMath.TsiolkovskyFuelUse(shipMass, ve, manvr.deltaV.Length());
                tsec += manvr.timeInSeconds;
                dateTime = dateTime + TimeSpan.FromSeconds(tsec);
                fuelUse += fuelBurned;
                shipMass -= fuelBurned;


                var preManuverState = OrbitMath.GetStateVectors(ke, dateTime);
                pos = preManuverState.position;
                vel = new Vector3(preManuverState.velocity.X, preManuverState.velocity.Y, 0);
                vel += manvr.deltaV;

                ke = OrbitalMath.KeplerFromPositionAndVelocity(sgpCurBdy, pos, vel, dateTime);
                var postManuverState = OrbitMath.GetStateVectors(ke, dateTime);
                pos = postManuverState.position;
                vel = new Vector3(postManuverState.velocity.X, postManuverState.velocity.Y, 0);

            }

            ManuverState mstate = new ManuverState()
            {
                At = startState.At + TimeSpan.FromSeconds(tsec),
                Mass = shipMass,
                Position = pos,
                Velocity = vel

            };

            return (mstate, fuelUse);

        }

        public static (ManuverState endState, double fuelBurned) ManuverToExternalObject(Entity ship, Entity cur, Entity target, ManuverState startState)
        {
            var shipMass = startState.Mass;
            double tsec = 0;
            DateTime dateTime = startState.At;
            double fuelUse = 0;
            Vector3 pos = startState.Position;
            Vector3 vel = startState.Velocity;
            var targetBody = target.GetSOIParentEntity();

            if(targetBody == null) throw new NullReferenceException("targetBody cannot be null");

            //var myMass = ship.GetDataBlob<MassVolumeDB>().MassTotal;
            var tgtBdyMass = target.GetSOIParentEntity().GetDataBlob<MassVolumeDB>().MassTotal;
            var sgpTgtBdy = GeneralMath.StandardGravitationalParameter(shipMass + tgtBdyMass);
            var curBdyMass = cur.GetSOIParentEntity().GetDataBlob<MassVolumeDB>().MassTotal;
            var sgpCurBdy = GeneralMath.StandardGravitationalParameter(shipMass + curBdyMass);
            var ke = OrbitalMath.KeplerFromPositionAndVelocity(sgpCurBdy, startState.Position, startState.Velocity, startState.At);

            double mySMA = 0;
            if (ship.HasDataBlob<OrbitDB>())
                mySMA = ship.GetDataBlob<OrbitDB>().SemiMajorAxis;
            if (ship.HasDataBlob<OrbitUpdateOftenDB>())
                mySMA = ship.GetDataBlob<OrbitUpdateOftenDB>().SemiMajorAxis;
            if (ship.HasDataBlob<NewtonSimDB>())
                mySMA = ship.GetDataBlob<NewtonSimDB>().GetElements().SemiMajorAxis;

            double targetRad = OrbitMath.LowOrbitRadius(targetBody);

            //This is wrong, we need the state for where we WILL BE at this point. 
            //this is getting our state *now* but we may be doing manuvers before we get to this manuver. 
            //var ourState = ship.GetRelativeState(); 

            //We should be inserting at a position where our velocity is 90deg from our position to the parent.
            //this should mean we only require a retrograde thrust to circularise.
            //we're assuming we have enough thrust(acceleration) to do this manuver in one go. (TODO handle better)

            //var departTime = ship.StarSysDateTime;
            OrbitDB targetOrbit = targetBody.GetDataBlob<OrbitDB>();
            (Vector3 position, DateTime eti) targetIntercept = WarpMath.GetInterceptPosition(ship, targetOrbit, startState.At);
            Vector3 insertionVector = OrbitProcessor.GetOrbitalInsertionVector(startState.Velocity, targetOrbit, targetIntercept.eti);
            var insertionSpeed = insertionVector.Length();
            var idealSpeed = Math.Sqrt(targetRad / sgpTgtBdy);//for a circular orbit
            var deltaV = insertionSpeed - idealSpeed;

            var targetInsertionPosition = Vector3.Normalise(startState.Velocity) * targetRad;
            var thrustVector = Vector3.Normalise(insertionVector) * -deltaV;

            var thrustV2 = OrbitalMath.ProgradeToStateVector(sgpTgtBdy, thrustVector, targetInsertionPosition, insertionVector);
            //should we expend deltaV now or when we get there?


            var cargoLibary = ship.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoGoods;
            var cmd = WarpMoveCommand.CreateCommand(
                cargoLibary,
                ship.FactionOwnerID,
                ship,
                targetBody,
                targetInsertionPosition,
                startState.At,
                thrustV2,
                shipMass);

            var dv = cmd.Item2.OrbitrelativeDeltaV.Length();

            double ve = ship.GetDataBlob<NewtonionThrustAbilityDB>().ExhaustVelocity;
            double fuelBurned = OrbitalMath.TsiolkovskyFuelUse(shipMass, ve, dv);
            tsec += OrbitMath.BurnTime(ship, dv, shipMass);
            dateTime = dateTime + TimeSpan.FromSeconds(tsec);
            fuelUse += fuelBurned;
            shipMass -= fuelBurned;


            var preManuverState = OrbitMath.GetStateVectors(ke, dateTime);
            pos = preManuverState.position;
            vel = new Vector3(preManuverState.velocity.X, preManuverState.velocity.Y, 0);
            vel += thrustV2;

            ke = OrbitalMath.KeplerFromPositionAndVelocity(sgpCurBdy, pos, vel, dateTime);
            var postManuverState = OrbitMath.GetStateVectors(ke, dateTime);
            pos = postManuverState.position;
            vel = new Vector3(postManuverState.velocity.X, postManuverState.velocity.Y, 0);

            //NewtonSimCommand.CreateCommand(ship.FactionOwner, ship, targetIntercept.eti, thrustVector, "Thrust: Circularize");
            //var secFromNow = targetIntercept.eti - ship.StarSysDateTime;
            //var circ = NewtonSimCommand.CreateCommand(ship, (thrustVector, secFromNow.TotalSeconds));
            //circ.DebugDetails.Add(("Insertion speed", insertionSpeed));
            //circ.DebugDetails.Add(("Ideal speed", idealSpeed));
            //circ.DebugDetails.Add(("ThrustVector x", thrustVector.X));
            //circ.DebugDetails.Add(("ThrustVector y", thrustVector.Y));

            ManuverState mstate = new ManuverState()
            {
                At = startState.At + TimeSpan.FromSeconds(tsec),
                Mass = shipMass,
                Position = pos,
                Velocity = vel

            };

            return (mstate, fuelUse);
        }

    }
}