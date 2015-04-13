using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    class DataBlobTestException : Exception
    {
        public DataBlobTestException()
        {
        }

        public DataBlobTestException(string message) : base(message)
        {
        }

        public DataBlobTestException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [TestFixture]
    [Description("DataBlob Tests")]
    class DataBlobTests
    {
        private List<Type> _dataBlobTypes;
        private EntityManager _manager;

        [SetUp]
        public void Init()
        {
            _manager = new EntityManager();
            _dataBlobTypes = new List<Type>(Assembly.GetAssembly(typeof(BaseDataBlob)).GetTypes().Where(type => type.IsSubclassOf(typeof(BaseDataBlob)) && !type.IsAbstract));
        }

        [Test]
        public void TypeCount()
        {
            Assert.AreEqual(_dataBlobTypes.Count, EntityManager.BlankDataBlobMask().Length);
        }

        [Test]
        public void DeepCopyConstructor()
        {
            foreach (Type dbType in _dataBlobTypes)
            {
                ConstructorInfo constructor = dbType.GetConstructor(new []{dbType});
                if (constructor == null)
                {
                    throw new DataBlobTestException(dbType.ToString() + " does not have a Deep Copy constructor.");
                }
            }
        }

    }
}