using System;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
 
    public class MatrixTests
    {
        private Vector3 testVectorX = new Vector3() {X = 10, Y = 0, Z = 0};
        private Vector3 testVectorY = new Vector3() {X = 0, Y = 10, Z = 0};
        private Vector3 testVectorZ = new Vector3() {X = 0, Y = 0, Z = 10};
        
        [Test]
        public void Transform()
        {
            var tfmtx = Matrix3d.IDMatrix();
            Assert.AreEqual(testVectorX, tfmtx.Transform(testVectorX));
        }

        [Test]
        public void RotateX()
        {
            var rotMtx = Matrix3d.IDRotateX(Math.PI * 0.5); //rotate 90 degrees
            var rotvec = rotMtx.Transform(testVectorZ);
            
            Assert.AreEqual(0, rotvec.X, 1.5e-15, "X should be 0, \r The vector is: " + rotvec);
            Assert.AreEqual(-testVectorZ.Z, rotvec.Y, 1.5e-15, "Y should be "+ (-testVectorY.Z) + ", \r The vector is: " + rotvec);
            Assert.AreEqual(0, rotvec.Z, 1.5e-15, "Z should be 0, \r The vector is: " + rotvec);
        }
        
        [Test]
        public void RotateY()
        {
            var rotMtx = Matrix3d.IDRotateY(Math.PI * 0.5); //rotate 90 degrees
            var rotvec = rotMtx.Transform(testVectorX);
            
            Assert.AreEqual(0, rotvec.X, 1.5e-15, "X should be 0, \r The vector is: " + rotvec);
            Assert.AreEqual(0, rotvec.Y, 1.5e-15, "Y should be 0, \r The vector is: " + rotvec);
            Assert.AreEqual(-testVectorX.X, rotvec.Z, 1.5e-15, "Z should be "+ (-testVectorY.X) + ", \r The vector is: " + rotvec);

        }
        
        [Test]
        public void RotateZ()
        {
            var rotMtx = Matrix3d.IDRotateZ(Math.PI * 0.5); //rotate 90 degrees
            var rotvec = rotMtx.Transform(testVectorX);
            
            Assert.AreEqual(0, rotvec.X, 1.5e-15, "X should be 0, \r The vector is: " + rotvec);
            Assert.AreEqual(-testVectorX.X, rotvec.Y, 1.5e-15, "Y should be "+ (-testVectorY.X) + ", \r The vector is: " + rotvec);
            Assert.AreEqual(0, rotvec.Z, 1.5e-15, "Z should be 0, \r The vector is: " + rotvec);

        }
        
        [Test]
        public void MultiRotate()
        {
            var rotMtx1 = Matrix3d.IDRotateZ(Math.PI * 0.5);
            var rotMtx2 = Matrix3d.IDRotateZ(Math.PI * 0.5);
            var rotMtx = rotMtx1 * rotMtx2;
            var rotvec = rotMtx.Transform(testVectorX);
            
            
            Assert.AreEqual(-testVectorX.X, rotvec.X, 1.5e-15);
            Assert.AreEqual(0, rotvec.Y, 1.5e-15);
            Assert.AreEqual(0, rotvec.Z, 1.5e-15);
            
        }

    }
}