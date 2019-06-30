using System;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
 
    public class MatrixTests
    {
        private Vector3 testVector = new Vector3()
        {
            X = 10,
            Y = 0,
            Z = 0
        };

        [Test]
        public void Transform()
        {
            var tfmtx = Matrix3d.IDMatrix();
            Assert.AreEqual(testVector, tfmtx.Transform(testVector));
        }

        [Test]
        public void Rotate()
        {
            var rotMtx = Matrix3d.IDRotateZ(Math.PI * 0.5); //rotate 90 degrees
            var rotvec = rotMtx.Transform(testVector);
            
            Assert.AreEqual(0, rotvec.X, 1.5e-15, "X should be 0, \r The vector is: " + rotvec);
            Assert.AreEqual(-testVector.X, rotvec.Y, 1.5e-15);
            Assert.AreEqual(0, rotvec.Z, 1.5e-15);

        }
        
        [Test]
        public void MultiRotate()
        {
            var rotMtx1 = Matrix3d.IDRotateZ(Math.PI * 0.5);
            var rotMtx2 = Matrix3d.IDRotateZ(Math.PI * 0.5);
            var rotMtx = rotMtx1 * rotMtx2;
            var rotvec = rotMtx.Transform(testVector);
            
            
            Assert.AreEqual(-testVector.X, rotvec.X, 1.5e-15);
            Assert.AreEqual(0, rotvec.Y, 1.5e-15);
            Assert.AreEqual(0, rotvec.Z, 1.5e-15);
            
        }

    }
}