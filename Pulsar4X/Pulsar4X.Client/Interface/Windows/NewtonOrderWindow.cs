using System;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Orbital;
using Vector2 = Pulsar4X.Orbital.Vector2;
using Vector3 = Pulsar4X.Orbital.Vector3;

namespace Pulsar4X.Client
{
    public class ChangeCurrentOrbitWindow : UniquePulsarGuiWindow<ChangeCurrentOrbitWindow>
    {
        int _entityId;
        string _systemId = "";

        DateTime _actionDateTime;
        Vector3 _deltaV_MS;
        KeplerElements _ke_m;
        OrbitOrderIcon? _orbitWidget;
        NewtonionOrderUI _newtonUI = new();
        bool _recalc;

        private ChangeCurrentOrbitWindow(int entityId, string systemId)
        {
            _flags = ImGuiWindowFlags.AlwaysAutoResize;
            OnEntityChange(entityId, systemId);
        }

        internal static ChangeCurrentOrbitWindow GetInstance(EntityState entity)
        {
            if(!_uiState.TryGetUniqueWindow<ChangeCurrentOrbitWindow>(out var window))
            {
                window = _uiState.AddUniqueWindow(new ChangeCurrentOrbitWindow(entity.Id, entity.StarSystemId!));
                return window; // Entity is already set from ctor.
            }

            if (window._entityId != entity.Id || !window.IsActive)
            {
                window.OnEntityChange(entity.Id, entity.StarSystemId!);
            }
            return window;
        }

        void OnEntityChange(int entityId, string systemId)
        {
            _entityId = entityId;
            _systemId = systemId;
            _actionDateTime = _uiState.PrimarySystemDateTime;
            _newtonUI = new NewtonionOrderUI();
            _deltaV_MS = Vector3.Zero;
            RemoveWidget();
            IsActive = true;
        }

        internal override void Display()
        {
            if (!IsActive)
                return;

            var system = _uiState.GameClient?.Galaxy.GetSystem(_systemId);
            var entity = system?.GetEntity(_entityId);
            var orbit = entity?.GetView<OrbitView>();
            var thrust = entity?.GetView<ThrustView>();
            var massVolume = entity?.GetView<MassVolumeView>();
            if (system == null || entity == null || orbit == null || thrust == null || massVolume == null)
            {
                CloseWindow();
                return;
            }

            string entityName = entity.GetView<NameView>()?.Name ?? "Unknown";
            if (Window.Begin("Change Orbit: " + entityName, ref IsActive, _flags))
            {
                if (_orbitWidget == null && orbit.ParentId is int parentId
                    && system.GetEntity(parentId) is { } parent)
                {
                    double soi = parent.SoiRadiusM();
                    _orbitWidget = new OrbitOrderIcon(
                        new SnapshotPosition(_uiState, _systemId, parentId),
                        double.IsInfinity(soi) ? 0 : soi,
                        parent.GetView<MassVolumeView>()?.RadiusMetres ?? 0);
                    _uiState.SelectedSysMapRender?.UIWidgets.Add(nameof(OrbitOrderIcon), _orbitWidget);
                }

                if (_newtonUI.Display(thrust.DeltaVMps, thrust.ExhaustVelocityMps, thrust.FuelBurnRateKgPerSec, massVolume.MassKg) || _recalc)
                    Calcs(orbit);

                if (ImGui.Button("Action Command"))
                    ActionCmd(thrust, massVolume);
            }
            Window.End();
        }

        public override void OnSystemTickChange(DateTime newDate)
        {
            if (_actionDateTime < newDate)
            {
                _actionDateTime = newDate;
                _recalc = true;
            }
        }

        void ActionCmd(ThrustView thrust, MassVolumeView massVolume)
        {
            double dvLen = _deltaV_MS.Length();
            if (dvLen <= 0)
                return;

            double fuelBurned = OrbitalMath.TsiolkovskyFuelUse(massVolume.MassKg, thrust.ExhaustVelocityMps, dvLen);
            double secondsBurn = thrust.FuelBurnRateKgPerSec > 0 ? fuelBurned / thrust.FuelBurnRateKgPerSec : 0;
            var manuverNodeTime = _actionDateTime + TimeSpan.FromSeconds(secondsBurn * 0.5);

            _uiState.GameClient?.SubmitCommandAsync(new Pulsar4X.Api.NewtonThrustCommand(
                _entityId, manuverNodeTime, new Vec3(_deltaV_MS.X, _deltaV_MS.Y, _deltaV_MS.Z)));

            CloseWindow();
        }

        void Calcs(OrbitView orbit)
        {
            _recalc = false;
            if (_orbitWidget == null)
                return;

            _deltaV_MS = _newtonUI.DeltaV;

            var stateAtChange = OrbitalMath.GetStateVectors(orbit.ToKeplerElements(), _actionDateTime);
            var positionAtChange = stateAtChange.position;
            var velocityAtChange = new Vector3(stateAtChange.velocity.X, stateAtChange.velocity.Y, 0);
            var newOrbitalVelocity = velocityAtChange + _deltaV_MS;

            _ke_m = OrbitalMath.KeplerFromPositionAndVelocity(orbit.StandardGravParameter, positionAtChange, newOrbitalVelocity, _actionDateTime);

            _newtonUI.Eccentricity = _ke_m.Eccentricity;
            _orbitWidget.SetParametersFromKeplerElements(_ke_m, positionAtChange);
        }

        void RemoveWidget()
        {
            if (_orbitWidget != null)
            {
                _uiState.SelectedSysMapRender?.UIWidgets.Remove(nameof(OrbitOrderIcon));
                _orbitWidget = null;
            }
        }

        internal void CloseWindow()
        {
            IsActive = false;
            RemoveWidget();
        }
    }


    public class NewtonionOrderUI
    {
        double _fuelToBurn = double.NaN;
        public Vector3 DeltaV { get; set; } = Vector3.Zero;

        float _progradeDV;
        float _radialDV;

        public double DepartureAngle { get; set; }
        public double Eccentricity { get; set; }

        public bool Display(double maxDV, double exhaustVelocity, double fuelRate, double currentMass)
        {
            bool changes = false;
            float maxprogradeDV = (float)(maxDV - Math.Abs(_radialDV));
            float maxradialDV = (float)(maxDV - Math.Abs(_progradeDV));

            if (ImGui.SliderFloat("Prograde DV", ref _progradeDV, -maxprogradeDV, maxprogradeDV))
            {
                Calcs(exhaustVelocity, currentMass);
                changes = true;
            }
            if (ImGui.SliderFloat("Radial DV", ref _radialDV, -maxradialDV, maxradialDV))
            {
                Calcs(exhaustVelocity, currentMass);
                changes = true;
            }

            ImGui.Text("Fuel to burn:" + Stringify.Mass(_fuelToBurn));
            ImGui.Text("Burn time: " + (int)(fuelRate > 0 ? _fuelToBurn / fuelRate : 0) + " s");
            ImGui.Text("DeltaV: " + Stringify.Distance(DeltaV.Length()) + "/s of " + Stringify.Distance(maxDV) + "/s");
            ImGui.Text("Eccentricity: " + Eccentricity.ToString("g3"));
            return changes;
        }

        private void Calcs(double exhaustVelocity, double currentMass)
        {
            var rmtx = Matrix.IDRotate(DepartureAngle);
            Vector2 dv = rmtx.TransformD(_radialDV, _progradeDV);
            DeltaV = new Vector3(dv.X, dv.Y, 0);
            _fuelToBurn = OrbitalMath.TsiolkovskyFuelUse(currentMass, exhaustVelocity, DeltaV.Length());
        }
    }

    public class NewtonionRadialOrderUI
    {
        double _fuelToBurn = double.NaN;

        public Vector3 DeltaV { get; private set; } = Vector3.Zero;

        float _progradeDV;
        float _radialDV;

        private float _minRad;
        private float _rad;
        public float Radius
        {
            get { return _rad; }
            set { _rad = value; }
        }
        private float _maxRad;

        public double ProgradeAngle { get; set; }

        private float _eccentricity;
        public float Eccentricity
        {
            get => _eccentricity;
            set => _eccentricity = value;
        }

        public NewtonionRadialOrderUI(float minRad, float maxRad)
        {
            _minRad = minRad;
            _maxRad = maxRad;
            _rad = _minRad;
        }

        public bool Display(double maxDV, double exhaustVelocity, double fuelRate, double currentMass)
        {
            bool changes = false;
            float maxprogradeDV = (float)(maxDV - Math.Abs(_radialDV));

            if (ImGui.SliderFloat("Prograde DV", ref _progradeDV, -maxprogradeDV, maxprogradeDV))
            {
                Calcs(exhaustVelocity, currentMass);
                changes = true;
            }
            if (ImGui.SliderFloat("Radius", ref _rad, _minRad, _maxRad))
            {
                Calcs(exhaustVelocity, currentMass);
                changes = true;
            }

            ImGui.Text("Burn time: " + (int)(fuelRate > 0 ? _fuelToBurn / fuelRate : 0) + " s");
            if (DeltaV.Length() > maxDV)
                ImGui.TextColored(new System.Numerics.Vector4(0.9f, 0, 0, 1), "DeltaV: " + Stringify.Distance(DeltaV.Length()) + "/s of " + Stringify.Distance(maxDV) + "/s");
            else
                ImGui.Text("DeltaV: " + Stringify.Distance(DeltaV.Length()) + "/s of " + Stringify.Distance(maxDV) + "/s");
            ImGui.Text("Eccentricity: " + Eccentricity.ToString("g3"));
            return changes;
        }

        public void SetDeltaV(Vector3 deltaV, double exhaustVelocity, double currentMass)
        {
            DeltaV = deltaV;
            var rmtx = Matrix.IDRotate(-ProgradeAngle);
            Vector2 dv = rmtx.TransformD(deltaV.Y, deltaV.X);
            _radialDV = (float)dv.X;
            _progradeDV = (float)dv.Y;
            _fuelToBurn = OrbitalMath.TsiolkovskyFuelUse(currentMass, exhaustVelocity, DeltaV.Length());
        }

        private void Calcs(double exhaustVelocity, double currentMass)
        {
            var rmtx = Matrix.IDRotate(-ProgradeAngle);
            Vector2 dv = rmtx.TransformD(_progradeDV, _radialDV);
            DeltaV = new Vector3(dv.X, dv.Y, 0);
            _fuelToBurn = OrbitalMath.TsiolkovskyFuelUse(currentMass, exhaustVelocity, DeltaV.Length());
        }
    }
}
