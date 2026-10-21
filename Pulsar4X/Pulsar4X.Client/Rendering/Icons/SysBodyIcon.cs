using System;
using Pulsar4X.Api;
using Pulsar4X.Orbital;
using SDL3;
using Pulsar4X.Input;

namespace Pulsar4X.Client
{
    class SysBodyIcon : Icon, IPointerHandler, IShape, IInteractable
    {
        BodyKind _bodyType;
        double _bodyRadiusAU;
        float _viewRadius;
        Random _rng;
        int _entityId;
        string _sysId;

        public byte Priority { get { return 100; } }

        public SysBodyIcon(EntitySnapshot entity, string systemId, IPosition position, double bodyRadiusAU)
            : base(position)
        {
            _bodyType = entity.Kind;
            _bodyRadiusAU = bodyRadiusAU;
            _entityId = entity.Id;
            _sysId = systemId;
            _rng = new Random(_entityId); //use entity id as a seed for psudoRandomness.

            BuildShape();
        }

        void BuildShape()
        {
            switch (_bodyType)
            {
                case BodyKind.Asteroid:
                    Asteroid();
                    break;
                case BodyKind.Planet:
                    Terestrial();
                    break;
                default:
                    Unknown();
                    break;
            }

        }

        public bool OnPointerUp(SDL.Event sevent)
        {
            if (_state == null)
                return false;
            var state = _state!;

            if (sevent.Button.Button == 1)
                state.EntityClicked(_entityId, _sysId, MouseButtons.Primary);
            else if (sevent.Button.Button == 3)
                state.EntityClicked(_entityId, _sysId, MouseButtons.Alt);
            return true;
        }

        public bool Contains(System.Drawing.PointF point)
        {
            System.Numerics.Vector2 v = new(ViewScreenPos.X, ViewScreenPos.Y);
            return System.Numerics.Vector2.Distance(v, point.ToVector2()) <= Scale * 100;
        }

        void Terestrial()
        {
            short segments = 32;
            var points = CreatePrimitiveShapes.Circle(0, 0, 100, segments);


            //colors picked out of my ass for a blue/green look.
            //TODO: use minerals for this? but migth not have that info. going to have to work in with sensor stuff.
            byte r = 0;
            byte g = 100;
            byte b = 100;
            byte a = 255;
            SDL.Color colour = new SDL.Color() { R = r, G = g, B = b, A = a };
            Shapes.Add(new Shape() { Color = colour, Points = points });
        }

        void Asteroid()
        {
            double vertDiameter = _rng.Next(50, 100);
            double horDiameter = _rng.Next(50, 100);
            int segments = _rng.Next(8, 32);
            int jagMax = _rng.Next(5, 8);
            int jagMin = _rng.Next(4, jagMax);

            var points = CreatePrimitiveShapes.CreateArc(0, 0, horDiameter, vertDiameter, 0, Math.PI * 2, segments);
            for (int i = 0; i < segments; i = i + 2)
            {
                //this is not right, need to pull the points in towards the center, not just pull them left.
                double x = points[i].X - _rng.Next(jagMin, jagMax);
                double y = points[i].Y - _rng.Next(jagMin, jagMax);
                points[i] = new Vector2() { X = x, Y = y };
            }
            //colors picked out of my ass for a brown look.
            //TODO: use minerals for this? but migth not have that info. going to have to work in with sensor stuff.
            byte r = 150;
            byte g = 100;
            byte b = 50;
            byte a = 255;
            SDL.Color colour = new SDL.Color() { R = r, G = g, B = b, A = a };
            Shapes.Add(new Shape() { Color = colour, Points = points });
        }

        void Unknown()
        {

            short segments = 24;
            var points = CreatePrimitiveShapes.Circle(0, 0, 100, segments);
            //colors picked out of my ass .
            //TODO: use minerals for this? but migth not have that info. going to have to work in with sensor stuff.
            byte r = 100;
            byte g = 100;
            byte b = 100;
            byte a = 255;
            SDL.Color colour = new SDL.Color() { R = r, G = g, B = b, A = a };
            Shapes.Add(new Shape() { Color = colour, Points = points });
        }


        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            _viewRadius = camera.ViewDistance(_bodyRadiusAU);
            var bodyType = UserOrbitSettings.FromBodyKind(_bodyType);
            float minPixels = _state == null
                ? UserOrbitSettings.DefaultIconMinPixels(bodyType)
                : UserOrbitSettings.IconMinPixelsFor(_state.UserOrbitSettingsMtx, bodyType);
            Scale = Math.Max(_viewRadius, minPixels) * 0.01f;
            base.OnFrameUpdate(matrix, camera);
        }

        public override void Draw(IntPtr rendererPtr, Camera camera)
        {
            if (DrawShapes == null || DrawShapes.Length == 0)
                return;

            // Draw filled circle for non-asteroid body types
            if (_bodyType != BodyKind.Asteroid)
            {
                var shape = DrawShapes[0];
                if (shape.Points != null && shape.Points.Length > 2)
                {
                    // True pixel radius. A planet at the zoom cap is millions of pixels
                    // across, so the fill walks only the rows inside the window, and the
                    // circle equation stays in double. An int square overflows past 46340px.
                    double radius = Scale * 100.0;
                    double cx = ViewScreenPos.X;
                    double cy = ViewScreenPos.Y;
                    int viewW = (int)camera.ViewPortSize.X;
                    int viewH = (int)camera.ViewPortSize.Y;

                    if (radius <= 0
                        || cx + radius < 0 || cy + radius < 0
                        || cx - radius > viewW || cy - radius > viewH)
                        return;

                    int y0 = (int)Math.Max(0, Math.Ceiling(cy - radius));
                    int y1 = (int)Math.Min(viewH, Math.Floor(cy + radius));
                    if (y0 > y1)
                        return;

                    // Brighter fill color derived from the body's base color, dimmed for moons
                    float brighten = _bodyType == BodyKind.Moon ? 0.8f : 1.0f;
                    byte fillR = (byte)Math.Min(255, (int)((shape.Color.R + 80) * brighten));
                    byte fillG = (byte)Math.Min(255, (int)((shape.Color.G + 80) * brighten));
                    byte fillB = (byte)Math.Min(255, (int)((shape.Color.B + 80) * brighten));
                    SDL.SetRenderDrawColor(rendererPtr, fillR, fillG, fillB, shape.Color.A);
                    for (int y = y0; y <= y1; y++)
                    {
                        double dy = y - cy;
                        double spanSq = radius * radius - dy * dy;
                        if (spanSq < 0)
                            continue;

                        double xSpan = Math.Sqrt(spanSq);
                        int xStart = (int)Math.Max(0, Math.Ceiling(cx - xSpan));
                        int xEnd = (int)Math.Min(viewW, Math.Floor(cx + xSpan));
                        if (xStart <= xEnd)
                            SDL.RenderLine(rendererPtr, xStart, y, xEnd, y);
                    }
                }
                return; // skip outline for filled bodies
            }

            // Draw outline for asteroids
            base.Draw(rendererPtr, camera);
        }
    }
}

