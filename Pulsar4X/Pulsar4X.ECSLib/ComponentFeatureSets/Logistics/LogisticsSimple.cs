using System;
using Pulsar4X.Orbital;

namespace Pulsar4X.ECSLib
{

    public static class LogisticsSimple
    {
        public static double TravelTimeToSource(Entity shippingEntity, LogiBaseDB tbase, OrbitDB odb, DateTime currentDateTime)
        {
            double travelTimeToSource = 0;
            (Vector3 position, DateTime atDateTime) sourceIntercept;
            if (shippingEntity.HasDataBlob<WarpAbilityDB>())
            {
                sourceIntercept = OrbitProcessor.GetInterceptPosition(shippingEntity, odb, currentDateTime);
                travelTimeToSource = (shippingEntity.StarSysDateTime - sourceIntercept.atDateTime).TotalSeconds;
            }
            else
                travelTimeToSource = double.PositiveInfinity;

            return travelTimeToSource;
        }

        public static (ManuverState endState, double fuelBurned) ManuverToParentColony(Entity ship, Entity cur, Entity target, ManuverState startState)
        {
            var shipMass = startState.Mass;
            // double tsec = 0;
            DateTime dateTime = startState.At;
            double fuelUse = 0;
            Vector3 pos = startState.Position;
            Vector3 vel = startState.Velocity;
            var targetBody = target.GetSOIParentEntity();

            //var myMass = ship.GetDataBlob<MassVolumeDB>().MassTotal;
            var tgtBdyMass = target.GetSOIParentEntity().GetDataBlob<MassVolumeDB>().MassTotal;
            var sgpTgtBdy = GeneralMath.StandardGravitationalParameter(shipMass + tgtBdyMass);
            var curBdyMass = cur.GetSOIParentEntity().GetDataBlob<MassVolumeDB>().MassTotal;
            var sgpCurBdy = GeneralMath.StandardGravitationalParameter(shipMass + curBdyMass);
            var ke = OrbitalMath.KeplerFromPositionAndVelocity(sgpCurBdy, startState.Position, startState.Velocity, startState.At);
            var sgptgt = GeneralMath.StandardGravitationalParameter(shipMass + tgtBdyMass);

            double mySMA = 0;
            if (ship.HasDataBlob<OrbitDB>())
                mySMA = ship.GetDataBlob<OrbitDB>().SemiMajorAxis;
            if (ship.HasDataBlob<OrbitUpdateOftenDB>())
                mySMA = ship.GetDataBlob<OrbitUpdateOftenDB>().SemiMajorAxis;
            if (ship.HasDataBlob<NewtonMoveDB>())
                mySMA = ship.GetDataBlob<NewtonMoveDB>().GetElements().SemiMajorAxis;

            double targetSMA = OrbitMath.LowOrbitRadius(targetBody);

            Vector3 targetPos = Vector3.Normalise(pos) * targetSMA;

            var cmd = WarpMoveCommand.CreateCommand(
                ship.FactionOwnerID,
                ship,
                targetBody,
                targetPos,
                startState.At,
                new Vector3(),
                shipMass);

            var s = ship.GetDataBlob<WarpAbilityDB>().MaxSpeed;
            var d = pos.Length() - targetSMA;
            var t = d / s;

            //we can use 0 for many of these since we're looking at a circular orbit. 
            var velVec = OrbitalMath.ParentLocalVeclocityVector(sgptgt, targetPos, targetSMA, 0, 0, 0, 0, 0);

            ManuverState mstate = new ManuverState()
            {
                At = startState.At + TimeSpan.FromSeconds(t),
                Mass = shipMass,
                Position = targetPos,
                Velocity = velVec
            };

            return (mstate, fuelUse);
        }

        public static (ManuverState endState, double fuelBurned) ManuverToSiblingObject(Entity ship, Entity cur, Entity target, ManuverState startState)
        {
            var shipMass = startState.Mass;
            // double tsec = 0;
            DateTime dateTime = startState.At;
            double fuelUse = 0;
            Vector3 pos = startState.Position;
            Vector3 vel = startState.Velocity;
            var targetBody = target.GetSOIParentEntity();

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
            if (ship.HasDataBlob<NewtonMoveDB>())
                mySMA = ship.GetDataBlob<NewtonMoveDB>().GetElements().SemiMajorAxis;


            double targetSMA = 0;
            if (target.HasDataBlob<OrbitDB>())
                targetSMA = target.GetDataBlob<OrbitDB>().SemiMajorAxis;
            if (target.HasDataBlob<OrbitUpdateOftenDB>())
                targetSMA = target.GetDataBlob<OrbitUpdateOftenDB>().SemiMajorAxis;
            if (target.HasDataBlob<NewtonMoveDB>())
                targetSMA = target.GetDataBlob<NewtonMoveDB>().GetElements().SemiMajorAxis;

            Vector3 targetPos = Vector3.Normalise(pos) * targetSMA;

            var cmd = WarpMoveCommand.CreateCommand(
                ship.FactionOwnerID,
                ship,
                targetBody,
                targetPos,
                startState.At,
                new Vector3(),
                shipMass);

            (Vector3 position, DateTime atDateTime) targetIntercept = OrbitProcessor.GetInterceptPosition
            (
                ship,
                target.GetDataBlob<OrbitDB>(),
                startState.At
            );

            ManuverState mstate = new ManuverState()
            {
                At = targetIntercept.atDateTime,
                Mass = shipMass,
                Position = targetIntercept.position,
                Velocity = target.GetRelativeFutureVelocity(targetIntercept.atDateTime)
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

            //var myMass = ship.GetDataBlob<MassVolumeDB>().MassTotal;
            var tgtBdyMass = target.GetSOIParentEntity().GetDataBlob<MassVolumeDB>().MassTotal;
            var sgpTgtBdy = GeneralMath.StandardGravitationalParameter(shipMass + tgtBdyMass);
            var curBdyMass = cur.GetSOIParentEntity().GetDataBlob<MassVolumeDB>().MassTotal;
            var sgpCurBdy = GeneralMath.StandardGravitationalParameter(shipMass + curBdyMass);
            var startKE = OrbitalMath.KeplerFromPositionAndVelocity(sgpCurBdy, startState.Position, startState.Velocity, startState.At);

            double mySMA = 0;
            if (ship.HasDataBlob<OrbitDB>())
                mySMA = ship.GetDataBlob<OrbitDB>().SemiMajorAxis;
            if (ship.HasDataBlob<OrbitUpdateOftenDB>())
                mySMA = ship.GetDataBlob<OrbitUpdateOftenDB>().SemiMajorAxis;
            if (ship.HasDataBlob<NewtonMoveDB>())
                mySMA = ship.GetDataBlob<NewtonMoveDB>().GetElements().SemiMajorAxis;

            double targetSMA = OrbitMath.LowOrbitRadius(targetBody);

            double targetRad = OrbitMath.LowOrbitRadius(targetBody);


            //var departTime = ship.StarSysDateTime;
            OrbitDB targetOrbit = targetBody.GetDataBlob<OrbitDB>();
            (Vector3 position, DateTime eti) targetIntercept = OrbitProcessor.GetInterceptPosition(ship, targetOrbit, startState.At);
            //get newtonion insertion vector (probibly our startState.Velocity)
            Vector3 insertionVector = OrbitProcessor.GetOrbitalInsertionVector(startState.Velocity, targetOrbit, targetIntercept.eti);
            var insertionSpeed = insertionVector.Length();
            var idealSpeed = Math.Sqrt(targetRad / sgpTgtBdy);//for a circular orbit
            var deltaV = insertionSpeed - idealSpeed;

            var ivecAng = Math.Atan2(insertionVector.Y, insertionVector.X);
            var iAng = Angle.NormaliseRadians(ivecAng - Math.PI * 0.5);
            var xpos = Math.Sin(iAng) * targetRad;
            var ypos = Math.Cos(iAng) * targetRad;
            
            var thrustVector = Vector3.Normalise(insertionVector) * -deltaV;
            
            //position end of warp so we're 90 degrees to our newtonion insertion vector
            var targetInsertionPosition = new Vector3(xpos, ypos, 0);
            var thrustV2 = OrbitalMath.ProgradeToStateVector(sgpTgtBdy, thrustVector, targetInsertionPosition, insertionVector);
            
            /*
            var cmd = WarpMoveCommand.CreateCommand(
                ship.FactionOwnerID,
                ship,
                targetBody,
                targetInsertionPosition,
                startState.At,
                thrustV2,
                shipMass);

            
            */
            var dv = thrustV2.Length();
            //KeplerElements endKE;

            KeplerElements postWarpKE = OrbitMath.KeplerFromPositionAndVelocity(sgpTgtBdy, targetInsertionPosition, insertionVector, targetIntercept.eti);
            
            
            
            double ve = ship.GetDataBlob<NewtonThrustAbilityDB>().ExhaustVelocity;
            double fuelBurned = OrbitalMath.TsiolkovskyFuelUse(shipMass, ve, dv);
            tsec += OrbitMath.BurnTime(ship, dv, shipMass);
            dateTime = dateTime + TimeSpan.FromSeconds(tsec);
            fuelUse += fuelBurned;
            shipMass -= fuelBurned;

            var preManuverState = OrbitMath.GetStateVectors(startKE, dateTime);
            pos = preManuverState.position;
            vel = new Vector3(preManuverState.velocity.X, preManuverState.velocity.Y, 0);
            vel += thrustV2;

            var endKE = OrbitalMath.KeplerFromPositionAndVelocity(sgpCurBdy, pos, vel, dateTime);
            var postManuverState = OrbitMath.GetStateVectors(startKE, dateTime);
            pos = postManuverState.position;
            vel = new Vector3(postManuverState.velocity.X, postManuverState.velocity.Y, 0);

            if(!ship.TryGetDatablob<NavSequenceDB>(out NavSequenceDB navDB))
            {
                navDB = new NavSequenceDB();
                ship.SetDataBlob(navDB);
            }

            DateTime startWarpTime = startState.At;
            DateTime endWarpTime = targetIntercept.eti;
            DateTime endNewtonThrustTime = targetIntercept.eti;
            
            navDB.AddManuver(
                Manuver.ManuverType.Warp, 
                startWarpTime,
                cur,
                startKE,
                endWarpTime,
                targetBody,
                postWarpKE
            );
            navDB.AddManuver(
                Manuver.ManuverType.NewtonSimple, 
                endWarpTime,
                targetBody,
                postWarpKE,
                endNewtonThrustTime,
                targetBody,
                endKE
            );
            
            
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