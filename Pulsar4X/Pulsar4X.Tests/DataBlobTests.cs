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

        private static readonly List<Type> DataBlobTypes = new List<Type>(Assembly.GetAssembly(typeof(BaseDataBlob)).GetTypes().Where(type => type.IsSubclassOf(typeof(BaseDataBlob)) && !type.IsAbstract)); 
        private static EntityManager _manager = new EntityManager();

        [SetUp]
        public void Init()
        {
            _manager = new EntityManager();
        }

        [Test]
        public void TypeCount()
        {
            Assert.AreEqual(DataBlobTypes.Count, EntityManager.BlankDataBlobMask().Length);
        }

        [Test, TestCaseSource("DataBlobTypes")]
        public void DeepCopyConstructor(Type dataBlobType)
        {
            ConstructorInfo constructor = dataBlobType.GetConstructor(new []{dataBlobType});
            if (constructor == null)
            {
                throw new DataBlobTestException(dataBlobType.ToString() + " does not have a Deep Copy constructor.");
            }
        }

    }
}