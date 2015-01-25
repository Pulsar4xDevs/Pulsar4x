using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Pulsar4X
{
    [TestFixture]
    public class EventQueueTester
    {
        int numberTestCases = 32000000;
        int testCasesToInsert = 100000;
        EventQueue queue;
        List<DateTime> eventListUnordered;
        List<DateTime> eventListOrdered;
        Random randomGen;

        public EventQueueTester()
        {
            queue = new EventQueue();
            eventListUnordered = new List<DateTime>();
            randomGen = new Random();
            for (int i = 0; i < numberTestCases + testCasesToInsert; i++)
            {
                int numberTicks = randomGen.Next();
                eventListUnordered.Add(new DateTime(numberTicks));
            }
            eventListOrdered = new List<DateTime>(eventListUnordered);
            eventListOrdered.Sort();
            for (int i = 0; i < numberTestCases; i++)
            {
                queue.insertEvent(eventListUnordered[i]);
            }

        }

        [Test]
        public void eventQueueTest()
        {
            for (int i = numberTestCases; i < testCasesToInsert + numberTestCases; i++)
            {
                queue.insertEvent(eventListUnordered[i]);
            }
            for (int i = 0; i < testCasesToInsert; i++)
            {
                DateTime dtime = eventListOrdered[i];
                if (queue.getNextEventTime() != dtime)
                {
                    Console.WriteLine("ERROR! Test cases left: " + eventListUnordered.Count);
                }
                if (queue.extractNextEvent() != dtime)
                {
                    Console.WriteLine("ERROR! Test cases left: " + eventListUnordered.Count);
                }
            }
            Console.WriteLine("Test successful. " + numberTestCases + " events generated.");
        }
    }
}

