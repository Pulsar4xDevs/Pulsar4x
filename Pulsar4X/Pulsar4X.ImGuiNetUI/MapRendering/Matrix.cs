using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;
using SDL2;

namespace Pulsar4X.SDL2UI
{

    
    public class Matrix
    {
        double[] X = new double[3] { 1, 0, 0 };
        double[] Y = new double[3] { 0, 1, 0};
        double[] Z = new double[3] {0, 0, 1};

        public static Matrix IDScale(double scaleX, double scaleY)
        {
            Matrix matrix = new Matrix()
            {
                X = new double[3] { scaleX, 0, 0 },
                Y = new double[3] { 0, scaleY, 0 },
                Z = new double[3] {0, 0, 1}
            };
            return matrix;
        }

        public static Matrix IDTranslate(double translateX, double tranlsateY)
        {
            Matrix matrix = new Matrix()
            {
                X = new double[3] { 1, 0, 0},
                Y = new double[3] { 0, 1, 0 },
                Z = new double[3] {translateX, tranlsateY, 1}
            };
            return matrix;
            
        }

        public static Matrix IDMirror(bool x, bool y)
        {
            Matrix matrix = new Matrix();
            if (y)
                matrix.X = new double[3] { -1, 0, 0 };
            if (x)
                matrix.Y = new double[3] { 0, -1, 0 };

            return matrix;
        }


        public static Matrix IDRotate(double radians)
        {
            Matrix matrix = new Matrix()
            {
                X = new double[3] { Math.Cos(radians), -Math.Sin(radians), 0 },
                Y = new double[3] { Math.Sin(radians), Math.Cos(radians), 0 },
                Z = new double[3] {0, 0, 1}
            };
            return matrix;
        }

        public static Matrix IDRotate90Deg()
        {
            Matrix matrix = new Matrix()
            {
                X = new double[3] { 0, -1, 0 },
                Y = new double[3] { 1, 0, 0 },
                Z = new double[3] {0, 0, 1},
            };
            return matrix;
        }

        public static Matrix IDRotate180Deg()
        {
            Matrix matrix = new Matrix()
            {
                X = new double[3] { -1, 0, 0 },
                Y = new double[3] { 0, -1, 0 },
                Z = new double[3] {0, 0, 1},
            };
            return matrix;
        }

        public static Matrix IDRotate270Deg()
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



        public Orbital.Vector2 TransformToVector2(double itemx, double itemy)
        {
            Orbital.Vector2 newpoint2 = new Orbital.Vector2();
            newpoint2.X = X[0] * itemx + Y[0] * itemy + Z[0] * 1;
            newpoint2.Y = X[1] * itemx + Y[1] * itemy + Z[1] * 1;
            return newpoint2;
        }
        public Orbital.Vector2 TransformToVector2(Orbital.Vector2 vector)
        {
            Orbital.Vector2 newpoint2 = new Orbital.Vector2();
            newpoint2.X = X[0] * vector.X + Y[0] * vector.Y + Z[0] * 1;
            newpoint2.Y = X[1] * vector.X + Y[1] * vector.Y + Z[1] * 1;
            return newpoint2;
        }
        public Orbital.Vector2[] TransformToVector2(ICollection<Orbital.Vector2> vectors)
        {
            Orbital.Vector2[] newpoints = new Orbital.Vector2[vectors.Count];
            int i = 0;
            foreach (var point in vectors)
            {
                newpoints[i] = new Orbital.Vector2()
                {
                    X = X[0] * point.X + Y[0] * point.Y + Z[0] * 1,
                    Y = X[1] * point.X + Y[1] * point.Y + Z[1] * 1,
                };
                i++;
            }
            return newpoints;
        }

        public SDL.SDL_Point TransformToSDL_Point(double itemx, double itemy)
        {
            /*
            SDL.SDL_Point newPoint = new SDL.SDL_Point();
            var newPointd = TransformD(itemx, itemy);
            newPoint.x = (int)newPointd.X;
            newPoint.y = (int)newPointd.Y;
            return newPoint;
            */
            SDL.SDL_Point newPoint = new SDL.SDL_Point();
            newPoint.x = (int)(X[0] * itemx + Y[0] * itemy + Z[0] * 1);
            newPoint.y = (int)(X[1] * itemx + Y[1] * itemy + Z[1] * 1);
            return newPoint;
            
            
        }
        
        public SDL.SDL_Point[] TransformToSDL_Point(ICollection<SDL.SDL_Point> points)
        {
            SDL.SDL_Point[] newPoints = new SDL.SDL_Point[points.Count];
            int i = 0;
            foreach (var item in points)
            {
                newPoints[i] = TransformToSDL_Point(item.x, item.y);
                i++;
            }
            return newPoints;
        }
        
        public SDL.SDL_Point[] TransformToSDL_Point(ICollection<Orbital.Vector2> points)
        {
            SDL.SDL_Point[] newPoints = new SDL.SDL_Point[points.Count];
            int i = 0;
            foreach (var item in points)
            {
                newPoints[i] = TransformToSDL_Point(item.X, item.Y);
                i++;
            }
            return newPoints;
        }

        
        
        
        
        
        public Vector2 TransformD(Vector2 point)
        {
            return TransformD(point.X, point.Y); 
        }

        public Vector2 TransformD(double itemx, double itemy)
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
                newPoints[i] = TransformD(item);
                i++;
            }
            return newPoints;
        }
    }
}
