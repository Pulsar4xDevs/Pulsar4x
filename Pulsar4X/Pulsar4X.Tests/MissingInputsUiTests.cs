using NUnit.Framework;
using Pulsar4X.Client;

namespace Pulsar4X.Tests;

/// <summary>The Production-tab Missing Inputs section is off unless the flag is flipped.</summary>
[TestFixture]
public class MissingInputsUiTests
{
    [Test]
    public void Enabled_DefaultsToFalse()
    {
        Assert.That(MissingInputsUi.Enabled, Is.False);
    }
}
