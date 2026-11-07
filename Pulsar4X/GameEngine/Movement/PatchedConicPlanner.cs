using System;
using Pulsar4X.Api;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Galaxy;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;

namespace Pulsar4X.Movement;

/// <summary>
/// One-level patched conic: escape the departure planet, cruise in the common
/// ancestor's frame, capture at the target. The porkchop picks the cheapest
/// window. A few iterations move that window from the planet's centre out to
/// the real sphere exit.
/// </summary>
internal static class PatchedConicPlanner
{
    /// <summary>
    /// Spare Δv held back for the one mid-course burn. Counted in the budget check,
    /// not in <see cref="Plan.TotalDeltaV"/>.
    /// </summary>
    internal const double CorrectionAllowance = TransferWindows.CorrectionAllowanceMps;

    const double ExitSettleMetres = 1000e3;
    const int Iterations = 5;
    const double YearSeconds = 365.25 * 24 * 3600;

    internal struct Plan
    {
        public bool Feasible;
        public string Reason;
        public DateTime DepartAt;
        public DateTime ArriveAt;
        public DateTime ExitAt;
        public KeplerElements EscapeStart;
        public KeplerElements EscapeEnd;
        public Vector3 VInf;
        public Vector3 ExitPosition;
        public Vector3 ExitVelocity;
        public Vector3 LambertPosition;
        public Vector3 LambertVelocity;
        public double EscapeDeltaV;
        public double CaptureDeltaV;
        public double TotalDeltaV;
        public double PlaneChange;
        public Vector3 ApproachVInf;
        public bool HasFlownWindow;
        public TransferWindow Flown;
        public bool HasTiming;
        public bool CanPick;
        public DepartureTiming Timing;
        public string TimingNote;
        public WindowAnswer TimingAnswer;
    }

    internal static Plan TryPlan(
        Entity ship, Entity target, DateTime now, double budget,
        double arrivalPeriapsis = 0, double arrivalApoapsis = 0,
        DepartureTiming timing = DepartureTiming.BestWindow,
        TransferWindow? picked = null)
    {
        try
        {
            return Solve(ship, target, now, budget, arrivalPeriapsis, arrivalApoapsis, picked, timing);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return Fail("transfer did not converge");
        }
    }

    /// <summary>
    /// Same iteration as <see cref="TryPlan"/>, starting from a chosen porkchop cell
    /// instead of scanning for the cheapest window. The cell's dates and v-infinity
    /// are a hint; the exit state is solved again from the parking orbit.
    /// </summary>
    internal static Plan TryPlanFromWindow(
        Entity ship, Entity target, DateTime now, double budget, TransferWindow window,
        double arrivalPeriapsis = 0, double arrivalApoapsis = 0)
    {
        try
        {
            if (window.Arrive <= window.Depart || !(window.DepartDeltaV.Length() > 1))
                return Fail("transfer did not converge");
            return Solve(ship, target, now, budget, arrivalPeriapsis, arrivalApoapsis, window, priceAlternative: false);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return Fail("transfer did not converge");
        }
    }

    static Plan Solve(
        Entity ship, Entity target, DateTime now, double budget,
        double arrivalPeriapsis, double arrivalApoapsis,
        TransferWindow? hint = null,
        DepartureTiming timing = DepartureTiming.BestWindow,
        bool priceAlternative = true)
    {
        if (ship == null || target == null)
            return Fail("transfer did not converge");
        if (!PatchedConic.IsOneLevelTransfer(ship, target))
            return Fail("target is under a different SOI parent; no interplanetary transfer maths yet");
        if (!ship.TryGetDataBlob<OrbitDB>(out var shipOrbit))
            return Fail("transfer did not converge");

        var planet = ship.GetSOIParentEntity();
        var ancestor = PatchedConic.FindCommonAncestor(ship, target);
        if (planet == null || ancestor == null)
            return Fail("transfer did not converge");
        if (!planet.TryGetDataBlob<OrbitDB>(out _) || !target.TryGetDataBlob<OrbitDB>(out var targetOrbit))
            return Fail("transfer did not converge");
        if (!ship.TryGetDataBlob<MassVolumeDB>(out var shipMassDb))
            return Fail("transfer did not converge");

        double shipMass = shipMassDb.MassTotal;
        double muPlanet = OrbitMath.SGP(planet, ship);
        double muTarget = OrbitMath.SGP(target, ship);
        double muStar = OrbitMath.SGP(ancestor, ship);
        double rPark = shipOrbit.GetPosition(now).Length();
        double rp = arrivalPeriapsis > 0 ? arrivalPeriapsis : OrbitMath.LowOrbitRadius(target);
        if (!(muPlanet > 0) || !(muTarget > 0) || !(muStar > 0) || !(rPark > 1) || !(rp > 1))
            return Fail("transfer did not converge");

        var shipNow = PlanetState(shipOrbit, now);
        Vector3 parkingNormal = Vector3.Cross(shipNow.pos, shipNow.vel);
        if (parkingNormal.Length() < 1)
            parkingNormal = new Vector3(0, 0, 1);
        parkingNormal = Vector3.Normalise(parkingNormal);

        double synodic = TransferWindows.SynodicSeconds(
            planet.GetDataBlob<OrbitDB>().OrbitalPeriod.TotalSeconds,
            targetOrbit.OrbitalPeriod.TotalSeconds);
        if (!double.IsFinite(synodic) || synodic > 3.0 * YearSeconds)
            synodic = 3.0 * YearSeconds;
        DateTime departTo;
        try
        {
            departTo = now.AddSeconds(synodic);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Fail("transfer did not converge");
        }

        double r1 = planet.GetDataBlob<OrbitDB>().SemiMajorAxis;
        double r2 = targetOrbit.SemiMajorAxis;
        var band = TransferWindows.HohmannTofBand(muStar, r1, r2);

        bool haveHint = hint is TransferWindow hintedCheck
            && hintedCheck.Arrive > hintedCheck.Depart
            && hintedCheck.DepartDeltaV.Length() > 1
            && double.IsFinite(hintedCheck.DepartDeltaV.X + hintedCheck.DepartDeltaV.Y + hintedCheck.DepartDeltaV.Z);
        if (timing == DepartureTiming.PickOnPorkchop && !haveHint)
            return Fail("Pick a cell on the porkchop.", canPick: true);

        WindowAnswer timingAnswer = default;
        TransferWindow window;
        if (haveHint && !priceAlternative)
        {
            window = hint.Value;
        }
        else
        {
        double DepartCost(Vector3 vInfVec, Vector3 pos)
        {
            double v = vInfVec.Length();
            if (!(v > 1) || !double.IsFinite(v))
                return double.NaN;
            return OrbitalMath.EscapeBurn(muPlanet, rPark, v);
        }

        double ArriveCost(Vector3 vInfVec, Vector3 pos)
        {
            double v = vInfVec.Length();
            if (!(v > 1) || !double.IsFinite(v))
                return double.NaN;
            return CaptureCost(muTarget, v, rp, arrivalApoapsis);
        }

        (Vector3 pos, Vector3 vel) Origin(DateTime t) => PatchedConic.StateRelativeToAncestor(planet, ancestor, t);
        (Vector3 pos, Vector3 vel) Dest(DateTime t) => PatchedConic.StateRelativeToAncestor(target, ancestor, t);

        var coarse = TransferWindows.Scan(
            muStar, Origin, Dest, now, departTo, 31, band.tofMin, band.tofMax, 21,
            DepartCost, ArriveCost, prograde: true);
        if (!coarse.HasBest && !haveHint)
            return Fail("transfer did not converge", canPick: true);

        double span = (departTo - now).TotalSeconds;
        double cap = TransferPlanner.WaitCapSeconds(band.tofMin, band.tofMax, span);
        double affordable = budget - CorrectionAllowance;
        DepartureTiming seedTiming = haveHint ? DepartureTiming.BestWindow : timing;
        if (coarse.Departures.Length > 0)
            timingAnswer = TransferPlanner.Choose(coarse, affordable, cap, seedTiming, null);

        if (haveHint)
        {
            window = hint.Value;
        }
        else
        {
            // Best window on this coarse grid is the earliest cell within 10% of the
            // floor. A step here is weeks, so that cell can leave the Hohmann basin
            // (212 days instead of ~259). Depart now is one column and keeps its own
            // seed. Same-sphere transfers still use the 10% rule in TransferPlanner.Choose.
            bool departNow = timing == DepartureTiming.DepartNow
                && timingAnswer.Feasible && timingAnswer.Window.IsFinite;
            TransferWindow seed = departNow ? timingAnswer.Window : coarse.Best;
            if (!seed.IsFinite)
                return Stamp(Fail("transfer did not converge", canPick: true), timing, timingAnswer);

            double departSpan = synodic / 30.0;
            double tofSpan = (band.tofMax - band.tofMin) / 20.0;
            double chosenTof = (seed.Arrive - seed.Depart).TotalSeconds;
            DateTime fineFrom = seed.Depart.AddSeconds(-departSpan);
            if (fineFrom < now)
                fineFrom = now;
            DateTime fineTo = seed.Depart.AddSeconds(departSpan);
            if (fineTo <= fineFrom)
                fineTo = fineFrom.AddDays(1);
            double fineTofMin = Math.Max(86400, chosenTof - tofSpan);
            double fineTofMax = Math.Max(fineTofMin + 86400, chosenTof + tofSpan);

            var fine = TransferWindows.Scan(
                muStar, Origin, Dest, fineFrom, fineTo, 15, fineTofMin, fineTofMax, 15,
                DepartCost, ArriveCost, prograde: true);
            window = fine.HasBest ? fine.Best : seed;
        }
        }

        Vector3 vInf = window.DepartDeltaV;
        if (!(vInf.Length() > 1) || !double.IsFinite(vInf.X + vInf.Y + vInf.Z))
            return Fail("transfer did not converge");

        DateTime arrive = window.Arrive;
        Vector3 approach = window.ArriveDeltaV;
        Vector3 exitPos = default;
        Vector3 exitVel = default;
        Vector3 prevExit = default;
        DateTime exitAt = default;
        DateTime burnAt = default;
        KeplerElements escapeStart = default;
        KeplerElements escapeEnd = default;
        double escapeDv = 0;
        double planeDv = 0;
        bool haveExit = false;
        bool settled = false;

        for (int iter = 0; iter < Iterations; iter++)
        {
            if (!TryEscape(
                    vInf, shipOrbit, planet, shipMass, muPlanet, rPark, parkingNormal, now, window.Depart,
                    out burnAt, out escapeStart, out escapeEnd, out escapeDv, out planeDv,
                    out exitAt, out exitPos, out exitVel))
                return Fail("transfer did not converge");

            if (haveExit && (exitPos - prevExit).Length() < ExitSettleMetres)
            {
                settled = true;
                break;
            }

            prevExit = exitPos;
            haveExit = true;
            if (!TryRetarget(muStar, muTarget, ancestor, target, vInf, exitPos, exitVel, exitAt, arrive, rp, approach, out Vector3 next, out approach))
                return Fail("transfer did not converge");
            vInf = next;
            if (!(vInf.Length() > 1) || vInf.Length() > 100_000)
                return Fail("transfer did not converge");
        }

        if (!settled)
            return Fail("transfer did not converge");

        if (!TryAim(muStar, muTarget, ancestor, target, exitPos, exitAt, arrive, rp, approach,
                out Vector3 aimPos, out Vector3 aimVel, out _, out double captureDv))
            return Fail("transfer did not converge");

        double trip = escapeDv + captureDv;
        double need = trip + CorrectionAllowance;
        if (!double.IsFinite(budget) || need > budget)
            return Stamp(Fail($"needs {need:N0} m/s Δv, have {budget:N0} m/s"), timing, timingAnswer);

        string reason = planeDv > 1 ? $"plane change {planeDv:N0} m/s" : "";
        // Depart is the Lambert epoch TryEscape aims the sphere exit at. PlanFromWindow
        // replans from this cell. The parking-orbit burn is earlier by the escape coast;
        // storing that burn here shifts the exit and the capture misses low orbit.
        var flown = new TransferWindow(window.Depart, arrive, vInf, approach, trip, vInf.Length(), approach.Length());
        return Stamp(new Plan
        {
            Feasible = true,
            Reason = reason,
            DepartAt = burnAt,
            ArriveAt = arrive,
            ExitAt = exitAt,
            EscapeStart = escapeStart,
            EscapeEnd = escapeEnd,
            VInf = vInf,
            ExitPosition = exitPos,
            ExitVelocity = exitVel,
            LambertPosition = aimPos,
            LambertVelocity = aimVel,
            EscapeDeltaV = escapeDv,
            CaptureDeltaV = captureDv,
            TotalDeltaV = trip,
            PlaneChange = planeDv,
            ApproachVInf = approach,
            HasFlownWindow = arrive > burnAt && vInf.Length() > 1 && flown.IsFinite,
            Flown = flown,
        }, timing, timingAnswer);
    }

    /// <summary>
    /// Hyperbola whose outgoing asymptote is <paramref name="vInf"/>, burned at periapsis
    /// on the parking orbit. The epoch is periapsis, which is what
    /// <see cref="OrbitMath.TimeToRadius"/> assumes for a hyperbola.
    /// </summary>
    static bool TryEscape(
        Vector3 vInf, OrbitDB shipOrbit, Entity planet, double shipMass, double mu, double rPark,
        Vector3 parkingNormal, DateTime now, DateTime windowDepart,
        out DateTime burnAt, out KeplerElements startKe, out KeplerElements endKe,
        out double escapeDv, out double planeDv,
        out DateTime exitAt, out Vector3 exitPos, out Vector3 exitVel)
    {
        burnAt = default;
        startKe = default;
        endKe = default;
        escapeDv = 0;
        planeDv = 0;
        exitAt = default;
        exitPos = default;
        exitVel = default;

        double vInfMag = vInf.Length();
        if (!(vInfMag > 1) || !(mu > 0) || !(rPark > 1))
            return false;

        var shape = OrbitalMath.HyperbolaForPeriapsis(mu, vInfMag, rPark);
        double e = shape.e;
        double a = shape.a;
        if (!(e > 1) || !double.IsFinite(a))
            return false;

        double thetaInf = Math.Acos(Math.Clamp(-1.0 / e, -1, 1));
        Vector3 pHat = PeriapsisDirection(vInf, parkingNormal, thetaInf);
        Vector3 periPos = pHat * rPark;
        double vPeri = Math.Sqrt(vInfMag * vInfMag + 2.0 * mu / rPark);
        Vector3 nHyp = Vector3.Cross(pHat, vInf);
        if (nHyp.Length() < 1e-6)
            nHyp = parkingNormal;
        nHyp = Vector3.Normalise(nHyp);
        Vector3 vDir = Vector3.Cross(nHyp, pHat);
        if (Vector3.Dot(vDir, vInf) < 0)
            vDir = -vDir;
        vDir = Vector3.Normalise(vDir);
        Vector3 periVel = vDir * vPeri;

        Vector3 circDir = Vector3.Cross(parkingNormal, pHat);
        if (circDir.Length() < 1e-8)
            return false;
        circDir = Vector3.Normalise(circDir);
        Vector3 circVel = circDir * Math.Sqrt(mu / rPark);
        Vector3 burn = periVel - circVel;
        escapeDv = burn.Length();
        planeDv = Math.Abs(Vector3.Dot(burn, parkingNormal));
        if (!double.IsFinite(escapeDv))
            return false;

        var probe = Hyperbola(a, e, periPos, periVel, mu, now);
        if (!PatchedConic.ExitStateAtSphere(probe, planet, shipMass, out DateTime probeExit, out _, out _))
            return false;
        double tEscape = (probeExit - now).TotalSeconds;
        if (!(tEscape > 0) || !double.IsFinite(tEscape))
            return false;

        double nShip = shipOrbit.MeanMotion;
        if (!(nShip > 0) || !double.IsFinite(nShip))
            nShip = Math.Sqrt(mu / (rPark * rPark * rPark));
        double want = Lon(periPos, parkingNormal);
        double have = Lon(shipOrbit.GetPosition(now), parkingNormal);
        double wait = WrapPositive(want - have) / nShip;
        DateTime first;
        try
        {
            first = now.AddSeconds(wait);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }

        double period = 2.0 * Math.PI / nShip;
        double target = (windowDepart - first).TotalSeconds - tEscape;
        int laps = (int)Math.Round(target / period);
        if (laps < 0)
            laps = 0;
        if (laps > 100000)
            laps = 100000;
        try
        {
            burnAt = first.AddSeconds(laps * period);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }

        startKe = OrbitMath.KeplerFromPositionAndVelocity(mu, periPos, circVel, burnAt);
        endKe = OrbitMath.KeplerFromPositionAndVelocity(mu, periPos, periVel, burnAt);
        var escape = Hyperbola(endKe.SemiMajorAxis, endKe.Eccentricity, periPos, periVel, mu, burnAt);
        // Angles from the state, epoch pinned to periapsis, mean anomaly left at 0
        // so TimeToRadius and the processor agree.
        escape.Inclination = endKe.Inclination;
        escape.LoAN = endKe.LoAN;
        escape.AoP = endKe.AoP;
        escape.MeanAnomalyAtEpoch = 0;
        escape.TrueAnomalyAtEpoch = 0;
        escape.Epoch = burnAt;
        endKe = escape;

        return PatchedConic.ExitStateAtSphere(escape, planet, shipMass, out exitAt, out exitPos, out exitVel);
    }

    static KeplerElements Hyperbola(double a, double e, Vector3 periPos, Vector3 periVel, double mu, DateTime epoch)
    {
        var ke = OrbitMath.KeplerFromPositionAndVelocity(mu, periPos, periVel, epoch);
        ke.SemiMajorAxis = a;
        ke.Eccentricity = e;
        ke.Epoch = epoch;
        ke.MeanAnomalyAtEpoch = 0;
        ke.TrueAnomalyAtEpoch = 0;
        ke.StandardGravParameter = mu;
        return ke;
    }

    static bool TryRetarget(
        double muStar, double muTarget, Entity ancestor, Entity target,
        Vector3 vInfIn,
        Vector3 exitPos, Vector3 exitVel, DateTime exitAt, DateTime arrive, double rp,
        Vector3 approach, out Vector3 vInf, out Vector3 nextApproach)
    {
        vInf = default;
        nextApproach = approach;
        if (!TryAim(muStar, muTarget, ancestor, target, exitPos, exitAt, arrive, rp, approach,
                out _, out Vector3 arriveVel, out Vector3 departVel, out _))
            return false;

        // departVel is the sun-frame velocity Lambert wants at the sphere exit.
        // The frames are parallel, so the same vector is the correction to the excess.
        Vector3 corrected = vInfIn + (departVel - exitVel);
        if (!double.IsFinite(corrected.X + corrected.Y + corrected.Z))
            return false;

        var tgt = PatchedConic.StateRelativeToAncestor(target, ancestor, arrive);
        Vector3 excess = arriveVel - tgt.vel;
        if (!double.IsFinite(excess.X + excess.Y + excess.Z))
            return false;

        vInf = corrected;
        nextApproach = excess;
        return true;
    }

    static bool TryAim(
        double muStar, double muTarget, Entity ancestor, Entity target,
        Vector3 exitPos, DateTime exitAt, DateTime arrive, double rp, Vector3 approach,
        out Vector3 aimPos, out Vector3 aimVel, out Vector3 departVel, out double captureDv)
    {
        aimPos = default;
        aimVel = default;
        departVel = default;
        captureDv = 0;
        double tof = (arrive - exitAt).TotalSeconds;
        if (!(tof > 3600) || !double.IsFinite(tof))
            return false;

        var tgt = PatchedConic.StateRelativeToAncestor(target, ancestor, arrive);
        double b = 0;
        double vApp = approach.Length();
        if (vApp > 1)
        {
            try
            {
                b = OrbitalMath.HyperbolaForPeriapsis(muTarget, vApp, rp).b;
            }
            catch (ArgumentOutOfRangeException)
            {
                b = 0;
            }
        }

        Vector3 aim = tgt.pos;
        if (b > 1 && vApp > 1)
        {
            Vector3 n = Vector3.Cross(tgt.pos, tgt.vel);
            if (n.Length() < 1)
                n = new Vector3(0, 0, 1);
            Vector3 bHat = Vector3.Cross(approach, n);
            if (bHat.Length() < 1e-3)
                bHat = Vector3.Cross(approach, new Vector3(0, 0, 1));
            if (bHat.Length() > 1e-8)
                aim += Vector3.Normalise(bHat) * b;
        }

        if (!LambertSolver.TrySolve(muStar, exitPos, aim, tof, true, out Vector3 v1, out Vector3 v2))
            return false;

        Vector3 vInfArrive = v2 - tgt.vel;
        double mag = vInfArrive.Length();
        if (!(mag > 0) || !double.IsFinite(mag))
            return false;
        captureDv = CaptureCost(muTarget, mag, rp, 0);
        if (!double.IsFinite(captureDv))
            return false;
        aimPos = aim;
        aimVel = v2;
        departVel = v1;
        return true;
    }

    static Vector3 PeriapsisDirection(Vector3 vInf, Vector3 n, double thetaInf)
    {
        Vector3 u = Vector3.Normalise(vInf);
        double cosInf = Math.Cos(thetaInf);
        Vector3 uIn = u - n * Vector3.Dot(u, n);
        double uLen = uIn.Length();
        if (uLen < 1e-8)
        {
            Vector3 fallback = Vector3.Cross(n, new Vector3(1, 0, 0));
            if (fallback.Length() < 1e-6)
                fallback = Vector3.Cross(n, new Vector3(0, 1, 0));
            return Vector3.Normalise(fallback);
        }

        Vector3 uHat = uIn / uLen;
        Vector3 side = Vector3.Cross(n, uHat);
        double cosA = Math.Clamp(cosInf / uLen, -1, 1);
        double sinA = Math.Sqrt(Math.Max(0, 1 - cosA * cosA));
        Vector3 back = uHat * cosA - side * sinA;
        Vector3 forward = uHat * cosA + side * sinA;
        double backScore = Vector3.Dot(Vector3.Cross(n, back), u);
        double forwardScore = Vector3.Dot(Vector3.Cross(n, forward), u);
        Vector3 p = backScore >= forwardScore ? back : forward;
        if (p.Length() < 1e-8)
            return uHat;
        return Vector3.Normalise(p);
    }

    static (Vector3 pos, Vector3 vel) PlanetState(OrbitDB orbit, DateTime at) =>
        (orbit.GetPosition(at), OrbitMath.InstantaneousOrbitalVelocityVector_m(orbit, at));

    static double CaptureCost(double mu, double vInf, double rp, double ra)
    {
        if (!(vInf > 0) || !(rp > 0))
            return double.NaN;
        if (ra > rp)
        {
            double vHyp = Math.Sqrt(vInf * vInf + 2.0 * mu / rp);
            double a = (rp + ra) / 2.0;
            double vEll = Math.Sqrt(mu * (2.0 / rp - 1.0 / a));
            return Math.Abs(vHyp - vEll);
        }
        return OrbitalMath.CaptureBurn(mu, vInf, rp);
    }

    static double Lon(Vector3 pos, Vector3 normal)
    {
        Vector3 n = Vector3.Normalise(normal);
        Vector3 axis = Math.Abs(Vector3.Dot(n, new Vector3(1, 0, 0))) > 0.9
            ? new Vector3(0, 1, 0)
            : new Vector3(1, 0, 0);
        Vector3 x = axis - n * Vector3.Dot(axis, n);
        x = Vector3.Normalise(x);
        Vector3 y = Vector3.Cross(n, x);
        Vector3 p = pos - n * Vector3.Dot(pos, n);
        return Math.Atan2(Vector3.Dot(p, y), Vector3.Dot(p, x));
    }

    static double WrapPositive(double a)
    {
        a %= 2.0 * Math.PI;
        if (a < 0)
            a += 2.0 * Math.PI;
        return a;
    }

    /// <summary>
    /// Lambert from the ship's real state to the aim point. The burn is the difference
    /// between the ship's velocity and the velocity that hits that point at <paramref name="arrive"/>.
    /// </summary>
    internal static bool TryCorrection(
        Entity ship, Entity target, Entity ancestor, DateTime now, DateTime arrive, double rp,
        out double deltaV, out KeplerElements start, out KeplerElements end)
    {
        deltaV = 0;
        start = default;
        end = default;
        if (ship == null || target == null || ancestor == null)
            return false;
        if (!ship.TryGetDataBlob<OrbitDB>(out var orbit))
            return false;
        double muStar = OrbitMath.SGP(ancestor, ship);
        double muTarget = OrbitMath.SGP(target, ship);
        if (!(muStar > 0) || !(muTarget > 0) || !(rp > 1))
            return false;

        var state = PlanetState(orbit, now);
        if (!(state.pos.Length() > 1) || !double.IsFinite(state.vel.X + state.vel.Y + state.vel.Z))
            return false;

        var tgt = PatchedConic.StateRelativeToAncestor(target, ancestor, arrive);
        double tof = (arrive - now).TotalSeconds;
        if (!(tof > 3600))
            return false;
        if (!LambertSolver.TrySolve(muStar, state.pos, tgt.pos, tof, true, out Vector3 v1, out Vector3 v2))
            return false;

        Vector3 depart = v1;
        Vector3 approach = v2 - tgt.vel;
        if (TryAim(muStar, muTarget, ancestor, target, state.pos, now, arrive, rp, approach,
                out _, out _, out Vector3 aimed, out _))
            depart = aimed;

        Vector3 burn = depart - state.vel;
        deltaV = burn.Length();
        if (!double.IsFinite(deltaV))
            return false;
        start = OrbitMath.KeplerFromPositionAndVelocity(muStar, state.pos, state.vel, now);
        end = OrbitMath.KeplerFromPositionAndVelocity(muStar, state.pos, depart, now);
        return double.IsFinite(start.SemiMajorAxis) && double.IsFinite(end.SemiMajorAxis);
    }

    static Plan Fail(string reason, bool canPick = false)
        => new Plan { Feasible = false, Reason = reason, CanPick = canPick };

    static Plan Stamp(Plan plan, DepartureTiming timing, WindowAnswer answer)
    {
        plan.HasTiming = answer.Scanned;
        plan.CanPick = true;
        plan.Timing = timing;
        plan.TimingNote = answer.Note ?? "";
        plan.TimingAnswer = answer;
        return plan;
    }
}
