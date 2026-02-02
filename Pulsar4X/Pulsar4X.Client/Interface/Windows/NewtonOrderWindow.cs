using System;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Engine;
using Pulsar4X.Orbital;
using Vector2 = Pulsar4X.Orbital.Vector2;
using Vector3 = Pulsar4X.Orbital.Vector3;
using Pulsar4X.Orbits;
using Pulsar4X.Galaxy;
using Pulsar4X.Movement;

namespace Pulsar4X.Client
{
    public class ChangeCurrentOrbitWindow : PulsarGuiWindow
    {
        EntityState OrderingEntity;
        OrbitDB? _orderEntityOrbit;

        float _maxDV;

        Vector3 _deltaV_MS;

        DateTime _actionDateTime;

        Vector3 _orbitalVelocityAtChange_m = Orbital.Vector3.NaN;
        double _originalAngle = double.NaN;

        double _newOrbitalSpeed_m = double.NaN;
        Vector3 _newOrbitalVelocity_m = Orbital.Vector3.NaN;
        double _newAngle = double.NaN;

        double _massOrderingEntity = double.NaN;
        double _massParentBody = double.NaN;
        double _stdGravParam_m = double.NaN;

        Vector3 _positionAtChange_m;

        KeplerElements _ke_m;

        string _displayText;
        string _tooltipText = "";
        OrbitOrderIcon? _orbitWidget;
        private NewtonionOrderUI? _newtonUI;
        private double _eccentricity;
        private double Eccentricity
        {
            get { return _eccentricity; }
            set
            {
                if (_newtonUI != null)
                    _newtonUI.Eccentricity = value;
                _eccentricity = value;
            }
        }

        private ChangeCurrentOrbitWindow(EntityState entityState)
        {
            _flags = ImGuiWindowFlags.AlwaysAutoResize;

            OrderingEntity = entityState;
            OnEntityChange(entityState);

            _displayText = "Change Orbit: " + OrderingEntity.Name;
            _tooltipText = "Expend Dv to change orbit";

            if (OrderingEntity.Entity.TryGetDataBlob<NewtonThrustAbilityDB>(out var propDB))
            {
                _maxDV = (float)propDB.DeltaV;
            }
        }

        internal static ChangeCurrentOrbitWindow GetInstance(EntityState entity)
        {
            if (!_uiState.LoadedWindows.ContainsKey(typeof(ChangeCurrentOrbitWindow)))
            {
                return new ChangeCurrentOrbitWindow(entity);
            }
            var instance = (ChangeCurrentOrbitWindow)_uiState.LoadedWindows[typeof(ChangeCurrentOrbitWindow)];
            if (instance.OrderingEntity != entity || !instance.IsActive)
                instance.OnEntityChange(entity);
            return instance;
        }

        void OnEntityChange(EntityState entity)
        {
            OrderingEntity = entity;
            _actionDateTime = _uiState.PrimarySystemDateTime;

            if (!entity.Entity.TryGetDataBlob<OrbitDB>(out _orderEntityOrbit))
                return;

            if (_orderEntityOrbit.Parent == null)
                throw new NullReferenceException("Orbit parent cannot be null");

            if (!_orderEntityOrbit.Parent.TryGetDataBlob<MassVolumeDB>(out var parentMassDB))
                throw new NullReferenceException("Parent must have MassVolumeDB");

            if (!OrderingEntity.Entity.TryGetDataBlob<MassVolumeDB>(out var entityMassDB))
                throw new NullReferenceException("Entity must have MassVolumeDB");

            if (!entity.Entity.TryGetDataBlob<NewtonThrustAbilityDB>(out var newtonDB))
                throw new NullReferenceException("Entity must have NewtonThrustAbilityDB");

            _massParentBody = parentMassDB.MassDry;
            _massOrderingEntity = entityMassDB.MassDry;
            _stdGravParam_m = GeneralMath.StandardGravitationalParameter(_massOrderingEntity + _massParentBody);

            _positionAtChange_m = _orderEntityOrbit.GetPosition(_actionDateTime);
            var velAtChange2d = OrbitProcessor.GetOrbitalVector(_orderEntityOrbit, _actionDateTime);
            _orbitalVelocityAtChange_m = new Vector3(velAtChange2d.X, velAtChange2d.Y, 0);
            _originalAngle = Math.Atan2(_orbitalVelocityAtChange_m.X, _orbitalVelocityAtChange_m.Y);

            _newtonUI = new NewtonionOrderUI(newtonDB, _massOrderingEntity);

            IsActive = true;
        }

        internal override void Display()
        {
            if (!IsActive)
                return;

            if (Window.Begin(_displayText, ref IsActive, _flags))
            {
                if (_orbitWidget == null && _orderEntityOrbit != null && _orderEntityOrbit.Parent != null)
                {
                    _orbitWidget = new OrbitOrderIcon(_orderEntityOrbit.Parent);
                    _uiState.SelectedSysMapRender.UIWidgets.Add(nameof(OrbitOrderIcon), _orbitWidget);
                }

                if (_newtonUI != null && _newtonUI.Display())
                    Calcs();

                if (ImGui.Button("Action Command"))
                    ActionCmd();
            }
            Window.End();
        }

        public override void OnSystemTickChange(DateTime newDate)
        {
            if (_actionDateTime < newDate && _orderEntityOrbit != null)
            {
                _actionDateTime = newDate;
                _positionAtChange_m = _orderEntityOrbit.GetPosition(_actionDateTime);
                var vector2 = OrbitProcessor.GetOrbitalVector(_orderEntityOrbit, _actionDateTime);
                _orbitalVelocityAtChange_m = new Vector3(vector2.X, vector2.Y, 0);
                _originalAngle = Math.Atan2(_orbitalVelocityAtChange_m.X, _orbitalVelocityAtChange_m.Y);
            }
        }

        void ActionCmd()
        {
            if (!OrderingEntity.Entity.TryGetDataBlob<NewtonThrustAbilityDB>(out var newtonDB))
                return;

            if (!OrderingEntity.Entity.TryGetDataBlob<MassVolumeDB>(out var massDB))
                return;

            double totalMass = massDB.MassTotal;
            double exhaustVelocity = newtonDB.ExhaustVelocity;
            double burnRate = newtonDB.FuelBurnRate;

            double fuelBurned = OrbitMath.TsiolkovskyFuelUse(totalMass, exhaustVelocity, _deltaV_MS.Length());
            double secondsBurn = fuelBurned / burnRate;
            var manuverNodeTime = _actionDateTime + TimeSpan.FromSeconds(secondsBurn * 0.5);

            var order = NewtonThrustCommand.CreateCommand(
                OrderingEntity.Entity.FactionOwnerID,
                OrderingEntity.Entity,
                manuverNodeTime,
                _deltaV_MS,
                secondsBurn);

            _uiState.Game?.OrderHandler.HandleOrder(order);

            CloseWindow();
        }

        void Calcs()
        {
            if (_newtonUI == null || _orbitWidget == null)
                throw new NullReferenceException();

            _deltaV_MS = _newtonUI.DeltaV;

            _newOrbitalVelocity_m = _orbitalVelocityAtChange_m + _deltaV_MS;
            _newOrbitalSpeed_m = _newOrbitalVelocity_m.Length();
            _newAngle = Math.Atan2(_newOrbitalVelocity_m.X, _newOrbitalVelocity_m.Y);

            _ke_m = OrbitMath.KeplerFromPositionAndVelocity(_stdGravParam_m, _positionAtChange_m, _newOrbitalVelocity_m, _actionDateTime);

            _orbitWidget.SetParametersFromKeplerElements(_ke_m, _positionAtChange_m);
        }

        internal void CloseWindow()
        {
            IsActive = false;
            if (_orbitWidget != null)
            {
                _uiState.SelectedSysMapRender.UIWidgets.Remove(nameof(OrbitOrderIcon));
                _orbitWidget = null;
            }
        }
    }


    public class NewtonionOrderUI
    {
        double _fuelToBurn = double.NaN;
        public Vector3 DeltaV { get; set; } = Vector3.Zero;

        float _progradeDV;
        float _radialDV;

        double _maxDV;
        private double _exhaustVelocity = double.NaN;
        private double _fuelRate = double.NaN;
        private double _currentMass;

        public double DepartureAngle { get; set; }
        public double Eccentricity { get; set; }

        public NewtonionOrderUI(NewtonThrustAbilityDB newtonAbility, double currentMass)
        {
            _exhaustVelocity = newtonAbility.ExhaustVelocity;
            _fuelRate = newtonAbility.FuelBurnRate;
            _maxDV = newtonAbility.DeltaV;
            _currentMass = currentMass;
        }

        public bool Display()
        {
            bool changes = false;
            float maxprogradeDV = (float)(_maxDV - Math.Abs(_radialDV));
            float maxradialDV = (float)(_maxDV - Math.Abs(_progradeDV));

            if (ImGui.SliderFloat("Prograde DV", ref _progradeDV, -maxprogradeDV, maxprogradeDV))
            {
                Calcs();
                changes = true;
            }
            if (ImGui.SliderFloat("Radial DV", ref _radialDV, -maxradialDV, maxradialDV))
            {
                Calcs();
                changes = true;
            }

            ImGui.Text("Fuel to burn:" + Stringify.Mass(_fuelToBurn));
            ImGui.Text("Burn time: " + (int)(_fuelToBurn / _fuelRate) + " s");
            ImGui.Text("DeltaV: " + Stringify.Distance(DeltaV.Length()) + "/s of " + Stringify.Distance(_maxDV) + "/s");
            ImGui.Text("Eccentricity: " + Eccentricity.ToString("g3"));
            return changes;
        }

        private void Calcs()
        {
            var rmtx = Matrix.IDRotate(DepartureAngle);
            Vector2 dv = rmtx.TransformD(_radialDV, _progradeDV);
            DeltaV = new Vector3(dv.X, dv.Y, 0);
            _fuelToBurn = OrbitMath.TsiolkovskyFuelUse(_currentMass, _exhaustVelocity, DeltaV.Length());
        }
    }

    public class NewtonionRadialOrderUI
    {
        double _fuelToBurn = double.NaN;

        public Vector3 DeltaV { get; private set; } = Vector3.Zero;

        float _progradeDV;
        float _radialDV;

        double _maxDV;
        private double _exhaustVelocity = double.NaN;
        private double _fuelRate = double.NaN;
        private double _currentMass;

        private float _minRad;
        private float _rad;
        public float Radius
        {
            get { return _rad; }
            set { _rad = value; }
        }
        private float _maxRad;
        private Vector2 _vector = new Vector2(0, 1);

        public double ProgradeAngle { get; set; }

        private float _eccentricity;
        public float Eccentricity
        {
            get => _eccentricity;
            set => _eccentricity = value;
        }

        public NewtonionRadialOrderUI(NewtonThrustAbilityDB newtonAbility, double currentMass, float minRad, float maxRad)
        {
            _exhaustVelocity = newtonAbility.ExhaustVelocity;
            _fuelRate = newtonAbility.FuelBurnRate;
            _maxDV = newtonAbility.DeltaV;
            _currentMass = currentMass;
            _minRad = minRad;
            _maxRad = maxRad;
            _rad = _minRad;
        }

        public bool Display()
        {
            bool changes = false;
            float maxprogradeDV = (float)(_maxDV - Math.Abs(_radialDV));

            if (ImGui.SliderFloat("Prograde DV", ref _progradeDV, -maxprogradeDV, maxprogradeDV))
            {
                Calcs();
                changes = true;
            }
            if (ImGui.SliderFloat("Radius", ref _rad, _minRad, _maxRad))
            {
                Calcs();
                changes = true;
            }

            ImGui.Text("Burn time: " + (int)(_fuelToBurn / _fuelRate) + " s");
            if (DeltaV.Length() > _maxDV)
                ImGui.TextColored(new System.Numerics.Vector4(0.9f, 0, 0, 1), "DeltaV: " + Stringify.Distance(DeltaV.Length()) + "/s of " + Stringify.Distance(_maxDV) + "/s");
            else
                ImGui.Text("DeltaV: " + Stringify.Distance(DeltaV.Length()) + "/s of " + Stringify.Distance(_maxDV) + "/s");
            ImGui.Text("Eccentricity: " + Eccentricity.ToString("g3"));
            return changes;
        }

        public void SetDeltaV(Vector3 deltaV)
        {
            DeltaV = deltaV;
            var rmtx = Matrix.IDRotate(-ProgradeAngle);
            Vector2 dv = rmtx.TransformD(deltaV.Y, deltaV.X);
            _radialDV = (float)dv.X;
            _progradeDV = (float)dv.Y;
            _fuelToBurn = OrbitMath.TsiolkovskyFuelUse(_currentMass, _exhaustVelocity, DeltaV.Length());
        }

        private void Calcs()
        {
            var rmtx = Matrix.IDRotate(-ProgradeAngle);
            Vector2 dv = rmtx.TransformD(_progradeDV, _radialDV);
            DeltaV = new Vector3(dv.X, dv.Y, 0);
            _fuelToBurn = OrbitMath.TsiolkovskyFuelUse(_currentMass, _exhaustVelocity, DeltaV.Length());
        }
    }
}
