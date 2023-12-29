using System;

namespace NUnit.Framework;

public class AssertExtensions
{
    public static void AreAngleEqual(double expected, double actual, double delta)
    {
        double value = Math.PI - Math.Abs(Math.Abs(expected - actual) % Math.Tau - Math.PI);
        Assert.IsTrue(value <= delta);
    }
    public static void AreAngleNotEqual(double expected, double actual, double delta)
    {
        double value = Math.PI - Math.Abs(Math.Abs(expected - actual) % Math.Tau - Math.PI);
        Assert.IsFalse(value <= delta);

    }
    
    public static void AreAngleEqual(double expected, double actual, double delta, string message)
    {
        double value = Math.PI - Math.Abs(Math.Abs(expected - actual) % Math.Tau - Math.PI);
        Assert.IsTrue(value <= delta, message);
    }
    public static void AreAngleNotEqual(double expected, double actual, double delta, string message)
    {
        double value = Math.PI - Math.Abs(Math.Abs(expected - actual) % Math.Tau - Math.PI);
        Assert.IsFalse(value <= delta, message);

    }


    [Test]
    public void TestAreAnglesEqual()
    {
        AreAngleEqual(0, Math.Tau,0);
        AreAngleEqual(Math.Tau, 0,0);
        AreAngleEqual(0, -Math.Tau,0);
        AreAngleEqual(-Math.Tau, 0,0);
        AreAngleEqual(0, 6.2831853071795845,1.0E-10);
        AreAngleEqual(6.2831853071795845, 0,1.0E-10);
    }

}