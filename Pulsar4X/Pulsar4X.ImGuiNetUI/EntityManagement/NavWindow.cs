using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;
using Pulsar4X.SDL2UI;
using Vector3 = Pulsar4X.ECSLib.Vector3;

namespace Pulsar4X.ImGuiNetUI.EntityManagement
{
    public class NavWindow : PulsarGuiWindow
    {
        private Entity _orderEntity;
        //OrbitDB _ourOrbit;
        private double _sgp;
        private KeplerElements _currentKE;
        private NewtonThrustAbilityDB _newtonThrust;

        private double _totalDV
        {
            get { return _newtonThrust.DeltaV; }
        }
        
        private double _totalDVUsage = 0;
        
        
        
        float _phaseAngleRadians = 0;
        private DateTime _minDateTime;
        DateTime _atDatetime;
        
        Entity[] _siblingEntities = new Entity[0];
        string[] _siblingNames = new string[0];
        private int _selectedSibling = -1;
        
        private NavWindow(Entity orderEntity)
        {
            _flags = ImGuiWindowFlags.None;
            _orderEntity = orderEntity;
        }


        public static NavWindow GetInstance(EntityState orderEntity)
        {
            NavWindow thisitem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(NavWindow)))
            {
                thisitem = new NavWindow(orderEntity.Entity);
                thisitem.HardRefresh(orderEntity);
                thisitem.OnSystemTickChange(_uiState.SelectedSystemTime);
            }
            else
            {
                thisitem = (NavWindow)_uiState.LoadedWindows[typeof(NavWindow)];
                if (thisitem._orderEntity != orderEntity.Entity)
                {
                    thisitem._orderEntity = orderEntity.Entity;
                    thisitem.HardRefresh(orderEntity);
                    thisitem.OnSystemTickChange(_uiState.SelectedSystemTime);
                }
            }

            return thisitem;
        }

        private void HardRefresh(EntityState orderEntity)
        {
            _orderEntity = orderEntity.Entity;
            _newtonThrust = _orderEntity.GetDataBlob<NewtonThrustAbilityDB>();
            var myMass = _orderEntity.GetDataBlob<MassVolumeDB>().MassDry;
            var parentMass = Entity.GetSOIParentEntity(_orderEntity).GetDataBlob<MassVolumeDB>().MassDry;
            _sgp = OrbitMath.CalculateStandardGravityParameterInM3S2(myMass, parentMass);

            _siblingEntities = Entity.GetSOIParentEntity(_orderEntity).GetDataBlob<PositionDB>().Children.ToArray();
            List<string> names = new List<string>();
            foreach (var entity in _siblingEntities)
            {
                //TODO: this is going to show *all* entities, not just the ones this faction can see.
                //going to need to come up with a way to get this data. (filtering for this should be done in the engine not ui)
                string name = entity.GetDataBlob<NameDB>().GetName(_orderEntity.FactionOwner);
                names.Add(name);
            }

            _siblingNames = names.ToArray();
            
            OnSystemTickChange(orderEntity.Entity.StarSysDateTime);
            
        }

        public override void OnSystemTickChange(DateTime newDate)
        {
            _minDateTime = newDate;
            if (_atDatetime < _minDateTime)
            {
                _atDatetime = _minDateTime;
            }
            if (_orderEntity.HasDataBlob<OrbitDB>())
                _currentKE = _orderEntity.GetDataBlob<OrbitDB>().GetElements();
            else if (_orderEntity.HasDataBlob<OrbitUpdateOftenDB>())
                _currentKE = _orderEntity.GetDataBlob<OrbitUpdateOftenDB>().GetElements();
            else if (_orderEntity.HasDataBlob<OrbitDB>())
                _currentKE = _orderEntity.GetDataBlob<NewtonMoveDB>().GetElements();            
            
            if (_targetSMA == 0)
                _targetSMA = (float)_currentKE.SemiMajorAxis;
        }


        enum NavMode
        {
            None,
            Thrust,
            HohmannTransfer,
            PhaseChange,
            HighDVIntercept,
            PorkChopPlot,
            EscapeSOI
        }

        private NavMode _navMode = NavMode.None;

        internal override void Display()
        {
            if (!IsActive)
                return;
            ImGui.SetNextWindowSize(new Vector2(600f, 400f), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Nav Control", ref IsActive, _flags))
            {
                BorderGroup.Begin("Mode");
                if (ImGui.Button("Manual Thrust"))
                {
                    _navMode = NavMode.Thrust;
                }
                if (ImGui.Button("Hohmann Transfer"))
                {
                    _navMode = NavMode.HohmannTransfer;
                }
                if (ImGui.Button("Phase Change"))
                {
                    _navMode = NavMode.PhaseChange;
                }
                if (ImGui.Button("High Δv Intercept"))
                {
                    _navMode = NavMode.HighDVIntercept;
                }
                if (ImGui.Button("Porkchop Plot"))
                {
                    _navMode = NavMode.PorkChopPlot;
                }

                if (ImGui.Button("Escape SOI"))
                {
                    _navMode = NavMode.EscapeSOI;
                }

                BorderGroup.End();
                ImGui.NewLine();
                ImGui.Text("Availible Δv: " + Stringify.Velocity(_totalDV));
                switch (_navMode)
                {                    
                    case NavMode.Thrust:
                    {
                        DisplayThrustMode();
                        break;
                    }
                    case NavMode.PhaseChange:
                        DisplayPhaseChangeMode();
                        break;
                    case NavMode.HohmannTransfer:
                        DisplayHohmannMode();
                        break;
                    case NavMode.EscapeSOI:
                        DisplayEscapeSOI();
                        break;
                    case NavMode.None:
                        break;
                    default:
                        break;
                }
                
            }
        }

        
        private float _radialDV;
        private float _progradeDV;
        void DisplayThrustMode()
        {
            bool changes = false;
            float maxprogradeDV = (float)(_totalDV - Math.Abs(_radialDV));
            float maxradialDV = (float)(_totalDV - Math.Abs(_progradeDV));
                        
            if (ImGui.SliderFloat("Prograde Δv", ref _progradeDV, -maxprogradeDV, maxprogradeDV))
            {
                //Calcs();
                changes = true;
            }
            if (ImGui.SliderFloat("Radial Δv", ref _radialDV, -maxradialDV, maxradialDV))
            {
                //Calcs();
                changes = true;
            }
            
            //ImGui.Text("Fuel to burn:" + Stringify.Mass(_fuelToBurn));
            //ImGui.Text("Burn time: " + (int)(_fuelToBurn / _fuelRate) +" s");
            //ImGui.Text("DeltaV: " + Stringify.Distance(DeltaV.Length())+ "/s of " + Stringify.Distance(_maxDV) + "/s");
            //ImGui.Text("Eccentricity: " + Eccentricity.ToString("g3"));
            //return changes;
        }


        void DisplayPhaseChangeMode()
        {
            ImGui.SliderAngle("PhaseAngle", ref _phaseAngleRadians);
      
            var manuvers = InterceptCalcs.OrbitPhasingManuvers(_currentKE, _sgp, _atDatetime, _phaseAngleRadians);
            
            
            double totalManuverDV = 0;
            foreach (var manuver in manuvers)
            {
                ImGui.Text(manuver.deltaV.Length() + "Δv");
                totalManuverDV += manuver.deltaV.Length();
                ImGui.Text("Seconds: " + manuver.timeInSeconds);
            }

            ImGui.Text("Total Δv");
            ImGui.SameLine();
            ImGui.Text("for all manuvers: " + Stringify.Velocity(totalManuverDV));


            
            if (ImGui.Button("Make it so"))
            {
                NewtonThrustCommand.CreateCommand(_orderEntity.FactionOwner, _orderEntity, _atDatetime, manuvers[0].deltaV);
                DateTime futureDate = _atDatetime + TimeSpan.FromSeconds(manuvers[1].timeInSeconds);
                NewtonThrustCommand.CreateCommand(_orderEntity.FactionOwner, _orderEntity, futureDate, manuvers[1].deltaV);
            }
        }

        private float _targetSMA = 0;
        
        void DisplayHohmannMode()
        {
            double mySMA = _currentKE.SemiMajorAxis;
            float smaMin = 1;
            float smaMax = (float)OrbitProcessor.GetSOI_m( Entity.GetSOIParentEntity(_orderEntity));
            
            if(ImGui.Combo("Target Object", ref _selectedSibling, _siblingNames, _siblingNames.Length  ))
            {
                Entity selectedSib = _siblingEntities[_selectedSibling];
                if(selectedSib.HasDataBlob<OrbitDB>())
                    _targetSMA = (float)_siblingEntities[_selectedSibling].GetDataBlob<OrbitDB>().SemiMajorAxis;
                if(selectedSib.HasDataBlob<OrbitUpdateOftenDB>())
                    _targetSMA = (float)_siblingEntities[_selectedSibling].GetDataBlob<OrbitUpdateOftenDB>().SemiMajorAxis;
                if(selectedSib.HasDataBlob<NewtonMoveDB>())
                    _targetSMA = (float)_siblingEntities[_selectedSibling].GetDataBlob<NewtonMoveDB >().GetElements().SemiMajorAxis;
            }

            //TODO this should be radius from orbiting body not major axies.  
            ImGui.SliderFloat("Target SemiMajorAxis", ref _targetSMA, smaMin, smaMax);
            var manuvers = InterceptCalcs.Hohmann2(_sgp, mySMA, _targetSMA);

            
            
            double totalManuverDV = 0;
            foreach (var manuver in manuvers)
            {
                ImGui.Text(manuver.deltaV.Length() + "Δv");
                totalManuverDV += manuver.deltaV.Length();
            }
            
            if(totalManuverDV > _totalDV)
                ImGui.TextColored(new Vector4(0.9f, 0, 0, 1), "Total Δv for all manuvers: " + Stringify.Velocity(totalManuverDV));
            else
                ImGui.Text("Total Δv for all manuvers: " + Stringify.Velocity(totalManuverDV));
            
            if (ImGui.Button("Make it so"))
            {
                NewtonThrustCommand.CreateCommand(_orderEntity.FactionOwner, _orderEntity, _atDatetime, manuvers[0].deltaV);
                DateTime futureDate = _atDatetime + TimeSpan.FromSeconds(manuvers[1].timeInSeconds);
                NewtonThrustCommand.CreateCommand(_orderEntity.FactionOwner, _orderEntity, futureDate, manuvers[1].deltaV);
            }
            
        }

        void DisplayHighDVIntercept()
        {
            if(ImGui.Combo("Target Object", ref _selectedSibling, _siblingNames, _siblingNames.Length  ))
            {
                Entity selectedSib = _siblingEntities[_selectedSibling];
                if(selectedSib.HasDataBlob<OrbitDB>())
                    _targetSMA = (float)_siblingEntities[_selectedSibling].GetDataBlob<OrbitDB>().SemiMajorAxis;
                if(selectedSib.HasDataBlob<OrbitUpdateOftenDB>())
                    _targetSMA = (float)_siblingEntities[_selectedSibling].GetDataBlob<OrbitUpdateOftenDB>().SemiMajorAxis;
                if(selectedSib.HasDataBlob<NewtonMoveDB>())
                    _targetSMA = (float)_siblingEntities[_selectedSibling].GetDataBlob<NewtonMoveDB >().GetElements().SemiMajorAxis;
            }
        }

        private bool _EscapeVelocityHigh = true; 
        void DisplayEscapeSOI()
        {
            var period = _orderEntity.GetDataBlob<OrbitDB>().OrbitalPeriod.TotalSeconds;
            var orbitDB = _orderEntity.GetDataBlob<OrbitDB>();
            var parentState = Entity.GetRalitiveState(Entity.GetSOIParentEntity(_orderEntity));
            var parentAngle = Math.Atan2(parentState.pos.Y, parentState.pos.X);
            
            double orbitalPeriod = orbitDB.OrbitalPeriod.TotalSeconds;
            double e = orbitDB.Eccentricity;

            var wc1 = Math.Sqrt((1 - e) / (1 + e));
            var wc2 = Math.Tan(parentAngle / 2);
            
            double E = 2 * Math.Atan(wc1 * wc2);

            double wc3 = orbitalPeriod / (Math.PI * 2);
            double wc4 = E - e * Math.Sin(E);

            double phaseTime = wc3 * wc4;



            Switch.Switch2State("Escape:", ref _EscapeVelocityHigh, "Low", "High");

            double secondsToManuver = phaseTime;
            if (!_EscapeVelocityHigh)
                secondsToManuver += period * 0.5;
                
            double mySMA = _currentKE.SemiMajorAxis;
            //double escapeSMA = 
            var manuverDateTime = _atDatetime + TimeSpan.FromSeconds(secondsToManuver);
            var manuverPos = Entity.GetRalitiveFuturePosition(_orderEntity, manuverDateTime);
            var manuverVel = Entity.GetRalitiveFutureVelocity(_orderEntity, manuverDateTime);
            var soi = Entity.GetSOIParentEntity(_orderEntity).GetDataBlob<OrbitDB>().SOI_m;
            var manuver = InterceptCalcs.Hohmann2(_sgp, manuverPos.Length(), soi)[0];


            manuver.deltaV.Y += 1;
            var totalManuverDV = manuver.deltaV.Length();
            if(totalManuverDV > _totalDV)
                ImGui.TextColored(new Vector4(0.9f, 0, 0, 1), "Total Δv for all manuvers: " + Stringify.Velocity(totalManuverDV));
            else
                ImGui.Text("Total Δv for all manuvers: " + Stringify.Velocity(totalManuverDV));
            
            if (ImGui.Button("Make it so"))
            {
                NewtonThrustCommand.CreateCommand(_orderEntity.FactionOwner, _orderEntity, manuverDateTime, manuver.deltaV);
            }

        }
    }
}