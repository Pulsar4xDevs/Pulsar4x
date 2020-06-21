using System;
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
        OrbitDB _ourOrbit;
        float _phaseAngle = 0;
        private DateTime _minDateTime;
        DateTime _atDatetime;
        
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
                //thisitem.HardRefresh(orderEntity);
                thisitem.OnSystemTickChange(_uiState.SelectedSystemTime);
            }
            else
            {
                thisitem = (NavWindow)_uiState.LoadedWindows[typeof(NavWindow)];
                if (thisitem._orderEntity != orderEntity.Entity)
                {
                    thisitem._orderEntity = orderEntity.Entity;
                    //thisitem.HardRefresh(orderEntity);
                    thisitem.OnSystemTickChange(_uiState.SelectedSystemTime);
                }
            }

            return thisitem;
        }

        public override void OnSystemTickChange(DateTime newDate)
        {
            _minDateTime = newDate;
            if (_atDatetime < _minDateTime)
            {
                _atDatetime = _minDateTime;
            }

            _ourOrbit = _orderEntity.GetDataBlob<OrbitDB>();
            if (_targetSMA == 0)
                _targetSMA = (float)_ourOrbit.SemiMajorAxis;
        }


        enum NavMode
        {
            None,
            Thrust,
            HohmannTransfer,
            PhaseChange,
            PorkChopPlot
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
                if (ImGui.Button("Porkchop Plot"))
                {
                    _navMode = NavMode.PorkChopPlot;
                }
                BorderGroup.End();

                switch (_navMode)
                {
                    case NavMode.PhaseChange:
                        DisplayPhaseChangeMode();
                        break;
                    case NavMode.HohmannTransfer:
                        DisplayPhaseChangeMode();
                        break;
                    case NavMode.None:
                        break;
                    default:
                        break;
                }
                
            }
        }

        private float _maxDV;
        private float _radialDV;
        private float _progradeDV;
        void DisplayThrustMode()
        {
            bool changes = false;
            float maxprogradeDV = (float)(_maxDV - Math.Abs(_radialDV));
            float maxradialDV = (float)(_maxDV - Math.Abs(_progradeDV));
                        
            if (ImGui.SliderFloat("Prograde DV", ref _progradeDV, -maxprogradeDV, maxprogradeDV))
            {
                //Calcs();
                changes = true;
            }
            if (ImGui.SliderFloat("Radial DV", ref _radialDV, -maxradialDV, maxradialDV))
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
            ImGui.SliderAngle("PhaseAngle", ref _phaseAngle);
            (Vector3 deltaV, double timeInSeconds)[] manuvers = InterceptCalcs.OrbitPhasingManuvers(_ourOrbit, _atDatetime, _phaseAngle);
        }

        private float _targetSMA = 0;
        void DisplayHohmannMode()
        {
            double mySMA = _ourOrbit.SemiMajorAxis;
            float smaMin = 1;
            float smaMax = float.MaxValue;
            var sgp = _ourOrbit.GravitationalParameter_m3S2;
            ImGui.SliderFloat("Target SemiMajorAxis", ref _targetSMA, smaMin, smaMax);
            var manuvers = InterceptCalcs.Hohmann(sgp, mySMA, _targetSMA);
        }
    }
}