using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Pulsar4X.Orbital;

namespace Pulsar4X.Tests
{
	internal class KeplerElementsTests
	{

		[Test]
		public void ConstructorFromStateVectorsSetsCorrectSGP([Random(10)] double sgp)
		{
			KeplerElements k = 
				new KeplerElements(sgp, Vector3.Zero, Vector3.Zero, DateTime.MinValue);
			Assert.That(k.StandardGravParameter, Is.EqualTo(sgp));
		}

		[Test]
		public void ConstructorFromStateVectorsSetsCorrectSemiMajorAxis()
		{
			// Circular orbit with r = 1
			KeplerElements k =
				new KeplerElements(1, Vector3.UnitX, Vector3.UnitY, DateTime.MinValue);
			Assert.That(k.SemiMajorAxis, Is.EqualTo(1), "Circular orbit fails");

			// Hyperbolic orbit
			k = new KeplerElements(1, Vector3.UnitX, 2*Vector3.UnitY, DateTime.MinValue);
			Assert.That(k.SemiMajorAxis, Is.EqualTo(.5), "Hyperbolic orbit fails");
		}

	}
}
