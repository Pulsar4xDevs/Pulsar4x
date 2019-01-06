using System;
using ImGuiNET;
using Pulsar4X.ECSLib;
using System.Numerics;


namespace Pulsar4X.SDL2UI
{
    public class ChangeCurrentOrbitWindow : PulsarGuiWindow// IOrderWindow
    {

        EntityState OrderingEntity;
        OrbitDB _orderEntityOrbit;

        float _maxDV;
        float _progradeDV;
        float _radialDV;

        ECSLib.Vector4 _deltaV_MS;

        DateTime _actionDateTime;

        //double _origionalOrbitalSpeed = double.NaN;
        ECSLib.Vector4 _orbitalVelocityAtChange = ECSLib.Vector4.NaN;
        double _origionalAngle = double.NaN;

        double _newOrbitalSpeed = double.NaN;
        ECSLib.Vector4 _newOrbitalVelocity = ECSLib.Vector4.NaN;
        double _newAngle = double.NaN;

        double _massOrderingEntity = double.NaN;
        double _massParentBody = double.NaN;
        double _stdGravParam = double.NaN;

        ECSLib.Vector4 _positonAtChange_AU;

        KeplerElements _ke;
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
            _state.ActiveSystem.ManagerSubpulses.SystemDateChangedEvent += instance.OnSystemDateTimeChange;

            return instance;
        }

        void OnEntityChange(EntityState entity)
        {
            OrderingEntity = entity;
            _actionDateTime = _state.CurrentSystemDateTime;
            _orderEntityOrbit = entity.Entity.GetDataBlob<OrbitDB>();

            _massParentBody = _orderEntityOrbit.Parent.GetDataBlob<MassVolumeDB>().Mass;
            _massOrderingEntity = OrderingEntity.Entity.GetDataBlob<MassVolumeDB>().Mass;
            _stdGravParam = GameConstants.Science.GravitationalConstant * (_massParentBody + _massOrderingEntity) / 3.347928976e33;

            _positonAtChange_AU = OrbitProcessor.GetPosition_AU(_orderEntityOrbit, _actionDateTime);
            _orbitalVelocityAtChange = OrbitProcessor.GetOrbitalVector(_orderEntityOrbit, _actionDateTime);
            _origionalAngle = Math.Atan2(_orbitalVelocityAtChange.X, _orbitalVelocityAtChange.Y);
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
                        _state.MapRendering.UIWidgets.Add(_orbitWidget);
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
                _positonAtChange_AU = OrbitProcessor.GetPosition_AU(_orderEntityOrbit, _actionDateTime);
                _orbitalVelocityAtChange = OrbitProcessor.GetOrbitalVector(_orderEntityOrbit, _actionDateTime);
                _origionalAngle = Math.Atan2(_orbitalVelocityAtChange.X, _orbitalVelocityAtChange.Y);
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
            _deltaV_MS = new ECSLib.Vector4(x, y, 0, 0);


            _newOrbitalVelocity = _orbitalVelocityAtChange + Distance.MToAU(_deltaV_MS);
            _newOrbitalSpeed = _newOrbitalVelocity.Length();
            var spdmps = Distance.AuToMt(_newOrbitalSpeed);
            _newAngle = Math.Atan2(_newOrbitalVelocity.X, _newOrbitalVelocity.Y);


            var ke = OrbitMath.KeplerFromVelocityAndPosition(_stdGravParam, _positonAtChange_AU, _newOrbitalVelocity);
            _ke = ke;

            _orbitWidget.SetParametersFromKeplerElements(ke, _positonAtChange_AU);

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
            _state.ActiveSystem.ManagerSubpulses.SystemDateChangedEvent -= OnSystemDateTimeChange;

            if (_orbitWidget != null)
            {
                _state.MapRendering.UIWidgets.Remove(_orbitWidget);
                _orbitWidget = null;
            }

        }
    }
}
