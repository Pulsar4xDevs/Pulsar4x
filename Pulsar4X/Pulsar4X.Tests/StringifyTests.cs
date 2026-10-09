using NUnit.Framework;
using Pulsar4X.Api;

namespace Pulsar4X.Tests;

[TestFixture]
public class StringifyTests
{
    [Test]
    public void Volume_KeepsShipScaleInCubicMetres()
    {
        Assert.AreEqual("1010 m^3", Stringify.Volume(1010));
        Assert.AreEqual("1000 m^3", Stringify.Volume(1000));
        Assert.AreEqual("1 Km^3", Stringify.Volume(1e9));
        Assert.AreEqual("1 Mm^3", Stringify.Volume(1e18));
    }

    [Test]
    public void Area_UsesSquareKilometresAtOneMillionSquareMetres()
    {
        Assert.AreEqual("1000 m^2", Stringify.Area(1000));
        Assert.AreEqual("1 Km^2", Stringify.Area(1e6));
        Assert.AreEqual("1 Mm^2", Stringify.Area(1e12));
    }

    [Test]
    public void VolumeLtr_UsesTheLitrePrefixAndExactBoundaries()
    {
        Assert.AreEqual("1KL", Stringify.VolumeLtr(1));
        Assert.AreEqual("1ML", Stringify.VolumeLtr(1000));
        Assert.AreEqual("1GL", Stringify.VolumeLtr(1e6));
        Assert.AreEqual("1 gigalitre", Stringify.VolumeLtr(1e6, fullSuffix: true));
    }
}
