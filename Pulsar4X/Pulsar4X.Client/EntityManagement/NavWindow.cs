using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;
using Pulsar4X.Extensions;
using Pulsar4X.Orbital;
using Pulsar4X.SDL2UI;
using Pulsar4X.SDL2UI.ManuverNodes;
using Vector2 = System.Numerics.Vector2;
using Vector3 = Pulsar4X.Orbital.Vector3;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.ImGuiNetUI.EntityManagement
{
    public class NavWindow : PulsarGuiWindow
    {
        private Entity _orderEntity;

        private string _orderEntityName = "";
        //OrbitDB _ourOrbit;
        private double _sgp;
        private KeplerElements? _currentKE;
        private NewtonThrustAbilityDB? _newtonThrust;
        private double _totalMass;
        private double _dryMass;
        private double _cargoMass;
        private double _fuelMass;
        private ICargoable? _fuelType;
        private double _totalDV => _newtonThrust?.DeltaV ?? 0;
        double _burnRate;
        double _exhaustVelocity;
        private double _totalDVUsage = 0;
        private (Vector3 deltaV, double tSec)[]? _manuvers;
        float _phaseAngleRadians = 0;
        private DateTime _minDateTime;
        DateTime _atDatetime;
        Entity[] _siblingEntities = new Entity[0];
        string[] _siblingNames = new string[0];
        private int _selectedSibling = -1;

        Entity[] _uncleEntites = new Entity[0];
        string[] _uncleNames = new string[0];

        int _selectedUncle = -1;

        private double _truelongTgt; ///The true longitude of the target. Used for hohmann and phasing calcs
        private double _truelongInt; ///The true longitude of the interceptor. Used for hohmann and phasing calcs
        private int _kRevolutions;   ///Number of allowed revolutions of the traget when phasing

        private NavWindow(Entity orderEntity)
        {
            _flags = ImGuiWindowFlags.None;
            _orderEntity = orderEntity;
        }

        private ManuverLinesComplete _manuverLines = new ManuverLinesComplete();

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
            _orderEntityName = _orderEntity.GetName(_uiState.Faction.Id);
            _newtonThrust = _orderEntity.GetDataBlob<NewtonThrustAbilityDB>();
            _totalMass = _orderEntity.GetDataBlob<MassVolumeDB>().MassTotal;
            _dryMass = _orderEntity.GetDataBlob<MassVolumeDB>().MassDry;
            var soiParent = _orderEntity.GetSOIParentEntity();
            if(soiParent == null)
                throw new NullReferenceException();
            var parentMass = soiParent.GetDataBlob<MassVolumeDB>().MassTotal;
            _sgp = GeneralMath.StandardGravitationalParameter(_totalMass + parentMass);
            var fuelTypeID = _newtonThrust.FuelType;
            _fuelType = _uiState.Faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny(fuelTypeID);

            _burnRate = _newtonThrust.FuelBurnRate;
            _exhaustVelocity = _newtonThrust.ExhaustVelocity;


            _siblingEntities = soiParent.GetDataBlob<PositionDB>().Children.ToArray();
            List<string> names = new List<string>();
            foreach (var entity in _siblingEntities)
            {
                //TODO: this is going to show *all* entities, not just the ones this faction can see.
                //going to need to come up with a way to get this data. (filtering for this should be done in the engine not ui)
                string name = entity.GetDataBlob<NameDB>().GetName(_orderEntity.FactionOwnerID);
                names.Add(name);
            }
            _siblingNames = names.ToArray();

            var soiGrandparent = soiParent.GetSOIParentEntity();
            if(soiParent == _orderEntity.GetDataBlob<PositionDB>().Root || soiGrandparent == null)
                _uncleEntites = new Entity[0];
            else
            {
                _uncleEntites = soiGrandparent.GetDataBlob<PositionDB>().Children.ToArray();
            }
            names = new List<string>();
            foreach (var entity in _uncleEntites)
            {
                string name = entity.GetDataBlob<NameDB>().GetName(_orderEntity.FactionOwnerID);
                names.Add(name);
            }
            _uncleNames = names.ToArray();

            OnSystemTickChange(orderEntity.Entity.StarSysDateTime);

            _uiState.SelectedSysMapRender.SelectedEntityExtras.Add(_manuverLines);

            var soiParentPosition = _orderEntity.GetSOIParentPositionDB();
            if(soiParentPosition == null)
                throw new NullReferenceException();
            _manuverLines.RootSequence.ParentPosition = soiParentPosition;

        }

        public override void OnSystemTickChange(DateTime newDate)
        {
            _minDateTime = newDate;
            if (_atDatetime < _minDateTime)
                _atDatetime = _minDateTime;

            if (_orderEntity.HasDataBlob<OrbitDB>())
                _currentKE = _orderEntity.GetDataBlob<OrbitDB>().GetElements();
            else if (_orderEntity.HasDataBlob<OrbitUpdateOftenDB>())
                _currentKE = _orderEntity.GetDataBlob<OrbitUpdateOftenDB>().GetElements();
            else if (_orderEntity.HasDataBlob<OrbitDB>())
                _currentKE = _orderEntity.GetDataBlob<NewtonMoveDB>().GetElements();

            if (_targetSMA == 0 && _currentKE != null)
                _targetSMA = (float)_currentKE.Value.SemiMajorAxis;

            _totalMass = _orderEntity.GetDataBlob<MassVolumeDB>().MassTotal;
            var soiParent = _orderEntity.GetSOIParentEntity();
            if(soiParent == null)
                throw new NullReferenceException();

            var parentMass = soiParent.GetDataBlob<MassVolumeDB>().MassTotal;
            _sgp = GeneralMath.StandardGravitationalParameter(_totalMass + parentMass);
            _cargoMass = _orderEntity.GetDataBlob<VolumeStorageDB>().TotalStoredMass;

            if(_fuelType == null)
                throw new NullReferenceException();

            _fuelMass = _orderEntity.GetDataBlob<VolumeStorageDB>().GetUnitsStored(_fuelType);
        }


        enum NavMode
        {
            None,
            Edit,
            Thrust,
            HohmannTransfer,
            HohmannTransfer2,
            HohmannTransferOE,
            InterplanetaryTransfer,
            PhaseChange,
            Phasing,
            HighDVIntercept,
            PorkChopPlot,
            EscapeSOI
        }

        private NavMode _navMode = NavMode.None;
        private string[] nodenames = new string[0];
        private int _selectedNode = 0;
        
        private float _radialDV;
        private float _progradeDV;
        
        private int _indentAmount = 4;
        private int _indentDepth = 0;
        private int indent
        {
            get { return _indentAmount * (_indentDepth); }
        }
        void ManuverTree(ManuverSequence mseq)
        {
            
            
            if (ImGui.Selectable(mseq.SequenceName))
            {
                _navMode = NavMode.None;
                _manuverLines.SelectedSequence = mseq;
            }

            ImGui.Indent(indent);
            foreach (var mnode in mseq.ManuverNodes)
            {
                if (ImGui.Selectable(mnode.NodeName))
                {
                    _navMode = NavMode.Edit;
                    _manuverLines.EditingNodes = new ManuverNode[1];
                    _manuverLines.EditingNodes[0] = mnode;
                    _progradeDV = (float)mnode.Prograde;
                    _radialDV = (float)mnode.Radial;
                    _atDatetime = mnode.NodeTime;


                }
            }
            ImGui.Unindent();
            ImGui.Indent(indent);
            foreach (var seq in mseq.ManuverSequences)
            {
                ManuverTree(seq);
            }
            ImGui.Unindent(indent);
        }

        internal override void Display()
        {
            var soiParentPosition = _orderEntity.GetSOIParentPositionDB();
            if (!IsActive || soiParentPosition == null)
                return;
            ImGui.SetNextWindowSize(new Vector2(600f, 400f), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Nav Control: " + _orderEntityName, ref IsActive, _flags))
            {
                ImGui.Columns(2);
                ManuverTree(_manuverLines.RootSequence);
                ImGui.NextColumn();
                if (_navMode == NavMode.None)
                {
                    if (ImGui.Button("Manual Thrust"))
                    {
                        _manuverLines.AddNewEditNode(_orderEntity, _atDatetime);
                        _navMode = NavMode.Thrust;
                    }

                    if (ImGui.Button("Hohmann Transfer"))
                    {
                        _manuverLines.EditingNodes = new ManuverNode[2];
                        _manuverLines.EditingNodes[0] = new ManuverNode(_orderEntity, _atDatetime);
                        var halfOrbit = _manuverLines.EditingNodes[0].TargetOrbit.Period * 0.5;
                        _manuverLines.EditingNodes[1] = new ManuverNode(_orderEntity, _atDatetime + TimeSpan.FromSeconds(halfOrbit));
                        _manuverLines.RootSequence.ParentPosition = soiParentPosition;
                        _navMode = NavMode.HohmannTransfer;
                    }
                    if (ImGui.Button("Hohmann Transfer2"))
                    {
                        _manuverLines.EditingNodes = new ManuverNode[2];
                        _manuverLines.EditingNodes[0] = new ManuverNode(_orderEntity, _atDatetime);
                        var halfOrbit = _manuverLines.EditingNodes[0].TargetOrbit.Period * 0.5;
                        _manuverLines.EditingNodes[1] = new ManuverNode(_orderEntity, _atDatetime + TimeSpan.FromSeconds(halfOrbit));
                        _manuverLines.RootSequence.ParentPosition = soiParentPosition;
                        _navMode = NavMode.HohmannTransfer2;
                    }

                    if (ImGui.Button("Hohmann TransferOE"))
                    {
                        _manuverLines.EditingNodes = new ManuverNode[2];
                        _manuverLines.EditingNodes[0] = new ManuverNode(_orderEntity, _atDatetime);
                        var halfOrbit = _manuverLines.EditingNodes[0].TargetOrbit.Period * 0.5;
                        _manuverLines.EditingNodes[1] = new ManuverNode(_orderEntity, _atDatetime + TimeSpan.FromSeconds(halfOrbit));
                        _manuverLines.RootSequence.ParentPosition = soiParentPosition;
                        _navMode = NavMode.HohmannTransferOE;
                    }


                    if (ImGui.Button("Interplanetary Transfer"))
                    {
                        _navMode = NavMode.InterplanetaryTransfer;
                    }

                    if (ImGui.Button("Phase Change"))
                    {
                        _manuverLines.EditingNodes = new ManuverNode[2];
                        _manuverLines.EditingNodes[0] = new ManuverNode(_orderEntity, _atDatetime);
                        var halfOrbit = _manuverLines.EditingNodes[0].TargetOrbit.Period * 0.5;
                        _manuverLines.EditingNodes[1] = new ManuverNode(_orderEntity, _atDatetime + TimeSpan.FromSeconds(halfOrbit));
                        _navMode = NavMode.PhaseChange;
                    }

                    if (ImGui.Button("Phase Change 2"))
                    {
                        _manuverLines.EditingNodes = new ManuverNode[2];
                        _manuverLines.EditingNodes[0] = new ManuverNode(_orderEntity, _atDatetime);
                        var halfOrbit = _manuverLines.EditingNodes[0].TargetOrbit.Period * 0.5;
                        _manuverLines.EditingNodes[1] = new ManuverNode(_orderEntity, _atDatetime + TimeSpan.FromSeconds(halfOrbit));
                        _navMode = NavMode.Phasing;
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
                        _manuverLines.EditingNodes = new ManuverNode[1];
                        _manuverLines.EditingNodes[0] = new ManuverNode(_orderEntity, _atDatetime);
                        _navMode = NavMode.EscapeSOI;
                    }
                }

                switch (_navMode)
                {
                    case NavMode.Edit:
                        DisplayEditMode();
                        break;
                    case NavMode.Thrust:
                        DisplayThrustMode();
                        break;
                    case NavMode.PhaseChange:
                        DisplayPhaseChangeMode();
                        break;
                    case NavMode.Phasing:
                        DisplayPhasing();
                        break;
                    case NavMode.HohmannTransfer:
                        DisplayHohmannMode();
                        break;
                    case NavMode.HohmannTransfer2:
                        DisplayHohmannMode2();
                        break;
                    case NavMode.HohmannTransferOE:
                        DisplayHohmannModeOE();
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
                ImGui.Columns(1);
                ImGui.NewLine();
                ImGui.Text("Availible Δv: " + Stringify.Velocity(_totalDV));
                ImGui.Text("Dry Mass:" + Stringify.Mass(_dryMass, "0.######"));
                ImGui.Text("Total Mass: " + Stringify.Mass(_totalMass));
                ImGui.Text("Non Fuel Cargo: " + Stringify.Mass(_cargoMass - _fuelMass));
                var fuelName = _fuelType?.Name ?? "Unknown";
                ImGui.Text(fuelName + " Fuel: " + Stringify.Mass(_fuelMass));
                var thrust = _newtonThrust?.ThrustInNewtons ?? 0;
                ImGui.Text("Total Thrust: " + Stringify.Thrust(thrust));
            }
        }

        void DisplayEditMode()
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
                changes = true;
            }

            ImGui.Text("Time: " + _atDatetime); //ImGui.SameLine();

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
            var halfPeriod = _manuverLines.EditingNodes[0].PriorOrbit.Period * .5;
            if (ImGui.Button("-Apsis##t"))
            {
                _atDatetime -= TimeSpan.FromSeconds(halfPeriod);
                tseconds -= halfPeriod;
                changes = true;
            } ImGui.SameLine();
            if (ImGui.Button("+Apsis##t"))
            {
                _atDatetime -= TimeSpan.FromSeconds(halfPeriod);
                tseconds += halfPeriod;
                changes = true;
            } ImGui.SameLine();

            if (changes)
            {
                _manuverLines.ManipulateNode(0, _progradeDV, _radialDV, tseconds);
            }

            if (!_uiState.SelectedSysMapRender.SelectedEntityExtras.Contains(_manuverLines))
                _uiState.SelectedSysMapRender.SelectedEntityExtras.Add(_manuverLines);

            if(_orderEntity.Manager == null)
                throw new NullReferenceException();

            var deltat = _manuverLines.EditingNodes[0].NodeTime - _orderEntity.Manager.StarSysDateTime;
            ImGui.Text("node in: " + deltat);
            //if (ImGui.Button("Make it so")) { }
        }


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
                changes = true;
            }

            ImGui.Text("Time: " + _atDatetime); //ImGui.SameLine();

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
            }
            ImGui.SameLine();
            var halfPeriod = _manuverLines.EditingNodes[0].PriorOrbit.Period * .5;
            if (ImGui.Button("-Apsis##t"))
            {
                _atDatetime -= TimeSpan.FromSeconds(halfPeriod);
                tseconds -= halfPeriod;
                changes = true;
            } ImGui.SameLine();
            if (ImGui.Button("+Apsis##t"))
            {
                _atDatetime -= TimeSpan.FromSeconds(halfPeriod);
                tseconds += halfPeriod;
                changes = true;
            } //ImGui.SameLine();

            if (changes)
            {
                _manuverLines.ManipulateNode(0, _progradeDV, _radialDV, tseconds);
                tseconds = 0;

            }
            ImGui.Text(_progradeDV.ToString());
            ImGui.Text(_radialDV.ToString());
            ImGui.Text(_manuverLines.EditingNodes[0].TargetOrbit.Eccentricity.ToString());
            ImGui.Text(_manuverLines.EditingNodes[0].TargetOrbit.SemiMajorAxis.ToString());
            ImGui.Text(_manuverLines.EditingNodes[0].TargetOrbit.SemiMinorAxis.ToString());
            ImGui.Text(_manuverLines.EditingNodes[0].TargetOrbit.LoAN.ToString());
            ImGui.Text(_manuverLines.EditingNodes[0].TargetOrbit.AoP.ToString());

            if (!_uiState.SelectedSysMapRender.SelectedEntityExtras.Contains(_manuverLines))
                _uiState.SelectedSysMapRender.SelectedEntityExtras.Add(_manuverLines);

            if(_orderEntity.Manager == null)
                throw new NullReferenceException();

            var deltat = _manuverLines.EditingNodes[0].NodeTime - _orderEntity.Manager.StarSysDateTime;
            ImGui.Text("node in: " + deltat);

            if (ImGui.Button("Make it so"))
            {
                _manuverLines.EditingNodes[0].NodeName = "Thrust";
                _manuverLines.AddSequence("Thrust Manuver");
                _navMode = NavMode.None;
            }
        }


        void DisplayPhaseChangeMode()
        {
            ImGui.SliderAngle("PhaseAngle", ref _phaseAngleRadians);

            if(_currentKE == null)
                throw new NullReferenceException();

            _manuvers = InterceptCalcs.OrbitPhasingManuvers(_currentKE.Value, _sgp, _atDatetime, _phaseAngleRadians);

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

                if(_fuelType == null)
                    throw new NullReferenceException();

                double mass2 = _totalMass - (fuelBurned1 * _fuelType.MassPerUnit);
                double fuelBurned2 = OrbitMath.TsiolkovskyFuelUse(mass2, _exhaustVelocity, _manuvers[1].deltaV.Length());
                double secondsBurn2 = fuelBurned2 / _burnRate;
                var manuverNodeTime2 = manuverNodeTime1 + TimeSpan.FromSeconds(_manuvers[1].tSec);

                NewtonThrustCommand.CreateCommand(_orderEntity.FactionOwnerID, _orderEntity, manuverNodeTime2, _manuvers[1].deltaV, secondsBurn2);
            }
        }

        void DisplayPhasing()
        {
            var soiParent = _orderEntity.GetSOIParentEntity();
            if (soiParent == null || _currentKE == null)
                throw new NullReferenceException();

            double mySMA = _currentKE.Value.SemiMajorAxis;
            ImGui.SliderAngle("PhaseAngle", ref _phaseAngleRadians);
            ImGui.SliderInt("Revolutions", ref _kRevolutions, 1, 10);

            if (_currentKE == null)
                throw new NullReferenceException();

            _manuvers = OrbitalMath.Phasing(_sgp, lowestOrbit, _phaseAngleRadians, _currentKE.Value, 0, _kRevolutions);

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

                if (_fuelType == null)
                    throw new NullReferenceException();

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
            var soiParent = _orderEntity.GetSOIParentEntity();
            if(soiParent == null || _currentKE == null)
                throw new NullReferenceException();

            double mySMA = _currentKE.Value.SemiMajorAxis;
            float smaMin = 1;
            float smaMax = (float)soiParent.GetSOI_m();

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
            if(totalManuverDV > 0)
            {
                DateTime t1 = _orderEntity.StarSysDateTime + TimeSpan.FromSeconds(_manuvers[0].tSec);
                DateTime t2 = t1 + TimeSpan.FromSeconds(_manuvers[1].tSec);
                _manuverLines.EditingNodes[0].SetNode(_manuvers[0].deltaV, t1 );
                _manuverLines.EditingNodes[1].PriorOrbit = _manuverLines.EditingNodes[0].TargetOrbit;
                _manuverLines.EditingNodes[1].SetNode(_manuvers[1].deltaV, t2);
            }

            if (ImGui.Button("Make it so"))
            {
                double fuelBurned1 = OrbitMath.TsiolkovskyFuelUse(_totalMass, _exhaustVelocity, _manuvers[0].deltaV.Length());
                double secondsBurn1 = fuelBurned1 / _burnRate;
                var manuverNodeTime1 = _atDatetime + TimeSpan.FromSeconds(secondsBurn1 * 0.5);

                NewtonThrustCommand.CreateCommand(_orderEntity.FactionOwnerID, _orderEntity, manuverNodeTime1, _manuvers[0].deltaV, secondsBurn1);

                if(_fuelType == null)
                    throw new NullReferenceException();

                double mass2 = _totalMass - (fuelBurned1 * _fuelType.MassPerUnit); 
                double fuelBurned2 = OrbitMath.TsiolkovskyFuelUse(mass2, _exhaustVelocity, _manuvers[1].deltaV.Length());
                double secondsBurn2 = fuelBurned2 / _burnRate;
                var manuverNodeTime2 = manuverNodeTime1 + TimeSpan.FromSeconds(_manuvers[1].tSec);

                NewtonThrustCommand.CreateCommand(_orderEntity.FactionOwnerID, _orderEntity, manuverNodeTime2, _manuvers[1].deltaV, secondsBurn2);

                var newseq = new ManuverSequence();
                newseq.SequenceName = "Hohmann Transfer";
                _manuverLines.EditingNodes[0].NodeName = "Raise Periapsis";
                newseq.ManuverNodes.Add(_manuverLines.EditingNodes[0]);
                _manuverLines.EditingNodes[1].NodeName = "Circularise";
                newseq.ManuverNodes.Add(_manuverLines.EditingNodes[1]);
                _manuverLines.SelectedSequence.ManuverSequences.Add(newseq);
            }
        }

        void DisplayHohmannMode2()
        {
            var soiParent = _orderEntity.GetSOIParentEntity();
            if(soiParent == null || _currentKE == null)
                throw new NullReferenceException();

            double mySMA = _currentKE.Value.SemiMajorAxis;
            float smaMin = 1;
            float smaMax = (float)soiParent.GetSOI_m();

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
            if(totalManuverDV > 0)
            {
                DateTime t1 = _orderEntity.StarSysDateTime + TimeSpan.FromSeconds(_manuvers[0].tSec);
                DateTime t2 = t1 + TimeSpan.FromSeconds(_manuvers[1].tSec);
                _manuverLines.EditingNodes[0].SetNode(_manuvers[0].deltaV, t1 );
                _manuverLines.EditingNodes[1].PriorOrbit = _manuverLines.EditingNodes[0].TargetOrbit;
                _manuverLines.EditingNodes[1].SetNode(_manuvers[1].deltaV, t2);
            }

            if (ImGui.Button("Make it so"))
            {
                double fuelBurned1 = OrbitMath.TsiolkovskyFuelUse(_totalMass, _exhaustVelocity, _manuvers[0].deltaV.Length());
                double secondsBurn1 = fuelBurned1 / _burnRate;
                var manuverNodeTime1 = _atDatetime + TimeSpan.FromSeconds(secondsBurn1 * 0.5);

                var startObt = _manuverLines.EditingNodes[0].PriorOrbit;
                var tgtObt = _manuverLines.EditingNodes[0].TargetOrbit;
                NewtonSimpeThrustCommand.CreateCommand(_orderEntity.FactionOwnerID, _orderEntity, manuverNodeTime1, startObt, tgtObt );

                if(_fuelType == null)
                    throw new NullReferenceException();

                double mass2 = _totalMass - (fuelBurned1 * _fuelType.MassPerUnit); 
                double fuelBurned2 = OrbitMath.TsiolkovskyFuelUse(mass2, _exhaustVelocity, _manuvers[1].deltaV.Length());
                double secondsBurn2 = fuelBurned2 / _burnRate;
                var manuverNodeTime2 = manuverNodeTime1 + TimeSpan.FromSeconds(_manuvers[1].tSec);

                startObt = _manuverLines.EditingNodes[1].PriorOrbit;
                tgtObt = _manuverLines.EditingNodes[1].TargetOrbit;
                NewtonSimpeThrustCommand.CreateCommand(_orderEntity.FactionOwnerID, _orderEntity, manuverNodeTime2, startObt, tgtObt );

                var newseq = new ManuverSequence();
                newseq.SequenceName = "Hohmann Transfer";
                _manuverLines.EditingNodes[0].NodeName = "Raise Periapsis";
                newseq.ManuverNodes.Add(_manuverLines.EditingNodes[0]);
                _manuverLines.EditingNodes[1].NodeName = "Circularise";
                newseq.ManuverNodes.Add(_manuverLines.EditingNodes[1]);
                _manuverLines.SelectedSequence.ManuverSequences.Add(newseq);
            }
        }

        void DisplayHohmannModeOE()
        {
            var soiParent = _orderEntity.GetSOIParentEntity();
            if(soiParent == null || _currentKE == null)
                throw new NullReferenceException();

            double intSMA = _currentKE.Value.SemiMajorAxis;
            double trueanomInt =0.0;
            double trueanomTgt =0.0;
            double lopInt =0.0;
            double lopTgt =0.0;
            float smaMin = 1;
            float smaMax = (float)soiParent.GetSOI_m();

            if(ImGui.Combo("Target Object", ref _selectedSibling, _siblingNames, _siblingNames.Length  ))
            {
                Entity selectedSib = _siblingEntities[_selectedSibling];
                var TgtOrbitDB = _siblingEntities[_selectedSibling].GetDataBlob<OrbitDB>();
                var IntOrbitDB = _orderEntity.GetDataBlob<OrbitDB>();

                if(selectedSib.HasDataBlob<OrbitDB>())
                    _targetSMA = (float)TgtOrbitDB.SemiMajorAxis;
                if(selectedSib.HasDataBlob<OrbitUpdateOftenDB>())
                    _targetSMA = (float)_siblingEntities[_selectedSibling].GetDataBlob<OrbitUpdateOftenDB>().SemiMajorAxis;
                if(selectedSib.HasDataBlob<NewtonMoveDB>())
                    _targetSMA = (float)_siblingEntities[_selectedSibling].GetDataBlob<NewtonMoveDB >().GetElements().SemiMajorAxis;
                
                trueanomTgt = OrbitMath.GetTrueAnomaly(TgtOrbitDB,_atDatetime);
                trueanomInt = OrbitMath.GetTrueAnomaly(IntOrbitDB,_atDatetime);

                lopTgt = OrbitMath.GetLongditudeOfPeriapsis(TgtOrbitDB.Inclination, TgtOrbitDB.ArgumentOfPeriapsis, TgtOrbitDB.LongitudeOfAscendingNode);
                lopInt = OrbitMath.GetLongditudeOfPeriapsis(IntOrbitDB.Inclination, IntOrbitDB.ArgumentOfPeriapsis, IntOrbitDB.LongitudeOfAscendingNode);

                _truelongTgt = trueanomTgt + lopTgt;
                _truelongInt = trueanomInt + lopInt;
            }

            //TODO this should be radius from orbiting body not major axies.  
            ImGui.SliderFloat("Target SemiMajorAxis", ref _targetSMA, smaMin, smaMax);
            
            _manuvers = OrbitalMath.HohmannOE(_sgp, intSMA, _truelongInt, _targetSMA, _truelongTgt);

            double totalManuverTime = 0;
            double totalManuverDV = 0;
            foreach (var manuver in _manuvers)
            {
                var time = manuver.tSec;
                var dv = manuver.deltaV.Length();
                totalManuverTime += time;
                totalManuverDV += dv;
                double fuelBurned = OrbitMath.TsiolkovskyFuelUse(_totalMass, _exhaustVelocity, dv);
                double secondsBurn = fuelBurned / _burnRate;

                ImGui.Text(time + "time");
                ImGui.Text(dv + "Δv");
                ImGui.Text(fuelBurned + " fuel");
                ImGui.Text(Stringify.Number(secondsBurn, "0.###") + " Second Burn");

            }

            if(totalManuverDV > _totalDV)
                ImGui.TextColored(new Vector4(0.9f, 0, 0, 1), "Total Δv for all manuvers: " + Stringify.Velocity(totalManuverDV));
            else
                ImGui.Text("Total Δv for all manuvers: " + Stringify.Velocity(totalManuverDV));
            if(totalManuverDV > 0)
            {
                DateTime t1 = _orderEntity.StarSysDateTime + TimeSpan.FromSeconds(_manuvers[0].tSec);
                DateTime t2 = t1 + TimeSpan.FromSeconds(_manuvers[1].tSec);
                _manuverLines.EditingNodes[0].SetNode(_manuvers[0].deltaV, t1 );
                _manuverLines.EditingNodes[1].PriorOrbit = _manuverLines.EditingNodes[0].TargetOrbit;
                _manuverLines.EditingNodes[1].SetNode(_manuvers[1].deltaV, t2);
            }

            if (ImGui.Button("Make it so"))
            {
                double fuelBurned1 = OrbitMath.TsiolkovskyFuelUse(_totalMass, _exhaustVelocity, _manuvers[0].deltaV.Length());
                double secondsBurn1 = fuelBurned1 / _burnRate;
                var manuverNodeTime1 = _atDatetime + TimeSpan.FromSeconds(secondsBurn1 * 0.5);

                var startObt = _manuverLines.EditingNodes[0].PriorOrbit;
                var tgtObt = _manuverLines.EditingNodes[0].TargetOrbit;
                NewtonSimpeThrustCommand.CreateCommand(_orderEntity.FactionOwnerID, _orderEntity, manuverNodeTime1, startObt, tgtObt );

                if(_fuelType == null)
                    throw new NullReferenceException();

                double mass2 = _totalMass - (fuelBurned1 * _fuelType.MassPerUnit); 
                double fuelBurned2 = OrbitMath.TsiolkovskyFuelUse(mass2, _exhaustVelocity, _manuvers[1].deltaV.Length());
                double secondsBurn2 = fuelBurned2 / _burnRate;
                var manuverNodeTime2 = manuverNodeTime1 + TimeSpan.FromSeconds(_manuvers[1].tSec);

                startObt = _manuverLines.EditingNodes[1].PriorOrbit;
                tgtObt = _manuverLines.EditingNodes[1].TargetOrbit;
                NewtonSimpeThrustCommand.CreateCommand(_orderEntity.FactionOwnerID, _orderEntity, manuverNodeTime2, startObt, tgtObt );

                var newseq = new ManuverSequence();
                newseq.SequenceName = "Hohmann Transfer";
                _manuverLines.EditingNodes[0].NodeName = "Raise Periapsis";
                newseq.ManuverNodes.Add(_manuverLines.EditingNodes[0]);
                _manuverLines.EditingNodes[1].NodeName = "Circularise";
                newseq.ManuverNodes.Add(_manuverLines.EditingNodes[1]);
                _manuverLines.SelectedSequence.ManuverSequences.Add(newseq);
            }
        }

        void DisplayInterPlanetaryHohmannMode()
        {
            var soiParent = _orderEntity.GetSOIParentEntity();
            if(soiParent == null || _currentKE == null)
                throw new NullReferenceException();

            double mySMA = _currentKE.Value.SemiMajorAxis;
            float smaMax = (float)soiParent.GetSOI_m();

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
                _manuvers = InterceptCalcs.InterPlanetaryHohmann(soiParent, _uncleEntites[_selectedUncle], _orderEntity);

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
            var soiParent = _orderEntity.GetSOIParentEntity();

            if(soiParent == null || _currentKE == null)
                throw new NullReferenceException();

            var parentState = soiParent.GetRelativeState();
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

            double mySMA = _currentKE.Value.SemiMajorAxis;
            //double escapeSMA = 
            var manuverDateTime = _atDatetime + TimeSpan.FromSeconds(secondsToManuver);
            var manuverPos = _orderEntity.GetRelativeFuturePosition(manuverDateTime);
            var manuverVel = _orderEntity.GetRelativeFutureVelocity(manuverDateTime);
            var soi = soiParent.GetDataBlob<OrbitDB>().SOI_m;
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