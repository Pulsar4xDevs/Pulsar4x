using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Orbital;
using Vector2 = System.Numerics.Vector2;
using Vector3 = Pulsar4X.Orbital.Vector3;

namespace Pulsar4X.Client
{
    public class NavWindow : UniquePulsarGuiWindow<NavWindow>
    {
        private int _entityId;
        private string _systemId = "";

        float _phaseAngleRadians = 0;
        private DateTime _minDateTime;
        DateTime _atDatetime;
        private int _selectedSiblingId = -1;
        private int _selectedUncleId = -1;
        private float _targetSMA = 0;
        private (Vector3 deltaV, double tSec)[]? _manuvers;

        private readonly PorkchopPanel _pork = new();
        private float _porkDepartDays;
        private float _porkTofMinDays;
        private float _porkTofMaxDays;
        private int _porkSteps = 48;
        private float _porkCeiling;
        private bool _porkPrograde = true;
        private int _porkAxesForId = int.MinValue;
        private Task<CommandResult>? _plotTask;
        private string _plotError = "";

        private ManuverLinesComplete _manuverLines = new ManuverLinesComplete();

        private NavWindow(int entityId, string systemId)
        {
            _flags = ImGuiWindowFlags.None;
            _entityId = entityId;
            _systemId = systemId;
        }

        public static NavWindow GetInstance(EntityState orderEntity)
        {
            if(!_uiState.TryGetUniqueWindow<NavWindow>(out var window))
            {
                window = _uiState.AddUniqueWindow(new NavWindow(orderEntity.Id, orderEntity.StarSystemId!));
                window.HardRefresh();
                return window;
            }

            if (window._entityId != orderEntity.Id)
            {
                window._entityId = orderEntity.Id;
                window._systemId = orderEntity.StarSystemId!;
                window.HardRefresh();
            }

            return window;
        }

        private void HardRefresh()
        {
            // Reset maneuver data when switching to a different entity
            _manuverLines = new ManuverLinesComplete();
            _navMode = NavMode.None;
            _selectedSiblingId = -1;
            _selectedUncleId = -1;
            _targetSMA = 0;
            _manuvers = null;
            _atDatetime = _uiState.PrimarySystemDateTime;
            _pork.Reset();
            _porkAxesForId = int.MinValue;
            _porkCeiling = 0;
            _porkDepartDays = 0;
            _plotTask = null;
            _plotError = "";

            var system = _uiState.GameClient?.Galaxy.GetSystem(_systemId);
            var entity = system?.GetEntity(_entityId);
            if (system == null || entity == null)
                return;

            if (entity.GetSoiParent(system) is { } soiParent)
                _manuverLines.RootSequence.ParentPosition = new SnapshotPosition(_uiState, _systemId, soiParent.Id);

            _uiState.SelectedSysMapRender?.SelectedEntityExtras.Add(_manuverLines);
        }

        public override void OnSystemTickChange(DateTime newDate)
        {
            _minDateTime = newDate;
            if (_atDatetime < _minDateTime)
                _atDatetime = _minDateTime;
        }


        enum NavMode
        {
            None,
            Edit,
            Thrust,
            HohmannTransfer,
            InterplanetaryTransfer,
            PhaseChange,
            EscapeSOI,
            Porkchop
        }

        private NavMode _navMode = NavMode.None;

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

            ImGui.PushID(mseq.GetHashCode());
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
            ImGui.PopID();
            ImGui.Indent(indent);
            foreach (var seq in mseq.ManuverSequences)
            {
                ManuverTree(seq);
            }
            ImGui.Unindent(indent);
        }

        /// <summary>The faction-visible bodies orbiting the given parent (transfer targets),
        /// excluding the ordering ship itself.</summary>
        private List<(EntitySnapshot Body, OrbitView Orbit)> ChildrenOf(IClientSystem system, int parentId)
        {
            var children = new List<(EntitySnapshot, OrbitView)>();
            foreach (var other in system.Entities)
            {
                if (other.Id == _entityId)
                    continue;
                var orbit = other.GetView<OrbitView>();
                if (orbit == null || orbit.ParentId != parentId || orbit.StandardGravParameter <= 0)
                    continue;
                children.Add((other, orbit));
            }
            return children;
        }

        /// <summary>An ImGui combo over candidate bodies; tracks selection by entity id.</summary>
        private EntitySnapshot? TargetBodyCombo(List<(EntitySnapshot Body, OrbitView Orbit)> candidates, ref int selectedId)
        {
            string[] names = new string[candidates.Count];
            int selectedIndex = -1;
            for (int i = 0; i < candidates.Count; i++)
            {
                names[i] = candidates[i].Body.GetView<NameView>()?.Name ?? "Unknown";
                if (candidates[i].Body.Id == selectedId)
                    selectedIndex = i;
            }

            if (ImGui.Combo("Target Object", ref selectedIndex, names, names.Length)
                && selectedIndex >= 0 && selectedIndex < candidates.Count)
            {
                selectedId = candidates[selectedIndex].Body.Id;
                _targetSMA = (float)candidates[selectedIndex].Orbit.SemiMajorAxisM;
            }

            return selectedIndex >= 0 && selectedIndex < candidates.Count ? candidates[selectedIndex].Body : null;
        }

        internal override void Display()
        {
            if (!IsActive)
                return;

            var system = _uiState.GameClient?.Galaxy.GetSystem(_systemId);
            var entity = system?.GetEntity(_entityId);
            var thrust = entity?.GetView<ThrustView>();
            var massVolume = entity?.GetView<MassVolumeView>();
            var orbit = entity?.ResolveOrbit();
            var soiParent = entity?.GetSoiParent(system!);
            if (system == null || entity == null || thrust == null || massVolume == null || orbit == null || soiParent == null)
            {
                IsActive = false;
                return;
            }

            var currentKE = orbit.ToKeplerElements();
            if (_targetSMA == 0)
                _targetSMA = (float)currentKE.SemiMajorAxis;

            string entityName = entity.GetView<NameView>()?.Name ?? "Unknown";
            ImGui.SetNextWindowSize(new Vector2(860f, 560f), ImGuiCond.FirstUseEver);
            if (Window.Begin("Nav Control: " + entityName, ref IsActive, _flags))
            {
                ImGui.Columns(2, "##nav-window", true);
                ManuverTree(_manuverLines.RootSequence);
                ImGui.NextColumn();
                if (_navMode == NavMode.None)
                {
                    if (ImGui.Button("Manual Thrust"))
                    {
                        _manuverLines.AddNewEditNode(_uiState, _systemId, _entityId, _atDatetime);
                        _navMode = NavMode.Thrust;
                    }

                    if (ImGui.Button("Hohmann Transfer"))
                    {
                        _manuverLines.EditingNodes = new ManuverNode[2];
                        _manuverLines.EditingNodes[0] = new ManuverNode(_uiState, _systemId, _entityId, _atDatetime);
                        var halfOrbit = _manuverLines.EditingNodes[0].TargetOrbit.Period * 0.5;
                        _manuverLines.EditingNodes[1] = new ManuverNode(_uiState, _systemId, _entityId, _atDatetime + TimeSpan.FromSeconds(halfOrbit));
                        _navMode = NavMode.HohmannTransfer;
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("Transfer Windows"))
                    {
                        _manuverLines.EditingNodes = new ManuverNode[2];
                        _manuverLines.EditingNodes[0] = new ManuverNode(_uiState, _systemId, _entityId, _atDatetime);
                        _manuverLines.EditingNodes[1] = new ManuverNode(_uiState, _systemId, _entityId, _atDatetime);
                        if (_porkCeiling <= 0)
                            _porkCeiling = (float)thrust.DeltaVMps;
                        _navMode = NavMode.Porkchop;
                    }

                    if (ImGui.Button("Interplanetary Transfer"))
                    {
                        _pork.Reset();
                        _porkAxesForId = int.MinValue;
                        _plotTask = null;
                        _plotError = "";
                        if (_porkCeiling <= 0)
                            _porkCeiling = (float)thrust.DeltaVMps;
                        _navMode = NavMode.InterplanetaryTransfer;
                    }

                    if (ImGui.Button("Phase Change"))
                    {
                        _manuverLines.EditingNodes = new ManuverNode[2];
                        _manuverLines.EditingNodes[0] = new ManuverNode(_uiState, _systemId, _entityId, _atDatetime);
                        var halfOrbit = _manuverLines.EditingNodes[0].TargetOrbit.Period * 0.5;
                        _manuverLines.EditingNodes[1] = new ManuverNode(_uiState, _systemId, _entityId, _atDatetime + TimeSpan.FromSeconds(halfOrbit));
                        _navMode = NavMode.PhaseChange;
                    }

                    if (ImGui.Button("Escape SOI"))
                    {
                        _manuverLines.EditingNodes = new ManuverNode[1];
                        _manuverLines.EditingNodes[0] = new ManuverNode(_uiState, _systemId, _entityId, _atDatetime);
                        _navMode = NavMode.EscapeSOI;
                    }
                }

                switch (_navMode)
                {
                    case NavMode.Edit:
                        DisplayEditMode(thrust);
                        break;
                    case NavMode.Thrust:
                        DisplayThrustMode(thrust);
                        break;
                    case NavMode.PhaseChange:
                        DisplayPhaseChangeMode(system, entity, thrust, massVolume, currentKE);
                        break;
                    case NavMode.HohmannTransfer:
                        DisplayHohmannMode(system, entity, soiParent, thrust, massVolume, currentKE);
                        break;
                    case NavMode.Porkchop:
                        DisplayPorkchopMode(system, soiParent, thrust, currentKE);
                        break;
                    case NavMode.InterplanetaryTransfer:
                        DisplayInterplanetaryMode(system, soiParent, thrust, massVolume, currentKE);
                        break;
                    case NavMode.EscapeSOI:
                        DisplayEscapeSOI(system, entity, soiParent, thrust, massVolume, currentKE, orbit);
                        break;
                    case NavMode.None:
                        break;
                    default:
                        break;
                }
                ImGui.Columns(1);
                ImGui.NewLine();
                double fuelMass = thrust.TotalFuelKg;
                double cargoMass = entity.GetView<CargoStorageView>()?.TotalStoredMassKg ?? 0;
                ImGui.Text("Availible Δv: " + Stringify.Velocity(thrust.DeltaVMps));
                ImGui.Text("Dry Mass:" + Stringify.Mass(massVolume.DryMassKg, "0.######"));
                ImGui.Text("Total Mass: " + Stringify.Mass(massVolume.MassKg));
                ImGui.Text("Non Fuel Cargo: " + Stringify.Mass(cargoMass - fuelMass));
                var fuelName = string.IsNullOrEmpty(thrust.FuelName) ? "Unknown" : thrust.FuelName;
                ImGui.Text(fuelName + " Fuel: " + Stringify.Mass(fuelMass));
                ImGui.Text("Total Thrust: " + Stringify.Thrust(thrust.ThrustNewtons));
            }
            Window.End();
        }

        /// <summary>Submits one queued burn; the node time centres the burn like the engine does.</summary>
        void SubmitBurn(Vector3 deltaV, DateTime nodeTime)
        {
            _uiState.GameClient?.SubmitCommandAsync(new Pulsar4X.Api.NewtonThrustCommand(
                _entityId, nodeTime, new Vec3(deltaV.X, deltaV.Y, deltaV.Z)));
        }

        /// <summary>The classic two-burn dispatch shared by the phase-change and Hohmann modes:
        /// first burn centred half its duration after now, second a fixed delay later.</summary>
        void SubmitTwoBurns((Vector3 deltaV, double tSec)[] manuvers, ThrustView thrust, MassVolumeView massVolume)
        {
            double fuelBurned1 = OrbitalMath.TsiolkovskyFuelUse(massVolume.MassKg, thrust.ExhaustVelocityMps, manuvers[0].deltaV.Length());
            double secondsBurn1 = thrust.FuelBurnRateKgPerSec > 0 ? fuelBurned1 / thrust.FuelBurnRateKgPerSec : 0;
            var manuverNodeTime1 = _atDatetime + TimeSpan.FromSeconds(secondsBurn1 * 0.5);

            SubmitBurn(manuvers[0].deltaV, manuverNodeTime1);

            var manuverNodeTime2 = manuverNodeTime1 + TimeSpan.FromSeconds(manuvers[1].tSec);
            SubmitBurn(manuvers[1].deltaV, manuverNodeTime2);
        }

        void DisplayEditMode(ThrustView thrust)
        {
            if (_manuverLines.EditingNodes.Length == 0)
                return;

            bool changes = false;
            float maxprogradeDV = (float)(thrust.DeltaVMps - Math.Abs(_radialDV));
            float maxradialDV = (float)(thrust.DeltaVMps - Math.Abs(_progradeDV));
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

            if (_uiState.SelectedSysMapRender != null && !_uiState.SelectedSysMapRender.SelectedEntityExtras.Contains(_manuverLines))
                _uiState.SelectedSysMapRender.SelectedEntityExtras.Add(_manuverLines);

            var deltat = _manuverLines.EditingNodes[0].NodeTime - _uiState.PrimarySystemDateTime;
            ImGui.Text("node in: " + deltat);
        }


        void DisplayThrustMode(ThrustView thrust)
        {
            if (_manuverLines.EditingNodes.Length == 0)
                return;

            bool changes = false;
            float maxprogradeDV = (float)(thrust.DeltaVMps - Math.Abs(_radialDV));
            float maxradialDV = (float)(thrust.DeltaVMps - Math.Abs(_progradeDV));
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

            if (_uiState.SelectedSysMapRender != null && !_uiState.SelectedSysMapRender.SelectedEntityExtras.Contains(_manuverLines))
                _uiState.SelectedSysMapRender.SelectedEntityExtras.Add(_manuverLines);

            var deltat = _manuverLines.EditingNodes[0].NodeTime - _uiState.PrimarySystemDateTime;
            ImGui.Text("node in: " + deltat);

            if (ImGui.Button("Make it so"))
            {
                var node = _manuverLines.EditingNodes[0];
                node.NodeName = "Thrust";
                SubmitBurn(new Vector3(node.Radial, node.Prograde, node.Normal), node.NodeTime);
                _manuverLines.AddSequence("Thrust Manuver");
                _navMode = NavMode.None;
            }
        }


        void DisplayPhaseChangeMode(IClientSystem system, EntitySnapshot entity, ThrustView thrust, MassVolumeView massVolume, KeplerElements currentKE)
        {
            ImGui.SliderAngle("PhaseAngle", ref _phaseAngleRadians);

            _manuvers = OrbitalMath.OrbitPhasingManuvers(currentKE, currentKE.StandardGravParameter, _atDatetime, _phaseAngleRadians);

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
                SubmitTwoBurns(_manuvers, thrust, massVolume);
            }
        }

        void DisplayHohmannMode(IClientSystem system, EntitySnapshot entity, EntitySnapshot soiParent, ThrustView thrust, MassVolumeView massVolume, KeplerElements currentKE)
        {
            double mySMA = currentKE.SemiMajorAxis;
            float smaMin = 1;
            float smaMax = (float)Math.Min(soiParent.SoiRadiusM(), 1e13);

            TargetBodyCombo(ChildrenOf(system, soiParent.Id), ref _selectedSiblingId);

            //TODO this should be radius from orbiting body not major axies.
            ImGui.SliderFloat("Target SemiMajorAxis", ref _targetSMA, smaMin, smaMax);
            _manuvers = OrbitalMath.Hohmann2(currentKE.StandardGravParameter, mySMA, _targetSMA);

            double totalManuverDV = 0;
            foreach (var manuver in _manuvers)
            {
                var dv = manuver.deltaV.Length();
                totalManuverDV += dv;
                double fuelBurned = OrbitalMath.TsiolkovskyFuelUse(massVolume.MassKg, thrust.ExhaustVelocityMps, dv);
                double secondsBurn = thrust.FuelBurnRateKgPerSec > 0 ? fuelBurned / thrust.FuelBurnRateKgPerSec : 0;
                ImGui.Text(dv + "Δv");
                ImGui.Text(fuelBurned + " fuel");
                ImGui.Text(Stringify.Quantity(secondsBurn, "0.###") + " Second Burn");

            }

            if(totalManuverDV > thrust.DeltaVMps)
                ImGui.TextColored(new Vector4(0.9f, 0, 0, 1), "Total Δv for all manuvers: " + Stringify.Velocity(totalManuverDV));
            else
                ImGui.Text("Total Δv for all manuvers: " + Stringify.Velocity(totalManuverDV));
            if(totalManuverDV > 0 && _manuverLines.EditingNodes.Length >= 2)
            {
                DateTime t1 = _uiState.PrimarySystemDateTime + TimeSpan.FromSeconds(_manuvers[0].tSec);
                DateTime t2 = t1 + TimeSpan.FromSeconds(_manuvers[1].tSec);
                _manuverLines.EditingNodes[0].SetNode(_manuvers[0].deltaV, t1 );
                _manuverLines.EditingNodes[1].PriorOrbit = _manuverLines.EditingNodes[0].TargetOrbit;
                _manuverLines.EditingNodes[1].SetNode(_manuvers[1].deltaV, t2);
            }

            if (ImGui.Button("Make it so"))
            {
                SubmitTwoBurns(_manuvers, thrust, massVolume);

                var newseq = new ManuverSequence();
                newseq.SequenceName = "Hohmann Transfer";
                _manuverLines.EditingNodes[0].NodeName = "Raise Periapsis";
                newseq.ManuverNodes.Add(_manuverLines.EditingNodes[0]);
                _manuverLines.EditingNodes[1].NodeName = "Circularise";
                newseq.ManuverNodes.Add(_manuverLines.EditingNodes[1]);
                _manuverLines.SelectedSequence.ManuverSequences.Add(newseq);
            }
        }

        void DisplayInterplanetaryMode(IClientSystem system, EntitySnapshot soiParent, ThrustView thrust, MassVolumeView massVolume, KeplerElements currentKE)
        {
            TakePlotResult();
            if (_navMode != NavMode.InterplanetaryTransfer)
                return;
            _pork.Take();

            var grandParent = soiParent.GetSoiParent(system);
            if (grandParent == null)
            {
                ImGui.Text("Orbit a planet first. This plots a transfer to one of its siblings.");
                return;
            }

            var uncles = ChildrenOf(system, grandParent.Id);
            uncles.RemoveAll(u => u.Body.Id == soiParent.Id);
            var selected = TargetBodyCombo(uncles, ref _selectedUncleId);
            var originOrbit = soiParent.ResolveOrbit();
            var targetOrbit = selected?.ResolveOrbit();
            if (selected == null || originOrbit == null || targetOrbit == null)
            {
                ImGui.Text("Pick another planet.");
                return;
            }

            double targetRadius = selected.LowOrbitRadiusM();
            double targetMass = selected.GetView<MassVolumeView>()?.MassKg ?? 0;
            if (!(targetRadius > 1) || !(targetMass > 0))
            {
                ImGui.Text("That body has no radius to capture at.");
                return;
            }

            KeplerElements originKe = originOrbit.ToKeplerElements();
            KeplerElements targetKe = targetOrbit.ToKeplerElements();
            if (selected.Id != _porkAxesForId || _porkDepartDays <= 0)
            {
                SuggestPorkchopAxes(originKe, targetKe);
                _porkAxesForId = selected.Id;
                _pork.Reset();
            }

            if (_porkCeiling <= 0)
                _porkCeiling = (float)thrust.DeltaVMps;

            ImGui.SliderFloat("Departure span (days)", ref _porkDepartDays, 1f, 2000f, "%.1f");
            ImGui.SliderFloat("TOF min (days)", ref _porkTofMinDays, 0.05f, 2000f, "%.2f");
            ImGui.SliderFloat("TOF max (days)", ref _porkTofMaxDays, 0.05f, 2000f, "%.2f");
            ImGui.SliderInt("Grid size", ref _porkSteps, 8, 120);
            float ceilMax = Math.Max(1000f, Math.Max(_porkCeiling, (float)thrust.DeltaVMps * 2f));
            ImGui.SliderFloat("Δv ceiling", ref _porkCeiling, 0f, ceilMax, "%.0f m/s");
            ImGui.Checkbox("Prograde", ref _porkPrograde);

            (Vector3 pos, Vector3 vel) park = PorkchopPanel.KeplerState(currentKE, _uiState.PrimarySystemDateTime);
            double rPark = park.pos.Length();
            double muPlanet = currentKE.StandardGravParameter;
            double muTarget = GeneralMath.StandardGravitationalParameter(massVolume.MassKg + targetMass);
            double rp = targetRadius;

            bool computing = _pork.Scanning;
            if (computing)
                ImGui.Text("Computing…");
            else if (ImGui.Button("Compute"))
            {
                _pork.Start(originKe, targetKe, _uiState.PrimarySystemDateTime,
                    Math.Max(1.0, _porkDepartDays) * 86400.0,
                    Math.Max(1.0, _porkTofMinDays) * 86400.0,
                    _porkTofMaxDays * 86400.0,
                    _porkSteps, _porkPrograde,
                    (vInf, _) => BurnCost(vInf, v => OrbitalMath.EscapeBurn(muPlanet, rPark, v)),
                    (vInf, _) => BurnCost(vInf, v => OrbitalMath.CaptureBurn(muTarget, v, rp)));
            }
            if (_pork.Error.Length > 0)
                ImGui.TextColored(new Vector4(0.9f, 0.3f, 0.2f, 1f), _pork.Error);
            if (_plotError.Length > 0)
                ImGui.TextColored(new Vector4(0.9f, 0.3f, 0.2f, 1f), _plotError);
            if (_pork.Grid == null)
                return;

            _pork.Draw(_porkCeiling, tripCost: true);
            if (_pork.Selected is not TransferWindow chosen)
                return;
            if (!chosen.IsFinite)
            {
                ImGui.Text("That cell has no solution.");
                return;
            }

            double escape = OrbitalMath.EscapeBurn(muPlanet, rPark, chosen.VInfDepart);
            double capture = OrbitalMath.CaptureBurn(muTarget, chosen.VInfArrive, rp);
            double allowance = TransferWindows.CorrectionAllowanceMps;
            double need = escape + capture + allowance;
            ImGui.Text("Depart " + chosen.Depart.ToString("yyyy-MM-dd HH:mm"));
            ImGui.Text("Arrive " + chosen.Arrive.ToString("yyyy-MM-dd HH:mm"));
            ImGui.Text("Escape " + Stringify.Velocity(escape));
            ImGui.Text("Correction allowance " + Stringify.Velocity(allowance));
            ImGui.Text("Capture " + Stringify.Velocity(capture));
            if (!double.IsFinite(need) || need > thrust.DeltaVMps)
                ImGui.TextColored(new Vector4(0.9f, 0f, 0f, 1f),
                    $"needs {need:N0} m/s Δv, have {thrust.DeltaVMps:N0} m/s");

            bool plotting = _plotTask != null && !_plotTask.IsCompleted;
            if (plotting)
                ImGui.Text("Plotting…");
            else if (ImGui.Button("Make it so"))
            {
                _plotError = "";
                _plotTask = _uiState.GameClient?.SubmitCommandAsync(new TransferFromWindowCommand(
                    _entityId,
                    selected.Id,
                    chosen.Depart,
                    chosen.Arrive,
                    new Vec3(chosen.DepartDeltaV.X, chosen.DepartDeltaV.Y, chosen.DepartDeltaV.Z),
                    new Vec3(chosen.ArriveDeltaV.X, chosen.ArriveDeltaV.Y, chosen.ArriveDeltaV.Z)));
            }
        }

        static double BurnCost(Vector3 vInf, Func<double, double> burn)
        {
            double v = vInf.Length();
            if (!(v > 1) || !double.IsFinite(v))
                return double.NaN;
            double dv = burn(v);
            return double.IsFinite(dv) && dv >= 0 ? dv : double.NaN;
        }

        void TakePlotResult()
        {
            if (_plotTask == null || !_plotTask.IsCompleted)
                return;
            if (_plotTask.Status == TaskStatus.RanToCompletion)
            {
                CommandResult result = _plotTask.Result;
                if (result.Accepted)
                    _navMode = NavMode.None;
                else
                    _plotError = string.IsNullOrEmpty(result.RejectionReason)
                        ? "transfer did not converge"
                        : result.RejectionReason;
            }
            else if (_plotTask.IsFaulted)
                _plotError = _plotTask.Exception?.GetBaseException().Message ?? "transfer did not converge";
            _plotTask = null;
        }

        private bool _EscapeVelocityHigh = true;
        void DisplayEscapeSOI(IClientSystem system, EntitySnapshot entity, EntitySnapshot soiParent, ThrustView thrust, MassVolumeView massVolume, KeplerElements currentKE, OrbitView orbit)
        {
            var parentState = soiParent.GetRelativeState(_uiState.PrimarySystemDateTime);
            var parentAngle = Math.Atan2(parentState.pos.Y, parentState.pos.X);

            double orbitalPeriod = currentKE.Period;
            double e = currentKE.Eccentricity;

            var wc1 = Math.Sqrt((1 - e) / (1 + e));
            var wc2 = Math.Tan(parentAngle / 2);
            double E = 2 * Math.Atan(wc1 * wc2);
            double wc3 = orbitalPeriod / (Math.PI * 2);
            double wc4 = E - e * Math.Sin(E);
            double phaseTime = wc3 * wc4;

            Switch.Switch2State("Escape:", ref _EscapeVelocityHigh, "Low", "High");

            double secondsToManuver = phaseTime;
            if (!_EscapeVelocityHigh)
                secondsToManuver += orbitalPeriod * 0.5;

            var manuverDateTime = _atDatetime + TimeSpan.FromSeconds(secondsToManuver);
            var manuverPos = entity.GetRelativeState(manuverDateTime).pos;
            var soi = soiParent.SoiRadiusM();
            if (double.IsInfinity(soi))
            {
                ImGui.Text("Already orbiting the system primary — there is no SOI to escape.");
                return;
            }
            double escapeDv = OrbitalMath.EscapeBurn(currentKE.StandardGravParameter, manuverPos.Length(), 0);
            if (!double.IsFinite(escapeDv) || escapeDv < 0)
                escapeDv = 0;
            var burn = new Vector3(0, escapeDv, 0);
            var totalManuverDV = burn.Length();
            if(totalManuverDV > thrust.DeltaVMps)
                ImGui.TextColored(new Vector4(0.9f, 0, 0, 1), "Total Δv for all manuvers: " + Stringify.Velocity(totalManuverDV));
            else
                ImGui.Text("Total Δv for all manuvers: " + Stringify.Velocity(totalManuverDV));

            if (ImGui.Button("Make it so"))
            {
                SubmitBurn(burn, manuverDateTime);
            }

        }

        void DisplayPorkchopMode(IClientSystem system, EntitySnapshot soiParent, ThrustView thrust, KeplerElements currentKE)
        {
            _pork.Take();
            var selected = TargetBodyCombo(ChildrenOf(system, soiParent.Id), ref _selectedSiblingId);
            var targetOrbit = selected?.GetView<OrbitView>();
            if (selected == null || targetOrbit == null)
            {
                ImGui.Text("Pick a body orbiting the same parent.");
                return;
            }

            KeplerElements targetKe = targetOrbit.ToKeplerElements();
            if (selected.Id != _porkAxesForId || _porkDepartDays <= 0)
            {
                SuggestPorkchopAxes(currentKE, targetKe);
                _porkAxesForId = selected.Id;
                _pork.Reset();
            }

            if (_porkCeiling <= 0)
                _porkCeiling = (float)thrust.DeltaVMps;

            ImGui.SliderFloat("Departure span (days)", ref _porkDepartDays, 1f, 2000f, "%.1f");
            ImGui.SliderFloat("TOF min (days)", ref _porkTofMinDays, 0.05f, 2000f, "%.2f");
            ImGui.SliderFloat("TOF max (days)", ref _porkTofMaxDays, 0.05f, 2000f, "%.2f");
            ImGui.SliderInt("Grid size", ref _porkSteps, 8, 120);
            float ceilMax = Math.Max(1000f, Math.Max(_porkCeiling, (float)thrust.DeltaVMps * 2f));
            ImGui.SliderFloat("Δv ceiling", ref _porkCeiling, 0f, ceilMax, "%.0f m/s");
            ImGui.Checkbox("Prograde", ref _porkPrograde);

            bool computing = _pork.Scanning;
            if (computing)
                ImGui.Text("Computing…");
            else if (ImGui.Button("Compute"))
            {
                _pork.Start(currentKE, targetKe, _uiState.PrimarySystemDateTime,
                    Math.Max(1.0, _porkDepartDays) * 86400.0,
                    Math.Max(1.0, _porkTofMinDays) * 86400.0,
                    _porkTofMaxDays * 86400.0,
                    _porkSteps, _porkPrograde,
                    null, null);
            }
            if (_pork.Error.Length > 0)
                ImGui.TextColored(new Vector4(0.9f, 0.3f, 0.2f, 1f), _pork.Error);

            if (_pork.Grid == null)
                return;

            _pork.Draw(_porkCeiling, tripCost: false);
            if (_pork.Selected is not TransferWindow chosen)
                return;
            if (!chosen.IsFinite)
            {
                ImGui.Text("That cell has no solution.");
                return;
            }

            PreviewPorkchop(chosen, currentKE, targetKe);
            ImGui.Text("Depart " + chosen.Depart.ToString("yyyy-MM-dd HH:mm"));
            ImGui.Text("Arrive " + chosen.Arrive.ToString("yyyy-MM-dd HH:mm"));
            ImGui.Text("Δv " + Stringify.Velocity(chosen.TotalDeltaV)
                + "  (depart " + Stringify.Velocity(chosen.DepartDeltaV.Length())
                + ", arrive " + Stringify.Velocity(chosen.ArriveDeltaV.Length()) + ")");
            if (chosen.TotalDeltaV > thrust.DeltaVMps)
                ImGui.TextColored(new Vector4(0.9f, 0f, 0f, 1f), "Over the ship's budget.");
            if (ImGui.Button("Make it so"))
                CommitPorkchop(chosen, currentKE, targetKe);
        }

        void SuggestPorkchopAxes(KeplerElements shipKe, KeplerElements targetKe)
        {
            double synodic = TransferWindows.SynodicSeconds(shipKe.Period, targetKe.Period);
            double days = double.IsFinite(synodic) && synodic > 0 ? synodic / 86400.0 : 365.25;
            if (days < 1)
                days = 1;
            if (days > 2000)
                days = 2000;
            _porkDepartDays = (float)days;

            (Vector3 pos, Vector3 vel) shipNow = PorkchopPanel.KeplerState(shipKe, _uiState.PrimarySystemDateTime);
            (Vector3 pos, Vector3 vel) targetNow = PorkchopPanel.KeplerState(targetKe, _uiState.PrimarySystemDateTime);
            var band = TransferWindows.HohmannTofBand(shipKe.StandardGravParameter, shipNow.pos.Length(), targetNow.pos.Length());
            if (double.IsFinite(band.tofMin) && double.IsFinite(band.tofMax) && band.tofMax > band.tofMin)
            {
                _porkTofMinDays = (float)(band.tofMin / 86400.0);
                _porkTofMaxDays = (float)(band.tofMax / 86400.0);
            }
            else
            {
                _porkTofMinDays = 1f;
                _porkTofMaxDays = 30f;
            }

            _porkPrograde = shipNow.pos.X * shipNow.vel.Y - shipNow.pos.Y * shipNow.vel.X >= 0;
        }


        void PreviewPorkchop(TransferWindow window, KeplerElements shipKe, KeplerElements targetKe)
        {
            if (_manuverLines.EditingNodes.Length < 2)
            {
                _manuverLines.EditingNodes = new ManuverNode[2];
                _manuverLines.EditingNodes[0] = new ManuverNode(_uiState, _systemId, _entityId, window.Depart);
                _manuverLines.EditingNodes[1] = new ManuverNode(_uiState, _systemId, _entityId, window.Arrive);
            }

            (Vector3 depart, Vector3 arrive) burns = PorkchopBurns(window, shipKe, targetKe);
            _manuverLines.EditingNodes[0].SetNode(burns.depart, window.Depart);
            _manuverLines.EditingNodes[1].PriorOrbit = _manuverLines.EditingNodes[0].TargetOrbit;
            _manuverLines.EditingNodes[1].SetNode(burns.arrive, window.Arrive);
        }

        void CommitPorkchop(TransferWindow window, KeplerElements shipKe, KeplerElements targetKe)
        {
            (Vector3 depart, Vector3 arrive) burns = PorkchopBurns(window, shipKe, targetKe);
            SubmitBurn(burns.depart, window.Depart);
            SubmitBurn(burns.arrive, window.Arrive);

            var sequence = new ManuverSequence();
            sequence.SequenceName = "Transfer window";
            if (_manuverLines.EditingNodes.Length >= 2)
            {
                _manuverLines.EditingNodes[0].NodeName = "Depart";
                _manuverLines.EditingNodes[1].NodeName = "Arrive";
                sequence.ManuverNodes.Add(_manuverLines.EditingNodes[0]);
                sequence.ManuverNodes.Add(_manuverLines.EditingNodes[1]);
            }
            _manuverLines.SelectedSequence.ManuverSequences.Add(sequence);
            _navMode = NavMode.None;
        }

        static (Vector3 depart, Vector3 arrive) PorkchopBurns(TransferWindow window, KeplerElements shipKe, KeplerElements targetKe)
        {
            (Vector3 pos, Vector3 vel) from = PorkchopPanel.KeplerState(shipKe, window.Depart);
            (Vector3 pos, Vector3 vel) to = PorkchopPanel.KeplerState(targetKe, window.Arrive);
            Vector3 depart = OrbitalMath.ToProgradeFrame(window.DepartDeltaV, from.pos, from.vel);
            Vector3 arrivalVelocity = to.vel + window.ArriveDeltaV;
            Vector3 arrive = OrbitalMath.ToProgradeFrame(-window.ArriveDeltaV, to.pos, arrivalVelocity);
            return (depart, arrive);
        }

    }
}