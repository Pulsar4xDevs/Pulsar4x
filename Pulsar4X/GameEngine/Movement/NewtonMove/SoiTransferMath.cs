using System;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;

namespace Pulsar4X.Movement;

/// <summary>
/// Closed-form one-level transfer. Down: a Hohmann ellipse about the shared parent,
/// apoapsis a radial miss of <see cref="OrbitalMath.HyperbolaForPeriapsis"/> outside
/// the moon, timed with the moon's own angular rate. Up: the same ellipse run
/// backwards, so the burn is an escape hyperbola in the moon's frame.
/// The moon's real Kepler state is what the window aims at. A few degrees of
/// inclination still enters; a larger tilt waits for the ship to cross the moon's plane.
/// </summary>
internal static class SoiTransferMath
{
    internal enum Kind { NotApplicable, Circularise, Refused, Ready }

    internal struct Plan
    {
        public Kind Kind;
        public string Reason;
        public double DeltaV;
        public double EtaSeconds;
        public DateTime DepartAt;
        public KeplerElements Start;
        public KeplerElements End;
        public DateTime Deadline;
    }

    internal static Plan Evaluate(
        Entity ship, Entity target, Entity shipParent,
        OrbitDB shipOrbit, OrbitDB targetOrbit,
        DateTime now, double budget,
        double arrivalPeriapsis = 0, double arrivalApoapsis = 0)
    {
        if (!Geometry(shipParent, target, targetOrbit, out bool upward))
            return new Plan { Kind = Kind.NotApplicable };

        if (shipOrbit.Eccentricity > 0.01)
        {
            return new Plan
            {
                Kind = Kind.Circularise,
                Reason = "Circularise before transfer to " + target.GetName(ship.FactionOwnerID),
            };
        }

        try
        {
            return upward
                ? PlanUp(ship, target, shipParent, shipOrbit, now, budget, arrivalPeriapsis, arrivalApoapsis)
                : PlanDown(ship, target, shipParent, shipOrbit, targetOrbit, now, budget, arrivalPeriapsis, arrivalApoapsis);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return Refuse("transfer solution did not converge");
        }
    }

    static bool Geometry(Entity shipParent, Entity target, OrbitDB targetOrbit, out bool upward)
    {
        upward = false;
        if (shipParent == null || target == null || targetOrbit == null)
            return false;
        if (targetOrbit.Parent == shipParent && shipParent != target)
            return true;
        if (shipParent.TryGetDataBlob<OrbitDB>(out var parentOrbit) && parentOrbit.Parent == target)
        {
            upward = true;
            return true;
        }
        return false;
    }

    static Plan PlanDown(
        Entity ship, Entity target, Entity parent,
        OrbitDB shipOrbit, OrbitDB targetOrbit,
        DateTime now, double budget, double arrivalPeriapsis, double arrivalApoapsis)
    {
        double mu = OrbitMath.SGP(parent, ship);
        double muTarget = OrbitMath.SGP(target, ship);
        if (!(mu > 0) || !(muTarget > 0))
            return Refuse("transfer solution did not converge");

        var shipKE = shipOrbit.GetElements();
        var tgtKE = targetOrbit.GetElements();
        double rPark = shipKE.SemiMajorAxis;
        double rMoon = tgtKE.SemiMajorAxis;
        if (!(rPark > 1) || !(rMoon > rPark))
            return Refuse("no usable radius");

        double rp = arrivalPeriapsis > 0 ? arrivalPeriapsis : OrbitMath.LowOrbitRadius(target);
        if (!(rp > 1))
            return Refuse("no usable radius");

        double b = AimingOffset(mu, muTarget, rPark, rMoon, rp);
        if (!TryWindow(shipKE, tgtKE, mu, rPark, b, now, out var window))
            return Refuse("no launch window into the target sphere");

        // The offset depends on the excess speed, which depends on the window.
        if (window.VInf > 1)
        {
            double b2 = OrbitalMath.HyperbolaForPeriapsis(muTarget, window.VInf, rp).b;
            if (double.IsFinite(b2) && b2 > 0 && Math.Abs(b2 - b) > 0.2 * Math.Max(b, 1))
            {
                b = b2;
                if (!TryWindow(shipKE, tgtKE, mu, rPark, b, now, out window))
                    return Refuse("no launch window into the target sphere");
            }
        }

        double soi = target.GetSOI_m();
        if (!double.IsFinite(soi) || soi <= 0 || window.Miss > soi * 0.8)
            return Refuse("no launch window into the target sphere");

        double dv1 = (window.DepartVel - window.ShipVel).Length();
        double dv2 = ArrivalCost(muTarget, window.VInf, rp, arrivalApoapsis, hyperbolic: true);
        return Finish(ship, target, parent, shipKE, mu, window, dv1, dv2, budget, now);
    }

    static Plan PlanUp(
        Entity ship, Entity earth, Entity moon,
        OrbitDB shipOrbit, DateTime now, double budget,
        double arrivalPeriapsis, double arrivalApoapsis)
    {
        if (!moon.TryGetDataBlob<OrbitDB>(out var moonOrbit))
            return Refuse("transfer solution did not converge");

        double muEarth = OrbitMath.SGP(earth, ship);
        double muMoon = OrbitMath.SGP(moon, ship);
        var shipKE = shipOrbit.GetElements();
        var moonKE = moonOrbit.GetElements();
        double rPark = shipKE.SemiMajorAxis;
        double rEarth = arrivalPeriapsis > 0 ? arrivalPeriapsis : OrbitMath.LowOrbitRadius(earth);
        if (!(muEarth > 0) || !(muMoon > 0) || !(rPark > 1) || !(rEarth > 1))
            return Refuse("no usable radius");

        var shipNow = At3(shipKE, now);
        Vector3 shipN = Unit(Vector3.Cross(shipNow.pos, shipNow.vel));
        if (shipN.Length() < 0.5)
            shipN = new Vector3(0, 0, 1);
        double nShip = Math.Sqrt(muMoon / (rPark * rPark * rPark));
        if (!(nShip > 0) || !double.IsFinite(nShip))
            return Refuse("transfer solution did not converge");

        // One lunar orbit is enough: the excess direction barely moves in two hours.
        // Leaving the sphere tens of thousands of kilometres from the moon pulls the
        // Earth periapsis down, so raise the aim until the predicted periapsis is
        // clear of the planet.
        double period = 2 * Math.PI / nShip;
        double floorRp = rEarth;
        double aim = floorRp;
        double soi = moon.GetSOI_m();
        Window best = default;
        Window chosen = default;
        double bestDv = double.PositiveInfinity;
        bool found = false;
        bool any = false;
        double chosenDv = 0;
        for (int lift = 0; lift < 6; lift++)
        {
            found = false;
            bestDv = double.PositiveInfinity;
            for (int pass = 0; pass < 2; pass++)
            {
                double guess = found ? (best.DepartAt - now).TotalSeconds : period * 0.5;
                if (!TryEscapeAt(moonKE, muEarth, muMoon, rPark, aim, shipN, now, guess,
                        out Vector3 periPos, out Vector3 periVel, out double tTrans, out double rMoon))
                    continue;

                double have = Lon(At3(shipKE, now).pos, shipN);
                double want = Lon(periPos, shipN);
                double wait = WrapPositive(want - have) / nShip;
                if (!TryEscapeAt(moonKE, muEarth, muMoon, rPark, aim, shipN, now, wait,
                        out periPos, out periVel, out tTrans, out rMoon))
                    continue;

                // The rebuilt periapsis can shift the angle. One more snap.
                want = Lon(periPos, shipN);
                double wait2 = wait + Wrap(want - Lon(At3(shipKE, now.AddSeconds(wait)).pos, shipN)) / nShip;
                if (wait2 < 0)
                    wait2 += period;
                if (!TryEscapeAt(moonKE, muEarth, muMoon, rPark, aim, shipN, now, wait2,
                        out periPos, out periVel, out tTrans, out rMoon))
                    continue;

                var shipAt = At3(shipKE, now.AddSeconds(wait2));
                double along = Vector3.Dot(Unit(shipAt.pos), Unit(periPos));
                if (along < 0.995)
                    continue;

                // Burn at the ship's real position. Keep the hyperbola speed, and the
                // prograde direction of the designed burn, retied to this radius.
                Vector3 burnDir = Unit(Vector3.Cross(shipN, shipAt.pos));
                if (Vector3.Dot(burnDir, periVel) < 0)
                    burnDir = -burnDir;
                Vector3 departVel = burnDir * periVel.Length();
                double dv1 = (departVel - shipAt.vel).Length();
                double captureDv = CaptureAtEarth(moonKE, muEarth, muMoon, soi, now.AddSeconds(wait2), shipAt.pos, departVel, arrivalApoapsis);
                if (!(captureDv >= 0) || !double.IsFinite(dv1 + captureDv))
                    continue;
                if (dv1 < bestDv)
                {
                    bestDv = dv1;
                    best = new Window
                    {
                        DepartAt = now.AddSeconds(wait2),
                        TTrans = tTrans,
                        ShipPos = shipAt.pos,
                        ShipVel = shipAt.vel,
                        DepartVel = departVel,
                        CaptureDv = captureDv,
                    };
                    found = true;
                }
            }

            if (!found)
                break;
            chosen = best;
            chosenDv = bestDv;
            any = true;
            double predicted = EarthPeriapsis(moonKE, muEarth, muMoon, soi, best.DepartAt, best.ShipPos, best.DepartVel);
            if (!double.IsFinite(predicted) || predicted >= floorRp || aim > 8.0 * floorRp)
                break;
            double scale = predicted > 1 ? floorRp / predicted : 1.5;
            aim *= Math.Max(1.2, scale);
        }

        if (!any)
            return Refuse("no launch window into the target sphere");

        // The burn the processor will charge is the flat velocity difference of the
        // two element sets. The 3D length is the real burn; the fuel test follows the processor.
        var endKE = OrbitalMath.KeplerFromPositionAndVelocity(
            muMoon, chosen.ShipPos, chosen.DepartVel, chosen.DepartAt);
        double charged = ProcessorDv(shipKE, endKE, chosen.DepartAt);
        if (!double.IsFinite(charged))
            charged = chosenDv;
        return Finish(ship, earth, moon, shipKE, muMoon, chosen, charged, chosen.CaptureDv, budget, now);
    }

    /// <summary>
    /// Earth periapsis after this moon-frame escape crosses the moon's sphere.
    /// The burn is inside the sphere, so Earth's gravity alone at that instant is the wrong conic.
    /// </summary>
    static double EarthPeriapsis(
        KeplerElements moonKE, double muEarth, double muMoon, double soi,
        DateTime depart, Vector3 shipPos, Vector3 departVel)
    {
        if (!TryEarthOrbit(moonKE, muEarth, muMoon, soi, depart, shipPos, departVel, out var earthKe))
            return double.NaN;
        if (double.IsFinite(earthKe.Periapsis) && earthKe.Periapsis > 0)
            return earthKe.Periapsis;
        return earthKe.SemiMajorAxis * (1.0 - earthKe.Eccentricity);
    }

    /// <summary>
    /// Circularise (or the asked ellipse) at the periapsis the exit state actually has.
    /// Pricing the aimed Hohmann instead undercharges when the sphere exit is offset from the moon.
    /// </summary>
    static double CaptureAtEarth(
        KeplerElements moonKE, double muEarth, double muMoon, double soi,
        DateTime depart, Vector3 shipPos, Vector3 departVel, double arrivalApoapsis)
    {
        if (!TryEarthOrbit(moonKE, muEarth, muMoon, soi, depart, shipPos, departVel, out var earth))
            return double.NaN;
        double rp = earth.Periapsis;
        if (!double.IsFinite(rp) || rp <= 1)
            rp = earth.SemiMajorAxis * (1.0 - earth.Eccentricity);
        double a = earth.SemiMajorAxis;
        if (!double.IsFinite(rp) || rp <= 1 || !double.IsFinite(a) || a == 0)
            return double.NaN;
        double vPeri = Math.Sqrt(muEarth * (2.0 / rp - 1.0 / a));
        double vWant = arrivalApoapsis > rp
            ? Math.Sqrt(muEarth * (2.0 / rp - 1.0 / ((rp + arrivalApoapsis) / 2.0)))
            : Math.Sqrt(muEarth / rp);
        if (!double.IsFinite(vPeri) || !double.IsFinite(vWant))
            return double.NaN;
        return Math.Abs(vPeri - vWant);
    }

    static bool TryEarthOrbit(
        KeplerElements moonKE, double muEarth, double muMoon, double soi,
        DateTime depart, Vector3 shipPos, Vector3 departVel, out KeplerElements earthKe)
    {
        earthKe = default;
        if (!(soi > shipPos.Length()) || !double.IsFinite(soi))
            return false;
        var ke = OrbitalMath.KeplerFromPositionAndVelocity(muMoon, shipPos, departVel, depart);
        if (!double.IsFinite(ke.Eccentricity) || ke.Eccentricity < 1)
            return false;

        double lo = 0;
        double hi = 30;
        while (hi < 86400.0 * 3 && OrbitalMath.GetPosition(ke, depart.AddSeconds(hi)).Length() < soi)
            hi *= 2;
        if (OrbitalMath.GetPosition(ke, depart.AddSeconds(hi)).Length() < soi)
            return false;
        for (int i = 0; i < 40; i++)
        {
            double mid = (lo + hi) / 2.0;
            if (OrbitalMath.GetPosition(ke, depart.AddSeconds(mid)).Length() < soi)
                lo = mid;
            else
                hi = mid;
        }

        Vector3 relPos = OrbitalMath.GetPosition(ke, depart.AddSeconds(hi));
        double ta = OrbitalMath.TrueAnomalyFromTime(
            ke.StandardGravParameter, ke.SemiMajorAxis, ke.Eccentricity, ke.MeanAnomalyAtEpoch, hi);
        Vector3 relVel = OrbitalMath.ParentLocalVeclocityVector(
            ke.StandardGravParameter, relPos, ke.SemiMajorAxis, ke.Eccentricity,
            ta, ke.AoP, ke.Inclination, ke.LoAN);
        if (!double.IsFinite(relPos.X) || !double.IsFinite(relVel.X))
            return false;

        // The sphere exit adds the moon's inclined velocity. The flat state vector does not.
        var moonAt = At3(moonKE, depart.AddSeconds(hi));
        earthKe = OrbitalMath.KeplerFromPositionAndVelocity(
            muEarth, moonAt.pos + relPos, moonAt.vel + relVel, depart.AddSeconds(hi));
        return double.IsFinite(earthKe.SemiMajorAxis) && double.IsFinite(earthKe.Eccentricity);
    }

    static bool TryEscapeAt(
        KeplerElements moonKE, double muEarth, double muMoon, double rPark, double rEarth,
        Vector3 shipN, DateTime now, double wait,
        out Vector3 periPos, out Vector3 periVel, out double tTrans, out double rMoon)
    {
        periPos = default;
        periVel = default;
        tTrans = 0;
        rMoon = 0;
        var moonState = At3(moonKE, now.AddSeconds(Math.Max(0, wait)));
        rMoon = moonState.pos.Length();
        if (!(rMoon > rEarth))
            return false;
        double aTrans = (rEarth + rMoon) / 2.0;
        tTrans = Math.PI * Math.Sqrt(aTrans * aTrans * aTrans / muEarth);
        double vApo = Math.Sqrt(muEarth * (2.0 / rMoon - 1.0 / aTrans));
        Vector3 vDesired = Unit(moonState.vel) * vApo;
        Vector3 vInf = vDesired - moonState.vel;
        if (!(vInf.Length() > 1) || !double.IsFinite(vInf.Length()))
            return false;
        return TryEscapePeriapsis(muMoon, rPark, vInf, shipN, out periPos, out periVel);
    }

    /// <summary>
    /// Hyperbola periapsis whose outgoing excess velocity is <paramref name="vInf"/>.
    /// Two mirror image burns; the caller keeps the one the ship can actually reach.
    /// </summary>
    static bool TryEscapePeriapsis(double mu, double rp, Vector3 vInf, Vector3 planeN,
        out Vector3 periPos, out Vector3 periVel)
    {
        periPos = default;
        periVel = default;
        double v = vInf.Length();
        var (e, _, _, _) = OrbitalMath.HyperbolaForPeriapsis(mu, v, rp);
        double thetaInf = Math.Acos(Math.Clamp(-1.0 / e, -1.0, 1.0));
        Vector3 u = Unit(vInf);
        Vector3 n = planeN - u * Vector3.Dot(planeN, u);
        if (n.Length() < 1e-8)
        {
            n = Vector3.Cross(u, new Vector3(0, 0, 1));
            if (n.Length() < 1e-8)
                n = Vector3.Cross(u, new Vector3(0, 1, 0));
        }
        n = Unit(n);

        double speed = Math.Sqrt(v * v + 2.0 * mu / rp);
        bool any = false;
        double bestAlign = -2;
        for (int sign = -1; sign <= 1; sign += 2)
        {
            Vector3 periDir = Rotate(u, n, -sign * thetaInf);
            Vector3 velDir = Rotate(periDir, n, sign * (Math.PI / 2.0));
            var pos = periDir * rp;
            var vel = velDir * speed;
            if (!AsymptotePointsAt(pos, vel, u, thetaInf))
                continue;
            // Prograde in the ship's plane is the smaller burn. The other mirror
            // flips the ship around.
            Vector3 prograde = Unit(Vector3.Cross(planeN, periDir));
            double align = Vector3.Dot(Unit(vel), prograde);
            if (!any || align > bestAlign)
            {
                any = true;
                bestAlign = align;
                periPos = pos;
                periVel = vel;
            }
        }
        return any;
    }

    static bool AsymptotePointsAt(Vector3 pos, Vector3 vel, Vector3 u, double thetaInf)
    {
        Vector3 h = Vector3.Cross(pos, vel);
        if (h.Length() < 1)
            return false;
        Vector3 n = Unit(h);
        Vector3 periDir = Unit(pos);
        Vector3 side = Rotate(periDir, n, Math.PI / 2.0);
        double spin = Vector3.Dot(side, vel) >= 0 ? 1.0 : -1.0;
        Vector3 outgoing = Rotate(periDir, n, spin * thetaInf);
        return Vector3.Dot(Unit(outgoing), u) > 0.9;
    }

    static Plan Finish(
        Entity ship, Entity target, Entity burnParent, KeplerElements startKE, double mu,
        Window window, double dv1, double dv2, double budget, DateTime now)
    {
        if (!double.IsFinite(dv1) || !double.IsFinite(dv2) || dv1 < 0 || dv2 < 0)
            return Refuse("transfer solution did not converge");
        double total = dv1 + dv2;
        if (total > budget)
            return Refuse($"needs {total:N0} m/s Δv, have {budget:N0} m/s");

        var end = OrbitalMath.KeplerFromPositionAndVelocity(mu, window.ShipPos, window.DepartVel, window.DepartAt);
        if (!double.IsFinite(end.SemiMajorAxis) || !double.IsFinite(end.Eccentricity))
            return Refuse("transfer solution did not converge");

        double eta = (window.DepartAt - now).TotalSeconds + window.TTrans;
        if (!double.IsFinite(eta) || eta < 0)
            return Refuse("transfer solution did not converge");

        string name = target.GetName(ship.FactionOwnerID);
        return new Plan
        {
            Kind = Kind.Ready,
            Reason = $"Transfer to {name}, {window.DepartAt.AddSeconds(window.TTrans):yyyy-MM-dd HH:mm:ss}",
            DeltaV = total,
            EtaSeconds = eta,
            DepartAt = window.DepartAt,
            Start = startKE,
            End = end,
            Deadline = window.DepartAt.AddSeconds(Math.Max(60, window.TTrans * 1.5)),
        };
    }

    static bool TryWindow(KeplerElements shipKE, KeplerElements tgtKE, double mu, double rPark, double b,
        DateTime now, out Window window)
    {
        window = default;
        var ship0 = At(shipKE, now);
        Vector3 n = Unit(Vector3.Cross(ship0.pos, ship0.vel));
        if (n.Length() < 0.5)
            n = new Vector3(0, 0, 1);

        double rMoon = tgtKE.SemiMajorAxis;
        double nShip = Math.Sqrt(mu / (rPark * rPark * rPark));
        double nMoon = Math.Sqrt(mu / (rMoon * rMoon * rMoon));
        if (!(nShip > 0) || !(Math.Abs(nShip - nMoon) > 0))
            return false;

        double tTrans = HalfPeriod(mu, rPark, rMoon + b);
        double shipLon = Lon(ship0.pos, n);
        double tgtLon = Lon(At(tgtKE, now).pos, n);
        double phase0 = PositiveAngle(shipLon) - PositiveAngle(tgtLon);
        double phaseAng = nMoon * tTrans - Math.PI;
        int k = 0;
        if (rMoon + b > rPark)
        {
            if (phaseAng - phase0 < 0)
                k = 1;
        }
        else if (phaseAng - phase0 > 0)
        {
            k = -1;
        }

        double tWait = (phaseAng - phase0 + 2.0 * Math.PI * k) / (nShip - nMoon);
        double synodic = 2.0 * Math.PI / Math.Abs(nShip - nMoon);
        if (!double.IsFinite(tWait))
            return false;
        if (tWait < 0)
            tWait += synodic;
        if (tWait > synodic * 2)
            return false;

        for (int i = 0; i < 6; i++)
        {
            var tgt = At(tgtKE, now.AddSeconds(tWait + tTrans));
            tTrans = HalfPeriod(mu, rPark, tgt.pos.Length() + b);
            double err = Wrap(Lon(tgt.pos, n) + Math.PI - Lon(At(shipKE, now.AddSeconds(tWait)).pos, n));
            tWait += err / nShip;
            if (tWait < 0)
                tWait += 2.0 * Math.PI / nShip;
        }

        double span = Math.Min(2.0 * Math.PI / nShip, 3600);
        double bestT = tWait;
        double bestMiss = double.PositiveInfinity;
        for (double t = tWait - span; t <= tWait + span; t += 10)
        {
            if (t < 0)
                continue;
            double miss = CheapMiss(shipKE, tgtKE, mu, rPark, b, now, t, out _);
            if (miss < bestMiss)
            {
                bestMiss = miss;
                bestT = t;
            }
        }
        for (double t = bestT - 10; t <= bestT + 10; t += 1)
        {
            if (t < 0)
                continue;
            double miss = CheapMiss(shipKE, tgtKE, mu, rPark, b, now, t, out double tt);
            if (miss < bestMiss)
            {
                bestMiss = miss;
                bestT = t;
                tTrans = tt;
            }
        }

        if (!BuildWindow(shipKE, tgtKE, mu, rPark, b, now, bestT, out window))
            return false;

        // A tilt the in-plane aim cannot close: leave from the node, in the moon's plane.
        if (window.Miss > Math.Max(b * 4, 20_000))
        {
            if (TryNodal(shipKE, tgtKE, mu, rPark, b, now, synodic, out var nodal) && nodal.Miss < window.Miss)
                window = nodal;
        }
        return double.IsFinite(window.Miss);
    }

    static bool TryNodal(KeplerElements shipKE, KeplerElements tgtKE, double mu, double rPark, double b,
        DateTime now, double maxWait, out Window window)
    {
        window = default;
        var ship0 = At(shipKE, now);
        var tgt0 = At(tgtKE, now);
        Vector3 shipN = Unit(Vector3.Cross(ship0.pos, ship0.vel));
        Vector3 tgtN = Unit(Vector3.Cross(tgt0.pos, tgt0.vel));
        if (shipN.Length() < 0.5 || tgtN.Length() < 0.5)
            return false;

        double nShip = Math.Sqrt(mu / (rPark * rPark * rPark));
        double shipPeriod = 2.0 * Math.PI / nShip;
        double bestMiss = double.PositiveInfinity;
        Window best = default;
        bool found = false;
        double horizon = Math.Min(maxWait + shipPeriod, 40.0 * 86400.0);
        for (double tArr = 0; tArr <= horizon; tArr += 600)
        {
            var tgt = At(tgtKE, now.AddSeconds(tArr));
            if (Math.Abs(Vector3.Dot(Unit(tgt.pos), shipN)) > 0.03)
                continue;
            double tTrans = HalfPeriod(mu, rPark, tgt.pos.Length() + b);
            double tDep = tArr - tTrans;
            Vector3 want = tgt.pos - shipN * Vector3.Dot(tgt.pos, shipN);
            if (want.Length() < 1)
                continue;
            want = -Unit(want);
            double baseWait = WrapPositive(Lon(want, shipN) - Lon(ship0.pos, shipN)) / nShip;
            double k = Math.Round((tDep - baseWait) / shipPeriod);
            double tSnap = baseWait + k * shipPeriod;
            if (tSnap < 0 || tSnap > maxWait)
                continue;
            if (!BuildWindow(shipKE, tgtKE, mu, rPark, b, now, tSnap, out var candidate, tgtN))
                continue;
            if (candidate.Miss < bestMiss)
            {
                bestMiss = candidate.Miss;
                best = candidate;
                found = true;
            }
        }
        window = best;
        return found;
    }

    static double CheapMiss(KeplerElements shipKE, KeplerElements tgtKE, double mu, double rPark, double b,
        DateTime now, double tDepart, out double tTrans)
    {
        double rApo = tgtKE.SemiMajorAxis + b;
        tTrans = HalfPeriod(mu, rPark, rApo);
        var tgt = At(tgtKE, now.AddSeconds(tDepart + tTrans));
        rApo = tgt.pos.Length() + b;
        tTrans = HalfPeriod(mu, rPark, rApo);
        tgt = At(tgtKE, now.AddSeconds(tDepart + tTrans));
        var ship = At(shipKE, now.AddSeconds(tDepart));
        if (ship.pos.Length() < 1)
            return double.PositiveInfinity;
        Vector3 apo = -Unit(ship.pos) * rApo;
        return (apo - tgt.pos).Length();
    }

    static bool BuildWindow(KeplerElements shipKE, KeplerElements tgtKE, double mu, double rPark, double b,
        DateTime now, double tDepart, out Window window, Vector3? planeNormal = null)
    {
        window = default;
        var ship = At(shipKE, now.AddSeconds(tDepart));
        if (ship.pos.Length() < 1 || ship.vel.Length() < 1)
            return false;

        double rApo = tgtKE.SemiMajorAxis + b;
        double tTrans = HalfPeriod(mu, rPark, rApo);
        (Vector3 pos, Vector3 vel) tgt = default;
        for (int i = 0; i < 3; i++)
        {
            tgt = At(tgtKE, now.AddSeconds(tDepart + tTrans));
            rApo = tgt.pos.Length() + b;
            tTrans = HalfPeriod(mu, rPark, rApo);
        }

        Vector3 rHat = Unit(ship.pos);
        Vector3 n = planeNormal ?? Unit(Vector3.Cross(ship.pos, ship.vel));
        if (n.Length() < 0.5)
            n = new Vector3(0, 0, 1);
        Vector3 vHat = Unit(Vector3.Cross(n, rHat));
        if (vHat.Length() < 0.5)
            return false;
        if (Vector3.Dot(vHat, ship.vel) < 0 && planeNormal == null)
            vHat = -vHat;

        double aTrans = (rPark + rApo) / 2.0;
        double vPeri = Math.Sqrt(mu * (2.0 / ship.pos.Length() - 1.0 / aTrans));
        double vApo = Math.Sqrt(mu * (2.0 / rApo - 1.0 / aTrans));
        Vector3 apoVel = -vHat * vApo;
        if (Vector3.Dot(apoVel, tgt.vel) < 0)
        {
            vHat = -vHat;
            apoVel = -vHat * vApo;
        }

        Vector3 apoPos = -rHat * rApo;
        window = new Window
        {
            DepartAt = now.AddSeconds(tDepart),
            TTrans = tTrans,
            ShipPos = ship.pos,
            ShipVel = ship.vel,
            DepartVel = vHat * vPeri,
            VInf = (apoVel - tgt.vel).Length(),
            Miss = (apoPos - tgt.pos).Length(),
        };
        return double.IsFinite(window.VInf) && double.IsFinite(window.Miss);
    }

    static double AimingOffset(double muParent, double muTarget, double rPark, double rMoon, double rp)
    {
        double a = (rPark + rMoon) / 2.0;
        double vShip = Math.Sqrt(muParent * (2.0 / rMoon - 1.0 / a));
        double vMoon = Math.Sqrt(muParent / rMoon);
        double vInf = Math.Abs(vMoon - vShip);
        if (!(vInf > 1) || !double.IsFinite(vInf))
            vInf = 1;
        return OrbitalMath.HyperbolaForPeriapsis(muTarget, vInf, rp).b;
    }

    static double ArrivalCost(double mu, double vInf, double rp, double ra, bool hyperbolic)
    {
        if (!hyperbolic)
            return 0;
        if (!(vInf > 0))
            return 0;
        if (ra > rp)
        {
            double vHyp = Math.Sqrt(vInf * vInf + 2.0 * mu / rp);
            double a = (rp + ra) / 2.0;
            double vEll = Math.Sqrt(mu * (2.0 / rp - 1.0 / a));
            return Math.Abs(vHyp - vEll);
        }
        return OrbitalMath.CaptureBurn(mu, vInf, rp);
    }

    static double HalfPeriod(double mu, double rp, double ra)
    {
        double a = (rp + ra) / 2.0;
        return Math.PI * Math.Sqrt(a * a * a / mu);
    }

    static (Vector3 pos, Vector3 vel) At(KeplerElements ke, DateTime t)
    {
        var state = OrbitalMath.GetStateVectors(ke, t);
        return (state.position, (Vector3)state.velocity);
    }

    /// <summary>
    /// Position and velocity in the parent frame, inclination included.
    /// <see cref="At"/> drops the velocity's out-of-plane part. The fuel burn uses that
    /// flat difference, but a sphere exit adds the real velocity.
    /// </summary>
    static (Vector3 pos, Vector3 vel) At3(KeplerElements ke, DateTime t)
    {
        Vector3 pos = OrbitalMath.GetPosition(ke, t);
        double seconds = (t - ke.Epoch).TotalSeconds;
        double ta = OrbitalMath.TrueAnomalyFromTime(
            ke.StandardGravParameter, ke.SemiMajorAxis, ke.Eccentricity, ke.MeanAnomalyAtEpoch, seconds);
        Vector3 vel = OrbitalMath.ParentLocalVeclocityVector(
            ke.StandardGravParameter, pos, ke.SemiMajorAxis, ke.Eccentricity,
            ta, ke.AoP, ke.Inclination, ke.LoAN);
        return (pos, vel);
    }

    /// <summary>What <c>NewtonSimpleProcessor</c> will charge for a burn between these elements.</summary>
    static double ProcessorDv(KeplerElements start, KeplerElements end, DateTime at)
    {
        var a = OrbitalMath.GetStateVectors(start, at);
        var b = OrbitalMath.GetStateVectors(end, at);
        if (!double.IsFinite(a.velocity.X) || !double.IsFinite(b.velocity.X))
            return double.NaN;
        return ((Vector3)b.velocity - (Vector3)a.velocity).Length();
    }

    static Plan Refuse(string reason) => new Plan { Kind = Kind.Refused, Reason = reason };

    static Vector3 Unit(Vector3 v) => Vector3.Normalise(v);

    static Vector3 Rotate(Vector3 v, Vector3 axisUnit, double radians)
    {
        double c = Math.Cos(radians);
        double s = Math.Sin(radians);
        return v * c
               + Vector3.Cross(axisUnit, v) * s
               + axisUnit * Vector3.Dot(axisUnit, v) * (1.0 - c);
    }

    static double Lon(Vector3 pos, Vector3 normal)
    {
        Vector3 n = Unit(normal);
        Vector3 axis = Math.Abs(Vector3.Dot(n, new Vector3(1, 0, 0))) > 0.9
            ? new Vector3(0, 1, 0)
            : new Vector3(1, 0, 0);
        Vector3 x = Unit(axis - n * Vector3.Dot(axis, n));
        Vector3 y = Vector3.Cross(n, x);
        Vector3 p = pos - n * Vector3.Dot(pos, n);
        return Math.Atan2(Vector3.Dot(p, y), Vector3.Dot(p, x));
    }

    static double Wrap(double a)
    {
        a %= 2.0 * Math.PI;
        if (a > Math.PI) a -= 2.0 * Math.PI;
        if (a < -Math.PI) a += 2.0 * Math.PI;
        return a;
    }

    static double WrapPositive(double a)
    {
        a %= 2.0 * Math.PI;
        if (a < 0) a += 2.0 * Math.PI;
        return a;
    }

    static double PositiveAngle(double a) => WrapPositive(a);

    struct Window
    {
        public DateTime DepartAt;
        public double TTrans;
        public Vector3 ShipPos;
        public Vector3 ShipVel;
        public Vector3 DepartVel;
        public double VInf;
        public double Miss;
        public double CaptureDv;
    }
}
