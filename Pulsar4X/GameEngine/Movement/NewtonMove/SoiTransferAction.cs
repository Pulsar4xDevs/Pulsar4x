using System;
using Newtonsoft.Json;
using GameEngine.Engine.Orders;
using Pulsar4X.Api;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Extensions;
using Pulsar4X.Galaxy;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;

namespace Pulsar4X.Movement;

/// <summary>
/// Leave a parking orbit, coast until the sphere of influence changes, then capture.
/// One level only: parent to child, or child to parent. The arrival burn is not queued
/// up front. A tank that cannot pay the whole trip is refused before the first burn.
/// <see cref="ArrivalPeriapsis"/> and <see cref="ArrivalApoapsis"/> are the arrival
/// orbit. Zero means a low circular orbit.
/// </summary>
public class SoiTransferAction : EntityAction
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement;
    public override bool IsBlocking => true;
    public override string Name => "Transfer";
    public override string Details => _details;

    [JsonProperty]
    string _details = "Transfer";

    [JsonProperty]
    public int TargetId { get; private set; }

    /// <summary>Arrival periapsis in metres. Zero selects <see cref="OrbitMath.LowOrbitRadius"/>.</summary>
    [JsonProperty]
    public double ArrivalPeriapsis { get; private set; }

    /// <summary>Arrival apoapsis in metres. Zero selects a circular orbit at the periapsis.</summary>
    [JsonProperty]
    public double ArrivalApoapsis { get; private set; }

    enum Phase { Depart, Coast, Capture }

    [JsonProperty]
    Phase _phase = Phase.Depart;

    [JsonProperty]
    KeplerElements _departStart;

    [JsonProperty]
    KeplerElements _departEnd;

    [JsonProperty]
    DateTime _departAt;

    [JsonProperty]
    DateTime _deadline;

    [JsonProperty]
    DateTime _wakeAt;

    [JsonProperty]
    bool _planned;

    NewtonSimpleMoveDB? _db;
    Entity _factionEntity;

    internal override Entity EntityCommanding => _entityCommanding;

    public static SoiTransferAction CreateCommand(
        Entity ship, Entity target, DateTime now,
        double arrivalPeriapsis = 0, double arrivalApoapsis = 0)
    {
        var cmd = new SoiTransferAction
        {
            RequestingFactionGuid = ship.FactionOwnerID,
            EntityCommandingGuid = ship.Id,
            _entityCommanding = ship,
            TargetId = target.Id,
            ArrivalPeriapsis = arrivalPeriapsis,
            ArrivalApoapsis = arrivalApoapsis,
            CreatedDate = ship.StarSysDateTime,
            ActionOnDate = now,
        };
        cmd.UpdateDetailString();
        return cmd;
    }

    public override void Rehydrate(Game game)
    {
        base.Rehydrate(game);
        if (Status != ActionStatus.Running || _entityCommanding == null)
            return;
        if (_entityCommanding.TryGetDataBlob<NewtonSimpleMoveDB>(out var burn))
            _db = burn;
        Wake(_wakeAt);
    }

    internal override void Execute(DateTime atDateTime)
    {
        if (atDateTime < ActionOnDate || _entityCommanding == null)
            return;

        if (!_planned)
        {
            if (!TryPlan(atDateTime))
                return;
        }

        if (_phase == Phase.Depart)
            TryDepart(atDateTime);
        if (_phase == Phase.Coast && !_isFinished)
            TryCoast(atDateTime);
        if (_phase == Phase.Capture && !_isFinished)
            TryCapture(atDateTime);
        UpdateDetailString();
    }

    bool TryPlan(DateTime now)
    {
        if (!ResolveTarget(out var target))
        {
            Fail("target is gone");
            _isFinished = true;
            return false;
        }
        if (!_entityCommanding.TryGetDataBlob<OrbitDB>(out var shipOrbit) || shipOrbit.Parent == null)
        {
            Fail("ship is not in a stable orbit");
            _isFinished = true;
            return false;
        }
        if (!target.TryGetDataBlob<OrbitDB>(out var targetOrbit))
        {
            Fail("target is not in a stable orbit");
            _isFinished = true;
            return false;
        }

        double budget = _entityCommanding.TryGetDataBlob<NewtonThrustAbilityDB>(out var thrust)
            ? thrust.DeltaV
            : 0;
        var plan = SoiTransferMath.Evaluate(
            _entityCommanding, target, shipOrbit.Parent, shipOrbit, targetOrbit, now, budget,
            ArrivalPeriapsis, ArrivalApoapsis);
        if (plan.Kind != SoiTransferMath.Kind.Ready)
        {
            Fail(string.IsNullOrEmpty(plan.Reason) ? "transfer solution did not converge" : plan.Reason);
            _isFinished = true;
            return false;
        }

        _departStart = plan.Start;
        _departEnd = plan.End;
        _departAt = plan.DepartAt;
        _deadline = plan.Deadline;
        _phase = Phase.Depart;
        _planned = true;
        IsRunning = true;
        _details = "Transfer to " + target.GetName(_entityCommanding.FactionOwnerID);
        Wake(_departAt);
        return true;
    }

    void TryDepart(DateTime at)
    {
        if (_db != null && !_db.IsComplete && !_db.IsFailed)
            return;
        if (_db != null && _db.IsFailed)
        {
            Fail("not enough ΔV");
            _isFinished = true;
            return;
        }
        if (_db != null && _db.IsComplete)
        {
            _phase = Phase.Coast;
            _db = null;
            Wake(_deadline);
            return;
        }
        if (at + TimeSpan.FromSeconds(1) < _departAt)
        {
            Wake(_departAt);
            return;
        }

        var parent = _entityCommanding.GetSOIParentEntity();
        if (parent == null)
        {
            Fail("no SOI parent");
            _isFinished = true;
            return;
        }

        try
        {
            _db = new NewtonSimpleMoveDB(parent, _departStart, _departEnd, _departAt);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            Fail("transfer solution did not converge");
            _isFinished = true;
            return;
        }

        _entityCommanding.SetDataBlob(_db);
        NewtonSimpleProcessor.ProcessEntity(_entityCommanding, at);
        if (_db.IsFailed)
        {
            Fail("not enough ΔV");
            _isFinished = true;
            return;
        }
        double dv = DvBetween(_departStart, _departEnd, _departAt);
        NoteBurn(at, "Departure burn", dv);
        if (_db.IsComplete)
        {
            _phase = Phase.Coast;
            _db = null;
            Wake(_deadline);
            return;
        }

        // The burn keeps running on the one-second hotloop. Wake when it should be done.
        double seconds = 30;
        if (_entityCommanding.TryGetDataBlob<NewtonThrustAbilityDB>(out var thrust)
            && thrust.FuelBurnRate > 0
            && _entityCommanding.TryGetDataBlob<MassVolumeDB>(out var mass))
        {
            double fuel = OrbitMath.TsiolkovskyFuelUse(mass.MassTotal, thrust.ExhaustVelocity, dv);
            seconds = fuel / thrust.FuelBurnRate + 5;
        }
        Wake(at.AddSeconds(Math.Clamp(seconds, 5, 86400)));
    }

    void TryCoast(DateTime at)
    {
        var parent = _entityCommanding.GetSOIParentEntity();
        if (parent != null && parent.Id == TargetId)
        {
            _phase = Phase.Capture;
            _details = "Capture at " + parent.GetName(_entityCommanding.FactionOwnerID);
            TryCapture(at);
            return;
        }
        if (at > _deadline)
        {
            Fail("Missed the encounter with " + TargetName());
            _isFinished = true;
            return;
        }
        Wake(_deadline);
    }

    void TryCapture(DateTime at)
    {
        if (_db != null && !_db.IsComplete && !_db.IsFailed)
            return;
        if (_db != null && _db.IsFailed)
        {
            Fail("not enough ΔV");
            _isFinished = true;
            return;
        }
        if (_db != null && _db.IsComplete)
        {
            _isFinished = true;
            if (!_details.StartsWith("Elliptical orbit"))
                _details = "Arrived at " + TargetName() + ".";
            return;
        }

        if (!_entityCommanding.TryGetDataBlob<OrbitDB>(out var orbit) || orbit.Parent == null)
            return;

        if (!TryPeriapsis(orbit, at, out DateTime peri))
        {
            Fail("Missed the encounter with " + TargetName());
            _isFinished = true;
            return;
        }
        if (peri > at.AddSeconds(2))
        {
            Wake(peri);
            return;
        }

        var parent = orbit.Parent;
        var state = StateAt(orbit, at);
        Vector3 pos = state.pos;
        Vector3 vel = state.vel;
        double budget = _entityCommanding.TryGetDataBlob<NewtonThrustAbilityDB>(out var thrust)
            ? thrust.DeltaV
            : 0;
        if (!MovePlanner.TryArrivalBurn(
                _entityCommanding, parent, pos, vel, budget, at,
                out bool circular, out bool hold, out Vector3 slowed, out double shortfall))
        {
            double mu = OrbitMath.SGP(parent, _entityCommanding);
            double need = (vel - ProgradeCircular(pos, vel, mu)).Length();
            Fail($"needs {need:N0} m/s Δv, have {budget:N0} m/s");
            _isFinished = true;
            return;
        }

        if (hold || (circular && (vel - slowed).Length() < 0.01 && orbit.Eccentricity < 0.05))
        {
            _isFinished = true;
            _details = hold
                ? MovePlanner.EllipticalOrbitDetails(parent, _entityCommanding, shortfall)
                : "Arrived at " + parent.GetName(_entityCommanding.FactionOwnerID) + ".";
            return;
        }

        double sgp = OrbitMath.SGP(parent, _entityCommanding);
        var startKE = OrbitMath.KeplerFromPositionAndVelocity(sgp, pos, vel, at);
        KeplerElements targetKE;
        if (!circular && ArrivalApoapsis > ArrivalPeriapsis && ArrivalPeriapsis > 0)
        {
            double r = pos.Length();
            double a = (r + ArrivalApoapsis) / 2.0;
            double vEll = Math.Sqrt(sgp * (2.0 / r - 1.0 / a));
            Vector3 dir = Vector3.Normalise(vel);
            if (dir.Length() < 0.5)
                dir = Vector3.Normalise(Vector3.Cross(new Vector3(0, 0, 1), pos));
            targetKE = OrbitMath.KeplerFromPositionAndVelocity(sgp, pos, dir * vEll, at);
        }
        else if (circular)
        {
            // Same plane as the arrival. KeplerCircularFromPosition tilts a new
            // orbit from the position alone, and the burn then costs the
            // plane change as well as the capture.
            targetKE = OrbitMath.KeplerFromPositionAndVelocity(sgp, pos, ProgradeCircular(pos, vel, sgp), at);
        }
        else
        {
            targetKE = OrbitMath.KeplerFromPositionAndVelocity(sgp, pos, slowed, at);
            _details = MovePlanner.EllipticalOrbitDetails(parent, _entityCommanding, shortfall);
        }

        if (!double.IsFinite(targetKE.SemiMajorAxis))
        {
            Fail("transfer solution did not converge");
            _isFinished = true;
            return;
        }

        try
        {
            _db = new NewtonSimpleMoveDB(parent, startKE, targetKE, at);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            Fail("not enough ΔV");
            _isFinished = true;
            return;
        }

        _entityCommanding.SetDataBlob(_db);
        NewtonSimpleProcessor.ProcessEntity(_entityCommanding, at);
        double dv = (vel - (Vector3)OrbitalMath.GetStateVectors(targetKE, at).velocity).Length();
        if (_db.IsFailed)
        {
            Fail("not enough ΔV");
            _isFinished = true;
            return;
        }
        NoteBurn(at, "Capture burn", dv);
        if (_db.IsComplete)
        {
            _isFinished = true;
            if (!_details.StartsWith("Elliptical orbit"))
                _details = "Arrived at " + parent.GetName(_entityCommanding.FactionOwnerID) + ".";
            return;
        }
        double seconds = 30;
        if (thrust != null && thrust.FuelBurnRate > 0
            && _entityCommanding.TryGetDataBlob<MassVolumeDB>(out var mass))
        {
            double fuel = OrbitMath.TsiolkovskyFuelUse(mass.MassTotal, thrust.ExhaustVelocity, dv);
            seconds = fuel / thrust.FuelBurnRate + 5;
        }
        Wake(at.AddSeconds(Math.Clamp(seconds, 5, 86400)));
    }

    static bool TryPeriapsis(OrbitDB orbit, DateTime at, out DateTime peri)
    {
        peri = at;
        var state = StateAt(orbit, at);
        double r = state.pos.Length();
        if (!(r > 1) || !double.IsFinite(r) || !double.IsFinite(state.vel.X))
            return false;
        double radial = Vector3.Dot(state.pos, state.vel) / r;
        if (radial >= -1)
            return true;

        double lo = 0;
        double hi = 60;
        while (hi < 86400.0 * 40)
        {
            var ahead = StateAt(orbit, at.AddSeconds(hi));
            double rr = ahead.pos.Length();
            if (!(rr > 1) || !double.IsFinite(ahead.vel.X))
                return false;
            if (Vector3.Dot(ahead.pos, ahead.vel) / rr >= 0)
                break;
            hi *= 2;
        }
        if (hi >= 86400.0 * 40)
            return true;

        for (int i = 0; i < 40; i++)
        {
            double mid = (lo + hi) / 2.0;
            var ahead = StateAt(orbit, at.AddSeconds(mid));
            double rr = Math.Max(ahead.pos.Length(), 1);
            if (Vector3.Dot(ahead.pos, ahead.vel) / rr < 0)
                lo = mid;
            else
                hi = mid;
        }
        peri = at.AddSeconds(hi);
        return true;
    }

    /// <summary>
    /// Parent-relative state. <see cref="OrbitalMath.GetStateVectors(KeplerElements, DateTime)"/>
    /// returns a 2D velocity and drops the out-of-plane part, which puts the capture
    /// burn on the wrong side of a sphere that is not in the ecliptic.
    /// </summary>
    static (Vector3 pos, Vector3 vel) StateAt(OrbitDB orbit, DateTime at)
    {
        var state = OrbitMath.GetStateVectors(orbit, at);
        return (state.pos, state.Velocity);
    }

    /// <summary>Circular velocity in the plane of <paramref name="vel"/>, prograde.</summary>
    static Vector3 ProgradeCircular(Vector3 pos, Vector3 vel, double mu)
    {
        double r = pos.Length();
        Vector3 h = Vector3.Cross(pos, vel);
        Vector3 dir = h.Length() > 1 ? Vector3.Normalise(Vector3.Cross(h, pos)) : Vector3.Normalise(vel);
        if (dir.Length() < 0.5)
            dir = new Vector3(0, 1, 0);
        double speed = r > 1 && mu > 0 ? Math.Sqrt(mu / r) : 0;
        return dir * speed;
    }

    static double DvBetween(KeplerElements start, KeplerElements end, DateTime at)
    {
        var v0 = (Vector3)OrbitalMath.GetStateVectors(start, at).velocity;
        var v1 = (Vector3)OrbitalMath.GetStateVectors(end, at).velocity;
        return (v1 - v0).Length();
    }

    void NoteBurn(DateTime at, string label, double dv)
    {
        if (_entityCommanding == null || !(dv >= 0.01) || !double.IsFinite(dv))
            return;
        ShipLogBook.Append(
            _entityCommanding, at, ShipLogKind.Action,
            label + ", " + Stringify.Velocity(dv) + ".");
    }

    void Wake(DateTime when)
    {
        if (_entityCommanding?.Manager == null)
            return;
        var pulses = _entityCommanding.Manager.ManagerSubpulses;
        if (when <= pulses.StarSysDateTime)
            return;
        _wakeAt = when;
        pulses.AddEntityInterupt(when, nameof(ActionQueueProcessor), _entityCommanding);
    }

    bool ResolveTarget(out Entity target)
    {
        target = null!;
        var game = _entityCommanding?.Manager?.Game;
        return game != null && game.GlobalManager.TryGetGlobalEntityById(TargetId, out target);
    }

    string TargetName()
    {
        if (ResolveTarget(out var target))
            return target.GetName(_entityCommanding.FactionOwnerID);
        return "the target";
    }

    public override void Stop(DateTime at)
    {
        if (_entityCommanding == null || _db == null)
            return;
        NewtonSimpleProcessor.Abort(_entityCommanding, at);
    }

    public override void UpdateDetailString()
    {
        if (string.IsNullOrEmpty(_details))
            _details = Name;
    }

    internal override bool IsFinished()
    {
        if (_isFinished || Status == ActionStatus.Failed)
            return true;
        if (_db != null && _db.IsFailed)
        {
            Fail("not enough ΔV");
            return _isFinished = true;
        }
        return false;
    }

    internal override bool IsValidCommand(Game game)
    {
        if (!CommandHelpers.IsCommandValid(
                game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid,
                out _factionEntity, out _entityCommanding))
            return false;
        return game.GlobalManager.TryGetGlobalEntityById(TargetId, out _);
    }

    public override EntityAction Clone()
    {
        var copy = new SoiTransferAction
        {
            UseActionLanes = UseActionLanes,
            ParentGoalId = ParentGoalId,
            RequestingFactionGuid = RequestingFactionGuid,
            EntityCommandingGuid = EntityCommandingGuid,
            _entityCommanding = _entityCommanding,
            CreatedDate = CreatedDate,
            ActionOnDate = ActionOnDate,
            TargetId = TargetId,
            ArrivalPeriapsis = ArrivalPeriapsis,
            ArrivalApoapsis = ArrivalApoapsis,
        };
        copy.UpdateDetailString();
        return copy;
    }
}
