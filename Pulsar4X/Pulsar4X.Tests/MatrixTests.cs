using System;
using NUnit.Framework;
using Pulsar4X.ECSLib;
using Pulsar4X.Orbital;


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
            Assert.AreEqual(-testVectorZ.Z, rotvec.Y, 1.5e-15, "Y should be " + (-testVectorY.Z) + ", \r The vector is: " + rotvec);
            Assert.AreEqual(0, rotvec.Z, 1.5e-15, "Z should be 0, \r The vector is: " + rotvec);
        }

        [Test]
        public void RotateY()
        {
            var rotMtx = Matrix3d.IDRotateY(Math.PI * 0.5); //rotate 90 degrees
            var rotvec = rotMtx.Transform(testVectorX);

            Assert.AreEqual(0, rotvec.X, 1.5e-15, "X should be 0, \r The vector is: " + rotvec);
            Assert.AreEqual(0, rotvec.Y, 1.5e-15, "Y should be 0, \r The vector is: " + rotvec);
            Assert.AreEqual(-testVectorX.X, rotvec.Z, 1.5e-15, "Z should be " + (-testVectorY.X) + ", \r The vector is: " + rotvec);

        }

        [Test]
        public void RotateZ()
        {
            var rotMtx = Matrix3d.IDRotateZ(Math.PI * 0.5); //rotate 90 degrees
            var rotvec = rotMtx.Transform(testVectorX);

            Assert.AreEqual(0, rotvec.X, 1.5e-15, "X should be 0, \r The vector is: " + rotvec);
            Assert.AreEqual(-testVectorX.X, rotvec.Y, 1.5e-15, "Y should be " + (-testVectorY.X) + ", \r The vector is: " + rotvec);
            Assert.AreEqual(0, rotvec.Z, 1.5e-15, "Z should be 0, \r The vector is: " + rotvec);

        }

        [Test]
        public void TranslateX()
        {
            var trlMtx = Matrix3d.IDTranslate(100, 0, 0);
            var trlvec = trlMtx.Transform(testVectorX);

            Assert.AreEqual(110, trlvec.X, 0, "X should be 110, \r The vector is: " + trlvec);
            Assert.AreEqual(0, trlvec.Y, 0, "Y should be 0, \r The vector is: " + trlvec);
            Assert.AreEqual(0, trlvec.Z, 0, "Z should be 0, \r The vector is: " + trlvec);
        }

        [Test]
        public void TranslateY()
        {
            var trlMtx = Matrix3d.IDTranslate(0, 100, 0);
            var trlvec = trlMtx.Transform(testVectorX);

            Assert.AreEqual(10, trlvec.X, 0, "X should be 10, \r The vector is: " + trlvec);
            Assert.AreEqual(100, trlvec.Y, 0, "Y should be 100, \r The vector is: " + trlvec);
            Assert.AreEqual(0, trlvec.Z, 0, "Z should be 0, \r The vector is: " + trlvec);
        }

        [Test]
        public void TranslateZ()
        {
            var trlMtx = Matrix3d.IDTranslate(0, 0, 100);
            var trlvec = trlMtx.Transform(testVectorX);

            Assert.AreEqual(10, trlvec.X, 0, "X should be 10, \r The vector is: " + trlvec);
            Assert.AreEqual(0, trlvec.Y, 0, "Y should be 0, \r The vector is: " + trlvec);
            Assert.AreEqual(100, trlvec.Z, 0, "Z should be 100, \r The vector is: " + trlvec);
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
    
    public class Matrix2dTests
    {
        private Vector2 testVector2X = new Vector2() {X = 10, Y = 0,};
        private Vector2 testVector2Y = new Vector2() {X = 0, Y = 10,};
        
        [Test]
        public void Transform()
        {
            var tfmtx = new Matrix2d();
            Assert.AreEqual(testVector2X, tfmtx.Transform(testVector2X));
        }

        [Test]
        public void Rotate()
        {
            var rotMtx = Matrix2d.IDRotate(Math.PI * 0.5); //rotate 90 degrees
            var rotvec = rotMtx.Transform(testVector2X );
            
            Assert.AreEqual(0, rotvec.X, 1.5e-15, "X should be 0, \r The vector is: " + rotvec);

        }
        
        [Test]
        public void Scale()
        {
            var sclID = Matrix2d.IDScale(2, 2);
            var scaled = sclID.Transform(testVector2X);
            Assert.AreEqual(20, scaled.X, "X should be 20, \r The vector is: " + scaled);
            Assert.AreEqual(0, scaled.Y, "Y should be 0, \r The vector is: " + scaled);
        }

        [Test]
        public void Translate2d()
        {
            
            var tmtx = Matrix2d.IDTranslate(100, 0);
            var trnsformed = tmtx.Transform(testVector2X);
            
            Assert.AreEqual(110, trnsformed.X, "X should be 100, \r The vector is: " + trnsformed);
            Assert.AreEqual(0, trnsformed.Y, "Y should be 0, \r The vector is: " + trnsformed);
            
        }
    }
}