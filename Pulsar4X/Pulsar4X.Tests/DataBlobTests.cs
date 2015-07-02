using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    [TestFixture]
    [Description("DataBlob Tests")]
    internal class DataBlobTests
    {
        private static readonly List<Type> DataBlobTypes = new List<Type>(Assembly.GetAssembly(typeof(BaseDataBlob)).GetTypes().Where(type => type.IsSubclassOf(typeof(BaseDataBlob)) && !type.IsAbstract));

        [SetUp]
        public void Init()
        {
            Game game = Game.NewGame("Test Game", DateTime.Now, 1);
        }

        [Test]
        public void TypeCount()
        {
            Assert.AreEqual(DataBlobTypes.Count, EntityManager.BlankDataBlobMask().Length);
        }

        /// <summary>
        /// This test ensures our DataBlobs can be created by Json during deserialization.
        /// Any type that fails this test cannot be instantiated by Json, and thus will throw an exception if you try to deserialize it.
        /// </summary>
        [Test]
        [TestCaseSource("DataBlobTypes")]
        public void JSONConstructor(Type dataBlobType)
        {
            ConstructorInfo[] constructors = dataBlobType.GetConstructors();
            Attribute jsonConstructorAttribute = new JsonConstructorAttribute();

            foreach (ConstructorInfo constructorInfo in constructors.Where(constructorInfo => constructorInfo.GetCustomAttributes().Contains(jsonConstructorAttribute)))
            {
                Assert.Pass(dataBlobType.ToString() + " will deserialize with the constructor marked with [JsonConstructor]");
            }

            foreach (ConstructorInfo constructorInfo in constructors.Where(constructorInfo => constructorInfo.GetParameters().Length == 0 && constructorInfo.IsPublic))
            {
                Assert.Pass(dataBlobType.ToString() + " will deserialize with the default parameterless constructor.");
            }

            if (constructors.Length == 1)
            {
                if (constructors[0].GetParameters().Length != 0)
                {
                    Assert.Pass(dataBlobType.ToString() + " will deserialize with the only parametrized constructor available. Make sure parameters match the Json property names saved in the Json file.");
                }
            }

            foreach (ConstructorInfo constructorInfo in constructors.Where(constructorInfo => constructorInfo.GetParameters().Length == 0 && constructorInfo.IsPrivate))
            {
                Assert.Pass(dataBlobType.ToString() + " will deserialize with the private default parameterless constructor.");
            }
            Assert.Fail(dataBlobType.ToString() + " does not have a Json constructor");

        }
    }
}