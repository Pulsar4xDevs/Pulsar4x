using System;
using System.Collections.Generic;
using SDL2;

namespace Pulsar4X.SDL2UI
{
    public class Matrix
    {
        double[] X = new double[3] { 1, 0, 0 };
        double[] Y = new double[3] { 0, 1, 0};
        double[] Z = new double[3] {0, 0, 1};

        public static Matrix NewScaleMatrix(double scaleX, double scaleY)
        {
            Matrix matrix = new Matrix()
            {
                X = new double[3] { scaleX, 0, 0 },
                Y = new double[3] { 0, scaleY, 0 },
                Z = new double[3] {0, 0, 1}
            };
            return matrix;
        }

        public static Matrix NewTranslateMatrix(double translateX, double tranlsateY)
        {
            Matrix matrix = new Matrix()
            {
                X = new double[3] { 1, 0, 0},
                Y = new double[3] { 0, 1, 0 },
                Z = new double[3] {translateX, tranlsateY, 1}
            };
            return matrix;
            
        }

        public static Matrix NewMirrorMatrix(bool x, bool y)
        {
            Matrix matrix = new Matrix();
            if (y)
            {
                matrix.X = new double[3] { -1, 0, 0 };
            }
            if (x)
            {
                matrix.Y = new double[3] { 0, -1, 0 };
            }
            return matrix;
        }


        public static Matrix NewRotateMatrix(double radians)
        {
            Matrix matrix = new Matrix()
            {
                X = new double[3] { Math.Cos(radians), -Math.Sin(radians), 0 },
                Y = new double[3] { Math.Sin(radians), Math.Cos(radians), 0 },
                Z = new double[3] {0, 0, 1}
            };
            return matrix;
        }

        public static Matrix New90DegreeMatrix()
        {
            Matrix matrix = new Matrix()
            {
                X = new double[3] { 0, -1, 0 },
                Y = new double[3] { 1, 0, 0 },
                Z = new double[3] {0, 0, 1},
            };
            return matrix;
        }

        public static Matrix New180DegreeMatrix()
        {
            Matrix matrix = new Matrix()
            {
                X = new double[3] { -1, 0, 0 },
                Y = new double[3] { 0, -1, 0 },
                Z = new double[3] {0, 0, 1},
            };
            return matrix;
        }

        public static Matrix New270DegreeMatrix()
        {
            Matrix matrix = new Matrix()
            {
                X = new double[3] { 0, 1, 0 },
                Y = new double[3] { -1, 0, 0 },
                Z = new double[3] {0, 0, 1},
            };
            return matrix;
        }

        public static Matrix operator *(Matrix matrixA, Matrix matrixB)
        {
            Matrix newMatrix = new Matrix();
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

        public SDL.SDL_Point Transform(double itemx, double itemy)
        {
            SDL.SDL_Point newPoint = new SDL.SDL_Point();
            var newPointd = TransformD(itemx, itemy);
            newPoint.x = (int)newPointd.X;
            newPoint.y = (int)newPointd.Y;
            return newPoint;
        }

        public PointD TransformD(PointD point)
        {
            return TransformD(point.X, point.Y); 
        }

        public PointD TransformD(double itemx, double itemy)
        {

            PointD newPoint = new PointD();
            //multiply a 2x2 matrix by a 2x1 matrix
            double x;
            x = X[0] * itemx;
            x += X[1] * itemy;
            x += X[2] * 1;
            newPoint.X = x;
            double y;
            y = Y[0] * itemx;
            y += Y[1] * itemy;
            y += Y[2] * 1;
            newPoint.Y = y; 


            return newPoint;
        }

        public SDL.SDL_Point[] Transform(ICollection<SDL.SDL_Point> points)
        {
            SDL.SDL_Point[] newPoints = new SDL.SDL_Point[points.Count];
            int i = 0;
            foreach (var item in points)
            {
                newPoints[i] = Transform(item.x, item.y);
                i++;
            }
            return newPoints;
        }


        public PointD[] Transform(ICollection<PointD> points)
        {
            PointD[] newPoints = new PointD[points.Count];
            int i = 0;
            foreach (var item in points)
            {
                newPoints[i] = TransformD(item);
                i++;
            }
            return newPoints;
        }
    }
}
