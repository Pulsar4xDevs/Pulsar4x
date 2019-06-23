using System;
using System.Collections.Generic;
using System.Linq;
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


            foreach (var item in _debugWidget.ElementItems)
            {
                ImGui.Text(item.NameString);
                if (ImGui.IsItemHovered())
                    item.SetHighlight(true);
                else
                    item.SetHighlight(false);
                ImGui.SameLine();
                ImGui.Text(item.DataString);
            }

        }
    }

    public class OrbitalDebugWidget : Icon
    {
        //OrbitIcon orbitIcon;
        OrbitDB _orbitDB;
        IPosition _bodyPosition;
        internal Guid EntityGuid;

        PointD _f1;
        PointD _f2;
        PointD _cP;
        PointD _coVertex;
        PointD _periapsisPnt;
        PointD _apoapsisPnt;
        double _semiMajAxis;
        double _semiMinAxis;

        double _loan;
        double _aop;
        double _loP;

        double _trueAnom;
        double _trueAnom_FromEVec;
        double _trueAnom_FromStateVec;
        
        double _eccentricAnom;
        double _eccentricAnom_FromTrueAnom;
        double _ecctricAnom_FromStateVectors;
        double _ecctricAnom_FromStateVectors2;
        
        
        double _meanAnom;
        PointD _bodyPosPnt;
        PointD _bodyEAPnt;

        double _sgp;
        double _ae;
        
        internal List<ElementItem> ElementItems = new List<ElementItem>();
        //updateables
        ElementItem _trueAnomalyAngleItem;
        
        ElementItem _trueAnomItem_FromEVec;
        ElementItem _trueAnomItem_FromStateVec;
        
        ElementItem _radiusToBody;

        ElementItem _meanAnomalyItem;
        ElementItem _eccentricAnomalyItem;
        ElementItem _eccentricAnomItem_FromTrueAnom;
        ElementItem _eccentricAnomItem_FromStateVec;
        ElementItem _eccentricAnomItem_FromStateVec2;
        ElementItem _eccentricityVectorItem;
        ElementItem _bodyPosItem;
        ElementItem _headingItem;

        List<ComplexShape> _drawComplexShapes = new List<ComplexShape>();

        public OrbitalDebugWidget(EntityState entityState) : base(entityState.OrbitIcon.BodyPositionDB)
        {
            var orbitIcon = entityState.OrbitIcon;
            _bodyPosition = orbitIcon.BodyPositionDB;

            _orbitDB = entityState.Entity.GetDataBlob<OrbitDB>();
            if (_orbitDB.Parent == null) //primary star
            {
                _positionDB = orbitIcon.BodyPositionDB;
            }
            else
            {
                _positionDB = _orbitDB.Parent.GetDataBlob<PositionDB>(); //orbit's position is parent's body position. 
            }



            EntityGuid = entityState.Entity.Guid;

            _loan = Angle.ToRadians( _orbitDB.LongitudeOfAscendingNode);
            _aop = Angle.ToRadians(_orbitDB.ArgumentOfPeriapsis);
            _loP = orbitIcon._loP_radians;

            var cP = new PointD() { X = orbitIcon.WorldPosition.X, Y = orbitIcon.WorldPosition.Y };
            cP.X -= orbitIcon._linearEccentricity;

            var f1 = new PointD() { X = cP.X + orbitIcon._linearEccentricity, Y = cP.Y};
            var f2 = new PointD() { X = cP.X - orbitIcon._linearEccentricity, Y = cP.Y};
            var coVertex = new PointD() { X = cP.X , Y = cP.Y + orbitIcon.SemiMinor };
            var periapsisPnt = new PointD() { X = cP.X - orbitIcon.SemiMaj, Y = cP.Y  };
            var apoapsisPnt = new PointD() { X = cP.X + orbitIcon.SemiMaj, Y = cP.Y  };

            _cP = DrawTools.RotatePoint(cP, _loP);
            _f1 = DrawTools.RotatePoint(f1, _loP);
            _f2 = DrawTools.RotatePoint(f2, _loP);
            _coVertex = DrawTools.RotatePoint(coVertex, _loP);
            _periapsisPnt = DrawTools.RotatePoint(periapsisPnt, _loP);
            _apoapsisPnt = DrawTools.RotatePoint(apoapsisPnt, _loP);


            _semiMajAxis = orbitIcon.SemiMaj;
            _semiMinAxis = orbitIcon.SemiMinor;
            _sgp = _orbitDB.GravitationalParameterAU;
            _ae = _semiMajAxis * _orbitDB.Eccentricity;
            
            DateTime systemDateTime = _orbitDB.Parent.Manager.ManagerSubpulses.StarSysDateTime;
            _trueAnom = OrbitProcessor.GetTrueAnomaly(_orbitDB, systemDateTime);

            
            var pos = _bodyPosition.RelativePosition_AU;
            var vel = OrbitProcessor.InstantaneousOrbitalVelocityVector(_orbitDB, systemDateTime);
            var ecvec = OrbitMath.EccentricityVector(_sgp, pos, (Vector3)vel);
            _trueAnom_FromEVec = OrbitMath.TrueAnomaly(ecvec, pos, (Vector3)vel);
            _trueAnom_FromStateVec = OrbitMath.TrueAnomaly(_sgp, pos, (Vector3)vel);
            
            double secondsFromEpoch = (systemDateTime - _orbitDB.Epoch).TotalSeconds;
            _meanAnom = OrbitMath.GetMeanAnomalyFromTime(Angle.ToRadians(_orbitDB.MeanAnomalyAtEpoch), Angle.ToRadians(_orbitDB.MeanMotion), secondsFromEpoch);

            _eccentricAnom = OrbitProcessor.GetEccentricAnomaly(_orbitDB, _meanAnom);
            _eccentricAnom_FromTrueAnom = OrbitMath.GetEccentricAnomalyFromTrueAnomaly(_trueAnom, _orbitDB.Eccentricity);
            _ecctricAnom_FromStateVectors = OrbitMath.GetEccentricAnomalyFromStateVectors(pos, _semiMajAxis, _ae, _aop);
            _ecctricAnom_FromStateVectors2 = OrbitMath.GetEccentricAnomalyFromStateVectors2(_sgp, _semiMajAxis, pos, (Vector3)vel);
            
            
            _bodyPosPnt = new PointD()
            {
                X = (_bodyPosition.AbsolutePosition_AU + _worldPosition).X,
                Y = (_bodyPosition.AbsolutePosition_AU + _worldPosition).Y
            };
            CreateLines();

        }



        void CreateLines()
        {
            SDL.SDL_Color[] SMAColour =
            {   new SDL.SDL_Color() { r = 0, g = 160, b = 0, a = 100 }};
            SDL.SDL_Color[] SMAHighlight =
            {   new SDL.SDL_Color() { r = 0, g = 160, b = 0, a = 255 },};

            ElementItem sma1 = new ElementItem()
            {
                NameString = "Semi Major Axis (a) ",
                Colour = SMAColour,
                HighlightColour = SMAHighlight,
                DataItem = _semiMajAxis,
                DataString = Distance.AuToKm(_semiMajAxis).ToString() + "Km",
                Shape = new ComplexShape()
                {
                    Points = new PointD[]
                {
                    _cP,
                    _periapsisPnt
                },
                    Colors = SMAColour,
                    ColourChanges = new (int pointIndex, int colourIndex)[]
                    {
                        (0, 0)
                    },
                    Scales = true
                },

            };
            ElementItems.Add(sma1);
            ElementItem sma2 = new ElementItem() 
            {
                NameString = "Semi Major Axis 2",
                Colour = SMAColour,
                HighlightColour = SMAHighlight,
                DataItem = _semiMajAxis,
                Shape = new ComplexShape()
            {
                Points = new PointD[]
                {
                    _cP,
                    _apoapsisPnt
                },
                Colors = SMAColour,
                    ColourChanges = new (int pointIndex, int colourIndex)[]
                    {
                        (0, 0)
                    },
                    Scales = true
            } 
            };
            ElementItems.Add(sma2);
            ElementItem sma3 = new ElementItem()
            {
                NameString = "Distance from focal point to the covertex is also (a)",
                Colour = SMAColour,
                HighlightColour = SMAHighlight,
                DataItem = _semiMajAxis,
                Shape = new ComplexShape()
                {
                    Points = new PointD[]
                    {
                    _f1,
                    _coVertex
                    },
                    Colors = SMAColour,
                    ColourChanges = new (int pointIndex, int colourIndex)[]
                    {
                        (0, 0)
                    },
                    Scales = true
                }
            };
            ElementItems.Add(sma3);
            
            var listSMaj = new List<PointD>();
            listSMaj.AddRange(CreatePrimitiveShapes.Circle(_cP, _semiMajAxis, 255));
            ElementItem sma4 = new ElementItem()
            {
                NameString = "Semi Major Axis Circle",
                Colour = SMAColour,
                HighlightColour = SMAHighlight,
                DataItem = _semiMajAxis,
                Shape = new ComplexShape()
                {
                    Points = listSMaj.ToArray(),
                    Colors = SMAColour,
                    ColourChanges = new (int pointIndex, int colourIndex)[]
                    {
                        (0, 0)
                    },
                    Scales = true
                }
            };
            ElementItems.Add(sma4);

            SDL.SDL_Color[] SMinAColour =
                { new SDL.SDL_Color(){ r = 0, g = 0, b = 150, a = 100 } };
            SDL.SDL_Color[] SMinAHighlight =
                { new SDL.SDL_Color(){ r = 0, g = 0, b = 150, a = 255 } };

            ElementItem smina1 = new ElementItem()
            {
                NameString = "Semi Minor Axis (b)",
                Colour = SMinAColour,
                HighlightColour = SMinAHighlight,
                DataItem = _semiMinAxis,
                DataString = Distance.AuToKm(_semiMinAxis).ToString() + " Km",
                Shape = new ComplexShape()
                {
                    Points = new PointD[]
                    {
                        _cP,
                        _coVertex
                    },
                    Colors = SMinAColour,
                    ColourChanges = new (int pointIndex, int colourIndex)[]
                    {
                        (0, 0)
                    },
                    Scales = true
                    }
            };
            ElementItems.Add(smina1);


            //fuckit I can't figure this one out
            /*
            ElementItem smina2 = new ElementItem()
            {
                NameString = "Semi Minor Axis (b)",
                Colour = SMinAColour,
                HighlightColour = SMinAHighlight,
                DataItem = _semiMinAxis,
                Shape = new ComplexShape()
                {
                    Points = new Points = new PointD[]
                    {
                        _cP,
                        new PointD(){X = _cP.X - _coVertex.X, Y = _cP.Y - _coVertex.Y }
                    },
                    Colors = SMinAColour,
                    ColourChanges = new Tuple<int, int>[]
                    {
                        new Tuple<int, int>(0,0),
                    },
                        Scales = true
                    }
            }};*/


            var listSMin = new List<PointD>();
            listSMin.AddRange(CreatePrimitiveShapes.Circle(_cP, _semiMinAxis, 255));
            ElementItem smina2 = new ElementItem()
            {
                NameString = "Semi Minor Axis Circle",
                Colour = SMinAColour,
                HighlightColour = SMinAHighlight,
                DataItem = _semiMinAxis,
                Shape = new ComplexShape()
                {
                    Points = listSMin.ToArray(),
                    Colors = SMinAColour,
                    ColourChanges = new (int pointIndex, int colourIndex)[] 
                    { 
                        (0, 0) 
                    },
                    Scales = true
                }
            };
            ElementItems.Add(smina2);
            SDL.SDL_Color[] LeColour =
            { new SDL.SDL_Color() { r = 0, g = 170, b = 10, a = 100 } };
            SDL.SDL_Color[] LeHighlight =
            { new SDL.SDL_Color() { r = 0, g = 170, b = 10, a = 255 } };
            var akm = Distance.AuToKm(_semiMajAxis) ;
            var e = _orbitDB.Eccentricity;
            string datastring = akm.ToString() + " * " + e.ToString() + " = " + (akm * e).ToString() + "Km";
           ElementItem linec1 = new ElementItem()
            {
                NameString = "Linear Eccentricity (a * e)",
                Colour = LeColour,
                HighlightColour = LeHighlight,
                DataItem = akm * e,
                DataString = datastring,
                Shape = new ComplexShape()
                {
                    Points = new PointD[]
                    {
                        _cP,
                        _f1
                    },
                    Colors = LeColour,
                    ColourChanges = new (int pointIndex, int colourIndex)[]
                    {
                        (0, 0)
                    },
                    Scales = true
                }
            };
            ElementItems.Add(linec1);
            ElementItem linec2 = new ElementItem()
            {
                NameString = "Linear Eccentricity 2",
                Colour = LeColour,
                HighlightColour = LeHighlight,
                DataItem = _semiMajAxis * _orbitDB.Eccentricity,
                
                Shape = new ComplexShape()
                {
                    Points = new PointD[]
                    {
                        _cP,
                        _f2
                    },
                    Colors = LeColour,
                    ColourChanges = new (int pointIndex, int colourIndex)[]
                    {
                        (0, 0)
                    },
                    Scales = true
                }
            };
            ElementItems.Add(linec2);

            SDL.SDL_Color[] F1PointColour =
            {   new SDL.SDL_Color(){ r = 200, g = 0, b = 0, a = 100 },
                new SDL.SDL_Color(){a = 0} };
            SDL.SDL_Color[] F1PointHighlight =
            {   new SDL.SDL_Color(){ r = 200, g = 0, b = 0, a = 255 },
                new SDL.SDL_Color(){a = 0} };
            ElementItem f1pnt = new ElementItem()
            {
                NameString = "Focal Point 1 (Barycenter)",
                Colour = F1PointColour,
                HighlightColour = F1PointHighlight,
                //DataItem = ,
                Shape = new ComplexShape()
                {
                    StartPoint = new PointD() { X = _f1.X, Y = _f1.Y },
                    Points = new PointD[]
                {
                    new PointD(){ X = - 8, Y =  0 },
                    new PointD(){ X = + 8, Y = 0 },
                    new PointD(){ X = 0 , Y =  0 - 8 },
                    new PointD(){ X = 0 , Y =  0 + 8 }
                },
                    Colors = F1PointColour,
                    ColourChanges = new (int pointIndex, int colourIndex)[]
                    {
                        (0, 0),               
                        (1,1),
                        (2,0),
                    },
                    Scales = false
                }
            };
            ElementItems.Add(f1pnt);
            ElementItem f2pnt = new ElementItem()
            {
                NameString = "Focal Point 2",
                Colour = F1PointColour,
                HighlightColour = F1PointHighlight,
                //DataItem = ,
                Shape = new ComplexShape()
                {
                    StartPoint = new PointD() { X = _f2.X, Y = _f2.Y },
                    Points = new PointD[]
                {
                    new PointD(){ X = - 8, Y =  0 },
                    new PointD(){ X =  + 8, Y = 0 },
                    new PointD(){ X = 0 , Y =   - 8 },
                    new PointD(){ X = 0 , Y =   + 8 }
                },
                    Colors = F1PointColour,
                    ColourChanges = new (int pointIndex, int colourIndex)[]
                    {
                        (0, 0),
                        (1,1),
                        (2,0),
                    },
                    Scales = false
                }
            };
            ElementItems.Add(f2pnt);

            SDL.SDL_Color[] coVertexColour =
            {   new SDL.SDL_Color(){ r = 50, g = 50, b = 0, a = 100 },
                new SDL.SDL_Color(){a = 0} };
            SDL.SDL_Color[] coVertexHColour =
            {   new SDL.SDL_Color(){ r = 150, g = 50, b = 0, a = 255 },
                new SDL.SDL_Color(){a = 0} };
            ElementItem coVertex = new ElementItem()
            {
                NameString = "Co-Vertex",
                Colour = coVertexColour,
                HighlightColour = coVertexHColour,
                //DataItem = ,
                Shape = new ComplexShape()
                {
                    StartPoint = new PointD() { X = _coVertex.X, Y = _coVertex.Y },
                    Points = new PointD[]
                    {
                    new PointD(){ X = - 8, Y =  0 },
                    new PointD(){ X =  + 8, Y = 0 },
                    new PointD(){ X = 0 , Y =   - 8 },
                    new PointD(){ X = 0 , Y =   + 8 }
                    },
                    Colors = coVertexColour,
                    ColourChanges = new (int pointIndex, int colourIndex)[]
                    {
                        (0, 0),
                        (1,1),
                        (2,0),
                    },
                    Scales = false
                }
            };
            ElementItems.Add(coVertex);

            SDL.SDL_Color[] objPntColour =
                {   new SDL.SDL_Color(){ r = 50, g = 50, b = 0, a = 100 },
                    new SDL.SDL_Color(){a = 0} };
            SDL.SDL_Color[] objPntHColour =
                {   new SDL.SDL_Color(){ r = 150, g = 50, b = 0, a = 255 },
                    new SDL.SDL_Color(){a = 0} };
            _bodyPosItem = new ElementItem()
            {
                NameString = "Object Position (P)",
                Colour = objPntColour,
                HighlightColour = objPntHColour,
                //DataItem = ,
                Shape = new ComplexShape()
                {
                    StartPoint = new PointD() { X = _bodyPosPnt.X, Y = _bodyPosPnt.Y },
                    Points = new PointD[]
                    {
                    new PointD(){ X = - 8, Y =  0 },
                    new PointD(){ X =  + 8, Y = 0 },
                    new PointD(){ X = 0 , Y =   - 8 },
                    new PointD(){ X = 0 , Y =   + 8 }
                    },
                    Colors = objPntColour,
                    ColourChanges = new (int pointIndex, int colourIndex)[]
                    {
                        (0, 0),
                        (1,1),
                        (2,0),
                    },
                    Scales = false
                }
            };
            ElementItems.Add(_bodyPosItem);

            //loan angle
            SDL.SDL_Color[] loanColour =
                { new SDL.SDL_Color() { r = 0, g = 100, b = 0, a = 100 } };
            SDL.SDL_Color[] loanHColour =
                { new SDL.SDL_Color() { r = 0, g = 100, b = 0, a = 255 } };
            ElementItem loanAngle = new ElementItem()
            {
                NameString = "Longditude Of Assending Node (Ω)",
                Colour = loanColour,
                HighlightColour = loanHColour,
                DataItem = Angle.ToDegrees(_loan),
                DataString = Angle.ToDegrees(_loan).ToString() + "°",
                Shape = new ComplexShape()
                {
                    Points = CreatePrimitiveShapes.AngleArc(_cP, 63, -6, 0, _loan, 128),
                    Colors = loanColour,
                    ColourChanges = new (int pointIndex, int colourIndex)[]
                    {
                        (0,0),
                    },
                    Scales = false
                }
            };
            ElementItems.Add(loanAngle);

            //aop angle
            SDL.SDL_Color[] aopColour = { new SDL.SDL_Color() { r = 100, g = 0, b = 100, a = 100 } };
            SDL.SDL_Color[] aopHColour = { new SDL.SDL_Color() { r = 100, g = 0, b = 100, a = 255 } };
            ElementItem aopAngle = new ElementItem()
            {
                NameString = "Argument Of Periapsis (ω)",
                Colour = aopColour,
                HighlightColour = aopHColour,
                DataItem = Angle.ToDegrees(_aop),
                DataString = Angle.ToDegrees(_aop).ToString() + "°",
                Shape = new ComplexShape()
                {
                    Points = CreatePrimitiveShapes.AngleArc(_cP, 63, -6, _loan, _aop, 128),
                    Colors = aopColour,
                    ColourChanges = new (int,int)[]
                    {
                        (0,0)
                    },
                    Scales = false
                }
            };
            ElementItems.Add(aopAngle);

            //lop angle
            SDL.SDL_Color[] lopColour = { new SDL.SDL_Color() { r = 100, g = 100, b = 60, a = 100 } };
            SDL.SDL_Color[] lopHColour = { new SDL.SDL_Color() { r = 100, g = 100, b = 60, a = 255 } };
            ElementItem lopAngle = new ElementItem()
            {
                NameString = "Longditude Of Periapsis",
                Colour = lopColour,
                HighlightColour = lopHColour,
                DataItem = Angle.ToDegrees(_loP),
                DataString = Angle.ToDegrees(_loP).ToString() + "°",
                Shape = new ComplexShape()
                {
                    Points = CreatePrimitiveShapes.AngleArc(_cP, 65, 6, 0, _loP, 128),
                    Colors = lopColour,
                    ColourChanges = new (int pointIndex,int colourIndex)[]{ (0, 0) },
                    Scales = false
                }
            };
            ElementItems.Add(lopAngle);

            #region TrueAnomaly
            
            //trueAnom angle index 3
            SDL.SDL_Color[] trueAnomColour = 
            {   
                new SDL.SDL_Color { r = 100, g = 0, b = 0, a = 100 },
            };
            SDL.SDL_Color[] trueAnomHColour = 
            { 
                new SDL.SDL_Color { r = 100, g = 0, b = 0, a = 255},
            };


            _trueAnomalyAngleItem = new ElementItem()
            {
                NameString = "True Anomoly ν",
                Colour = trueAnomColour,
                HighlightColour = trueAnomHColour,
                DataItem = Angle.ToDegrees(_trueAnom),
                DataString = Angle.ToDegrees(_trueAnom).ToString() + "°",
                Shape = new ComplexShape()
                {
                    Points = CreatePrimitiveShapes.AngleArc(_cP, 78, 6, _loP, _trueAnom, 128),
                Colors = trueAnomColour,
                    ColourChanges = new (int,int)[]
                    {
                        (0,0),
                    },
                    Scales = false
            } };
            ElementItems.Add(_trueAnomalyAngleItem);
            
            _trueAnomItem_FromEVec = new ElementItem()
            {
                NameString = "True Anomoly ν - EcentrictyVectorCalc",
                Colour = trueAnomColour,
                HighlightColour = trueAnomHColour,
                DataItem = Angle.ToDegrees(_trueAnom_FromEVec),
                DataString = Angle.ToDegrees(_trueAnom_FromEVec).ToString() + "°",
                Shape = new ComplexShape()
                {
                    Points = CreatePrimitiveShapes.AngleArc(_cP, 80, 6, _loP, _trueAnom_FromEVec, 128),
                    Colors = trueAnomColour,
                    ColourChanges = new (int,int)[]
                    {
                        (0,0),
                    },
                    Scales = false
                } };
            ElementItems.Add(_trueAnomItem_FromEVec);
            
            _trueAnomItem_FromStateVec = new ElementItem()
            {
                NameString = "True Anomoly ν - StateVectorsCalc",
                Colour = trueAnomColour,
                HighlightColour = trueAnomHColour,
                DataItem = Angle.ToDegrees(_trueAnom_FromStateVec),
                DataString = Angle.ToDegrees(_trueAnom_FromStateVec).ToString() + "°",
                Shape = new ComplexShape()
                {
                    Points = CreatePrimitiveShapes.AngleArc(_cP, 84, 6, _loP, _trueAnom_FromStateVec, 128),
                    Colors = trueAnomColour,
                    ColourChanges = new (int,int)[]
                    {
                        (0,0),
                    },
                    Scales = false
                } };
            ElementItems.Add(_trueAnomItem_FromStateVec);
            #endregion
            
            _radiusToBody = new ElementItem()
            {
                NameString = "Radius (r)",
                Colour = trueAnomColour,
                HighlightColour = trueAnomHColour,
                DataItem = _bodyPosition.RelativePosition_AU.Length(),
                DataString = Distance.AuToKm(_bodyPosition.RelativePosition_AU.Length()) + "Km",
                Shape = new ComplexShape()
                {
                    Points = new PointD[]{
                        _f1,
                        _bodyPosPnt
                        },
                    Colors = trueAnomColour,
                    ColourChanges = new (int,int)[]
                    {
                        (0,0),
                    },
                    Scales = true
                }
            };
            ElementItems.Add(_radiusToBody);



            //MeanAnom angle index 4
            SDL.SDL_Color[] meanAnomColour = { new SDL.SDL_Color() { r = 100, g = 100, b = 100, a = 100 } };
            SDL.SDL_Color[] meanAnomHColour = { new SDL.SDL_Color() { r = 100, g = 100, b = 100, a = 255 } };

            _meanAnomalyItem = new ElementItem()
            {
                NameString = "Mean Anomoly (M)",
                Colour = meanAnomColour,
                HighlightColour = meanAnomHColour,
                DataItem = Angle.ToDegrees(_meanAnom),
                DataString = Angle.ToDegrees(_meanAnom).ToString() + "°",
                Shape = new ComplexShape()
                {
                    Points = CreatePrimitiveShapes.AngleArc(_cP, 67, 6, 0, _meanAnom, 128),
                    Colors = meanAnomColour,
                    ColourChanges = new (int,int)[]
                    {
                        (0,0),
                    },
                    Scales = false
                }
            };
            ElementItems.Add(_meanAnomalyItem);

            #region EccentricAnomaly

            //EccentricAnom angle index 5
            SDL.SDL_Color[] eAnomColour = { new SDL.SDL_Color() { r = 100, g = 0, b = 100, a = 100 } };
            SDL.SDL_Color[] eAnomHColour = { new SDL.SDL_Color() { r = 100, g = 0, b = 100, a = 255 } };
            _eccentricAnomalyItem = new ElementItem()
            {
                NameString = "Eccentric Anomoly (E)",
                Colour = eAnomColour,
                HighlightColour = eAnomHColour,
                DataItem = Angle.ToDegrees(_eccentricAnom),
                DataString = Angle.ToDegrees(_eccentricAnom).ToString() + "°",
                Shape = new ComplexShape()
                {
                    StartPoint = new PointD() { X = _cP.X, Y = _cP.Y },
                    Points = CreatePrimitiveShapes.AngleArc(new PointD() { X = 0, Y = 0 }, 69, 6, _loP, _eccentricAnom, 128),
                    Colors = eAnomColour,
                    ColourChanges = new (int,int)[]
                    {
                        (0,0),
                    },
                    Scales = false
                }
            };
            ElementItems.Add(_eccentricAnomalyItem);

            _eccentricAnomItem_FromTrueAnom = new ElementItem()
            {
                NameString = "Eccentric Anomoly (E) - From True Anom Calc",
                Colour = eAnomColour,
                HighlightColour = eAnomHColour,
                DataItem = Angle.ToDegrees(_eccentricAnom_FromTrueAnom),
                DataString = Angle.ToDegrees(_eccentricAnom_FromTrueAnom).ToString() + "°",
                Shape = new ComplexShape()
                {
                    StartPoint = new PointD() { X = _cP.X, Y = _cP.Y },
                    Points = CreatePrimitiveShapes.AngleArc(new PointD() { X = 0, Y = 0 }, 73, 6, _loP, _eccentricAnom_FromTrueAnom, 128),
                    Colors = eAnomColour,
                    ColourChanges = new (int,int)[]
                    {
                        (0,0),
                    },
                    Scales = false
                }
            };
            ElementItems.Add(_eccentricAnomItem_FromTrueAnom);
            
            _eccentricAnomItem_FromStateVec = new ElementItem()
            {
                NameString = "Eccentric Anomoly (E) - From State Vec Calc",
                Colour = eAnomColour,
                HighlightColour = eAnomHColour,
                DataItem = Angle.ToDegrees(_ecctricAnom_FromStateVectors),
                DataString = Angle.ToDegrees(_ecctricAnom_FromStateVectors).ToString() + "°",
                Shape = new ComplexShape()
                {
                    StartPoint = new PointD() { X = _cP.X, Y = _cP.Y },
                    Points = CreatePrimitiveShapes.AngleArc(new PointD() { X = 0, Y = 0 }, 75, 6, _loP, _ecctricAnom_FromStateVectors, 128),
                    Colors = eAnomColour,
                    ColourChanges = new (int,int)[]
                    {
                        (0,0),
                    },
                    Scales = false
                }
            };
            ElementItems.Add(_eccentricAnomItem_FromStateVec);
            
            _eccentricAnomItem_FromStateVec2 = new ElementItem()
            {
                NameString = "Eccentric Anomoly (E) - From State Vec Calc2",
                Colour = eAnomColour,
                HighlightColour = eAnomHColour,
                DataItem = Angle.ToDegrees(_ecctricAnom_FromStateVectors2),
                DataString = Angle.ToDegrees(_ecctricAnom_FromStateVectors2).ToString() + "°",
                Shape = new ComplexShape()
                {
                    StartPoint = new PointD() { X = _cP.X, Y = _cP.Y },
                    Points = CreatePrimitiveShapes.AngleArc(new PointD() { X = 0, Y = 0 }, 77, 6, _loP, _ecctricAnom_FromStateVectors2, 128),
                    Colors = eAnomColour,
                    ColourChanges = new (int,int)[]
                    {
                        (0,0),
                    },
                    Scales = false
                }
            };
            ElementItems.Add(_eccentricAnomItem_FromStateVec2);

            
            var pos = _bodyPosition.RelativePosition_AU;
            var vel = OrbitProcessor.InstantaneousOrbitalVelocityVector(_orbitDB, _orbitDB.Parent.Manager.ManagerSubpulses.StarSysDateTime);
            var ecvec = OrbitMath.EccentricityVector(_sgp, pos, (Vector3)vel);
            var ecvec2 = OrbitMath.EccentricityVector2(_sgp, pos, (Vector3)vel);
            var evenorm = Vector3.Normalise(ecvec) * 84;
            var evenorm2 = Vector3.Normalise(ecvec2) * 84;
            PointD[] evLine =
            {
                new PointD() { X = 0, Y = 0 }, 
                new PointD(){X=evenorm.X, Y = evenorm.Y},
                new PointD() { X = 0, Y = 0 },
                new PointD(){X= evenorm2.X, Y = evenorm2.Y}
            };
            
            _eccentricityVectorItem = new ElementItem()
            {
                NameString = "EccentricityVector",
                Colour = eAnomColour,
                HighlightColour = eAnomHColour,
                //DataItem = Angle.ToDegrees(_eccentricAnom),
                //DataString = Angle.ToDegrees(heading).ToString() + "°",
                Shape = new ComplexShape()
                {
                    StartPoint = _f1,
                    Points =  evLine,
                    Colors = eAnomColour,
                    ColourChanges = new (int,int)[]
                    {
                        (0,0),
                    },
                    Scales = false
                }
            };

            ElementItems.Add(_eccentricityVectorItem);
            
            #endregion
            
            
            
            SDL.SDL_Color[] headingColour = 
            { 
                new SDL.SDL_Color() { r = 100, g = 100, b = 100, a = 100 },
                new SDL.SDL_Color() {a = 0} 
            };
            SDL.SDL_Color[] headingHColour = 
            { 
                new SDL.SDL_Color() { r = 100, g = 100, b = 100, a = 255 },
                new SDL.SDL_Color() {a = 0} 
            };

            var speed = OrbitMath.InstantaneousOrbitalSpeed(_orbitDB.GravitationalParameterAU, _bodyPosition.RelativePosition_AU.Length(), _semiMajAxis);
            speed = Distance.AuToKm(speed);
            var heading = OrbitMath.HeadingFromPeriaps(_bodyPosition.RelativePosition_AU, _orbitDB.Eccentricity, _semiMajAxis, _trueAnom);
            heading += _loP;
            var vector = OrbitProcessor.InstantaneousOrbitalVelocityVector(_orbitDB, _orbitDB.OwningEntity.Manager.ManagerSubpulses.StarSysDateTime);
            var vnorm = Distance.AuToKm(vector) * 2;//Vector4.Normalise(vector) * 64;
            var headingPoints = CreatePrimitiveShapes.AngleArc(new PointD() { X = 0, Y = 0 }, 32, 6, 0, heading, 128);
            PointD[] headingLine = { new PointD() { X=0,Y=0 }, new PointD() { X = vnorm.X, Y = vnorm.Y }, };

            _headingItem = new ElementItem()
            {
                NameString = "Heading",
                Colour = headingColour,
                HighlightColour = headingHColour,
                //DataItem = Angle.ToDegrees(_eccentricAnom),
                DataString = Angle.ToDegrees(heading).ToString() + "°",
                Shape = new ComplexShape()
                {
                    StartPoint = _bodyPosPnt,
                    Points = headingPoints.Concat(headingLine).ToArray(),//CreatePrimitiveShapes.AngleArc(new PointD() { X = 0, Y = 0 }, 32, 6, 0, -heading, 128),
                    Colors = headingColour,
                    ColourChanges = new (int,int)[]
                    {
                        (0,0),
                    },
                    Scales = false
                }
            };

            ElementItems.Add(_headingItem);

        }

        public override void OnPhysicsUpdate()
        {
            DateTime systemDateTime = _orbitDB.Parent.Manager.ManagerSubpulses.StarSysDateTime;
            double secondsFromEpoch = (systemDateTime - _orbitDB.Epoch).TotalSeconds;
            _trueAnom = OrbitProcessor.GetTrueAnomaly(_orbitDB, systemDateTime);
            _meanAnom = OrbitMath.GetMeanAnomalyFromTime(Angle.ToRadians(_orbitDB.MeanAnomalyAtEpoch), Angle.ToRadians( _orbitDB.MeanMotion), secondsFromEpoch);

            _eccentricAnom = OrbitProcessor.GetEccentricAnomaly(_orbitDB, _meanAnom);
            
            
            var meanAnom2 = OrbitMath.GetMeanAnomaly(_orbitDB.Eccentricity, _eccentricAnom);

            _trueAnomalyAngleItem.Shape.Points = CreatePrimitiveShapes.AngleArc(_cP, 80, 4, _loP, _trueAnom, 128);
            _trueAnomalyAngleItem.DataItem = Angle.ToDegrees(_trueAnom);
            _trueAnomalyAngleItem.DataString = Angle.ToDegrees(_trueAnom).ToString() + "°";

            
            var pos = _bodyPosition.RelativePosition_AU;
            var vel = OrbitProcessor.InstantaneousOrbitalVelocityVector(_orbitDB, systemDateTime);
            var ecvec = OrbitMath.EccentricityVector(_sgp, pos, (Vector3)vel);
            var ecvec2 = OrbitMath.EccentricityVector2(_sgp, pos, (Vector3)vel);
            _trueAnom_FromEVec = OrbitMath.TrueAnomaly(ecvec, pos, (Vector3)vel);
            _trueAnom_FromStateVec = OrbitMath.TrueAnomaly(_sgp, pos, (Vector3)vel);
            
            _trueAnomItem_FromEVec.Shape.Points = CreatePrimitiveShapes.AngleArc(_cP, 82, 4, _loP, _trueAnom_FromEVec, 128);
            _trueAnomItem_FromEVec.DataItem = Angle.ToDegrees(_trueAnom_FromEVec);
            _trueAnomItem_FromEVec.DataString = Angle.ToDegrees(_trueAnom_FromEVec).ToString() + "°";
            
            _trueAnomItem_FromStateVec.Shape.Points = CreatePrimitiveShapes.AngleArc(_cP, 84, 4, _loP, _trueAnom_FromStateVec, 128);
            _trueAnomItem_FromStateVec.DataItem = Angle.ToDegrees(_trueAnom_FromStateVec);
            _trueAnomItem_FromStateVec.DataString = Angle.ToDegrees(_trueAnom_FromStateVec).ToString() + "°";
            
            _radiusToBody.Shape.Points = new PointD[]
            {
                new PointD{X = _f1.X, Y = _f1.Y },
                new PointD{X = _bodyPosPnt.X, Y = _bodyPosPnt.Y }};
            _radiusToBody.DataItem = _bodyPosition.RelativePosition_AU.Length();
            _radiusToBody.DataString = Distance.AuToKm(_bodyPosition.RelativePosition_AU.Length()) + "Km";

            _meanAnomalyItem.Shape.Points = CreatePrimitiveShapes.AngleArc(_cP, 67, 6, 0, _meanAnom, 128);
            _meanAnomalyItem.DataItem = Angle.ToDegrees(_meanAnom);
            _meanAnomalyItem.DataString = Angle.ToDegrees(_meanAnom).ToString() + "°";

            _eccentricAnom_FromTrueAnom = OrbitMath.GetEccentricAnomalyFromTrueAnomaly(_trueAnom, _orbitDB.Eccentricity);
            _ecctricAnom_FromStateVectors = OrbitMath.GetEccentricAnomalyFromStateVectors(pos, _semiMajAxis, _ae, _aop);
            _ecctricAnom_FromStateVectors2 = OrbitMath.GetEccentricAnomalyFromStateVectors2(_sgp, _semiMajAxis, pos, (Vector3)vel);
            
            _eccentricAnomalyItem.Shape.Points = CreatePrimitiveShapes.AngleArc(new PointD() { X = 0, Y = 0 }, 69, 6, _loP, _eccentricAnom, 128);
            _eccentricAnomalyItem.DataItem = Angle.ToDegrees(_eccentricAnom);
            _eccentricAnomalyItem.DataString = Angle.ToDegrees(_eccentricAnom).ToString() + "°";

            _eccentricAnomItem_FromTrueAnom.Shape.Points = CreatePrimitiveShapes.AngleArc(new PointD() { X = 0, Y = 0 }, 73, 6, _loP, _eccentricAnom_FromTrueAnom, 128);
            _eccentricAnomItem_FromTrueAnom.DataItem = Angle.ToDegrees(_eccentricAnom_FromTrueAnom);
            _eccentricAnomItem_FromTrueAnom.DataString = Angle.ToDegrees(_eccentricAnom_FromTrueAnom).ToString() + "°";
            
            
            _eccentricAnomItem_FromStateVec.Shape.Points = CreatePrimitiveShapes.AngleArc(new PointD() { X = 0, Y = 0 }, 75, 6, _loP, _ecctricAnom_FromStateVectors, 128);
            _eccentricAnomItem_FromStateVec.DataItem = Angle.ToDegrees(_ecctricAnom_FromStateVectors);
            _eccentricAnomItem_FromStateVec.DataString = Angle.ToDegrees(_ecctricAnom_FromStateVectors).ToString() + "°";
            
            _eccentricAnomItem_FromStateVec2.Shape.Points = CreatePrimitiveShapes.AngleArc(new PointD() { X = 0, Y = 0 }, 77, 6, _loP, _ecctricAnom_FromStateVectors2, 128);
            _eccentricAnomItem_FromStateVec2.DataItem = Angle.ToDegrees(_ecctricAnom_FromStateVectors2);
            _eccentricAnomItem_FromStateVec2.DataString = Angle.ToDegrees(_ecctricAnom_FromStateVectors2).ToString() + "°";


            var evenorm = Vector3.Normalise(ecvec) * 84;
            var evenorm2 = Vector3.Normalise(ecvec2) * 84;
            
            PointD[] evLine =
            {
                new PointD() { X = _f1.X, Y = _f1.Y }, 
                new PointD(){X= _f1.X + evenorm.X, Y = _f1.X+ evenorm.Y},
                new PointD() { X = _f1.X, Y = _f1.Y },
                new PointD(){X= _f1.X + evenorm2.X, Y = _f1.X+ evenorm2.Y}
            };
            _eccentricityVectorItem.Shape.Points = evLine;
            
            _bodyPosPnt = new PointD() 
            { 
                X = (_bodyPosition.AbsolutePosition_AU + _worldPosition).X, 
                Y = (_bodyPosition.AbsolutePosition_AU + _worldPosition).Y 
            };
            _bodyPosItem.Shape.StartPoint = _bodyPosPnt;


            var heading = OrbitMath.HeadingFromPeriaps(_bodyPosition.RelativePosition_AU, _orbitDB.Eccentricity, _semiMajAxis, _trueAnom);
            heading += _loP;
            var vector = OrbitProcessor.InstantaneousOrbitalVelocityVector(_orbitDB, _orbitDB.OwningEntity.Manager.ManagerSubpulses.StarSysDateTime);
            var vnorm = Vectors.Vector2.Normalise(vector) * 64;
            var headingPoints = CreatePrimitiveShapes.AngleArc(new PointD() { X = 0, Y = 0 }, 32, 6, 0, heading, 128);
            PointD[] headingLine = { new PointD() { X = 0, Y = 0 }, new PointD() { X = vnorm.X, Y = vnorm.Y }, };
            //headingPoints.Concat(headingLine);
            _headingItem.Shape.StartPoint = _bodyPosPnt;
            _headingItem.Shape.Points = headingPoints.Concat(headingLine).ToArray(); // CreatePrimitiveShapes.AngleArc(new PointD() { X = 0, Y = 0 }, 32, 6, 0, -heading, 128);
            _headingItem.DataString = Angle.ToDegrees(heading).ToString() + "°";
        }

        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {

            ViewScreenPos = camera.ViewCoordinate(WorldPosition);
            Matrix nonZoomMatrix = Matrix.NewMirrorMatrix(true, false);
 
            _drawComplexShapes = new List<ComplexShape>() {};

            foreach (var item in ElementItems)
            {
                var shape = item.Shape;
                var startPoint = matrix.TransformD(shape.StartPoint.X, shape.StartPoint.Y); //add zoom transformation. 

                PointD[] points = new PointD[shape.Points.Length];

                for (int i = 0; i < shape.Points.Length; i++)
                {
                    var pnt = shape.Points[i];;

                    int x;
                    int y;
                    PointD transformedPoint;
                    if (shape.Scales)
                        transformedPoint = matrix.TransformD(pnt.X, pnt.Y); //add zoom transformation. 
                    else
                        transformedPoint = nonZoomMatrix.TransformD(pnt.X, pnt.Y);

                    x = (int)(ViewScreenPos.x + transformedPoint.X + startPoint.X);
                    y = (int)(ViewScreenPos.y + transformedPoint.Y + startPoint.Y);
                    points[i] = new PointD() { X = x, Y = y };

                }

                _drawComplexShapes.Add( new ComplexShape()
                {
                    Points = points,
                    Colors = shape.Colors,
                    ColourChanges = shape.ColourChanges
                });
            }
        }

        public override void Draw(IntPtr rendererPtr, Camera camera)
        {
            foreach (var shape in _drawComplexShapes)
            {
                int ci = 0;
                var colour = shape.Colors[shape.ColourChanges[ci].colourIndex];
                SDL.SDL_SetRenderDrawColor(rendererPtr, colour.r, colour.g, colour.b, colour.a);

                for (int i = 0; i < shape.Points.Length - 1; i++)
                {
                    if(shape.ColourChanges.Length > i && shape.ColourChanges[ci].pointIndex == i)
                    {
                        colour = shape.Colors[shape.ColourChanges[ci].colourIndex];
                        SDL.SDL_SetRenderDrawColor(rendererPtr, colour.r, colour.g, colour.b, colour.a);
                        ci++;
                    }
                    int x1 = Convert.ToInt32(shape.Points[i].X);
                    int y1 = Convert.ToInt32(shape.Points[i].Y);
                    int x2 = Convert.ToInt32(shape.Points[i + 1].X);
                    int y2 = Convert.ToInt32(shape.Points[i + 1].Y);
                    SDL.SDL_RenderDrawLine(rendererPtr, x1, y1, x2, y2);
                }
            }
        }

    }
}
