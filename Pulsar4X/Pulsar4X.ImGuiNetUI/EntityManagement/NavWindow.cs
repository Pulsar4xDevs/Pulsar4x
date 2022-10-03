using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;
using Pulsar4X.Orbital;
using Pulsar4X.SDL2UI;
using Pulsar4X.SDL2UI.ManuverNodes;
using Vector2 = System.Numerics.Vector2;
using Vector3 = Pulsar4X.Orbital.Vector3;

namespace Pulsar4X.ImGuiNetUI.EntityManagement
{
    public class NavWindow : PulsarGuiWindow
    {
        private Entity _orderEntity;
        //OrbitDB _ourOrbit;
        private double _sgp;
        private KeplerElements _currentKE;
        private NewtonThrustAbilityDB _newtonThrust;
        private double _totalMass;
        private double _dryMass;
        private double _cargoMass;
        private double _fuelMass;
        private ICargoable _fuelType; 
        private double _totalDV
        {
            get { return _newtonThrust.DeltaV; }
        }
        
        double _burnRate;
        double _exhaustVelocity;

        private double _totalDVUsage = 0;
        
        private (Vector3 deltaV, double tSec)[] _manuvers;
        
        float _phaseAngleRadians = 0;
        private DateTime _minDateTime;
        DateTime _atDatetime;
        
        Entity[] _siblingEntities = new Entity[0];
        string[] _siblingNames = new string[0];
        private int _selectedSibling = -1;
        

        Entity[] _uncleEntites = new Entity[0];
        string[] _uncleNames = new string[0];

        int _selectedUncle = -1;

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
            _totalMass = _orderEntity.GetDataBlob<MassVolumeDB>().MassTotal; 
            _dryMass = _orderEntity.GetDataBlob<MassVolumeDB>().MassDry;
            var parentMass = _orderEntity.GetSOIParentEntity().GetDataBlob<MassVolumeDB>().MassTotal;
            _sgp = GeneralMath.StandardGravitationalParameter(_totalMass + parentMass);
            var fuelTypeID = _newtonThrust.FuelType;
            _fuelType = StaticRefLib.StaticData.CargoGoods.GetAny(fuelTypeID);

            _burnRate = _newtonThrust.FuelBurnRate;
            _exhaustVelocity = _newtonThrust.ExhaustVelocity;


            _siblingEntities = _orderEntity.GetSOIParentEntity().GetDataBlob<PositionDB>().Children.ToArray();
            List<string> names = new List<string>();
            foreach (var entity in _siblingEntities)
            {
                //TODO: this is going to show *all* entities, not just the ones this faction can see.
                //going to need to come up with a way to get this data. (filtering for this should be done in the engine not ui)
                string name = entity.GetDataBlob<NameDB>().GetName(_orderEntity.FactionOwnerID);
                names.Add(name);
            }
            _siblingNames = names.ToArray();
            
            if(_orderEntity.GetSOIParentEntity() == _orderEntity.GetDataBlob<PositionDB>().Root || _orderEntity.GetSOIParentEntity() is null)
                _uncleEntites = new Entity[0];
            else
            {
                _uncleEntites = _orderEntity.GetSOIParentEntity().GetSOIParentEntity().GetDataBlob<PositionDB>().Children.ToArray();
                
            }
            names = new List<string>();
            foreach (var entity in _uncleEntites)
            {
                string name = entity.GetDataBlob<NameDB>().GetName(_orderEntity.FactionOwnerID);
                names.Add(name);
            }
            _uncleNames = names.ToArray();

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

            _totalMass = _orderEntity.GetDataBlob<MassVolumeDB>().MassTotal;
            var parentMass = _orderEntity.GetSOIParentEntity().GetDataBlob<MassVolumeDB>().MassTotal;
            _sgp = GeneralMath.StandardGravitationalParameter(_totalMass + parentMass);
            _cargoMass = _orderEntity.GetDataBlob<VolumeStorageDB>().TotalStoredMass;
            _fuelMass = _orderEntity.GetDataBlob<VolumeStorageDB>().GetUnitsStored(_fuelType);


        }


        enum NavMode
        {
            None,
            Thrust,
            HohmannTransfer,
            InterplanetaryTransfer,
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
                ImGui.Columns(2);
                BorderGroup.Begin("Nodes");
                
                BorderGroup.End(); //ImGui.SameLine();
                ImGui.NextColumn();
                BorderGroup.Begin("Mode");
                if (ImGui.Button("Manual Thrust"))
                {
                    _navMode = NavMode.Thrust;
                }
                if (ImGui.Button("Hohmann Transfer"))
                {
                    _navMode = NavMode.HohmannTransfer;
                }
                if (ImGui.Button("Interplanetary Transfer"))
                {
                    _navMode = NavMode.InterplanetaryTransfer;
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
                ImGui.Columns(1);
                ImGui.NewLine();
                ImGui.Text("Availible Δv: " + Stringify.Velocity(_totalDV));
                ImGui.Text("Dry Mass:" + Stringify.Mass(_dryMass, "0.######"));
                ImGui.Text("Total Mass: " + Stringify.Mass(_totalMass));
                ImGui.Text("Non Fuel Cargo: " + Stringify.Mass(_cargoMass - _fuelMass));
                ImGui.Text(_fuelType.Name + " Fuel: " + Stringify.Mass(_fuelMass));
                ImGui.Text("Total Thrust: " + Stringify.Thrust(_newtonThrust.ThrustInNewtons));
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
                    case NavMode.InterplanetaryTransfer:
                        DisplayInterPlanetaryHohmannMode();
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
        private ManuverNode _node;
        private RouteTrajectory _routeTrajectory;
        void DisplayThrustMode()
        {
            bool changes = false;
            float maxprogradeDV = (float)(_totalDV - Math.Abs(_radialDV));
            float maxradialDV = (float)(_totalDV - Math.Abs(_progradeDV));
            double tseconds = 0;
                        
            
            
            if (ImGui.Button("-1##pg"))
            {
                _progradeDV -= 1;
                changes = true;
            } ImGui.SameLine();            
            if (ImGui.Button("+1##pg"))
            {
                _progradeDV += 1;
                changes = true;
            }ImGui.SameLine();
            if (ImGui.SliderFloat("Prograde Δv", ref _progradeDV, -maxprogradeDV, maxprogradeDV))
            {
                //Calcs();
                changes = true;
            } 
            
            
            if (ImGui.Button("-1##rd"))
            {
                _radialDV -= 1;
                changes = true;
            } ImGui.SameLine();            
            if (ImGui.Button("+1##rd"))
            {
                _radialDV += 1;
                changes = true;
            } ImGui.SameLine();
            if (ImGui.SliderFloat("Radial Δv", ref _radialDV, -maxradialDV, maxradialDV))
            {
                //Calcs();
                changes = true;
            }
            

            ImGui.Text("Time: " + _atDatetime); ImGui.SameLine();
            
            if (ImGui.Button("-1##t"))
            {
                _atDatetime -= TimeSpan.FromSeconds(1);
                tseconds -= 1;
                changes = true;
            } ImGui.SameLine();            
            if (ImGui.Button("+1##t"))
            {
                _atDatetime += TimeSpan.FromSeconds(1);
                tseconds += 1;
                changes = true;
            } ImGui.SameLine();

            //ImGui.Text("Fuel to burn:" + Stringify.Mass(_fuelToBurn));
            //ImGui.Text("Burn time: " + (int)(_fuelToBurn / _fuelRate) +" s");
            //ImGui.Text("DeltaV: " + Stringify.Distance(DeltaV.Length())+ "/s of " + Stringify.Distance(_maxDV) + "/s");
            //ImGui.Text("Eccentricity: " + Eccentricity.ToString("g3"));
            //return changes;


            if (_node is null)
                _node = new ManuverNode(_orderEntity, _atDatetime);
            if (_routeTrajectory is null)
            {
                _routeTrajectory = new RouteTrajectory(_orderEntity, _node);
                _routeTrajectory.GetSegment(0).StartPositionDB = _orderEntity.GetDataBlob<PositionDB>();
                _uiState.SelectedSysMapRender.SelectedEntityExtras.Add(_routeTrajectory);
            }
            if (changes)
            {
                _node.ManipulateNode(_progradeDV, _radialDV, 0, tseconds);
                _routeTrajectory.UpdateNode(0);
            }
            
            if (!_uiState.SelectedSysMapRender.SelectedEntityExtras.Contains(_routeTrajectory))
                _uiState.SelectedSysMapRender.SelectedEntityExtras.Add(_routeTrajectory);
            var deltat = _node.NodeTime - _orderEntity.Manager.StarSysDateTime;
            ImGui.Text("node in: " + deltat);
            var span = _routeTrajectory.GetSegment(0).SegmentTimeSpan;
            ImGui.Text("Span: " + span);
            

        }


        void DisplayPhaseChangeMode()
        {
            ImGui.SliderAngle("PhaseAngle", ref _phaseAngleRadians);
      
            _manuvers = InterceptCalcs.OrbitPhasingManuvers(_currentKE, _sgp, _atDatetime, _phaseAngleRadians);
            
            
            double totalManuverDV = 0;
            foreach (var manuver in _manuvers)
            {
                ImGui.Text(manuver.deltaV.Length() + "Δv");
                totalManuverDV += manuver.deltaV.Length();
                ImGui.Text("Seconds: " + manuver.tSec);
            }

            ImGui.Text("Total Δv");
            ImGui.SameLine();
            ImGui.Text("for all manuvers: " + Stringify.Velocity(totalManuverDV));

            
            
            if (ImGui.Button("Make it so"))
            {

                double fuelBurned1 = OrbitMath.TsiolkovskyFuelUse(_totalMass, _exhaustVelocity, _manuvers[0].deltaV.Length());
                double secondsBurn1 = fuelBurned1 / _burnRate;
                var manuverNodeTime1 = _atDatetime + TimeSpan.FromSeconds(secondsBurn1 * 0.5);

                NewtonThrustCommand.CreateCommand(_orderEntity.FactionOwnerID, _orderEntity, manuverNodeTime1, _manuvers[0].deltaV, secondsBurn1);


                double mass2 = _totalMass - (fuelBurned1 * _fuelType.MassPerUnit);
                double fuelBurned2 = OrbitMath.TsiolkovskyFuelUse(mass2, _exhaustVelocity, _manuvers[1].deltaV.Length());
                double secondsBurn2 = fuelBurned2 / _burnRate;
                var manuverNodeTime2 = manuverNodeTime1 + TimeSpan.FromSeconds(_manuvers[1].tSec);

                NewtonThrustCommand.CreateCommand(_orderEntity.FactionOwnerID, _orderEntity, manuverNodeTime2, _manuvers[1].deltaV, secondsBurn2);
            }
        }

        private float _targetSMA = 0;
        
        void DisplayHohmannMode()
        {
            double mySMA = _currentKE.SemiMajorAxis;
            float smaMin = 1;
            float smaMax = (float)_orderEntity.GetSOIParentEntity().GetSOI_m();
            
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
            _manuvers = OrbitalMath.Hohmann2(_sgp, mySMA, _targetSMA);

            

            double totalManuverDV = 0;
            foreach (var manuver in _manuvers)
            {
                var dv = manuver.deltaV.Length();
                totalManuverDV += dv;
                double fuelBurned = OrbitMath.TsiolkovskyFuelUse(_totalMass, _exhaustVelocity, dv);
                double secondsBurn = fuelBurned / _burnRate;
                ImGui.Text(dv + "Δv");
                ImGui.Text(fuelBurned + " fuel");
                ImGui.Text(Stringify.Number(secondsBurn, "0.###") + " Second Burn");

            }
            
            if(totalManuverDV > _totalDV)
                ImGui.TextColored(new Vector4(0.9f, 0, 0, 1), "Total Δv for all manuvers: " + Stringify.Velocity(totalManuverDV));
            else
                ImGui.Text("Total Δv for all manuvers: " + Stringify.Velocity(totalManuverDV));
            
            if (ImGui.Button("Make it so"))
            {
                double fuelBurned1 = OrbitMath.TsiolkovskyFuelUse(_totalMass, _exhaustVelocity, _manuvers[0].deltaV.Length());
                double secondsBurn1 = fuelBurned1 / _burnRate;
                var manuverNodeTime1 = _atDatetime + TimeSpan.FromSeconds(secondsBurn1 * 0.5);

                NewtonThrustCommand.CreateCommand(_orderEntity.FactionOwnerID, _orderEntity, manuverNodeTime1, _manuvers[0].deltaV, secondsBurn1);

                double mass2 = _totalMass - (fuelBurned1 * _fuelType.MassPerUnit); 
                double fuelBurned2 = OrbitMath.TsiolkovskyFuelUse(mass2, _exhaustVelocity, _manuvers[1].deltaV.Length());
                double secondsBurn2 = fuelBurned2 / _burnRate;
                var manuverNodeTime2 = manuverNodeTime1 + TimeSpan.FromSeconds(_manuvers[1].tSec);

                NewtonThrustCommand.CreateCommand(_orderEntity.FactionOwnerID, _orderEntity, manuverNodeTime2, _manuvers[1].deltaV, secondsBurn2);
            }
            
        }

        void DisplayInterPlanetaryHohmannMode()
        {
            double mySMA = _currentKE.SemiMajorAxis;
            float smaMin = 1;
            float smaMax = (float)_orderEntity.GetSOIParentEntity().GetSOI_m();

            if (ImGui.Combo("Target Object", ref _selectedUncle, _uncleNames, _uncleNames.Length))
            {
                Entity selectedUnc = _uncleEntites[_selectedUncle];
                if (selectedUnc.HasDataBlob<OrbitDB>())
                    _targetSMA = (float)_uncleEntites[_selectedUncle].GetDataBlob<OrbitDB>().SemiMajorAxis;
                if (selectedUnc.HasDataBlob<OrbitUpdateOftenDB>())
                    _targetSMA = (float)_uncleEntites[_selectedUncle].GetDataBlob<OrbitUpdateOftenDB>().SemiMajorAxis;
                if (selectedUnc.HasDataBlob<NewtonMoveDB>())
                    _targetSMA = (float)_uncleEntites[_selectedUncle].GetDataBlob<NewtonMoveDB>().GetElements().SemiMajorAxis;
            }

            //TODO this should be radius from orbiting body not major axies.  
            //ImGui.SliderFloat("Target SemiMajorAxis", ref _targetSMA, smaMin, smaMax);
            if(_selectedUncle > -1)
            {
                _manuvers = InterceptCalcs.InterPlanetaryHohmann(_orderEntity.GetSOIParentEntity(), _uncleEntites[_selectedUncle], _orderEntity);

                double totalManuverDV = 0;
                foreach (var manuver in _manuvers)
                {
                    var dv = manuver.deltaV.Length();
                    totalManuverDV += dv;
                    double fuelBurned = OrbitMath.TsiolkovskyFuelUse(_totalMass, _exhaustVelocity, dv);
                    double secondsBurn = fuelBurned / _burnRate;
                    ImGui.Text(dv + "Δv");
                    ImGui.Text(fuelBurned + " fuel");
                    ImGui.Text(Stringify.Number(secondsBurn, "0.###") + " Second Burn");

                }

                if (totalManuverDV > _totalDV)
                    ImGui.TextColored(new Vector4(0.9f, 0, 0, 1), "Total Δv for all manuvers: " + Stringify.Velocity(totalManuverDV));
                else
                    ImGui.Text("Total Δv for all manuvers: " + Stringify.Velocity(totalManuverDV));

                if (ImGui.Button("Make it so"))
                {
                    var date = _atDatetime;
                    var mass = _totalMass;
                    foreach (var manuver in _manuvers)
                    {

                        double fuelBurned = OrbitMath.TsiolkovskyFuelUse(mass, _exhaustVelocity, manuver.deltaV.Length());
                        double secondsBurn = fuelBurned / _burnRate;
                        date += TimeSpan.FromSeconds(manuver.tSec);
                        var manuverNodeTime = date + TimeSpan.FromSeconds(secondsBurn * 0.5);
                        mass -= fuelBurned;

                        NewtonThrustCommand.CreateCommand(_orderEntity.FactionOwnerID, _orderEntity, manuverNodeTime, manuver.deltaV, secondsBurn);
                    }


                }
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
            var parentState = _orderEntity.GetSOIParentEntity().GetRelativeState();
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
            var manuverPos = _orderEntity.GetRelativeFuturePosition(manuverDateTime);
            var manuverVel = _orderEntity.GetRelativeFutureVelocity(manuverDateTime);
            var soi = _orderEntity.GetSOIParentEntity().GetDataBlob<OrbitDB>().SOI_m;
            var manuver = OrbitalMath.Hohmann2(_sgp, manuverPos.Length(), soi)[0];


            manuver.deltaV.Y += 1;
            var totalManuverDV = manuver.deltaV.Length();
            if(totalManuverDV > _totalDV)
                ImGui.TextColored(new Vector4(0.9f, 0, 0, 1), "Total Δv for all manuvers: " + Stringify.Velocity(totalManuverDV));
            else
                ImGui.Text("Total Δv for all manuvers: " + Stringify.Velocity(totalManuverDV));
            
            if (ImGui.Button("Make it so"))
            {

                double fuelBurned = OrbitMath.TsiolkovskyFuelUse(_totalMass, _exhaustVelocity, manuver.deltaV.Length());
                double secondsBurn = fuelBurned / _burnRate;
                //var manuverNodeTime = _atDatetime + TimeSpan.FromSeconds(secondsBurn * 0.5);
            
                NewtonThrustCommand.CreateCommand(_orderEntity.FactionOwnerID, _orderEntity, manuverDateTime, manuver.deltaV, secondsBurn);
            }

        }
    }
}