using System;
using ImGuiNET;
using Pulsar4X.ECSLib;
using Pulsar4X.Vectors;
using Vector2 = Pulsar4X.Vectors.Vector2;

//using System.Numerics;


namespace Pulsar4X.SDL2UI
{
    public class ChangeCurrentOrbitWindow : PulsarGuiWindow// IOrderWindow
    {

        EntityState OrderingEntity;
        OrbitDB _orderEntityOrbit;

        float _maxDV;
        float _progradeDV;
        float _radialDV;

        ECSLib.Vector3 _deltaV_MS;

        DateTime _actionDateTime;

        //double _origionalOrbitalSpeed = double.NaN;
        ECSLib.Vector3 _orbitalVelocityAtChange_m = ECSLib.Vector3.NaN;
        double _origionalAngle = double.NaN;

        double _newOrbitalSpeed_m = double.NaN;
        ECSLib.Vector3 _newOrbitalVelocity_m = ECSLib.Vector3.NaN;
        double _newAngle = double.NaN;

        double _massOrderingEntity = double.NaN;
        double _massParentBody = double.NaN;
        double _stdGravParam_m = double.NaN;

        ECSLib.Vector3 _positonAtChange_m;

        KeplerElements _ke_m;
        //double _apoapsisKm;
        //double _periapsisKM;
        //double _targetRadiusAU;
        //double _targetRadiusKM;
        //double _peAlt { get { return _periapsisKM - _targetRadiusKM; } }
        //double _apAlt { get { return _apoapsisKm - _targetRadiusKM; } }

        //double _apMax;
        //double _peMin { get { return _targetRadiusKM; } }

        //double _eccentricity = double.NaN;

        string _displayText;
        string _tooltipText = "";
        OrbitOrderWiget _orbitWidget;


        private ChangeCurrentOrbitWindow(EntityState entity)
        {


            OnEntityChange(entity);

            _displayText = "Change Orbit: " + OrderingEntity.Name;
            _tooltipText = "Expend Dv to change orbit";
            //CurrentState = States.NeedsTarget;


            if(OrderingEntity.Entity.HasDataBlob<PropulsionAbilityDB>())
            {
                var propDB = OrderingEntity.Entity.GetDataBlob<PropulsionAbilityDB>();
                _maxDV = propDB.RemainingDV_MS;
            }
            IsActive = true;
        }

        internal static ChangeCurrentOrbitWindow GetInstance(EntityState entity)
        {
            if (!_state.LoadedWindows.ContainsKey(typeof(ChangeCurrentOrbitWindow)))
            {
                return new ChangeCurrentOrbitWindow(entity);
            }
            var instance = (ChangeCurrentOrbitWindow)_state.LoadedWindows[typeof(ChangeCurrentOrbitWindow)];
            if(instance.OrderingEntity != entity)
                instance.OnEntityChange(entity);
            _state.SelectedSystem.ManagerSubpulses.SystemDateChangedEvent += instance.OnSystemDateTimeChange;

            return instance;
        }

        void OnEntityChange(EntityState entity)
        {
            OrderingEntity = entity;
            _actionDateTime = _state.PrimarySystemDateTime;
            _orderEntityOrbit = entity.Entity.GetDataBlob<OrbitDB>();

            _massParentBody = _orderEntityOrbit.Parent.GetDataBlob<MassVolumeDB>().Mass;
            _massOrderingEntity = OrderingEntity.Entity.GetDataBlob<MassVolumeDB>().Mass;
            _stdGravParam_m = OrbitMath.CalculateStandardGravityParameterInM3S2(_massOrderingEntity, _massParentBody);

            _positonAtChange_m = OrbitProcessor.GetPosition_m(_orderEntityOrbit, _actionDateTime);
            var velAtChange2d = OrbitProcessor.GetOrbitalVector_m(_orderEntityOrbit, _actionDateTime);
            _orbitalVelocityAtChange_m = new Vector3(velAtChange2d.X, velAtChange2d.Y, 0);
            _origionalAngle = Math.Atan2(_orbitalVelocityAtChange_m.X, _orbitalVelocityAtChange_m.Y);
            IsActive = true;
        }

        internal override void Display()
        {
            if (IsActive) 
            { 
                if (ImGui.Begin(_displayText, ref IsActive, _flags))
                {
                    //put calcs that needs refreshing each frame in here. (ie calculations from mouse cursor position)
                    if (_orbitWidget == null)
                    {
                        _orbitWidget = new OrbitOrderWiget(_orderEntityOrbit.Parent);
                        _state.SelectedSysMapRender.UIWidgets.Add(nameof(OrbitOrderWiget), _orbitWidget);
                    }

                    var maxprogradeDV = _maxDV - Math.Abs(_radialDV);
                    var maxradialDV = _maxDV - Math.Abs(_progradeDV);
                    if (ImGui.SliderFloat("Prograde DV", ref _progradeDV, -maxprogradeDV, maxprogradeDV))
                    {
                        Calcs();
                    }
                    if (ImGui.SliderFloat("Radial DV", ref _radialDV, -maxradialDV, maxradialDV))
                    {
                        Calcs();
                    }

                    if (ImGui.Button("Action Command"))
                        ActionCmd();


                    //ImGui.SetTooltip(_tooltipText);
                }
            }
        }


        void OnSystemDateTimeChange(DateTime newDate)
        {

            if (_actionDateTime < newDate)
            { 
                _actionDateTime = newDate;
                _positonAtChange_m = OrbitProcessor.GetPosition_m(_orderEntityOrbit, _actionDateTime);
                var vector2 = OrbitProcessor.GetOrbitalVector_m(_orderEntityOrbit, _actionDateTime);
                _orbitalVelocityAtChange_m = new Vector3(vector2.X, vector2.Y,0);
                _origionalAngle = Math.Atan2(_orbitalVelocityAtChange_m.X, _orbitalVelocityAtChange_m.Y);
            }
        }

        void ActionCmd()
        {

            ChangeCurrentOrbitCommand.CreateCommand(
                _state.Game,
                _state.Faction,
                OrderingEntity.Entity,
                _actionDateTime,
                Distance.MToAU(_deltaV_MS));

            CloseWindow();
        }

        void Calcs()
        {
       
            double x = (_radialDV * Math.Cos(_origionalAngle)) - (_progradeDV * Math.Sin(_origionalAngle));
            double y = (_radialDV * Math.Sin(_origionalAngle)) + (_progradeDV * Math.Cos(_origionalAngle));
            _deltaV_MS = new ECSLib.Vector3(x, y, 0);


            _newOrbitalVelocity_m = _orbitalVelocityAtChange_m + _deltaV_MS;
            _newOrbitalSpeed_m = _newOrbitalVelocity_m.Length();
            
            _newAngle = Math.Atan2(_newOrbitalVelocity_m.X, _newOrbitalVelocity_m.Y);


            _ke_m = OrbitMath.KeplerFromPositionAndVelocity(_stdGravParam_m, _positonAtChange_m, _newOrbitalVelocity_m, _actionDateTime);
             

            _orbitWidget.SetParametersFromKeplerElements(_ke_m, _positonAtChange_m);

            /*
            var sgpCBAU = GameConstants.Science.GravitationalConstant * (_massCurrentBody + _massOrderingEntity) / 3.347928976e33;// (149597870700 * 149597870700 * 149597870700);
            var ralPosCBAU = OrderingEntity.Entity.GetDataBlob<PositionDB>().RelativePosition_AU;
            var smaCurrOrbtAU = OrderingEntity.Entity.GetDataBlob<OrbitDB>().SemiMajorAxis;
            var velAU = OrbitProcessor.PreciseOrbitalVector(sgpCBAU, ralPosCBAU, smaCurrOrbtAU);
            */
        }

        internal void CloseWindow()
        {
            IsActive = false;
            _state.SelectedSystem.ManagerSubpulses.SystemDateChangedEvent -= OnSystemDateTimeChange;

            if (_orbitWidget != null)
            {
                _state.SelectedSysMapRender.UIWidgets.Remove(nameof(OrbitOrderWiget));
                _orbitWidget = null;
            }

        }
    }
}
