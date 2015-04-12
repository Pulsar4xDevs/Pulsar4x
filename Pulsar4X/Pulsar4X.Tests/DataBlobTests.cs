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
            _dataBlobTypes = new List<Type>(Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsSubclassOf(typeof(BaseDataBlob)) && !type.IsAbstract));
        }


    }
}