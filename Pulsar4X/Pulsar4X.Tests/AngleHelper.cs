using System;

namespace NUnit.Framework;

public static class AssertExtensions
{
    const double pi2 = Math.PI * 2;
    public static void AreAngleEqual(double expected, double actual, double delta)
    {
        double mod_e = (expected % pi2 + pi2 ) % pi2;
        double mod_a = (actual % pi2 + pi2 ) % pi2;
        Assert.AreEqual(mod_e, mod_a, delta);
    }
    public static void AreAngleNotEqual(double expected, double actual)
    {
        double mod_e = (expected % pi2 + pi2 ) % pi2;
        double mod_a = (actual % pi2 + pi2 ) % pi2;
        Assert.AreNotEqual(mod_e, mod_a);

    }
    
    public static void AreAngleEqual(double expected, double actual, double delta, string message)
    {
        double mod_e = (expected % pi2 + pi2 ) % pi2;
        double mod_a = (actual % pi2 + pi2 ) % pi2;
        Assert.AreEqual(mod_e, mod_a, delta, message );
    }
    public static void AreAngleNotEqual(double expected, double actual, string message)
    {
        double mod_e = (expected % pi2 + pi2 ) % pi2;
        double mod_a = (actual % pi2 + pi2 ) % pi2;
        Assert.AreNotEqual(mod_e, mod_a, message);

    }
    

}