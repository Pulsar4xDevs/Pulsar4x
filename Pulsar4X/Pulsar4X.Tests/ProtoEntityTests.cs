using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;

namespace Pulsar4X.Tests;

public class MockDataBlob1 : BaseDataBlob
{
    public string Data;
}

public class MockDataBlob2 : BaseDataBlob
{
    public string Data;
}

[TestFixture]
public class ProtoEntityTests
{
    [Test]
    public void TryGetDataBlob_ReturnsTrueWhenBlobExists()
    {
        var protoEntity = new ProtoEntity(new List<BaseDataBlob> { new MockDataBlob1() });

        bool result = protoEntity.TryGetDatablob<MockDataBlob1>(out var value);

        Assert.IsTrue(result);
        Assert.IsNotNull(value);
    }

    [Test]
    public void TryGetDataBlob_ReturnsFalseWhenBlobDoesNotExist()
    {
        var protoEntity = new ProtoEntity();

        bool result = protoEntity.TryGetDatablob<MockDataBlob1>(out var value);

        Assert.IsFalse(result);
        Assert.IsNull(value);
    }

    [Test]
    public void GetDataBlob_ThrowsWhenBlobDoesNotExist()
    {
        var protoEntity = new ProtoEntity();

        Assert.Throws<System.InvalidOperationException>(() => protoEntity.GetDataBlob<MockDataBlob1>());
    }

    [Test]
    public void SetDataBlob_AddsBlob()
    {
        var protoEntity = new ProtoEntity();
        var mockDataBlob = new MockDataBlob1();

        protoEntity.SetDataBlob(mockDataBlob);

        Assert.IsTrue(protoEntity.DataBlobTypes.Contains(typeof(MockDataBlob1)));
        Assert.AreEqual(mockDataBlob, protoEntity.GetDataBlob<MockDataBlob1>());
    }

    [Test]
    public void SetDataBlob_DoesNotAddDuplicate()
    {
        var mockDataBlob = new MockDataBlob1();
        var protoEntity = new ProtoEntity(new List<BaseDataBlob> { mockDataBlob });

        protoEntity.SetDataBlob(new MockDataBlob1());

        int dbCount = protoEntity.DataBlobs.Count(dataBlob => dataBlob.GetType() == mockDataBlob.GetType());

        Assert.AreEqual(1, dbCount);
    }

    [Test]
    public void SetDataBlob_ReplacesExistingBlob()
    {
        var mockDataBlob1 = new MockDataBlob1 { Data = "OldData" };
        var protoEntity = new ProtoEntity(new List<BaseDataBlob> { mockDataBlob1 });

        var newMockDataBlob1 = new MockDataBlob1 { Data = "NewData" };
        protoEntity.SetDataBlob(newMockDataBlob1);

        Assert.AreEqual(newMockDataBlob1, protoEntity.GetDataBlob<MockDataBlob1>());
        Assert.AreEqual("NewData", protoEntity.GetDataBlob<MockDataBlob1>().Data);
    }

    [Test]
    public void IHasDataBlobs_StoresAndRetrievesDataCorrectly()
    {
        var mockDataBlob1 = new MockDataBlob1 { Data = "TestData" };
        var protoEntity = new ProtoEntity(new List<BaseDataBlob> { mockDataBlob1 });

        Assert.AreEqual("TestData", protoEntity.GetDataBlob<MockDataBlob1>().Data);
    }

    [Test]
    public void SetDataBlob_AddsNewBlobTypeAndData()
    {
        var protoEntity = new ProtoEntity(new List<BaseDataBlob> { new MockDataBlob1() });
        var mockDataBlob2 = new MockDataBlob2 { Data = "NewTypeData" };

        protoEntity.SetDataBlob(mockDataBlob2);

        Assert.IsTrue(protoEntity.DataBlobTypes.Contains(typeof(MockDataBlob2)));
        Assert.AreEqual("NewTypeData", protoEntity.GetDataBlob<MockDataBlob2>().Data);
        Assert.AreEqual(2, protoEntity.DataBlobs.Count);
    }



}