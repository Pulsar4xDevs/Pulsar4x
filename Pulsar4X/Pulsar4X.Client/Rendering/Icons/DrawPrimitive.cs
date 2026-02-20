using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;
using SDL3;

namespace Pulsar4X.Client
{
    public static class DrawPrimitive
    {
        public static void DrawEllipse(IntPtr renderer, int posX, int posY, double xRadius, double yRadius)
        {
            byte _numberOfArcSegments = 255;

            double angle = (Math.PI * 2.0) / (_numberOfArcSegments);

            int lastX = posX + (int)Math.Round(xRadius * Math.Sin(angle));
            int lastY = posY + (int)Math.Round(yRadius * Math.Cos(angle));
            int drawX;
            int drawY;
            for (int i = 0; i < _numberOfArcSegments + 1; i++)
            {
                drawX = posX + (int)Math.Round(xRadius * Math.Sin(angle * i));
                drawY = posY + (int)Math.Round(yRadius * Math.Cos(angle * i));
                //SDL.SDL_RenderDrawPoint(renderer, drawX, drawY);
                SDL.RenderLine(renderer, lastX, lastY, drawX, drawY);
                lastX = drawX;
                lastY = drawY;
            }
        }


        /// <summary>
        /// This is maybe a naive possibly slow way of drawing a filled circle. maybe revisit this if it turns out to be slow.
        /// </summary>
        /// <param name="renderer">Renderer.</param>
        /// <param name="posX">RelativePosition x.</param>
        /// <param name="posY">RelativePosition y.</param>
        /// <param name="radius">Radius.</param>
        public static void DrawFilledCircle(IntPtr renderer, int posX, int posY, int radius)
        {

            for (int w = 0; w < radius * 2; w++)
            {
                for (int h = 0; h < radius * 2; h++)
                {
                    int dx = radius - w; // horizontal offset
                    int dy = radius - h; // vertical offset
                    if ((dx * dx + dy * dy) <= (radius * radius))
                    {
                        SDL.RenderPoint(renderer, posX, posY);
                    }
                }
            }
        }

        public static void DrawArc(IntPtr renderer, int posX, int posY, double xWidth, double yWidth, double startAngleRadians, double arcAngleRadians)
        {
            byte _numberOfArcSegments = 255;

            double incrementAngle = (Math.PI * 2.0) / (_numberOfArcSegments);

            int drawX;
            int drawY;
            int totalSegments = (int)(Math.Abs(arcAngleRadians) / incrementAngle);


            SDL.Point[] points = new SDL.Point[totalSegments];

            for (int i = 0; i < totalSegments; i++)
            {
                double nextAngle = startAngleRadians + incrementAngle * i;
                drawX = posX + (int)Math.Round(xWidth * Math.Sin(nextAngle));
                drawY = posY + (int)Math.Round(yWidth * Math.Cos(nextAngle));
                points[i] = new SDL.Point() { X = drawX, Y = drawY };
            }

            for (int i = 0; i < points.Length - 1; i++)
            {
                SDL.RenderLine(renderer, points[i].X, points[i].Y, points[i+1].X, points[i+1].Y);
            }

        }


        public static void DrawAlphaFadeArc(IntPtr rendererPtr, int posX, int posY, double xWidth, double yWidth, double startAngleRadians, double arcAngleRadians, byte startAlpha, byte endAlpha)
        {
            byte r, g, b, a;
            SDL.GetRenderDrawColor(rendererPtr, out r, out g, out b, out a);

            SDL.BlendMode blendMode;
            SDL.GetRenderDrawBlendMode(rendererPtr, out blendMode);

            SDL.SetRenderDrawBlendMode(rendererPtr, SDL.BlendMode.Blend);
            byte _numberOfArcSegments = 254;


            byte alpha = endAlpha;

            double incrementAngle = (Math.PI * 2.0) / (_numberOfArcSegments);

            int lastX = posX + (int)Math.Round(xWidth * Math.Sin(startAngleRadians));
            int lastY = posY + (int)Math.Round(yWidth * Math.Cos(startAngleRadians));
            int drawX;
            int drawY;
            int totalSegments = (int)(arcAngleRadians / incrementAngle);
            double alphaIncrement = (startAlpha - endAlpha) / totalSegments;
            for (int i = 1; i < totalSegments; i++)
            {
                alpha += (byte)(alphaIncrement);
                SDL.SetRenderDrawColor(rendererPtr, r, g, b, alpha);

                double nextAngle = startAngleRadians + incrementAngle * i;
                drawX = posX + (int)Math.Round(xWidth * Math.Sin(nextAngle));
                drawY = posY + (int)Math.Round(yWidth * Math.Cos(nextAngle));
                SDL.RenderLine(rendererPtr, lastX, lastY, drawX, drawY);
                lastX = drawX;
                lastY = drawY;
            }

            SDL.SetRenderDrawColor(rendererPtr, r, g, b, a); //set the colour back to what it was originaly
            SDL.SetRenderDrawBlendMode(rendererPtr, blendMode);
        }
    }

    public static class CreatePrimitiveShapes
    {

        public const double PI2 = Math.PI * 2;
        public const double HalfCircle = Math.PI;
        public const double QuarterCircle = Math.PI * 0.5;
        public const double ThreeQuarterCircle = HalfCircle + QuarterCircle;


        private static Shape _centerWidget;

        public static Shape CenterWidget(Matrix matrix)
        {
            if(_centerWidget.Points == null)
            {
                Orbital.Vector2[] drawpoints = new Orbital.Vector2[5];
                drawpoints[0] = new Orbital.Vector2(0, -16);
                drawpoints[1] = new Orbital.Vector2(0, 16);
                drawpoints[2] = new Orbital.Vector2(16, 0);
                drawpoints[3] = new Orbital.Vector2(-16, 0);
                drawpoints[4] = new Orbital.Vector2(0, -16);
                byte r = 150;
                byte g = 50;
                byte b = 200;
                byte a = 255;
                SDL.Color colour = new SDL.Color() { R = r, G = g, B = b, A = a};
                _centerWidget = new Shape() {Points = drawpoints, Color = colour};
            }


            Shape centerWidget = new Shape();
            centerWidget.Points = matrix.TransformToVector2(_centerWidget.Points);
            centerWidget.Color = _centerWidget.Color;
            return centerWidget;
        }

                /// <summary>
        /// Parametric ellipse taken from:
        /// "Drawing ellipses, hyperbolas or parabolas with a fixed number of points and maximum inscribed area"
        /// by L. B. Smith
        /// published in The Computer journal jan 1971 https://academic.oup.com/comjnl/article/14/1/81/356378
        /// </summary>
        /// <param name="loP">londitude of periapsis</param>
        /// <param name="a">semi major axis</param>
        /// <param name="b">semi minor axis</param>
        /// <param name="n">number of required points for a full elipse</param>
        /// <param name="arcStart"></param>
        /// <param name="arcEnd"></param>
        /// <returns></returns>
        public static Vector2[] KeplerPoints(double loP, double a, double b, int n, double arcStart, double arcEnd)
        {

            double linerEccentricity =  EllipseMath.LinearEccentricityFromAxies(a, b);

            double dphi = 2 * Math.PI / (n - 1);


            double cosTheta = Math.Cos(loP);
            double sinTheta = Math.Sin(loP);
            double cosdphi = Math.Cos(dphi);
            double sindphi = Math.Sin(dphi);
            double cosLoP = Math.Cos(loP);
            double sinLoP = Math.Sin(loP);

            //double xs = startPos.X - linerEccentricity * cosLoP;
            //double ys = startPos.Y - linerEccentricity * sinLoP;
            //double xe = endPos.X - linerEccentricity * cosLoP;
            //double ye = endPos.Y - linerEccentricity * sinLoP;
            //double arcStart = Math.Atan2(ys, xs);
            //double arcEnd = Math.Atan2(ye, xe);

            double cosStrt = Math.Cos(arcStart);
            double sinStrt = Math.Sin(arcStart);
            double cosEnd = Math.Cos(arcEnd);
            double sinEnd = Math.Sin(arcEnd);

            double arcSize = Angle.DifferenceBetweenRadians(arcStart, arcEnd);
            if (arcSize < 1 || arcSize > 2 * Math.PI)
                arcSize = 2 * Math.PI;
            int nPoints = (int)(arcSize / dphi) + 1;

            double xc = -linerEccentricity * cosLoP;
            double yc = -linerEccentricity * sinLoP;

            double alpha = cosdphi + sindphi * sinTheta * cosTheta * (a / b - b / a);
            double bravo = - sindphi * ((b * sinTheta) * (b * sinTheta) + (a * cosTheta) * (a * cosTheta)) / (a * b);
            double chrly = sindphi * ((b * cosTheta) * (b * cosTheta) + (a * sinTheta) * (a * sinTheta)) / (a * b);
            double delta = cosdphi + sindphi * sinTheta * cosTheta * (b / a - a / b);
            delta = delta - (chrly * bravo) / alpha;
            chrly = chrly / alpha;
            double x = a * cosStrt;
            double y = a * sinStrt;
            Vector2[] points = new Vector2[nPoints];
            for (int i = 0; i < nPoints -1; i++)
            {
                double xn = xc + x;
                double yn = yc + y;
                points[i] = new Vector2(xn, yn);
                x = alpha * x + bravo * y;
                y = chrly * x + delta * y;
            }

            x = a * cosEnd;
            y = a * sinEnd;
            points[nPoints - 1] = new Vector2(xc + x, yc + y);
            return points;
        }

        public static Vector2[] KeplerPoints(KeplerElements ke, Vector2 startPnt, Vector2 endPnt)
        {
            var a = ke.SemiMajorAxis;
            var e = ke.Eccentricity;
            var lop = ke.LoAN + ke.AoP;
            return KeplerPoints(a, e, lop, startPnt, endPnt);
        }


        public static Vector2[] HyperbolicPoints(double semiMaj, double eccentricity, double loP, double startAng,
                                                 int numPoints = 128)
        {
            double sweep = startAng * 2;
            double Δθ = sweep / (numPoints - 1);

            double θ = 0;
            double x = 0;
            double y = 0;
            double r = 0;

            Vector2[] points = new Vector2[numPoints + 1];
            for (int i = 0; i < numPoints; i++)
            {
                θ = Angle.NormaliseRadians((loP + startAng) - Δθ * i);
                r = EllipseMath.RadiusAtTrueAnomaly(semiMaj, eccentricity, loP, θ);
                x = r * Math.Cos(θ);
                y = r * Math.Sin(θ);
                points[i] = new Vector2(x, y);
            }
            return points;
        }

        /// <summary>
        /// Creates points for an ellipse.
        /// This formula creates more points at the periapsis and less at the apoapsis.
        /// </summary>
        /// <param name="semiMaj"></param>
        /// <param name="eccentricity"></param>
        /// <param name="loP">Longditude of Periapsis, tilt</param>
        /// <param name="startPnt"></param>
        /// <param name="endPnt"></param>
        /// <param name="numPoints"></param>
        /// <returns></returns>
        public static Vector2[] KeplerPoints(double semiMaj, double eccentricity, double loP, Vector2 startPnt, Vector2 endPnt,
                                             int numPoints = 128)
        {

            double startAng = Math.Atan2(startPnt.Y, startPnt.X);
            double endAng =  Math.Atan2(endPnt.Y, endPnt.X);


            double θ = 0;
            double x = 0;
            double y = 0;
            double r = EllipseMath.RadiusAtTrueAnomaly(semiMaj, eccentricity, loP, startAng);
            double sweep = ( endAng - startAng);
            double Δθ = 2 * Math.PI / (numPoints - 1) * Math.Sign(sweep);
            if(eccentricity >= 1)
            {
                sweep = startAng - endAng;
                Δθ = sweep / (numPoints - 1) * Math.Sign(sweep);
            }
            if (Δθ == 0)
            {
                return new Vector2[]
                {
                    startPnt,
                    endPnt
                };
            }
            numPoints = (int)Math.Abs(sweep / Δθ) + 1; //numpoints for just the arc

            Vector2[] points = new Vector2[numPoints + 1];
            for (int i = 0; i < numPoints; i++)
            {
                θ = startAng + Δθ * i;
                r = EllipseMath.RadiusAtTrueAnomaly(semiMaj, eccentricity, loP, θ);
                x = r * Math.Cos(θ);
                y = r * Math.Sin(θ);
                points[i] = new Vector2(x, y);
            }
            //lastPoint:
            points[^1] = endPnt;

            return points;
        }

                /// <summary>
        /// Creates points for an ellipse.
        /// This formula creates more points at the periapsis and less at the apoapsis.
        /// </summary>
        /// <param name="semiMaj"></param>
        /// <param name="eccentricity"></param>
        /// <param name="loP">Longditude of Periapsis, tilt</param>
        /// <param name="startPnt"></param>
        /// <param name="endPnt"></param>
        /// <param name="numPoints"></param>
        /// <returns></returns>
        public static void KeplerPoints(KeplerElements ke, Vector2 startPnt, Vector2 endPnt, ref Vector2[] points)
        {

            double startAng = Math.Atan2(startPnt.Y, startPnt.X);
            double endAng =  Math.Atan2(endPnt.Y, endPnt.X);
            double sweep = Angle.NormaliseRadiansPositive( endAng - startAng);
            var loP = ke.LoAN + ke.AoP;
            var numPoints = points.Length;
            double θ = 0;
            double x = 0;
            double y = 0;
            double r = EllipseMath.RadiusAtTrueAnomaly(ke.SemiMajorAxis, ke.Eccentricity, loP, startAng);
            //this is the amount of sweep per point, for a full circle/ellipse.
            double Δθ = sweep / numPoints;
            if (Δθ == 0)
            {
                for (int i = 0; i < numPoints; i++)
                {
                    points[i] = startPnt;
                }
                return;
            }

            for (int i = 0; i < numPoints; i++)
            {
                θ = startAng + Δθ * i;
                r = EllipseMath.RadiusAtTrueAnomaly(ke.SemiMajorAxis, ke.Eccentricity, loP, θ);
                x = r * Math.Cos(θ);
                y = r * Math.Sin(θ);
                points[i] = new Vector2(x, y);
            }
            //lastPoint:
            θ = endAng;
            r = EllipseMath.RadiusAtTrueAnomaly(ke.SemiMajorAxis, ke.Eccentricity, loP, θ);
            points[^1] = endPnt;
        }

        /// <summary>
        /// Creates the arc.
        /// </summary>
        /// <returns>The arc.</returns>
        /// <param name="posX">RelativePosition x.</param>
        /// <param name="posY">RelativePosition y.</param>
        /// <param name="xRadius">X width.</param>
        /// <param name="yRadius">Y width.</param>
        /// <param name="startAngleRadians">Start angle in radians.</param>
        /// <param name="arcAngleRadians">Arc angle in radians.</param>
        /// <param name="segments">Number of segments this arc will have, resolution. ie a full circle with 6 arcs will draw a hexigon.</param>
        public static Orbital.Vector2[] CreateArc(int posX, int posY, double xRadius, double yRadius, double startAngleRadians, double arcAngleRadians, int segments)
        {
            Orbital.Vector2[] points = new Orbital.Vector2[segments + 1];

            double incrementAngle = arcAngleRadians / segments;

            double drawX;
            double drawY;

            for (int i = 0; i < segments + 1; i++)
            {
                double nextAngle = startAngleRadians + incrementAngle * i;
                drawX = posX + xRadius * Math.Sin(nextAngle);
                drawY = posY + yRadius * Math.Cos(nextAngle);
                points[i] = new Orbital.Vector2() { X = drawX, Y = drawY };
            }

            return points;
        }

        public static Orbital.Vector2[] AngleArc(Orbital.Vector2 ctrPos, double radius, double tipLen, double startAngleRadians, double arcAngleRadians, int segments)
        {

            double drawX;
            double drawY;

            Orbital.Vector2[] points = new Orbital.Vector2[segments + 3];

            points[0] = new Orbital.Vector2()
            {
                Y = ctrPos.Y + (radius + tipLen) * Math.Sin(startAngleRadians),
                X = ctrPos.X + (radius + tipLen) * Math.Cos(startAngleRadians)
            };

            double incrementAngle = arcAngleRadians / segments;

            for (int i = 0; i < points.Length - 2; i++)
            {
                double nextAngle = startAngleRadians + incrementAngle * i;
                drawY = ctrPos.Y + radius * Math.Sin(nextAngle);
                drawX = ctrPos.X + radius * Math.Cos(nextAngle);
                points[i+1] = new Orbital.Vector2() { X = drawX, Y = drawY };
            }

            points[points.Length - 1] = new Orbital.Vector2()
            {
                Y = ctrPos.Y + (radius + tipLen) * Math.Sin(startAngleRadians + arcAngleRadians),
                X = ctrPos.X + (radius + tipLen) * Math.Cos(startAngleRadians + arcAngleRadians)
            };
            return points;
        }

        public static Orbital.Vector2[] RoundedCylinder(int minorRadius, int majorRadius, int offsetX, int offsetY)
        {
            List<Orbital.Vector2> points = new List<Orbital.Vector2>();
            double x1 = (minorRadius * 0.5);
            double y1 = (majorRadius * 0.5 - minorRadius * 0.5);

            points.AddRange(CreateArc(offsetX, (int)(y1 + offsetY), x1, x1, ThreeQuarterCircle, HalfCircle, 16));
            points.Add(new Orbital.Vector2() { X = x1 + offsetX, Y = y1 + offsetY });
            points.Add(new Orbital.Vector2() { X = x1 + offsetX, Y = -y1 + offsetY });

            points.AddRange(CreateArc(offsetX, (int)(-y1 + offsetY), x1, x1, QuarterCircle, HalfCircle, 16));
            points.Add(new Orbital.Vector2() { X = -x1 + offsetX, Y = -y1 + offsetY });
            points.Add(new Orbital.Vector2() { X = -x1 + offsetX, Y = y1 + offsetY });
            return points.ToArray();
        }

        public static Orbital.Vector2[] Crosshair()
        {
            return new Vector2[]
            {
                new Vector2(){ X = - 8, Y =  0 },
                new Vector2(){ X =  + 8, Y = 0 },
                new Vector2(){ X = 0 , Y =   - 8 },
                new Vector2(){ X = 0 , Y =   + 8 }
            };
        }

        public enum PosFrom
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
            Center
        }
        public static Vector2[] Rectangle(int posX, int posY, int width, int height, PosFrom positionFrom = PosFrom.TopLeft)
        {

            var points = new Vector2[4] ;
            Vector2 tl;
            Vector2 tr;
            Vector2 br;
            Vector2 bl;

            switch (positionFrom)
            {
                case PosFrom.TopLeft:
                    {
                        tl.X = posX;
                        tl.Y = posY;
                        tr.X = posX + width;
                        tr.Y = posY;
                        br.X = posX + width;
                        br.Y = posY + height;
                        bl.X = posX;
                        bl.Y = posY + height;
                        points = new Vector2[] { tl, tr, br, bl };
                    }
                    break;
                case PosFrom.TopRight:
                    {
                        tr.X = posX;
                        tr.Y = posY;
                        br.X = posX;
                        br.Y = posY + height;
                        bl.X = posX - width;
                        bl.Y = posY + height;
                        tl.X = posX - width;
                        tl.Y = posY;
                        points = new Vector2[] { tr, br, bl, tl };
                    }
                    break;
                case PosFrom.BottomRight:
                    {
                        br.X = posX;
                        br.Y = posY;
                        bl.X = posX - width;
                        bl.Y = posY;
                        tl.X = posX - width;
                        tl.Y = posY - height;
                        tr.X = posX;
                        tr.Y = posY - height;
                        points = new Vector2[] { br, bl, tl, tr };
                    }
                    break;
                case PosFrom.BottomLeft:
                    {

                        bl.X = posX;
                        bl.Y = posY;
                        tl.X = posX;
                        tl.Y = posY - height;
                        tr.X = posX + width;
                        tr.Y = posY - height;
                        br.X = posX + width;
                        br.Y = posY;
                        points = new Vector2[] { bl, tl, tr, br };
                    }
                    break;
                case PosFrom.Center:
                    {
                        tl.X = posX - (int)(width * 0.5);
                        tl.Y = posY - (int)(height * 0.5);
                        tr.X = posX + (int)(width * 0.5);
                        tr.Y = posY - (int)(height * 0.5);
                        br.X = posX + (int)(width * 0.5);
                        br.Y = posY + (int)(height * 0.5);
                        bl.X = posX - (int)(width * 0.5);
                        bl.Y = posY + (int)(height * 0.5);
                        points = new Vector2[] { tl, tr, br, bl, tl };
                    }
                    break;
            }
            return points;
        }

        /// <summary>
        /// CopyPasta straight from the french wikipedia page here: https://fr.wikipedia.org/wiki/Algorithme_de_trac%C3%A9_d%27arc_de_cercle_de_Bresenham#En_C#
        /// </summary>
        /// <param name="xc">center</param>
        /// <param name="yc">center</param>
        /// <param name="r">radius</param>
        /// <returns></returns>
        public static List<SDL.Point> BresenhamCircle(int xc,int yc,int r)
        {
            List<SDL.Point> ret = new List<SDL.Point>();
            int x,y,p;

            x=0;
            y=r;

            ret.Add(new SDL.Point(){ X = xc+x, Y = yc-y});

            p=3-(2*r);

            for(x=0;x<=y;x++)
            {
                if (p<0)
                {
                    p=(p+(4*x)+6);
                }
                else
                {
                    y-=1;
                    p+=((4*(x-y)+10));
                }

                ret.Add(new SDL.Point(){X = xc+x, Y = yc-y});
                ret.Add(new SDL.Point(){X = xc-x, Y = yc-y});
                ret.Add(new SDL.Point(){X = xc+x, Y = yc+y});
                ret.Add(new SDL.Point(){X = xc-x, Y = yc+y});
                ret.Add(new SDL.Point(){X = xc+y, Y = yc-x});
                ret.Add(new SDL.Point(){X = xc-y, Y = yc-x});
                ret.Add(new SDL.Point(){X = xc+y, Y = yc+x});
                ret.Add(new SDL.Point(){X = xc-y, Y = yc+x});
            }
            return ret;
        }
        public static Orbital.Vector2[] Circle(int posX, int posY, double radius, short segments)
        {
            Orbital.Vector2[] points = new Orbital.Vector2[segments + 1];
            double incrementAngle = PI2 / segments;

            double drawX;
            double drawY;

            for (int i = 0; i < segments + 1; i++)
            {
                double nextAngle = incrementAngle * i;
                drawX = posX + radius * Math.Sin(nextAngle);
                drawY = posY + radius * Math.Cos(nextAngle);
                points[i] = new Orbital.Vector2() { X = drawX, Y = drawY };
            }

            return points;

        }

        public static Vector2[] Circle(Vector2 pos, double radius, short segments)
        {
            Vector2[] points = new Vector2[segments + 1];
            double incrementAngle = PI2 / segments;

            double drawX;
            double drawY;

            for (int i = 0; i < segments + 1; i++)
            {
                double nextAngle = 0 + incrementAngle * i;
                drawX = pos.X + radius * Math.Sin(nextAngle);
                drawY = pos.Y + radius * Math.Cos(nextAngle);
                points[i] = new Vector2() { X = drawX, Y = drawY };
            }

            return points;

        }

        public static Orbital.Vector2[] CreateArrow(int len = 32)
        {
            var arrowPoints = new Orbital.Vector2[7];

            arrowPoints[0] = new Orbital.Vector2() { X =  0, Y = 0 };
            arrowPoints[1] = new Orbital.Vector2() { X =  0, Y = len -2 };
            arrowPoints[2] = new Orbital.Vector2() { X =  3, Y = len -3 };
            arrowPoints[3] = new Orbital.Vector2() { X =  0, Y = len };
            arrowPoints[4] = new Orbital.Vector2() { X = -3, Y = len -3 };
            arrowPoints[5] = new Orbital.Vector2() { X =  0, Y = len -2 };
            arrowPoints[6] = new Orbital.Vector2() { X =  0, Y = 0  };

            return arrowPoints;
        }

        public static Vector2[] BezierPoints(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float dt)
        {
            List<Vector2> _linePoints = new List<Vector2>();
            for (float t = 0.0f; t < 1.0; t += dt)
            {
                var x = BezCalc(t, p0.X, p1.X, p2.X, p3.X);
                var y = BezCalc(t, p0.Y, p1.Y, p2.Y, p3.Y);

                _linePoints.Add(new Vector2() {X = x, Y = y});
            }
            _linePoints.Add(p3);
            return _linePoints.ToArray();
        }
        private static double BezCalc(double t, double a0, double a1, double a2, double a3)
        {
            double foo = a0 * Math.Pow((1 - t), 3);
            foo += a1 * 3 * t * Math.Pow((1 - t), 2);
            foo += a2 * 3 * Math.Pow(t, 2) * (1 - t);
            foo += a3 * Math.Pow(t, 3);
            return foo;
        }

    }

    public static class DrawShapes
    {
        public static void Draw(IntPtr rendererPtr, Camera camera, Shape[] shapes)
        {
            //first get the current colour and blend mode and store this.
            byte r, g, b, a;
            SDL.BlendMode blendMode;
            SDL.GetRenderDrawColor(rendererPtr, out r, out g, out b, out a);
            SDL.GetRenderDrawBlendMode(rendererPtr, out blendMode);

            //change the blendmode to blend (maybe we should store this in Shape? probilby not, I think we'll always want blend.)
            SDL.SetRenderDrawBlendMode(rendererPtr, SDL.BlendMode.Blend);

            //go through each of the shapes
            foreach (var shape in shapes)
            {
                //set the colour as defined in the shape.
                SDL.SetRenderDrawColor(rendererPtr, shape.Color.R, shape.Color.G, shape.Color.B, shape.Color.A);
                //then go through each of the points and draw a line from one point to the next.
                for (int i = 0; i < shape.Points.Length - 1; i++)
                {
                    SDL.RenderLine(rendererPtr, (int)shape.Points[i].X, (int)shape.Points[i].Y, (int)shape.Points[i + 1].X, (int)shape.Points[i + 1].Y);
                }
            }

            //set the colour and blendmode back to what it was originaly.
            SDL.SetRenderDrawColor(rendererPtr, r, g, b, a); //set the colour back to what it was originaly
            SDL.SetRenderDrawBlendMode(rendererPtr, blendMode);
        }
    }
}
