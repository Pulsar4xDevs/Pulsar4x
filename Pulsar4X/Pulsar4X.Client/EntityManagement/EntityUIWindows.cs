using System;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Orbital;
using Pulsar4X.ImGuiNetUI;
using Pulsar4X.SDL2UI;
using Pulsar4X.ImGuiNetUI.EntityManagement;


namespace Pulsar4X.SDL2UI
{
    //a do nothing helper class that is plugged into generics for static checks
    public class PinCameraBlankMenuHelper : PulsarGuiWindow
    {
        internal override void Display()
        {
        }
    }

    //a do nothing helper class that is plugged into generics for static checks
    public class GotoSystemBlankMenuHelper : PulsarGuiWindow
    {
        internal override void Display()
        {

        }
    }


    //a do nothing helper class that is plugged into generics for static checks
    public class SelectPrimaryBlankMenuHelper : PulsarGuiWindow
    {
        internal override void Display()
        {

        }
    }

    public class JumpThroughJumpPointBlankMenuHelper : PulsarGuiWindow
    {
        internal override void Display()
        {

        }
    }

    //has all initialization rutines for common entity management related UI windows, also has a function that checks if a window can be opened for a given EntityState
    public class EntityUIWindows
    {
        //checks if given menu can be opened for given entity
        [PublicAPI]
        internal static bool CheckIfCanOpenWindow(Type T, EntityState _entityState, GlobalUIState _state)
        {


            if(T == typeof(JumpThroughJumpPointBlankMenuHelper))
            {
                if(CheckIfCanOpenWindow(typeof(GotoSystemBlankMenuHelper), _entityState, _state))
                {
                    if(_state.PrimaryEntity != null)
                    {
                        if(_state.PrimaryEntity.BodyType == UserOrbitSettings.OrbitBodyType.Ship && (Orbital.Distance.DistanceBetween(_state.PrimaryEntity.Position.AbsolutePosition, _entityState.Position.AbsolutePosition) < _entityState.Entity.GetDataBlob<JPSurveyableDB>().MinimumDistanceToJump_m))
                        {
                            return true;
                        }
                        else { return false; }
                    }
                    else { return false; }
                }
                else { return false; }
            }
            else { return false; }
        }

        internal static bool CheckIfCanOpenWindow(Type T, EntityState _entityState)
        {
            //Checks if the power gen menu can be opened
            if (_entityState.Entity.HasDataBlob<EnergyGenAbilityDB>() && T == typeof(PowerGen))
            {
                return true;
            }
            //Check if the pin menu can be opened
            else if (T == typeof(PinCameraBlankMenuHelper))
            {
                return true;
            }
            //if can be used to go to another system
            else if (_entityState.Entity.HasDataBlob<JPSurveyableDB>() && T == typeof(GotoSystemBlankMenuHelper))
            {
                if (_entityState.Entity.GetDataBlob<JPSurveyableDB>().JumpPointTo != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            //if can be selected as primary
            else if (T == typeof(SelectPrimaryBlankMenuHelper))
            {
                return true;
            }
            //if entity can warp
            else if (_entityState.Entity.HasDataBlob<WarpAbilityDB>() && T == typeof(WarpOrderWindow))
            {
                return true;
            }
            //if entity can move
            else if (_entityState.Entity.HasDataBlob<NewtonThrustAbilityDB>() && T == typeof(ChangeCurrentOrbitWindow))
            {
                return true;
            }
            else if (_entityState.Entity.HasDataBlob<NewtonThrustAbilityDB>() && T == typeof(NavWindow))
            {
                return true;
            }
            //if entity can fire?
            else if (_entityState.Entity.HasDataBlob<FireControlAbilityDB>() && T == typeof(FireControl))
            {
                return true;
            }
            //if entity can be renamed?
            else if (T == typeof(RenameWindow))
            {
                return true;
            }
            //if entity can target
            else if (_entityState.Entity.HasDataBlob<VolumeStorageDB>() && T == typeof(CargoTransfer))
            {
                return true;
            }
            //if entity can mine || refine || build
            else if (_entityState.Entity.HasDataBlob<ColonyInfoDB>() && T == typeof(ColonyPanel))
            {
                return true;
            }
            else if (_entityState.BodyType != UserOrbitSettings.OrbitBodyType.Ship && T == typeof(PlanetaryWindow))
            {
                return true;
            }
            // if entity can be given orders
            else if (_entityState.Entity.HasDataBlob<OrderableDB>() && T == typeof(OrdersListUI))
            {
                return true;
            }
            else if (
                _entityState.Entity.HasDataBlob<VolumeStorageDB>() &&
                _entityState.Entity.HasDataBlob<NewtonThrustAbilityDB>() &&
                T == typeof(LogiShipWindow))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // use type PinCameraBlankMenuHelper to pin camara, should use checkIfCanOpenWindow with type before trying to open a given window
        //type parameter is the type of window opened, first parameter indicates wether the window should be opened, second parameter is EntityState for the entity using the window
        //(or window using the entity?) third is the GlobalUIState and fourth indicates wether this function should manage closing preopened pop-ups(mostly utility for EntityContextMenu class[should be set to true when this is used in it])
        [PublicAPI]
        internal static void OpenUIWindow(Type T, EntityState _entityState , GlobalUIState _state , bool open = true, bool managesUIPopUps = false)
        {
            if (open)
            {
                //If the user has requested a menu be opened and if
                //Menu is pin menu
                if (T == typeof(PinCameraBlankMenuHelper))
                {
                    _state.Camera.PinToEntity(_entityState.Entity);
                    if (managesUIPopUps)
                    {
                        ImGui.CloseCurrentPopup();
                    }
                }
                //Menu is goto system menu
                else if (T == typeof(GotoSystemBlankMenuHelper))
                {
                    _state.SetActiveSystem(_entityState.Entity.GetDataBlob<JPSurveyableDB>().JumpPointTo.GetDataBlob<PositionDB>().SystemGuid);
                }
                else if (T == typeof(SelectPrimaryBlankMenuHelper))
                {
                    _state.EntitySelectedAsPrimary(_entityState.Entity.Id, _entityState.StarSysGuid);
                }
                //if entity can warp
                else if (T == typeof(WarpOrderWindow))
                {
                    WarpOrderWindow.GetInstance(_entityState).ToggleActive();
                    _state.ActiveWindow = WarpOrderWindow.GetInstance(_entityState);
                }
                //Menu is change orbit menu
                else if (T == typeof(ChangeCurrentOrbitWindow))
                {
                    ChangeCurrentOrbitWindow.GetInstance(_entityState).ToggleActive();
                    _state.ActiveWindow = ChangeCurrentOrbitWindow.GetInstance(_entityState);
                }
                //Menu is ficrecontrol menu
                else if (T == typeof(FireControl))
                {
                    var instance = FireControl.GetInstance(_entityState);
                    //instance.SetOrderEntity(_entityState);
                    instance.ToggleActive();
                    _state.ActiveWindow = instance;
                }
                //Menu is rename menu
                else if (T == typeof(RenameWindow))
                {
                    // RenameWindow.GetInstance(_entityState).ToggleActive();
                    // _state.ActiveWindow = RenameWindow.GetInstance(_entityState);
                    // if (managesUIPopUps)
                    // {
                    //     ImGui.CloseCurrentPopup();
                    // }

                }
                //Menu is cargo menu
                else if (T == typeof(CargoTransfer))
                {
                    var instance = CargoTransfer.GetInstance(_state.Faction.GetDataBlob<FactionInfoDB>().Data, _entityState);
                    instance.ToggleActive();
                    _state.ActiveWindow = instance;
                }
                // else if (T == typeof(LogiBaseWindow))
                // {
                //     var instance = LogiBaseWindow.GetInstance(_state.Game.StaticData, _entityState);
                //     instance.ToggleActive();
                //     _state.ActiveWindow = instance;
                // }
                else if (T == typeof(LogiShipWindow))
                {
                    var instance = LogiShipWindow.GetInstance(_state.Faction.GetDataBlob<FactionInfoDB>().Data, _entityState);
                    instance.ToggleActive();
                    _state.ActiveWindow = instance;
                }
                //Menu is econ menu
                else if (T == typeof(ColonyPanel))
                {
                    var instance = ColonyPanel.GetInstance(_state.Faction.GetDataBlob<FactionInfoDB>().Data, _entityState);
                    instance.ToggleActive();
                    _state.ActiveWindow = instance;
                }
                else if (T == typeof(NavWindow))
                {
                    var instance = NavWindow.GetInstance(_entityState);
                    instance.ToggleActive();
                    _state.ActiveWindow = instance;
                }
                else if (T == typeof(OrderCreationUI))
                {
                    var instance = OrderCreationUI.GetInstance();
                    instance.ToggleActive();
                    _state.ActiveWindow = instance;
                }
                else if (T == typeof(PlanetaryWindow))
                {
                    var instance = PlanetaryWindow.GetInstance(_entityState, _state);
                    instance.ToggleActive();
                }
                else if (T == typeof(OrdersListUI))
                {
                    var instance = OrdersListUI.GetInstance(_entityState, _state);
                    instance.ToggleActive();
                }

                //TODO: implement this(moving a ship entity[_uiState.PrimaryEntity] from one system to another one and placing it at a given location[_entityState.Entity.GetDataBlob<JPSurveyableDB>().JumpPointTo.GetDataBlob<PositionDB>(). etc...])
                if (T == typeof(JumpThroughJumpPointBlankMenuHelper))
                {

                }
            }
        }

        public static bool CheckOpenUIWindow(Type T, EntityState _entityState, GlobalUIState _state)
        {
            //If the user has requested a menu be opened and if
            bool returnval;

            // Global Windows
            if (T == typeof(OrderCreationUI)) returnval = OrderCreationUI.GetInstance().GetActive();
            else if (T == typeof(WarpOrderWindow)) returnval = WarpOrderWindow.GetInstance(_entityState).GetActive();
            else if (T == typeof(ChangeCurrentOrbitWindow)) returnval = ChangeCurrentOrbitWindow.GetInstance(_entityState).GetActive();
            else if (T == typeof(FireControl)) returnval = FireControl.GetInstance(_entityState).GetActive();
            //else if (T == typeof(RenameWindow)) returnval = RenameWindow.GetInstance(_entityState).GetActive();
            else if (T == typeof(NavWindow)) returnval = NavWindow.GetInstance(_entityState).GetActive();
            else if (T == typeof(CargoTransfer)) returnval = CargoTransfer.GetInstance(_state.Faction.GetDataBlob<FactionInfoDB>().Data, _entityState).GetActive();
            else if (T == typeof(ColonyPanel)) returnval = ColonyPanel.GetInstance(_state.Faction.GetDataBlob<FactionInfoDB>().Data, _entityState).GetActive();
            // Instance Windows
            else if (T == typeof(OrdersListUI)) returnval = OrdersListUI.GetInstance(_entityState, _state).GetActive();
            else returnval = false;
            return returnval;


        }
    }

}
