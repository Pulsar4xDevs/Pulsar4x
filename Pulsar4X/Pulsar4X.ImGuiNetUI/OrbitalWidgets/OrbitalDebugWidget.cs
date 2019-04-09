using System;
using System.Collections.Generic;
using ImGuiNET;
using Pulsar4X.ECSLib;
using SDL2;

namespace Pulsar4X.SDL2UI
{
    public class OrbitalDebugWindow
    {
        public static OrbitalDebugWindow _orbitalDebugWindow;
        OrbitalDebugWidget _debugWidget;
        bool _isEnabled = false;
        public OrbitalDebugWindow(EntityState entityState)
        {
            _debugWidget = new OrbitalDebugWidget(entityState); 
        }

        public static OrbitalDebugWindow GetWindow(EntityState entityState)
        { 
            if(_orbitalDebugWindow == null)
                _orbitalDebugWindow = new OrbitalDebugWindow(entityState); 
            
            if (_orbitalDebugWindow._debugWidget.EntityGuid != entityState.Entity.Guid)
                _orbitalDebugWindow = new OrbitalDebugWindow(entityState);

            return _orbitalDebugWindow;
        }

        public void Enable(bool enable, GlobalUIState state)
        {
            if (enable && !_isEnabled)
            {
                if (!state.SelectedSysMapRender.SelectedEntityExtras.Contains(_debugWidget))
                    state.SelectedSysMapRender.SelectedEntityExtras.Add(_debugWidget);
                _isEnabled = true;
            }
            else if(!enable && _isEnabled)
            {
                if (state.SelectedSysMapRender.SelectedEntityExtras.Contains(_debugWidget))
                    state.SelectedSysMapRender.SelectedEntityExtras.Remove(_debugWidget);
                _isEnabled = false;
            }
        }

        internal void Display()
        {


            if(ImGui.TreeNode("SemiMajorAxis"))
            {
                int i = 1;
                foreach (var item in _debugWidget.SemiMajorAxisLines)
                {
                    ImGui.Text(i.ToString());
                    if(ImGui.IsItemHovered())
                    {
                        item.Color = new SDL2.SDL.SDL_Color()
                        {
                            r = item.Color.r,
                            g = item.Color.g,
                            b = item.Color.b,
                            a = 255
                        };
                    }
                    else                   
                        item.Color = _debugWidget.SMAColour; 

                    i++;
                }
                ImGui.TreePop();
            }


            if (ImGui.TreeNode("SemiMinorAxis"))
            {
                int i = 1;
                foreach (var item in _debugWidget.SemiMinorAxisLines)
                {
                    ImGui.Text(i.ToString());
                    if (ImGui.IsItemHovered())
                    {
                        item.Color = new SDL.SDL_Color()
                        {
                            r = item.Color.r,
                            g = item.Color.g,
                            b = item.Color.b,
                            a = 255
                        };
                    }
                    else
                        item.Color = _debugWidget.SMinAColour;

                    i++;
                }
                ImGui.TreePop();
            }


            if (ImGui.TreeNode("LinnierEccentricity"))
            {
                int i = 1;
                foreach (var item in _debugWidget.LinierEccentricityLines)
                {
                    ImGui.Text(i.ToString());
                    if (ImGui.IsItemHovered())
                    {
                        item.Color = new SDL.SDL_Color()
                        {
                            r = item.Color.r,
                            g = item.Color.g,
                            b = item.Color.b,
                            a = 255
                        };
                    }
                    else
                        item.Color = _debugWidget.LeColour;

                    i++;
                }

                ImGui.TreePop();
            }


            if (ImGui.TreeNode("Angles "))
            {
                int i = 0;
                foreach (var item in _debugWidget.Angles)
                {
                    ImGui.Text(_debugWidget.AngleNames[i]);
                    if (ImGui.IsItemHovered())
                    {
                        item.Color = new SDL.SDL_Color()
                        {
                            r = item.Color.r,
                            g = item.Color.g,
                            b = item.Color.b,
                            a = 255
                        };
                    }
                    else
                        item.Color = _debugWidget.AngleColours[i];

                    i++;
                }

                ImGui.TreePop();
            }
        }
    }

    public class OrbitalDebugWidget : Icon
    {
        //OrbitIcon orbitIcon;
        OrbitDB _orbitDB;
        internal Guid EntityGuid;

        PointD _f1;
        PointD _f2;
        PointD _cP;
        PointD _coVertex;
        PointD _periapsisPnt;
        PointD _apoapsisPnt;
        double _semiMajAxis;
        double _semiMinAxis;

        double _loP;
        double _trueAnom;


        internal List<MutableShape> SemiMajorAxisLines = new List<MutableShape>();
        internal SDL.SDL_Color SMAColour = new SDL.SDL_Color() { r = 0, g = 160, b = 0, a = 100 };

        internal List<MutableShape> SemiMinorAxisLines = new List<MutableShape>();
        internal SDL.SDL_Color SMinAColour = new SDL.SDL_Color() { r = 0, g = 0, b = 150, a = 100 };

        internal List<MutableShape> LinierEccentricityLines = new List<MutableShape>();
        internal SDL.SDL_Color LeColour = new SDL.SDL_Color() { r = 0, g = 170, b = 10, a = 100 };

        internal List<MutableShape> Angles = new List<MutableShape>();
        internal List<SDL.SDL_Color> AngleColours = new List<SDL.SDL_Color>();
        internal List<string> AngleNames = new List<string>();

        internal List<MutableShape> AllShapes;

        public OrbitalDebugWidget(EntityState entityState) : base(entityState.OrbitIcon.BodyPositionDB)
        {
            var orbitIcon = entityState.OrbitIcon;


            _orbitDB = entityState.Entity.GetDataBlob<OrbitDB>();
            if (_orbitDB.Parent == null) //primary star
            {
                _positionDB = orbitIcon.BodyPositionDB;
            }
            else
            {
                _positionDB = _orbitDB.Parent.GetDataBlob<PositionDB>(); //orbit's position is parent's body position. 
            }


            //double x1 = _semiMajAxis * Math.Sin(_loP) - _linearEccentricity; //we add the focal distance so the focal point is "center"
            //double y1 = _semiMinAxis * Math.Cos(_loP);

            //double x1 = _orbitEllipseSemiMinor * Math.Cos(angle);
            //double y1 = _orbitEllipseSemiMaj * Math.Sin(angle) - _linearEccentricity; //we add the linearEccentricity so the focal point is "center"


            //rotates the points to allow for the LongditudeOfPeriapsis. 
            //double x2 = (x1 * Math.Cos(_loP)) - (y1 * Math.Sin(_loP));
            //double y2 = (x1 * Math.Sin(_loP)) + (y1 * Math.Cos(_loP));

            EntityGuid = entityState.Entity.Guid;


            _loP = orbitIcon._orbitAngleRadians;

            var cP = new PointD() { X = orbitIcon.WorldPosition.X, Y = orbitIcon.WorldPosition.Y };
            cP.X -= orbitIcon._linearEccentricity;

            var f1 = new PointD() { X = cP.X, Y = cP.Y + orbitIcon._linearEccentricity };
            var f2 = new PointD() { X = cP.X, Y = cP.Y - orbitIcon._linearEccentricity };
            var coVertex = new PointD() { X = cP.X + orbitIcon._orbitEllipseSemiMinor, Y = cP.Y };
            var periapsisPnt = new PointD() { X = cP.X, Y = cP.Y - orbitIcon._orbitEllipseSemiMaj };
            var apoapsisPnt = new PointD() { X = cP.X, Y = cP.Y + orbitIcon._orbitEllipseSemiMaj };

            _cP = RotatePoint(cP, _loP);
            _f1 = RotatePoint(f1, _loP);
            _f2 = RotatePoint(f2, _loP);
            _coVertex = RotatePoint(coVertex, _loP);
            _periapsisPnt = RotatePoint(periapsisPnt, _loP);
            _apoapsisPnt = RotatePoint(apoapsisPnt, _loP);


            _semiMajAxis = orbitIcon._orbitEllipseSemiMaj;
            _semiMinAxis = orbitIcon._orbitEllipseSemiMinor;


            _trueAnom = OrbitProcessor.GetTrueAnomaly(_orbitDB, _orbitDB.Parent.Manager.ManagerSubpulses.StarSysDateTime);
            CreateLines();

        }

        PointD RotatePoint(PointD point, double angle)
        {
            PointD newPoint = new PointD()
            {
                X = (point.X * Math.Cos(angle)) - (point.Y * Math.Sin(angle)),
                Y = (point.X * Math.Sin(angle)) + (point.Y * Math.Cos(angle))
            };

            return newPoint;
        }

        void CreateLines()
        {

            SemiMajorAxisLines.Add( new MutableShape()
            {
                Points = new List<PointD>
                {
                    _cP,
                    _periapsisPnt
                },
                Color = SMAColour 
            });
            SemiMajorAxisLines.Add( new MutableShape()
            {
                Points = new List<PointD>
                {
                    _cP,
                    _apoapsisPnt
                },
                Color = SMAColour
            });
            SemiMajorAxisLines.Add( new MutableShape()
            {
                Points = new List<PointD>
                {
                    _f1,
                    _coVertex
                },
                Color = SMAColour
            });

            var listSMaj = new List<PointD>();
            listSMaj.AddRange(CreatePrimitiveShapes.Circle(_cP, _semiMajAxis, 255));
            SemiMajorAxisLines.Add(new MutableShape()
            {
                Points = listSMaj,
                Color = SMAColour
            });

            SemiMinorAxisLines.Add(new MutableShape()
            {
                Points = new List<PointD>
                {
                    _cP,
                    _coVertex
                },
                Color = SMinAColour
            });
            SemiMinorAxisLines.Add(new MutableShape()
            {
                Points = new List<PointD>
                {
                    _cP,
                    new PointD(){X = -_coVertex.X, Y= _coVertex.Y }
                },
                Color = SMinAColour
            });
            var listSMin = new List<PointD>();
            listSMin.AddRange(CreatePrimitiveShapes.Circle(_cP, _semiMinAxis, 255));
            SemiMinorAxisLines.Add(new MutableShape()
            {
                Points = listSMin,
                Color = SMinAColour
            });


            LinierEccentricityLines.Add(new MutableShape()
            {
                Points = new List<PointD>
                {
                    _cP,
                    _f1 
                },
                Color = LeColour
            });
            LinierEccentricityLines.Add(new MutableShape()
            {
                Points = new List<PointD>
                {
                    _cP,
                    _f2
                },
                Color = LeColour
            });


            //lop angle
            var lopColour = new SDL.SDL_Color() { r = 100, g = 100, b = 0, a = 100 };
            var listlop = new List<PointD>();
            listlop.AddRange(CreatePrimitiveShapes.AngleArc(_cP, 64, -6, 0, _loP, 128));
            Angles.Add(new MutableShape()
            {
                Points = listlop,
                Color = lopColour,
                Scales = false
            });
            AngleColours.Add(lopColour);
            AngleNames.Add("Longditude Of Periapsis");


            //trueAnom angle
            var trueAnomColour = new SDL.SDL_Color() { r = 100, g = 0, b = 100, a = 100 };
            var listtrueAnom = new List<PointD>();
            listtrueAnom.AddRange(CreatePrimitiveShapes.AngleArc(_cP, 64, 6, _loP, _trueAnom, 128));
            Angles.Add(new MutableShape()
            {
                Points = listtrueAnom,
                Color = trueAnomColour,
                Scales = false
            });
            AngleColours.Add(trueAnomColour);
            AngleNames.Add("True Anomoly");

            AllShapes = new List<MutableShape>();
            AllShapes.AddRange(SemiMajorAxisLines);
            AllShapes.AddRange(SemiMinorAxisLines);
            AllShapes.AddRange(LinierEccentricityLines);
            AllShapes.AddRange(Angles);
        }

        public override void OnPhysicsUpdate()
        {
            _trueAnom = OrbitProcessor.GetTrueAnomaly(_orbitDB, _orbitDB.Parent.Manager.ManagerSubpulses.StarSysDateTime);
            var listtrueAnom = new List<PointD>();
            listtrueAnom.AddRange(CreatePrimitiveShapes.AngleArc(_cP, 64, 4, _loP, _trueAnom, 128));
            Angles[1].Points = listtrueAnom;
        }

        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {

            ViewScreenPos = matrix.Transform(WorldPosition.X, WorldPosition.Y);//sets the zoom position. 

            var camerapoint = camera.CameraViewCoordinate();
            var vsp = new SDL.SDL_Point()
            {
                x = ViewScreenPos.x + camerapoint.x,
                y = ViewScreenPos.y + camerapoint.y
            };
            ViewScreenPos = vsp;

            DrawShapes = new Shape[AllShapes.Count];

            int lineIndex = 0;
            foreach (MutableShape shape in AllShapes)
            {

                PointD[] points = new PointD[shape.Points.Count];
                int pntIndex = 0;
                foreach (var pnt in shape.Points)
                {
                    int x;
                    int y;
                    if (shape.Scales)
                    {
                        var translated = matrix.TransformD(pnt.X, pnt.Y); //add zoom transformation. 

                        x = (int)(ViewScreenPos.x + translated.X);
                        y = (int)(ViewScreenPos.y + translated.Y);
                    }
                    else
                    {
                        x = (int)(ViewScreenPos.x + pnt.X);
                        y = (int)(ViewScreenPos.y + pnt.Y);
                    }
                    points[pntIndex] = new PointD() { X = x, Y = y };
                    pntIndex++;
                }
                DrawShapes[lineIndex] = new Shape()
                {
                    Points = points,
                    Color = shape.Color, 
                };
                lineIndex++;
            }
            foreach (MutableShape shape in Angles)
            {

            }

        }

        public override void Draw(IntPtr rendererPtr, Camera camera)
        {
            base.Draw(rendererPtr, camera);
        }

    }
}
