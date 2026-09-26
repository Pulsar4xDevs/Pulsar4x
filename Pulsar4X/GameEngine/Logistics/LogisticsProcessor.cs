using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;
using Pulsar4X.Interfaces;
using Pulsar4X.DataStructures;
using Pulsar4X.Extensions;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Orbits;
using Pulsar4X.Storage;
using Pulsar4X.Galaxy;
using Pulsar4X.Movement;

namespace Pulsar4X.Logistics;

public class LogiBaseProcessor : IHotloopProcessor
{
    public TimeSpan RunFrequency
    {
        get { return TimeSpan.FromHours(1); }
    }

    public TimeSpan FirstRunOffset => TimeSpan.FromHours(0.25);

    public Type GetParameterType => typeof(LogiBaseDB);

    public void Init(Game game)
    {

    }

    public void ProcessEntity(Entity entity, int deltaSeconds)
    {
    }

    public int ProcessManager(EntityManager manager, int deltaSeconds)
    {
        return manager.GetAllDataBlobsOfType<LogiBaseDB>().Count;
    }
}

public class LogiShipProcessor : IHotloopProcessor
{
    public TimeSpan RunFrequency
    {
        get { return TimeSpan.FromHours(1); }
    }

    public TimeSpan FirstRunOffset => TimeSpan.FromHours(0);

    public Type GetParameterType => typeof(LogiShipperDB);

    public void Init(Game game)
    {

    }

    public void ProcessEntity(Entity entity, int deltaSeconds)
    {
    }

    public int ProcessManager(EntityManager manager, int deltaSeconds)
    {
        return manager.GetAllEntitiesWithDataBlob<LogiShipperDB>().Count;
    }
}
public static class LogisticsCycle
{
    public static List<Entity> ShippingEntites = new List<Entity>();

    public static void Clear()
    {
        ShippingEntites.Clear();
    }

    public class CargoTask
    {
        public double Profit = 0;
        public Entity Source;
        public Entity Destination;

        public ICargoable item;

        public long NumberOfItems;
        public double timeInSeconds;
        public double fuelUseDV;
    }


    public static void LogiShipBidding(Entity shippingEntity, List<LogiBaseDB> tradingBases)
    {
    }

    public static void LogiBaseBidding(LogiBaseDB tradeBase)
    {
    }

    public static (ManuverState endState, double fuelBurned) Manuvers(Entity ship, Entity cur, Entity target, ManuverState startState)
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

        (ManuverState mstate, double fuelBurned) mfstate = (startState, fuelUse);

        if (ship.GetSOIParentEntity() == target.GetSOIParentEntity())
        {
            var dvdif = CargoTransferProcessor.CalcDVDifference_m(target, ship);
            var cargoDBLeft = ship.GetDataBlob<CargoStorageDB>();
            var cargoDBRight = target.GetDataBlob<CargoStorageDB>();
            var dvMaxRangeDiff_ms = Math.Max(cargoDBLeft.TransferRangeDv_mps, cargoDBRight.TransferRangeDv_mps);

            if (target.HasDataBlob<ColonyInfoDB>())  //if target is a colony,
            {
                if (dvdif > dvMaxRangeDiff_ms * 0.01) //TODO:, whats the best dv dif for a colony on a planet? can we land? do we want to?
                {
                    if(ship.Manager.Game.Settings.StrictNewtonion)
                        mfstate = LogisticsNewtonion.ManuverToParentColony(ship, cur, target, startState);
                    else
                        mfstate = LogisticsSimple.ManuverToParentColony(ship, cur, target, startState);
                }
            }

            //we're moving between two objects who are in orbit, we shoudl be able to match orbit.
            else
            {
                if (dvdif > dvMaxRangeDiff_ms * 0.01)//if we're less than 10% of perfect
                {
                    if (ship.Manager.Game.Settings.StrictNewtonion)
                        mfstate = LogisticsNewtonion.ManuverToSiblingObject(ship, cur, target, startState);
                    else
                        mfstate = LogisticsSimple.ManuverToSiblingObject(ship, cur, target, startState);
                }
            }
        }
        else //if we're not orbiting the same parent as the source, we have to warpmove
        {
            if (ship.Manager.Game.Settings.StrictNewtonion)
                mfstate = LogisticsNewtonion.ManuverToExternalObject(ship, cur, target, startState);
            else
            {
                mfstate = LogisticsSimple.ManuverToExternalObject(ship, cur, target, startState);
            }
        }

        return mfstate;
    }
}