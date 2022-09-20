using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;
using Pulsar4X.Orbital;
using SDL2;

namespace Pulsar4X.SDL2UI
{


    public class OrbitalDebugWindow : PulsarGuiWindow
    {
        public static OrbitalDebugWindow _orbitalDebugWindow;
        OrbitalDebugWidget _debugWidget;
        public OrbitalDebugWindow()
        {
            IsActive = false;
        }

        public static OrbitalDebugWindow GetInstance()
        { 
            if(_orbitalDebugWindow == null)
                _orbitalDebugWindow = new OrbitalDebugWindow(); 
            
            if(_uiState.LastClickedEntity != null)
            {
                if (_orbitalDebugWindow._debugWidget == null || 
                    _orbitalDebugWindow._debugWidget.EntityGuid != _uiState.LastClickedEntity.Entity.Guid)
                {
                    _orbitalDebugWindow.HardRefresh();
                }
            }
            return _orbitalDebugWindow;
        }


        public void HardRefresh()
        {
            var entityState = _uiState.LastClickedEntity;
            bool wasActive = IsActive;
            var entity = entityState.Entity;
            var hasParent = entity.GetSOIParentEntity() != null;
            IsActive = wasActive;
            if(hasParent &&
               (entity.HasDataBlob<OrbitDB>() 
                || entity.HasDataBlob<OrbitUpdateOftenDB>()) 
                || entity.HasDataBlob<NewtonMoveDB>())
                _debugWidget = new OrbitalDebugWidget(entityState);
            else
            {
                IsActive = false;
            }
        }

        private Dictionary<string, bool> lineEnabledPersistance = new Dictionary<string, bool>();

        internal override void Display()
        {
            if (_uiState.LastClickedEntity == null)
                return;            
            var entityID = _uiState.LastClickedEntity.Entity.Guid;
            if (_debugWidget == null || entityID != _debugWidget.EntityGuid)
            {
                HardRefresh();
            }


            if(IsActive && ImGui.Begin("Orbit Lines"))
            {
                ImGui.Text($"Parent: {_debugWidget.parentname}");
                ImGui.Text($"ParentPos: {_debugWidget.parentPos}");
                ImGui.Text($"focal point abs: { _debugWidget._f1a}");
                ImGui.Text($"focal point rel: { _debugWidget._f1r}");
                foreach (var item in _debugWidget.ElementItems)
                {
                    bool showlines = item.IsEnabled; //this feels like a cludgy bit of code...
                    if (lineEnabledPersistance.ContainsKey(item.NameString))
                    {
                        showlines = lineEnabledPersistance[item.NameString];
                        item.IsEnabled = showlines;
                    }
                    else
                    {
                        lineEnabledPersistance[item.NameString] = showlines;
                    }

                    
                    //ImGui.Text(item.NameString);
                    if (ImGui.Checkbox(item.NameString, ref showlines))
                    {
                        item.ShowLines = showlines;
                        item.IsEnabled = showlines;
                        lineEnabledPersistance[item.NameString] = showlines;
                    }

                    if (ImGui.IsItemHovered())
                    {
                        item.SetHighlight(true);
                        item.ShowLines = true;
                        //ImGui.Text("StartPoint: " + item.Shape.StartPoint);
                    }
                    else
                    {
                        item.SetHighlight(false);
                        item.ShowLines = showlines;
                    }

                    ImGui.SameLine();
                    ImGui.Text(item.DataString);
                }

                ImGui.End();

                if (!_uiState.SelectedSysMapRender.SelectedEntityExtras.Contains(_debugWidget))
                    _uiState.SelectedSysMapRender.SelectedEntityExtras.Add(_debugWidget);

            }
            else
            {
                if (_uiState.SelectedSysMapRender.SelectedEntityExtras.Contains(_debugWidget))
                    _uiState.SelectedSysMapRender.SelectedEntityExtras.Remove(_debugWidget);
            }

        }
    }

    public class OrbitalDebugWidget : Icon
    {
        IKepler _orbitIcon;
        KeplerElements _keplerElements;
        private IPosition _bodyPosition;

        internal Guid EntityGuid;
        internal string parentname;
        internal Vector3 parentPos;
        internal Vector2 _f1a;
        internal Vector2 _f1r;
        Vector2 _f2;
        Vector2 _cP;
        Vector2 _coVertex;
        Vector2 _periapsisPnt;
        Vector2 _apoapsisPnt;
        double _semiMajAxis;
        double _semiMinAxis;

        double _loan;
        double _aop;
        double _loP;

        double _trueAnom;
        double _trueAnom_FromEVec;
        double _trueAnom_FromStateVec;

        double _aopFromCalc1;
        double _aopFromCalc2;
        double _aopFromCalc3;
        double _aopFromCalc4;
        
        double _eccentricAnom;
        double _eccentricAnom_FromTrueAnom;
        double _ecctricAnom_FromStateVectors;
        double _ecctricAnom_FromStateVectors2;
        
        
        double _meanAnom;
        Vector2 _bodyPosPnt_m;
        Vector2 _bodyEAPnt;

        double _sgp;
        double _ae;
        
        internal List<ElementItem> ElementItems = new List<ElementItem>();
        //updateables

        ElementItem _aopItem_FromCalc1;
        ElementItem _aopItem_FromCalc2;
        //ElementItem _aopItem_FromCalc3;
        ElementItem _aopItem_FromCalc4;
        
        ElementItem _trueAnomalyAngleItem;
        
        ElementItem _trueAnomItem_FromEVec;
        ElementItem _trueAnomItem_FromStateVec;
        
        ElementItem _radiusToBody;

        ElementItem _meanAnomalyItem;
        ElementItem _eccentricAnomalyItem;
        ElementItem _eccentricAnomItem_FromTrueAnom;
        //ElementItem _eccentricAnomItem_FromStateVec;
        //ElementItem _eccentricAnomItem_FromStateVec2;
        ElementItem _eccentricityVectorItem;
        ElementItem _bodyPosItem;
        private ElementItem _bodyPosFromState;
        HeadingElement _headingItemRel;
        HeadingElement _headingItemRel2;
        HeadingElement _headingItemAbs;
        
        
        private VelVecElement _velvecItem;

        List<ComplexShape> _drawComplexShapes = new List<ComplexShape>();

        private Entity _entity;
        
        public OrbitalDebugWidget(EntityState entityState) : base(entityState.Entity.GetSOIParentPositionDB())
        {
            _entity = entityState.Entity;
            _bodyPosition = _entity.GetDataBlob<PositionDB>();
            _orbitIcon = entityState.OrbitIcon;
            
            

            
            //NOTE! ParentPositionDB references the focal point (ie parent's position) *not* the orbiting object position.
            
            var parentEntity = _entity.GetSOIParentEntity();
            _positionDB = parentEntity.GetDataBlob<PositionDB>();

            var parentMass = parentEntity.GetDataBlob<MassVolumeDB>().MassDry;
            var myMass = _entity.GetDataBlob<MassVolumeDB>().MassDry;
            _sgp = GeneralMath.StandardGravitationalParameter(myMass + parentMass); 

            EntityGuid = entityState.Entity.Guid;
            parentname = parentEntity.GetOwnersName();
            parentPos = parentEntity.GetAbsolutePosition();
            
            RefreshEccentricity();

            CreateLines();

        }

        void RefreshEccentricity()
        {
            
            if (_entity.HasDataBlob<OrbitDB>() || _entity.HasDataBlob<OrbitUpdateOftenDB>())
            {
                var orbitDB = _entity.GetDataBlob<OrbitDB>();
                if (orbitDB == null)
                    orbitDB = _entity.GetDataBlob<OrbitUpdateOftenDB>();
                _keplerElements = orbitDB.GetElements();
                
                
            }
            else
            {
                if (_entity.HasDataBlob<NewtonMoveDB>())
                {
                    _keplerElements = _entity.GetDataBlob<NewtonMoveDB>().GetElements();
                }
            }
            
            _loan =  _keplerElements.LoAN;
            _aop = _keplerElements.AoP;
            _loP = _orbitIcon.LoP_radians;
            

            var cP_a = new Vector2() { X = _orbitIcon.ParentPosDB.AbsolutePosition.X, Y = _orbitIcon.ParentPosDB.AbsolutePosition.Y };
            _f1a = new Vector2(){ X = _orbitIcon.ParentPosDB.AbsolutePosition.X, Y = _orbitIcon.ParentPosDB.AbsolutePosition.Y };
            //_f1r = new Vector2(){ X = _orbitIcon.ParentPosDB.RelativePosition_m.X, Y = _orbitIcon.ParentPosDB.RelativePosition_m.Y };
            cP_a.X -= _orbitIcon.LinearEccent;
            
            //var f1_m = new Vector2() { X = cP_r.X + _orbitIcon.LinearEccent, Y = cP_r.Y};
            var f2_m = new Vector2() { X = cP_a.X - _orbitIcon.LinearEccent, Y = cP_a.Y};
            var coVertex = new Vector2() { X = cP_a.X , Y = cP_a.Y + _orbitIcon.SemiMin };
            var periapsisPnt = new Vector2() { X = cP_a.X + _orbitIcon.SemiMaj, Y = cP_a.Y  };
            var apoapsisPnt = new Vector2() { X = cP_a.X - _orbitIcon.SemiMaj, Y = cP_a.Y  };

            
            _cP = DrawTools.RotatePointAround(cP_a, _loP, _f1a);
            
            _f2 = DrawTools.RotatePointAround(f2_m, _loP, _f1a);
            _coVertex = DrawTools.RotatePointAround(coVertex, _loP, _f1a);
            _periapsisPnt = DrawTools.RotatePointAround(periapsisPnt, _loP, _f1a);
            _apoapsisPnt = DrawTools.RotatePointAround(apoapsisPnt, _loP, _f1a);


            _semiMajAxis = _orbitIcon.SemiMaj;
            _semiMinAxis = _orbitIcon.SemiMin;
            
            _ae = _semiMajAxis * _keplerElements.Eccentricity;
            
                        DateTime systemDateTime = _entity.StarSysDateTime;
            _trueAnom = OrbitProcessor.GetTrueAnomaly(_keplerElements, systemDateTime);

            /*
            var pos_m = _bodyPosition.RelativePosition_m;
            var vel_m = OrbitMath.ObjectLocalVelocityVector(
                _sgp, 
                pos_m, 
                _semiMajAxis, 
                _keplerElements.Eccentricity, 
                _trueAnom, 
                _keplerElements.AoP); //OrbitProcessor.InstantaneousOrbitalVelocityVector_AU(_keplerElements, systemDateTime);
            */
            var state = _entity.GetRelativeState();
            var pos_m = state.pos;
            var vel_m = state.Velocity;
            
            var ecvec = OrbitMath.EccentricityVector(_sgp, pos_m, (Vector3)vel_m);
            _trueAnom_FromEVec = OrbitMath.TrueAnomaly(ecvec, pos_m, (Vector3)vel_m);
            _trueAnom_FromStateVec = OrbitMath.TrueAnomaly(_sgp, pos_m, (Vector3)vel_m);
            
            double secondsFromEpoch = (systemDateTime - _keplerElements.Epoch).TotalSeconds;
            _meanAnom = OrbitMath.GetMeanAnomalyFromTime(_keplerElements.MeanAnomalyAtEpoch, _keplerElements.MeanMotion, secondsFromEpoch);

            _eccentricAnom = OrbitProcessor.GetEccentricAnomaly(_keplerElements, _meanAnom);
            _eccentricAnom_FromTrueAnom = OrbitMath.GetEccentricAnomalyFromTrueAnomaly(_trueAnom, _keplerElements.Eccentricity);
            //_ecctricAnom_FromStateVectors = OrbitMath.GetEccentricAnomalyFromStateVectors(pos, _semiMajAxis, _ae, _aop);
            //_ecctricAnom_FromStateVectors2 = OrbitMath.GetEccentricAnomalyFromStateVectors2(_sgp, _semiMajAxis, pos, (Vector3)vel);
            
            Vector3 angularVelocity = Vector3.Cross(pos_m, (Vector3)vel_m);
            Vector3 nodeVector = Vector3.Cross(new Vector3(0, 0, 1), angularVelocity);
            
            _aopFromCalc1 = OrbitMath.GetArgumentOfPeriapsis1(nodeVector, ecvec, pos_m, vel_m);
            _aopFromCalc2 = OrbitMath.GetArgumentOfPeriapsis(pos_m, _keplerElements.Inclination, _loan, _trueAnom);
            _aopFromCalc3 = OrbitMath.GetArgumentOfPeriapsis3(_keplerElements.Inclination, ecvec, nodeVector);
            //_aopFromCalc4 = OrbitMath.GetArgumentOfPeriapsis3(_orbitDB.Inclination, ecvec, nodeVector);
            
            _bodyPosPnt_m = new Vector2()
            {
                X = (_bodyPosition.AbsolutePosition ).X,
                Y = (_bodyPosition.AbsolutePosition ).Y
            };

        }

        void CreateLines()
        {
            
            
            SDL.SDL_Color[] ctrColour =
                {   new SDL.SDL_Color() { r = 0, g = 160, b = 0, a = 100 }};
            SDL.SDL_Color[] ctrHighlight =
                {   new SDL.SDL_Color() { r = 0, g = 160, b = 0, a = 255 },};

  
            
            ElementItem ctr = new ElementItem()
            {
                NameString = "Center ",
                Colour = ctrColour,
                HighlightColour = ctrHighlight,
                DataItem = _semiMajAxis,
                DataString = Stringify.Distance(_semiMajAxis),

                Shape = new ComplexShape()
                {
                    StartPoint = _cP,
                    Points = new Vector2[]
                    {
                        new Vector2(){ X = - 8, Y =  0 },
                        new Vector2(){ X = + 8, Y = 0 },
                        new Vector2(){ X = 0 , Y =  0 - 8 },
                        new Vector2(){ X = 0 , Y =  0 + 8 }
                    },
                    Colors = ctrColour,
                    ColourChanges = new (int pointIndex, int colourIndex)[]
                    {
                        (0, 0)
                    },
                    Scales = false
                },

            };
            ElementItems.Add(ctr);
            
            
            
            
            
            
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
                DataString = Stringify.Distance(_semiMajAxis),   
                Shape = new ComplexShape()
                {
                    Points = new Vector2[]
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
                Points = new Vector2[]
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
                    Points = new Vector2[]
                    {
                    _f1a,
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
            
            var listSMaj = new List<Vector2>();
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
                DataString = Stringify.Distance(_semiMinAxis), //Distance.AuToKm(_semiMinAxis).ToString() + " Km",
                Shape = new ComplexShape()
                {
                    Points = new Vector2[]
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


            var listSMin = new List<Vector2>();
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

            //string datastring = a_m.ToString() + " * " + e.ToString() + " = " + Stringify.Distance(a_m * e);
            ElementItem linec0 = new ElementItem()
            {
                NameString = "Linear Eccentricity (from widget)",
                Colour = LeColour,
                HighlightColour = LeHighlight,
                DataItem = _orbitIcon.LinearEccent,
                DataString = Stringify.Distance(_orbitIcon.LinearEccent),
                Shape = new ComplexShape()
                {
                    Points = new Vector2[]
                    {
                        _cP,
                        _f1a
                    },
                    Colors = LeColour,
                    ColourChanges = new (int pointIndex, int colourIndex)[]
                    {
                        (0, 0)
                    },
                    Scales = true
                }
            };
            ElementItems.Add(linec0);
            
            var a_m = (_semiMajAxis) ;
            var e = _keplerElements.Eccentricity;
            string datastring = a_m.ToString() + " * " + e.ToString() + " = " + Stringify.Distance(a_m * e);
            ElementItem linec1 = new ElementItem()
            {
                NameString = "Linear Eccentricity (a * e)",
                Colour = LeColour,
                HighlightColour = LeHighlight,
                DataItem = a_m * e,
                DataString = datastring,
                Shape = new ComplexShape()
                {
                    Points = new Vector2[]
                    {
                        _cP,
                        _f1a
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
                DataItem = _semiMajAxis * _keplerElements.Eccentricity,
                
                Shape = new ComplexShape()
                {
                    Points = new Vector2[]
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
                    StartPoint = new Vector2() { X = _f1a.X, Y = _f1a.Y },
                    Points = new Vector2[]
                {
                    new Vector2(){ X = - 8, Y =  0 },
                    new Vector2(){ X = + 8, Y = 0 },
                    new Vector2(){ X = 0 , Y =  0 - 8 },
                    new Vector2(){ X = 0 , Y =  0 + 8 }
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
                    StartPoint = new Vector2() { X = _f2.X, Y = _f2.Y },
                    Points = new Vector2[]
                {
                    new Vector2(){ X = - 8, Y =  0 },
                    new Vector2(){ X =  + 8, Y = 0 },
                    new Vector2(){ X = 0 , Y =   - 8 },
                    new Vector2(){ X = 0 , Y =   + 8 }
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
                    StartPoint = new Vector2() { X = _coVertex.X, Y = _coVertex.Y },
                    Points = new Vector2[]
                    {
                    new Vector2(){ X = - 8, Y =  0 },
                    new Vector2(){ X =  + 8, Y = 0 },
                    new Vector2(){ X = 0 , Y =   - 8 },
                    new Vector2(){ X = 0 , Y =   + 8 }
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
                NameString = "Object Position (P) - from PosDB",
                Colour = objPntColour,
                HighlightColour = objPntHColour,
                //DataItem = ,
                Shape = new ComplexShape()
                {
                    StartPoint = new Vector2() { X = _bodyPosPnt_m.X, Y = _bodyPosPnt_m.Y },
                    Points = new Vector2[]
                    {
                    new Vector2(){ X = - 8, Y =  0 },
                    new Vector2(){ X =  + 8, Y = 0 },
                    new Vector2(){ X = 0 , Y =   - 8 },
                    new Vector2(){ X = 0 , Y =   + 8 }
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
            
            var state = _entity.GetRelativeState();
            var pos_m = state.pos;
            var vel_m = state.Velocity;

            var posA_m = _entity.GetAbsolutePosition();
            _bodyPosFromState = new ElementItem()
            {
                NameString = "Object Position (P) - from State",
                Colour = objPntColour,
                HighlightColour = objPntHColour,
                //DataItem = ,
                Shape = new ComplexShape()
                {
                    StartPoint = new Vector2() { X = posA_m.X, Y = posA_m.Y },
                    Points = new Vector2[]
                    {
                        new Vector2(){ X = - 8, Y =  0 },
                        new Vector2(){ X =  + 8, Y = 0 },
                        new Vector2(){ X = 0 , Y =   - 8 },
                        new Vector2(){ X = 0 , Y =   + 8 }
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
            ElementItems.Add(_bodyPosFromState);

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
                    StartPoint = _f1a,
                    Points = CreatePrimitiveShapes.AngleArc(Vector2.Zero, 63, -6, 0, _loan, 128),
                    Colors = loanColour,
                    ColourChanges = new (int pointIndex, int colourIndex)[]
                    {
                        (0,0),
                    },
                    Scales = false
                }
            };
            //
            
            ElementItems.Add(loanAngle);

            //aop angle
            SDL.SDL_Color[] aopColour = { new SDL.SDL_Color() { r = 100, g = 0, b = 100, a = 100 } };
            SDL.SDL_Color[] aopHColour = { new SDL.SDL_Color() { r = 100, g = 0, b = 100, a = 255 } };
            ElementItem aopAngle = new ElementItem()
            {
                NameString = "Argument Of Periapsis (ω) - from elements",
                Colour = aopColour,
                HighlightColour = aopHColour,
                DataItem = Angle.ToDegrees(_aop),
                DataString = Angle.ToDegrees(_aop).ToString() + "°",
                Shape = new ComplexShape()
                {
                    StartPoint = _f1a,
                    Points = CreatePrimitiveShapes.AngleArc(Vector2.Zero, 63, -6, _loan, _aop, 128),
                    Colors = aopColour,
                    ColourChanges = new (int,int)[]
                    {
                        (0,0)
                    },
                    Scales = false
                }
            };
            ElementItems.Add(aopAngle);


            
            _aopItem_FromCalc1 = new ElementItem()
            {
                NameString = "Argument Of Periapsis (ω) - using vector calc1",
                Colour = aopColour,
                HighlightColour = aopHColour,
                DataItem = Angle.ToDegrees(_aopFromCalc1),
                DataString = Angle.ToDegrees(_aopFromCalc1).ToString() + "°",
                Shape = new ComplexShape()
                {
                    StartPoint = _f1a,
                    Points = CreatePrimitiveShapes.AngleArc(Vector2.Zero, 90, -6, _loan, _aopFromCalc1, 128),
                    Colors = aopColour,
                    ColourChanges = new (int,int)[]
                    {
                        (0,0)
                    },
                    Scales = false
                }
            };
            ElementItems.Add(_aopItem_FromCalc1);
            
            _aopItem_FromCalc2 = new ElementItem()
            {
                NameString = "Argument Of Periapsis (ω) - using vector calc2",
                Colour = aopColour,
                HighlightColour = aopHColour,
                DataItem = Angle.ToDegrees(_aopFromCalc2),
                DataString = Angle.ToDegrees(_aopFromCalc2).ToString() + "°",
                Shape = new ComplexShape()
                {
                    StartPoint = _f1a,
                    Points = CreatePrimitiveShapes.AngleArc(Vector2.Zero, 93, -6, _loan, _aopFromCalc2, 128),
                    Colors = aopColour,
                    ColourChanges = new (int,int)[]
                    {
                        (0,0)
                    },
                    Scales = false
                }
            };
            ElementItems.Add(_aopItem_FromCalc2);
/*
            _aopItem_FromCalc3 = new ElementItem()
            {
                NameString = "Argument Of Periapsis (ω) - using vector calc3",
                Colour = aopColour,
                HighlightColour = aopHColour,
                DataItem = Angle.ToDegrees(_aopFromCalc2),
                DataString = Angle.ToDegrees(_aopFromCalc2).ToString() + "°",
                Shape = new ComplexShape()
                {
                    Points = CreatePrimitiveShapes.AngleArc(_cP, 96, -6, _loan, _aopFromCalc3, 128),
                    Colors = aopColour,
                    ColourChanges = new (int,int)[]
                    {
                        (0,0)
                    },
                    Scales = false
                }
            };
            ElementItems.Add(_aopItem_FromCalc3);
            */
            _aopItem_FromCalc4 = new ElementItem()
            {
                NameString = "Argument Of Periapsis (ω) - using vector calc4",
                Colour = aopColour,
                HighlightColour = aopHColour,
                DataItem = Angle.ToDegrees(_aopFromCalc4),
                DataString = Angle.ToDegrees(_aopFromCalc4).ToString() + "°",
                Shape = new ComplexShape()
                {
                    StartPoint = _f1a,
                    Points = CreatePrimitiveShapes.AngleArc(Vector2.Zero, 99, -6, _loan, _aopFromCalc4, 128),
                    Colors = aopColour,
                    ColourChanges = new (int,int)[]
                    {
                        (0,0)
                    },
                    Scales = false
                }
            };
            ElementItems.Add(_aopItem_FromCalc4);
            
            //lop angle
            SDL.SDL_Color[] lopColour = { new SDL.SDL_Color() { r = 100, g = 100, b = 60, a = 100 } };
            SDL.SDL_Color[] lopHColour = { new SDL.SDL_Color() { r = 100, g = 100, b = 60, a = 255 } };
            ElementItem lopAngle = new ElementItem()
            {
                NameString = "Longditude Of Periapsis (ϖ = Ω + ω)",
                Colour = lopColour,
                HighlightColour = lopHColour,
                DataItem = Angle.ToDegrees(_loP),
                DataString = Angle.ToDegrees(_loP).ToString() + "°",
                Shape = new ComplexShape()
                {
                    StartPoint = _f1a,
                    Points = CreatePrimitiveShapes.AngleArc(Vector2.Zero, 65, 6, 0, _loP, 128),
                    Colors = lopColour,
                    ColourChanges = new (int pointIndex,int colourIndex)[]{ (0, 0) },
                    Scales = false
                }
            };
            ElementItems.Add(lopAngle);

            ElementItem periapsLine = new ElementItem()
            {
                NameString = "Periapsis Line",
                Colour = aopColour,
                HighlightColour = aopHColour,
                //DataItem = _semiMinAxis,
                DataString = Stringify.Distance(_apoapsisPnt.Length()), 
                Shape = new ComplexShape()
                {
                    Points = new Vector2[]
                    {
                        _f1a,
                        _periapsisPnt
                    },
                    Colors = aopColour,
                    ColourChanges = new (int pointIndex, int colourIndex)[]
                    {
                        (0, 0)
                    },
                    Scales = true
                }
            };
            ElementItems.Add(periapsLine);
            
            ElementItem periapsPnt = new ElementItem()
            {
                NameString = "Periapsis Point",
                Colour = aopColour,
                HighlightColour = aopHColour,
                Shape = new ComplexShape()
                {
                    StartPoint = _periapsisPnt,
                    Points = new Vector2[]
                    {
                        new Vector2(){ X = - 8, Y =  0 },
                        new Vector2(){ X =  + 8, Y = 0 },
                        new Vector2(){ X = 0 , Y =   - 8 },
                        new Vector2(){ X = 0 , Y =   + 8 }
                    },
                    Colors = aopColour,
                    ColourChanges = new (int pointIndex, int colourIndex)[]
                    {
                        (0, 0)
                    },
                    Scales = false
                }
            };
            ElementItems.Add(periapsPnt);
            
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
                    StartPoint = _f1a,
                    Points = CreatePrimitiveShapes.AngleArc(Vector2.Zero, 78, 6, _loP, _trueAnom, 128),
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
                    StartPoint = new Vector2() { X = _f1a.X, Y = _f1a.Y },
                    Points = CreatePrimitiveShapes.AngleArc(Vector2.Zero, 80, 6, _loP, _trueAnom_FromEVec, 128),
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
                    StartPoint = new Vector2() { X = _f1a.X, Y = _f1a.Y },
                    Points = CreatePrimitiveShapes.AngleArc(Vector2.Zero, 84, 6, _loP, _trueAnom_FromStateVec, 128),
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
                DataItem = _bodyPosition.RelativePosition.Length(),
                DataString = Stringify.Distance(_bodyPosition.RelativePosition.Length())  ,
                Shape = new ComplexShape()
                {
                    Points = new Vector2[]{
                        _f1a,
                        _bodyPosPnt_m
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
                    StartPoint = _cP,
                    Points = CreatePrimitiveShapes.AngleArc(Vector2.Zero, 67, 6, 0, _meanAnom, 128),
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
                    StartPoint = _cP,
                    Points = CreatePrimitiveShapes.AngleArc(Vector2.Zero, 69, 6, _loP, _eccentricAnom, 128),
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
                    StartPoint = _cP,
                    Points = CreatePrimitiveShapes.AngleArc(Vector2.Zero, 73, 6, _loP, _eccentricAnom_FromTrueAnom, 128),
                    Colors = eAnomColour,
                    ColourChanges = new (int,int)[]
                    {
                        (0,0),
                    },
                    Scales = false
                }
            };
            ElementItems.Add(_eccentricAnomItem_FromTrueAnom);
            /*
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

            */
            
            /*
            var pos = _bodyPosition.RelativePosition_m;
            //var vel = OrbitProcessor.InstantaneousOrbitalVelocityVector_AU(_keplerElements, _keplerElements.Parent.Manager.ManagerSubpulses.StarSysDateTime);
            
            var vel = OrbitMath.ObjectLocalVelocityVector(
                _sgp, 
                pos, 
                _semiMajAxis, 
                _keplerElements.Eccentricity, 
                _trueAnom, 
                _aop);
            */

            //var state = _entity.GetRelativeState();
            //var pos_m = state.pos;
            //var vel_m = state.Velocity;
            
            var ecvec = OrbitMath.EccentricityVector(_sgp, pos_m, (Vector3)vel_m);
            var ecvec2 = OrbitMath.EccentricityVector2(_sgp, pos_m, (Vector3)vel_m);
            var evenorm = Vector3.Normalise(ecvec) * 84;
            var evenorm2 = Vector3.Normalise(ecvec2) * 84;
            Vector2[] evLine =
            {
                new Vector2() { X = 0, Y = 0 }, 
                new Vector2(){X=evenorm.X, Y = evenorm.Y},
                new Vector2() { X = 0, Y = 0 },
                new Vector2(){X= evenorm2.X, Y = evenorm2.Y}
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
                    StartPoint = _f1a,
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

            
            _headingItemRel = new HeadingElement(
                _keplerElements, 
                _sgp, 
                _worldPosition_m, 
                _bodyPosition, 
                _trueAnom, 
                (Vector3)vel_m);
            _headingItemRel.NameString = "Heading (rel)";
            ElementItems.Add(_headingItemRel);
            
            
            var vel2 = OrbitalMath.HackVelocityVector(_keplerElements, _entity.StarSysDateTime);
            _headingItemRel2 = new HeadingElement(
                _keplerElements, 
                _sgp, 
                _worldPosition_m, 
                _bodyPosition, 
                _trueAnom, 
                (Vector3)vel_m);
            _headingItemRel2.NameString = "Heading (rel2)";
            ElementItems.Add(_headingItemRel2);
            
            var absVel = _entity.GetAbsoluteState().Velocity;
            _headingItemAbs = new HeadingElement(
                _keplerElements, 
                _sgp, 
                _worldPosition_m, 
                _bodyPosition, 
                _trueAnom, 
                (Vector3)absVel);
            _headingItemAbs.NameString = "Heading (abs)";
            ElementItems.Add(_headingItemAbs);

            
            _velvecItem = new VelVecElement(
                _keplerElements, 
                _sgp, 
                _worldPosition_m, 
                _bodyPosition, 
                _trueAnom, 
                (Vector3)vel_m);
            ElementItems.Add(_velvecItem);
            
            
        }

        public override void OnPhysicsUpdate()
        {

            if (_orbitIcon is NewtonMoveIcon)
            {
                ElementItems = new List<ElementItem>();
                RefreshEccentricity();
                CreateLines();
                
            }

            DateTime systemDateTime = _entity.StarSysDateTime;
            double secondsFromEpoch = (systemDateTime - _keplerElements.Epoch).TotalSeconds;
            _trueAnom = OrbitProcessor.GetTrueAnomaly(_keplerElements, systemDateTime);
            _meanAnom = OrbitMath.GetMeanAnomalyFromTime(_keplerElements.MeanAnomalyAtEpoch, _keplerElements.MeanMotion, secondsFromEpoch);

            _eccentricAnom = OrbitProcessor.GetEccentricAnomaly(_keplerElements, _meanAnom);
            
            
            var meanAnom2 = OrbitMath.GetMeanAnomaly(_keplerElements.Eccentricity, _eccentricAnom);

            _trueAnomalyAngleItem.Shape.Points = CreatePrimitiveShapes.AngleArc(Vector2.Zero, 80, 4, _loP, _trueAnom, 128);
            _trueAnomalyAngleItem.DataItem = Angle.ToDegrees(_trueAnom);
            _trueAnomalyAngleItem.DataString = Angle.ToDegrees(_trueAnom) + "°";

            
            
            _trueAnom = OrbitProcessor.GetTrueAnomaly(_keplerElements, systemDateTime);
            /*
            var pos_m = _bodyPosition.RelativePosition_m;
            var vel_m = OrbitMath.ObjectLocalVelocityVector(
                _sgp, 
                pos_m, 
                _semiMajAxis, 
                _keplerElements.Eccentricity, 
                _trueAnom, 
                _keplerElements.AoP);
            */
            var state = _entity.GetRelativeState();
            var pos_m = state.pos;
            var vel_m = state.Velocity;
            
            var ecvec = OrbitMath.EccentricityVector(_sgp, pos_m, (Vector3)vel_m);
            var ecvec2 = OrbitMath.EccentricityVector2(_sgp, pos_m, (Vector3)vel_m);
            _trueAnom_FromEVec = OrbitMath.TrueAnomaly(ecvec, pos_m, (Vector3)vel_m);
            _trueAnom_FromStateVec = OrbitMath.TrueAnomaly(_sgp, pos_m, (Vector3)vel_m);
            
            _trueAnomItem_FromEVec.Shape.Points = CreatePrimitiveShapes.AngleArc(Vector2.Zero, 82, 4, _loP, _trueAnom_FromEVec, 128);
            _trueAnomItem_FromEVec.DataItem = Angle.ToDegrees(_trueAnom_FromEVec);
            _trueAnomItem_FromEVec.DataString = Angle.ToDegrees(_trueAnom_FromEVec) + "°";
            
            _trueAnomItem_FromStateVec.Shape.Points = CreatePrimitiveShapes.AngleArc(Vector2.Zero, 84, 4, _loP, _trueAnom_FromStateVec, 128);
            _trueAnomItem_FromStateVec.DataItem = Angle.ToDegrees(_trueAnom_FromStateVec);
            _trueAnomItem_FromStateVec.DataString = Angle.ToDegrees(_trueAnom_FromStateVec).ToString() + "°";
            
            
            _bodyPosPnt_m = new Vector2() 
            { 
                X = (_bodyPosition.AbsolutePosition ).X, 
                Y = (_bodyPosition.AbsolutePosition ).Y 
            };
            _bodyPosItem.Shape.StartPoint = _bodyPosPnt_m;

            var posA_m = _entity.GetAbsolutePosition();
            _bodyPosFromState.Shape.StartPoint = new Vector2(posA_m.X, posA_m.Y);
            
            _radiusToBody.Shape.Points = new Vector2[]
            {
                new Vector2{X = _f1a.X, Y = _f1a.Y },
                new Vector2{X = _bodyPosPnt_m.X, Y = _bodyPosPnt_m.Y }};
            _radiusToBody.DataItem = _bodyPosition.RelativePosition.Length();
            _radiusToBody.DataString = Stringify.Distance(_bodyPosition.RelativePosition.Length());
            
            _meanAnomalyItem.Shape.Points = CreatePrimitiveShapes.AngleArc(Vector2.Zero, 67, 6, 0, _meanAnom, 128);
            _meanAnomalyItem.DataItem = Angle.ToDegrees(_meanAnom);
            _meanAnomalyItem.DataString = Angle.ToDegrees(_meanAnom).ToString() + "°";

            _eccentricAnom_FromTrueAnom = OrbitMath.GetEccentricAnomalyFromTrueAnomaly(_trueAnom, _keplerElements.Eccentricity);
            //_ecctricAnom_FromStateVectors = OrbitMath.GetEccentricAnomalyFromStateVectors(pos, _semiMajAxis, _ae, _aop);
            //_ecctricAnom_FromStateVectors2 = OrbitMath.GetEccentricAnomalyFromStateVectors2(_sgp, _semiMajAxis, pos, (Vector3)vel);
            
            _eccentricAnomalyItem.Shape.Points = CreatePrimitiveShapes.AngleArc(Vector2.Zero, 69, 6, _loP, _eccentricAnom, 128);
            _eccentricAnomalyItem.DataItem = Angle.ToDegrees(_eccentricAnom);
            _eccentricAnomalyItem.DataString = Angle.ToDegrees(_eccentricAnom).ToString() + "°";

            _eccentricAnomItem_FromTrueAnom.Shape.Points = CreatePrimitiveShapes.AngleArc(Vector2.Zero, 73, 6, _loP, _eccentricAnom_FromTrueAnom, 128);
            _eccentricAnomItem_FromTrueAnom.DataItem = Angle.ToDegrees(_eccentricAnom_FromTrueAnom);
            _eccentricAnomItem_FromTrueAnom.DataString = Angle.ToDegrees(_eccentricAnom_FromTrueAnom).ToString() + "°";
            
            
            //_eccentricAnomItem_FromStateVec.Shape.Points = CreatePrimitiveShapes.AngleArc(new PointD() { X = 0, Y = 0 }, 75, 6, _loP, _ecctricAnom_FromStateVectors, 128);
            //_eccentricAnomItem_FromStateVec.DataItem = Angle.ToDegrees(_ecctricAnom_FromStateVectors);
            //_eccentricAnomItem_FromStateVec.DataString = Angle.ToDegrees(_ecctricAnom_FromStateVectors).ToString() + "°";
            
            //_eccentricAnomItem_FromStateVec2.Shape.Points = CreatePrimitiveShapes.AngleArc(new PointD() { X = 0, Y = 0 }, 77, 6, _loP, _ecctricAnom_FromStateVectors2, 128);
            //_eccentricAnomItem_FromStateVec2.DataItem = Angle.ToDegrees(_ecctricAnom_FromStateVectors2);
            //_eccentricAnomItem_FromStateVec2.DataString = Angle.ToDegrees(_ecctricAnom_FromStateVectors2).ToString() + "°";


            var evenorm = Vector3.Normalise(ecvec) * 84;
            var evenorm2 = Vector3.Normalise(ecvec2) * 84;
            /*
            Vector2[] evLine =
            {
                new Vector2() { X = _f1a.X, Y = _f1a.Y }, 
                new Vector2(){X= _f1a.X + evenorm.X, Y = _f1a.X+ evenorm.Y},
                new Vector2() { X = _f1a.X, Y = _f1a.Y },
                new Vector2(){X= _f1a.X + evenorm2.X, Y = _f1a.X+ evenorm2.Y}
            };*/
            Vector2[] evLine =
            {
                new Vector2() { X = 0, Y = 0 }, 
                new Vector2(){X=evenorm.X, Y = evenorm.Y},
                new Vector2() { X = 0, Y = 0 },
                new Vector2(){X= evenorm2.X, Y = evenorm2.Y}
            };
            _eccentricityVectorItem.Shape.Points = evLine;
            


            Vector3 angularVelocity = Vector3.Cross(pos_m, (Vector3)vel_m);
            Vector3 nodeVector = Vector3.Cross(new Vector3(0, 0, 1), angularVelocity);



            _aopFromCalc1 = OrbitMath.GetArgumentOfPeriapsis1(nodeVector, ecvec, pos_m, vel_m);
            _aopItem_FromCalc1.Shape.Points = CreatePrimitiveShapes.AngleArc(Vector2.Zero, 90, -6, _loan, _aopFromCalc1, 128);
            _aopItem_FromCalc1.DataItem = Angle.ToDegrees(_aopFromCalc1);
            _aopItem_FromCalc1.DataString = Angle.ToDegrees(_aopFromCalc1).ToString() + "°";
            
            _aopFromCalc2 = OrbitMath.GetArgumentOfPeriapsis(pos_m, _keplerElements.Inclination, _loan, _trueAnom);
            _aopItem_FromCalc2.Shape.Points = CreatePrimitiveShapes.AngleArc(Vector2.Zero, 93, -6, _loan, _aopFromCalc2, 128);
            _aopItem_FromCalc2.DataItem = Angle.ToDegrees(_aopFromCalc2);
            _aopItem_FromCalc2.DataString = Angle.ToDegrees(_aopFromCalc2).ToString() + "°";
            
            /*
            _aopFromCalc3 = OrbitMath.GetArgumentOfPeriapsis3(nodeVector, ecvec, pos, (Vector3)vel, _loan); 
            _aopItem_FromCalc3.Shape.Points = CreatePrimitiveShapes.AngleArc(_cP, 96, -6, _loan, _aopFromCalc3, 128);
            _aopItem_FromCalc3.DataItem = Angle.ToDegrees(_aopFromCalc3);
            _aopItem_FromCalc3.DataString = Angle.ToDegrees(_aopFromCalc3).ToString() + "°";
            */
            
            _aopFromCalc4 = OrbitMath.GetArgumentOfPeriapsis3(_keplerElements.Inclination, ecvec, nodeVector);
            _aopItem_FromCalc4.Shape.Points = CreatePrimitiveShapes.AngleArc(Vector2.Zero, 99, -6, _loan, _aopFromCalc4, 128);
            _aopItem_FromCalc4.DataItem = Angle.ToDegrees(_aopFromCalc4);
            _aopItem_FromCalc4.DataString = Angle.ToDegrees(_aopFromCalc4).ToString() + "°";

            
            _headingItemRel.Update( vel_m);

            var vel2 = OrbitalMath.HackVelocityVector(_keplerElements, systemDateTime);
            _headingItemRel2.Update( vel2);
            
            var absVel = _entity.GetAbsoluteState().Velocity;
            _headingItemAbs.Update( absVel);
            
            _velvecItem.Update(vel_m);

        }

        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {

            ViewScreenPos = camera.ViewCoordinate_m(WorldPosition_m);
            var scAU = Matrix.IDScale(6.6859E-12, 6.6859E-12);
            var mirMtx = Matrix.IDMirror(false, true);
            _drawComplexShapes = new List<ComplexShape>() {};

            foreach (var item in ElementItems)
            {
                if(!item.ShowLines)//don't draw the lines if we're not suposed to. 
                    continue;
                
                var shape = item.Shape;
                
                Vector2[] points = new Vector2[shape.Points.Length];
                var startPoint = camera.ViewCoordinateV2_m(shape.StartPoint);
                //var mtx = Matrix.IDTranslate(startPoint.X , startPoint.Y );
                //var mtxz = scAU * matrix * mtx;
                if (shape.Scales)
                {
                    for (int i = 0; i < shape.Points.Length; i++)
                    {
                        var pnt = shape.Points[i];;
                        //Vector2 transformedPoint;
                        points[i] = camera.ViewCoordinateV2_m(pnt);
                    }
                }
                else
                {
                    for (int i = 0; i < shape.Points.Length; i++)
                    {
                        var pnt = shape.Points[i];
                        points[i] = new  Vector2(startPoint.X + pnt.X, startPoint.Y + pnt.Y * -1);
                        if (points[i].X > int.MaxValue || points[i].X < int.MinValue || points[i].X is Double.NaN)
                        {
                            //throw new Exception("point outside bounds, probilby a scale issue");
                            points[i] = new  Vector2(startPoint.X, startPoint.Y);
                        }
                    }
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
        
        
        class HeadingElement : ElementItem
        {
            private KeplerElements _ke;
            private double _sgp;
            private Vector3 _worldPosition_m;
            IPosition _bodyPosition;
            private Vector2 _bodyPosPnt_m;
            
            public HeadingElement(KeplerElements ke, double sgp, Vector3 worldPos_m, IPosition bodyPos, double trueAnomaly, Vector3 vel_m)
            {
                _ke = ke;
                _sgp = sgp;
                _worldPosition_m = worldPos_m;
                _bodyPosition = bodyPos;
                _bodyPosPnt_m = new Vector2()
                {
                    X = (_bodyPosition.AbsolutePosition + _worldPosition_m).X,
                    Y = (_bodyPosition.AbsolutePosition + _worldPosition_m).Y
                };
                double lop = _ke.LoAN + _ke.AoP;
                
                
                
                double speed = OrbitalMath.InstantaneousOrbitalSpeed(_sgp, _bodyPosition.RelativePosition.Length(), _ke.SemiMajorAxis);
            

                SDL.SDL_Color[] headingColour = 
                { 
                    new () { r = 100, g = 100, b = 100, a = 100 },
                    new () {a = 0} 
                };
                SDL.SDL_Color[] headingHColour = 
                { 
                    new () { r = 100, g = 100, b = 100, a = 255 },
                    new () {a = 0} 
                };

                NameString = "Heading";
                Colour = headingColour;
                HighlightColour = headingHColour;

                Shape = new ComplexShape()
                {
                    StartPoint = new Vector2( _bodyPosPnt_m.X, _bodyPosPnt_m.Y),
                    Colors = headingColour,
                    ColourChanges = new (int, int)[] {(0, 0),},
                    Scales = false
                };
                
                Update(vel_m);

            }

            public void Update( Vector3 vel_m)
            {
                _bodyPosPnt_m = new Vector2()
                {
                    X = (_bodyPosition.AbsolutePosition ).X,
                    Y = (_bodyPosition.AbsolutePosition ).Y
                };

                var vnorm = Vector3.Normalise(vel_m) * 60;
                
                Shape.StartPoint = new Vector2( _bodyPosPnt_m.X, _bodyPosPnt_m.Y);
                Vector2[] headingLine = 
                {
                    new () { X = 0, Y = 0 }, 
                    new () { X = vnorm.X, Y = vnorm.Y}, 
                };
               
                Shape.Points = headingLine;
                
                var heading = Angle.RadiansFromVector3(vel_m);

                DataString = Angle.ToDegrees(heading) + "°";
            }



        }
        
        
        class VelVecElement : ElementItem
        {
            private KeplerElements _ke;
            private double _sgp;
            private Vector3 _worldPosition_m;
            IPosition _bodyPosition;
            private Vector2 _bodyPosPnt_m;
            
            public VelVecElement(KeplerElements ke, double sgp, Vector3 worldPos_m, IPosition bodyPos, double trueAnomaly, Vector3 vel_m)
            {
                _ke = ke;
                _sgp = sgp;
                _worldPosition_m = worldPos_m;
                _bodyPosition = bodyPos;
                _bodyPosPnt_m = new Vector2()
                {
                    X = (_bodyPosition.AbsolutePosition ).X,
                    Y = (_bodyPosition.AbsolutePosition ).Y
                };
                double lop = _ke.LoAN + _ke.AoP;
                
                
                
                //double speed = OrbitMath.InstantaneousOrbitalSpeed(_sgp, _bodyPosition.RelativePosition_m.Length(), _ke.SemiMajorAxis);
            

                SDL.SDL_Color[] headingColour = 
                { 
                    new() { r = 255, g = 100, b = 100, a = 100 },
                    new SDL.SDL_Color() {a = 0} 
                };
                SDL.SDL_Color[] headingHColour = 
                { 
                    new() { r = 255, g = 100, b = 100, a = 255 },
                    new SDL.SDL_Color() {a = 0} 
                };

                NameString = "VelocityVector";
                Colour = headingColour;
                HighlightColour = headingHColour;
                Shape = new ComplexShape()
                {
                    StartPoint = new Vector2(_bodyPosPnt_m.X, _bodyPosPnt_m.Y),
                    Colors = headingColour,
                    ColourChanges = new (int, int)[] {(0, 0),},
                    Scales = false
                };
                
                Update(vel_m);

            }

            public void Update( Vector3 vel_m)
            {
                _bodyPosPnt_m = new Vector2()
                {
                    X = (_bodyPosition.AbsolutePosition).X,
                    Y = (_bodyPosition.AbsolutePosition).Y
                };


                var ptp = OrbitalMath.StateToProgradeVector(vel_m, _ke.TrueAnomalyAtEpoch, _ke.AoP, _ke.LoAN, _ke.Inclination);

                var stp = OrbitalMath.ProgradeToStateVector(new(0, 60, 0), _ke.TrueAnomalyAtEpoch, _ke.AoP, _ke.LoAN, _ke.Inclination);


                var pgd = new Vector3(0, vel_m.Length(), 0);
                pgd = Vector3.Normalise(pgd) * 60;
                
                var vnorm = Vector3.Normalise(ptp) * 60;

                Shape.StartPoint = new Vector2(_bodyPosPnt_m.X,_bodyPosPnt_m.Y);
                Vector2[] vline =
                {
                    new () { X = 0, Y = 0 }, 
                    new () { X = vnorm.X, Y = vnorm.Y },
                    new () { X = 0, Y = 0 }, 
                    new () { X = pgd.X, Y = pgd.Y },
                    new () { X = 0, Y = 0 }, 
                    new () { X = stp.X, Y = stp.Y }
                };

                
                Shape.Points = vline;
                double ang = Angle.RadiansFromVector3(ptp);
                double ang2 = Angle.RadiansFromVector3(pgd);
                double ang3 = Angle.RadiansFromVector3(stp);
                DataString = Angle.ToDegrees(ang) + "° " + Angle.ToDegrees(ang2) + " " + Angle.ToDegrees(ang2);
            }
        }
    }
}
