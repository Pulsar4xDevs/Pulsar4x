using System;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// A basic 3d matrix class and functions.
    /// To use: create Identity matrices for the operation you want to do,
    /// ie: rotateX, rotateY, -RotateX, then multiply those identity matrces together.
    /// from there, do a Translate on the product of the above with a Vector3d. 
    /// </summary>
    public struct Matrix3d
    {
        double[] X;// = new double[4] { 1, 0, 0, 0 };
        double[] Y;// = new double[4] { 0, 1, 0, 0 };
        double[] Z;// = new double[4] { 0, 0, 1, 0 };
        double[] W;// = new double[4] { 0, 0, 0, 1 };

        public Matrix3d(double single)
        {
            X = new double[4] { single, 0, 0, 0 };
            Y = new double[4] { 0, single, 0, 0 };
            Z = new double[4] { 0, 0, single, 0 };
            W = new double[4] { 0, 0, 0, single };
        }


        double[] Row(int row)
        {
            if (row == 0)
                return X;
            if (row == 1)
                return Y;
            if (row == 2)
                return Z;
            if (row == 3)
                return W;
            else throw new IndexOutOfRangeException();
        }


        public static Matrix3d IDMatrix()
        {
            return new Matrix3d()
            {
                X = new double[4] {1, 0, 0, 0 },
                Y = new double[4] {0, 1, 0, 0 },
                Z = new double[4] {0, 0, 1, 0 }, 
                W = new double[4] {0, 0, 0, 1 }
            };
            
        }

        public static Matrix3d IDRotateZ(double theta)
        {

            return new Matrix3d()
            {
                X = new double[4] { Math.Cos(theta),     Math.Sin(theta), 0, 0 },
                Y = new double[4] { -Math.Sin(theta),    Math.Cos(theta), 0, 0 },
                Z = new double[4] { 0,                   0,               1, 0 },
                W = new double[4] { 0,                   0,               0, 1 }
            };
        }
        
        public static Matrix3d IDRotateX(double theta)
        {

            return new Matrix3d()
            {
                X = new double[4] { 1, 0,                 0,                 0 },
                Y = new double[4] { 0, Math.Cos(theta),   -Math.Sin(theta),   0 },
                Z = new double[4] { 0, Math.Sin(theta),  Math.Cos(theta),   0 },
                W = new double[4] { 0, 0,                 0,                 1 }
            };
        }
        
        public static Matrix3d IDRotateY(double theta)
        {

            return new Matrix3d()
            {
                X = new double[4] { Math.Cos(theta),  Math.Sin(theta),  0,                0 },
                Y = new double[4] { 0,                1,                0,                0 },
                Z = new double[4] { -Math.Sin(theta), 0,                Math.Cos(theta),  0 },
                W = new double[4] { 0,                0,                0,                1 }
            };
        }

        public static Matrix3d IDScale(double x, double y, double z)
        {
            return new Matrix3d()
            {
                X = new double[4] {x, 0, 0, 0 },
                Y = new double[4] {0, y, 0, 0 },
                Z = new double[4] {0, 0, z, 0 }, 
                W = new double[4] {0, 0, 0, 1 }
            };
        }
        
        public static Matrix3d IDTranslate(double x, double y, double z)
        {
            return new Matrix3d()
            {
                X = new double[4] {0, 0, 0, 0 },
                Y = new double[4] {0, 0, 0, 0 },
                Z = new double[4] {0, 0, 0, 0 }, 
                W = new double[4] {x, y, z, 1 }
            };
        }

        /// <summary>
        /// Dot product of left matrix dot right matrix
        /// </summary>
        /// <param name="l"> lefthand matrix</param>
        /// <param name="r"> righthand matrix</param>
        /// <returns></returns>
        public static Matrix3d operator *(Matrix3d l, Matrix3d r)
        {
            
            Matrix3d newMatrix = new Matrix3d(1);
            
            //row0
            for (int i = 0; i < 4; i++)
            {
                newMatrix.X[i] = MultiplyRowbyColomn(l, r, 0, i);
            }
            
            //row1
            for (int i = 0; i < 4; i++)
            {
                newMatrix.Y[i] = MultiplyRowbyColomn(l, r, 1, i);
            }
            
            //row2
            for (int i = 0; i < 4; i++)
            {
                newMatrix.Z[i] = MultiplyRowbyColomn(l, r, 2, i);
            }
            
            //row3
            for (int i = 0; i < 4; i++)
            {
                newMatrix.W[i] = MultiplyRowbyColomn(l, r, 3, i);
            }
            
            return newMatrix;
        }
        


        static double MultiplyRowbyColomn(Matrix3d l, Matrix3d r, int lRow, int rColumn)
        {
            double value = 0;
            for (int i = 0; i < 4; i++)
            {
                value += l.Row(lRow)[i] * r.Row(i)[rColumn];
            }
            return value;
        }
        
        static double MultiplyRowbyColomn(Matrix3d l, Vector3 r, int lRow)
        {
            double[] rarray = new[] {r.X, r.Y, r.Z, 0}; 
            double value = 0;
            for (int i = 0; i < 4; i++)
            {
                value += l.Row(lRow)[i] * rarray[i];
            }
            return value;
        }

        public Vector3 Transform(Vector3 vector)
        {
            //multiply a 4x4 matrx by a 3x1 vector;
            return new Vector3()
            {
                X = MultiplyRowbyColomn(this,vector,0),  
                Y = MultiplyRowbyColomn(this,vector,1), 
                Z = MultiplyRowbyColomn(this,vector,2), 
            };
        }

    }
}