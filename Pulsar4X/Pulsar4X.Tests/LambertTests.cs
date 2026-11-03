using System;
using NUnit.Framework;
using Pulsar4X.Orbital;

namespace Pulsar4X.Tests;

/// <summary>
/// Lambert solver and the transfer-window scan. Pure maths: no game, no UI.
/// </summary>
[TestFixture]
public class LambertTests
{
    const double EarthMu = 3.986004418e14;

    [Test]
    public void CurtisExample52_MatchesTheTextbookVelocities()
    {
        // Curtis, Orbital Mechanics for Engineering Students, example 5.2. Kilometres.
        const double mu = 398600;
        var r1 = new Vector3(5000, 10000, 2100);
        var r2 = new Vector3(-14600, 2500, 7000);
        var expect1 = new Vector3(-5.99249, 1.92536, 3.24564);
        var expect2 = new Vector3(-3.31246, -4.19662, -0.385288);

        Assert.That(LambertSolver.TrySolve(mu, r1, r2, 3600, true, out Vector3 v1, out Vector3 v2), Is.True);
        AssertClose(v1, expect1, 1e-3);
        AssertClose(v2, expect2, 1e-3);
    }

    [Test]
    public void RoundTrip_SolvedDepartureMatchesTheKeplerVelocity()
    {
        var epoch = new DateTime(2050, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        double r = 7000e3;
        double speed = Math.Sqrt(EarthMu / r);
        var position = new Vector3(r, 0, 0);
        var velocity = new Vector3(0, speed, 0);
        KeplerElements ke = OrbitalMath.KeplerFromPositionAndVelocity(EarthMu, position, velocity, epoch);

        var later = epoch.AddSeconds(800);
        var atStart = OrbitalMath.GetStateVectors(ke, epoch);
        var atEnd = OrbitalMath.GetStateVectors(ke, later);
        var r1 = atStart.position;
        var r2 = atEnd.position;
        var vTrue = new Vector3(atStart.velocity.X, atStart.velocity.Y, 0);
        double tof = (later - epoch).TotalSeconds;

        Assert.That(LambertSolver.TrySolve(EarthMu, r1, r2, tof, true, out Vector3 v1, out _), Is.True);
        AssertClose(v1, vTrue, 1e-3);

        KeplerElements solved = OrbitalMath.KeplerFromPositionAndVelocity(EarthMu, r1, v1, epoch);
        Assert.That(solved.SemiMajorAxis, Is.EqualTo(ke.SemiMajorAxis).Within(Math.Abs(ke.SemiMajorAxis) * 1e-3));
        Assert.That(solved.Eccentricity, Is.EqualTo(ke.Eccentricity).Within(1e-3));
        Assert.That(solved.Inclination, Is.EqualTo(ke.Inclination).Within(1e-3));
    }

    [Test]
    public void HohmannPoint_DepartureSpeedMatchesTheClosedForm()
    {
        double r1 = 7000e3;
        double r2 = 42164e3;
        double angle = 179.9 * Math.PI / 180.0;
        var p1 = new Vector3(r1, 0, 0);
        var p2 = new Vector3(r2 * Math.Cos(angle), r2 * Math.Sin(angle), 0);
        double a = 0.5 * (r1 + r2);
        double tof = Math.PI * Math.Sqrt(a * a * a / EarthMu);

        Assert.That(LambertSolver.TrySolve(EarthMu, p1, p2, tof, true, out Vector3 v1, out _), Is.True);

        double vCirc = Math.Sqrt(EarthMu / r1);
        double lambertDv = v1.Length() - vCirc;
        double hohmannDv = OrbitalMath.Hohmann(EarthMu, r1, r2)[0].deltaV.Length();
        Assert.That(lambertDv, Is.EqualTo(hohmannDv).Within(Math.Abs(hohmannDv) * 0.01));
    }

    [Test]
    public void FastTransfer_IsHyperbolic()
    {
        const double mu = 398600;
        var r1 = new Vector3(5000, 10000, 2100);
        var r2 = new Vector3(-14600, 2500, 7000);
        Assert.That(LambertSolver.TrySolve(mu, r1, r2, 600, true, out Vector3 v1, out _), Is.True);

        var epoch = new DateTime(2050, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        KeplerElements ke = OrbitalMath.KeplerFromPositionAndVelocity(mu, r1, v1, epoch);
        Assert.That(ke.Eccentricity, Is.GreaterThan(1));
    }

    [Test]
    public void RandomCases_NeverReturnNaNWithTrue()
    {
        var rng = new Random(20261103);
        for (int i = 0; i < 1000; i++)
        {
            double mu = 1e11 * (0.05 + rng.NextDouble());
            Vector3 r1 = RandomRadius(rng, 1e6, 2e8);
            Vector3 r2 = RandomRadius(rng, 1e6, 5e8);
            double tof = 60 + rng.NextDouble() * 5e6;
            bool prograde = i % 2 == 0;
            if (!LambertSolver.TrySolve(mu, r1, r2, tof, prograde, out Vector3 v1, out Vector3 v2))
                continue;
            Assert.That(double.IsFinite(v1.X) && double.IsFinite(v1.Y) && double.IsFinite(v1.Z), Is.True, "v1 " + i);
            Assert.That(double.IsFinite(v2.X) && double.IsFinite(v2.Y) && double.IsFinite(v2.Z), Is.True, "v2 " + i);
        }
    }

    [Test]
    public void Rejects_NonPositiveTime_Origin_AndTheStraightLine()
    {
        var r1 = new Vector3(7000e3, 0, 0);
        var r2 = new Vector3(0, 7000e3, 0);
        Assert.That(LambertSolver.TrySolve(EarthMu, r1, r2, 0, true, out _, out _), Is.False);
        Assert.That(LambertSolver.TrySolve(EarthMu, default, r2, 1000, true, out _, out _), Is.False);
        var opposite = new Vector3(-14000e3, 0, 0);
        Assert.That(LambertSolver.TrySolve(EarthMu, r1, opposite, 5000, true, out _, out _), Is.False);
    }

    [Test]
    public void EarthToMars_BestCellMatchesHohmann_AndTheNextWindowIsOneSynodicLater()
    {
        const double mu = 1.32712440018e20;
        const double au = 1.495978707e11;
        double rEarth = au;
        double rMars = 1.523679 * au;
        double a = 0.5 * (rEarth + rMars);
        double hohmannTof = Math.PI * Math.Sqrt(a * a * a / mu);
        double v1 = Math.Sqrt(mu * (2 / rEarth - 1 / a));
        double vc1 = Math.Sqrt(mu / rEarth);
        double v2 = Math.Sqrt(mu * (2 / rMars - 1 / a));
        double vc2 = Math.Sqrt(mu / rMars);
        double hohmannDv = (v1 - vc1) + (vc2 - v2);
        double year = OrbitalMath.GetOrbitalPeriodInSeconds(mu, rEarth);
        double marsYear = OrbitalMath.GetOrbitalPeriodInSeconds(mu, rMars);
        double synodic = TransferWindows.SynodicSeconds(year, marsYear);

        var epoch = new DateTime(2050, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        // Mars starts ahead, so the first window is not on the first sample.
        var earth = Circular(rEarth, mu, 0);
        var mars = Circular(rMars, mu, 0.8);

        var one = TransferWindows.Scan(
            mu, earth, mars, epoch, epoch.AddSeconds(synodic), 80,
            0.5 * hohmannTof, 1.5 * hohmannTof, 40,
            TransferWindows.MagnitudeCost, TransferWindows.MagnitudeCost);

        Assert.That(one.HasBest, Is.True);
        Assert.That(one.Best.TotalDeltaV, Is.EqualTo(hohmannDv).Within(hohmannDv * 0.02),
            $"best {one.Best.TotalDeltaV / 1000:F3} km/s, Hohmann {hohmannDv / 1000:F3} km/s");
        double tof = (one.Best.Arrive - one.Best.Depart).TotalSeconds;
        Assert.That(tof, Is.EqualTo(hohmannTof).Within(hohmannTof * 0.05));
        Assert.That(one.Best.VInfDepart, Is.EqualTo(2940).Within(400),
            "departure v-infinity should sit near 2.9 km/s");

        var two = TransferWindows.Scan(
            mu, earth, mars, epoch, epoch.AddSeconds(2.15 * synodic), 90,
            0.5 * hohmannTof, 1.5 * hohmannTof, 24,
            TransferWindows.MagnitudeCost, TransferWindows.MagnitudeCost);
        Assert.That(two.HasBest, Is.True);
        TransferWindow other = default;
        bool found = false;
        for (int i = 0; i < two.Departures.Length; i++)
        {
            for (int j = 0; j < two.TimeOfFlightSeconds.Length; j++)
            {
                TransferWindow cell = two.Cells[i, j];
                if (!cell.IsFinite)
                    continue;
                double apart = Math.Abs((cell.Depart - two.Best.Depart).TotalSeconds);
                if (apart < 0.75 * synodic || apart > 1.25 * synodic)
                    continue;
                if (!found || cell.TotalDeltaV < other.TotalDeltaV)
                {
                    other = cell;
                    found = true;
                }
            }
        }

        Assert.That(found, Is.True, "a second window one synodic period away");
        double gap = Math.Abs((other.Depart - two.Best.Depart).TotalSeconds);
        Assert.That(gap, Is.EqualTo(synodic).Within(synodic * 0.05),
            $"windows {gap / 86400:F0} days apart, synodic {synodic / 86400:F0} days");
        Assert.That(other.TotalDeltaV, Is.EqualTo(hohmannDv).Within(hohmannDv * 0.02));
    }

    static Func<DateTime, (Vector3 pos, Vector3 vel)> Circular(double radius, double mu, double angle0)
    {
        double speed = Math.Sqrt(mu / radius);
        double meanMotion = speed / radius;
        var epoch = new DateTime(2050, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return at =>
        {
            double theta = angle0 + meanMotion * (at - epoch).TotalSeconds;
            var pos = new Vector3(radius * Math.Cos(theta), radius * Math.Sin(theta), 0);
            var vel = new Vector3(-speed * Math.Sin(theta), speed * Math.Cos(theta), 0);
            return (pos, vel);
        };
    }

    static Vector3 RandomRadius(Random rng, double min, double max)
    {
        double radius = min + rng.NextDouble() * (max - min);
        double theta = rng.NextDouble() * 2 * Math.PI;
        double phi = Math.Acos(2 * rng.NextDouble() - 1);
        return new Vector3(
            radius * Math.Sin(phi) * Math.Cos(theta),
            radius * Math.Sin(phi) * Math.Sin(theta),
            radius * Math.Cos(phi));
    }

    static void AssertClose(Vector3 actual, Vector3 expect, double relative)
    {
        double scale = Math.Max(expect.Length(), 1e-9);
        double miss = (actual - expect).Length() / scale;
        Assert.That(miss, Is.LessThan(relative),
            $"actual ({actual.X:G6}, {actual.Y:G6}, {actual.Z:G6}), expect ({expect.X:G6}, {expect.Y:G6}, {expect.Z:G6}), relative {miss:G4}");
    }
}
