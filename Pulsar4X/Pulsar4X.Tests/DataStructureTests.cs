using System;
using NUnit.Framework;
using Newtonsoft.Json;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Tests
{
    [TestFixture]
    [Description("Tests the SerialzationManagers Import/Export capabilities.")]
    internal class DataStructureTests
    {
        [Test]
        public void SafeDictionarySerialization()
        {
            SafeDictionary<Guid, int> safeDictionary = new SafeDictionary<Guid, int>();

            for(int i = 0; i < 100; i++)
            {
                safeDictionary.Add(Guid.NewGuid(), i);
            }

            string json = JsonConvert.SerializeObject(safeDictionary);

            SafeDictionary<Guid, int> deserializedDictionary = JsonConvert.DeserializeObject<SafeDictionary<Guid, int>>(json);

            Assert.IsTrue(safeDictionary.Equals(deserializedDictionary));
        }

        [Test]
        public void SafeListSerialization()
        {
            SafeList<Guid> safeList = new SafeList<Guid>();

            for(int i = 0; i < 100; i++)
            {
                safeList.Add(Guid.NewGuid());
            }

            string json = JsonConvert.SerializeObject(safeList);

            SafeList<Guid> deserializedList = JsonConvert.DeserializeObject<SafeList<Guid>>(json);

            Assert.IsTrue(safeList.Equals(deserializedList));
        }
    }
}