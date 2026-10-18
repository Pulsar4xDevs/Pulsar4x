using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Fleets;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Orbital;

namespace Pulsar4X.Tests
{
    public class MovementActionCloneTests
    {
        [Test]
        public void Clone_CopiesTheOrderAndDropsRuntimeState()
        {
            var warp = new WarpMoveAction
            {
                TargetEntityGuid = 7,
                EndpointRelitivePosition = new Vector3(3, 4, 0),
            };
            var warpCopy = (WarpMoveAction)warp.Clone();
            Assert.AreNotSame(warp, warpCopy);
            Assert.AreNotEqual(warp.CmdID, warpCopy.CmdID);
            Assert.AreEqual(7, warpCopy.TargetEntityGuid);
            Assert.AreEqual(new Vector3(3, 4, 0), warpCopy.EndpointRelitivePosition);
            Assert.AreEqual(ActionStatus.Queued, warpCopy.Status);
            Assert.IsFalse(warpCopy.IsRunning);

            var burn = new NewtonSimpleAction
            {
                OrbitrelativeDeltaV = new Vector3(1, 2, 3),
                StartKE = new KeplerElements { SemiMajorAxis = 12_345 },
                TargetKE = new KeplerElements { SemiMajorAxis = 99_000 },
            };
            var burnCopy = (NewtonSimpleAction)burn.Clone();
            Assert.AreEqual(12_345, burnCopy.StartKE.SemiMajorAxis, 1e-6);
            Assert.AreEqual(99_000, burnCopy.TargetKE.SemiMajorAxis, 1e-6);
            Assert.AreEqual(new Vector3(1, 2, 3), burnCopy.OrbitrelativeDeltaV);
            Assert.AreEqual(ActionStatus.Queued, burnCopy.Status);
            Assert.IsFalse(burnCopy.IsRunning);

            EntityAction[] actions =
            {
                new CirculariseAction(),
                new MatchOrbitAction(),
                new ChangeOrbitalAltitudeAction(),
                new NewtonThrustAction(),
                new ThrustToTargetCmd(),
                new Thrust90ToTargetCmd(),
                new WarpFleetTowardsTargetOrder(),
            };
            foreach (var action in actions)
            {
                var copy = action.Clone();
                Assert.AreNotSame(action, copy);
                Assert.AreEqual(action.GetType(), copy.GetType());
                Assert.AreEqual(ActionStatus.Queued, copy.Status);
                Assert.IsFalse(copy.IsRunning);
            }
        }

        [Test]
        public void StandingOrder_ClonesAWarpAndABurnWithoutThrowing()
        {
            var modLoader = new ModLoader();
            var modDataStore = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);
            var game = new Game(new NewGameSettings(), modDataStore);
            var sys = new StarSystem();
            sys.Initialize(game, "Clone", -1);

            var fleet = Entity.Create();
            var fleetDB = new FleetDB { FlagShipID = 1 };
            var queue = new ActionQueueDB();
            sys.AddEntity(fleet, new BaseDataBlob[] { fleetDB, queue });

            var warp = new WarpMoveAction { TargetEntityGuid = 7 };
            var actions = new SafeList<EntityAction>
            {
                warp,
                new NewtonSimpleAction(),
                new CirculariseAction(),
            };
            fleetDB.StandingOrders.Add(new ConditionalOrder(new CompoundCondition(), actions));

            new FleetOrderProcessor().ProcessEntity(fleet, 3600);

            Assert.AreEqual(3, queue.ActionList.Count);
            var copiedWarp = queue.ActionList.OfType<WarpMoveAction>().Single();
            Assert.AreNotSame(warp, copiedWarp);
            Assert.AreEqual(7, copiedWarp.TargetEntityGuid);
            Assert.AreEqual(ActionStatus.Queued, copiedWarp.Status);
            Assert.IsFalse(copiedWarp.IsRunning);
            Assert.AreEqual(1, queue.ActionList.OfType<NewtonSimpleAction>().Count());
            Assert.AreEqual(1, queue.ActionList.OfType<CirculariseAction>().Count());
        }
    }
}
