using System;
using System.Collections.Generic;

namespace Pulsar4X.Orbital
{
    public class Matrix2d
    {
        double[] X = new double[3] { 1, 0, 0 };
        double[] Y = new double[3] { 0, 1, 0};
        double[] Z = new double[3] { 0, 0, 1};

        public static Matrix2d IDScale(double scaleX, double scaleY)
        {
            Matrix2d matrix = new Matrix2d()
            {
                X = new double[3] { scaleX, 0, 0 },
                Y = new double[3] { 0, scaleY, 0 },
                Z = new double[3] {0, 0, 1}
            };
            return matrix;
        }

        public static Matrix2d IDTranslate(double translateX, double tranlsateY)
        {
            Matrix2d matrix = new Matrix2d()
            {
                X = new double[3] { 1, 0, 0},
                Y = new double[3] { 0, 1, 0 },
                Z = new double[3] {translateX, tranlsateY, 1}
            };
            return matrix;
            
        }

        public static Matrix2d IDMirror(bool x, bool y)
        {
            Matrix2d matrix = new Matrix2d();
            if (y)
                matrix.X = new double[3] { -1, 0, 0 };
            if (x)
                matrix.Y = new double[3] { 0, -1, 0 };

            return matrix;
        }


        public static Matrix2d IDRotate(double radians)
        {
            Matrix2d matrix = new Matrix2d()
            {
                X = new double[3] { Math.Cos(radians), -Math.Sin(radians), 0 },
                Y = new double[3] { Math.Sin(radians), Math.Cos(radians), 0 },
                Z = new double[3] {0, 0, 1}
            };
            return matrix;
        }

        public static Matrix2d IDRotate90Deg()
        {
            Matrix2d matrix = new Matrix2d()
            {
                X = new double[3] { 0, -1, 0 },
                Y = new double[3] { 1, 0, 0  },
                Z = new double[3] { 0, 0, 1  },
            };
            return matrix;
        }

        public static Matrix2d IDRotate180Deg()
        {
            Matrix2d matrix = new Matrix2d()
            {
                X = new double[3] { -1,  0, 0 },
                Y = new double[3] {  0, -1, 0 },
                Z = new double[3] {  0,  0, 1},
            };
            return matrix;
        }

        public static Matrix2d IDRotate270Deg()
        {
            Matrix2d matrix = new Matrix2d()
            {
                X = new double[3] { 0, 1, 0 },
                Y = new double[3] { -1, 0, 0 },
                Z = new double[3] {0, 0, 1},
            };
            return matrix;
        }

        public static Matrix2d operator *(Matrix2d matrixA, Matrix2d matrixB)
        {
            Matrix2d newMatrix = new Matrix2d();
            newMatrix.X[0] = matrixA.X[0] * matrixB.X[0] + matrixA.X[1] * matrixB.Y[0] + matrixA.X[2] * matrixB.Z[0];
            newMatrix.X[1] = matrixA.X[0] * matrixB.X[1] + matrixA.X[1] * matrixB.Y[1] + matrixA.X[2] * matrixB.Z[1];
            newMatrix.X[2] = matrixA.X[0] * matrixB.X[2] + matrixA.X[1] * matrixB.Y[2] + matrixA.X[2] * matrixB.Z[2];

            newMatrix.Y[0] = matrixA.Y[0] * matrixB.X[0] + matrixA.Y[1] * matrixB.Y[0] + matrixA.Y[2] * matrixB.Z[0];
            newMatrix.Y[1] = matrixA.Y[0] * matrixB.X[1] + matrixA.Y[1] * matrixB.Y[1] + matrixA.Y[2] * matrixB.Z[1];
            newMatrix.Y[2] = matrixA.Y[0] * matrixB.X[2] + matrixA.Y[1] * matrixB.Y[2] + matrixA.Y[2] * matrixB.Z[2];
            
            newMatrix.Z[0] = matrixA.Z[0] * matrixB.X[0] + matrixA.Z[1] * matrixB.Y[0] + matrixA.Z[2] * matrixB.Z[0];
            newMatrix.Z[1] = matrixA.Z[0] * matrixB.X[1] + matrixA.Z[1] * matrixB.Y[1] + matrixA.Z[2] * matrixB.Z[1];
            newMatrix.Z[2] = matrixA.Z[0] * matrixB.X[2] + matrixA.Z[1] * matrixB.Y[2] + matrixA.Z[2] * matrixB.Z[2];
            return newMatrix;
        }



        public Vector2 Transform(Vector2 point)
        {
            return Transform(point.X, point.Y); 
        }

        public Vector2 Transform(double itemx, double itemy)
        {
            Vector2 newpoint2 = new Vector2();
            newpoint2.X = X[0] * itemx + Y[0] * itemy + Z[0] * 1;
            newpoint2.Y = X[1] * itemx + Y[1] * itemy + Z[1] * 1;
            
            return newpoint2;
        }


        public Vector2[] Transform(ICollection<Vector2> points)
        {
            Vector2[] newPoints = new Vector2[points.Count];
            int i = 0;
            foreach (var item in points)
            {
                newPoints[i] = Transform(item);
                i++;
            }
            return newPoints;
        }
    }
}