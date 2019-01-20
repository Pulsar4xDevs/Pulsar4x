using System;
using System.Collections.Generic;
using SDL2;

namespace Pulsar4X.SDL2UI
{
    public class Matrix
    {
        double[] X = new double[2] { 1, 0 };
        double[] Y = new double[2] { 0, 1 };

        public void Translate(double x, double y)
        {
            for (int i = 0; i < 2; i++)
            {
                X[i] = X[i] + x;
                Y[i] = Y[i] - y;
            }
        }

        public void Scale(double zoom)
        {
            for (int i = 0; i < 2; i++)
            {
                X[i] = X[i] * zoom;
                Y[i] = Y[i] * zoom;
            }
        }

        public void Rotate(double radians)
        {
            X[0] = X[0] * Math.Cos(radians);
            X[1] = X[1] * Math.Sin(radians);
            Y[0] = Y[0] * -Math.Sin(radians);
            Y[1] = Y[1] * Math.Cos(radians);
        }

        public static Matrix GetRotationMatrix(double radians)
        {
            Matrix matrix = new Matrix()
            {
                X = new double[2] {Math.Cos(radians) , Math.Sin(radians) },
                Y = new double[2] {-Math.Sin(radians), Math.Cos(radians) }
            };
            return matrix;
        }

        public static Matrix operator *(Matrix matrixA, Matrix matrixB)
        {
            Matrix newMatrix = new Matrix();
            newMatrix.X[0] = matrixA.X[0] * matrixB.X[0] + matrixA.X[1] * matrixB.Y[0];
            newMatrix.X[1] = matrixA.X[0] * matrixB.X[1] + matrixA.X[1] * matrixB.Y[1];

            newMatrix.Y[0] = matrixA.Y[0] * matrixB.X[0] + matrixA.Y[1] * matrixB.Y[0];
            newMatrix.Y[1] = matrixA.Y[0] * matrixB.X[1] + matrixA.Y[1] * matrixB.Y[1];
            return newMatrix;
        }

        public SDL.SDL_Point Transform(double itemx, double itemy)
        {

                SDL.SDL_Point newPoint = new SDL.SDL_Point();
                //multiply a 2x2 matrix by a 2x1 matrix
                double x;
                x = X[0] * itemx;
                x += X[1] * itemy;
                newPoint.x = (int)x;
                double y;
                y = Y[0] * itemx;
                y += Y[1] * itemy;
                newPoint.y = -(int)y;


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
            newPoint.X = x;
            double y;
            y = Y[0] * itemx;
            y += Y[1] * itemy;
            newPoint.Y = -y;


            return newPoint;
        }

        public SDL.SDL_Point[] Transform(ICollection<SDL.SDL_Point> points)
        {
            SDL.SDL_Point[] newPoints = new SDL.SDL_Point[points.Count];
            int i = 0;
            foreach (var item in points)
            {
                SDL.SDL_Point newPoint = new SDL.SDL_Point();
                //multiply a 2x2 matrix by a 2x1 matrix
                double x;
                x = X[0] * item.x;
                x += X[1] * item.y;
                newPoint.x = (int)x;
                double y; 
                y = Y[0] * item.x;
                y += Y[1] * item.y;
                newPoint.y = -(int)y;

                newPoints[i] = newPoint;
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
                PointD newPoint = new PointD();
                //multiply a 2x2 matrix by a 2x1 matrix
                double x;
                x = X[0] * item.X;
                x += X[1] * item.Y;
                newPoint.X = x;
                double y;
                y = Y[0] * item.X;
                y += Y[1] * item.Y;
                newPoint.Y = -y;

                newPoints[i] = newPoint;
                i++;
            }
            return newPoints;
        }
    }
}
