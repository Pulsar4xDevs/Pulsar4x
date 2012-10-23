using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;

namespace Pulsar4X.Tests
{
    [TestFixture]
    public class shipTests
    {
        [Test]
        public void testArmor()
        {
            Ship ts = new Ship();

            ts.ShipArmor = new Armor(5,1);

            Console.WriteLine("ArmorPerHS: {0}", ts.ShipArmor.ArmorPerHS);
            Console.WriteLine("Size: {0}", ts.ShipArmor.Size);
            Console.WriteLine("Cost: {0}", ts.ShipArmor.Cost);
            Console.WriteLine("Area: {0}", ts.ShipArmor.Area);
            Console.WriteLine("Depth: {0}", ts.ShipArmor.Depth);

            ts.ShipArmor.CalcArmor(5, 38.0, 5);

            Console.WriteLine("ArmorPerHS: {0}", ts.ShipArmor.ArmorPerHS);
            Console.WriteLine("Size: {0}", ts.ShipArmor.Size);
            Console.WriteLine("Cost: {0}", ts.ShipArmor.Cost);
            Console.WriteLine("Area: {0}", ts.ShipArmor.Area);
            Console.WriteLine("Depth: {0}", ts.ShipArmor.Depth);

            Console.WriteLine("Column Number {0}", ts.ShipArmor.CNum);

            for (int loop = 0; loop < ts.ShipArmor.CNum; loop++)
            {
                Console.WriteLine("Column {0} Value {1}", loop, ts.ShipArmor.Columns[loop]);
            }
        }
    }



}