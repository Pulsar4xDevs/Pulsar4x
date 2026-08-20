using System;
using ImGuiNET;
using Pulsar4X.Api;

namespace Pulsar4X.Client
{
    //a do nothing helper class that is plugged into generics for static checks
    public class PinCameraBlankMenuHelper : UniquePulsarGuiWindow<PinCameraBlankMenuHelper>
    {
        internal override void Display()
        {
        }
    }

    //a do nothing helper class that is plugged into generics for static checks
    public class GotoSystemBlankMenuHelper : UniquePulsarGuiWindow<GotoSystemBlankMenuHelper>
    {
        internal override void Display()
        {

        }
    }


    //a do nothing helper class that is plugged into generics for static checks
    public class SelectPrimaryBlankMenuHelper : UniquePulsarGuiWindow<SelectPrimaryBlankMenuHelper>
    {
        internal override void Display()
        {

        }
    }

    //has all initialization rutines for common entity management related UI windows, also has a function that checks if a window can be opened for a given EntityState
    public static class EntityUIWindows
    {
        /// <summary>The clicked entity's faction-scoped snapshot. The views the server projected
        /// for this faction gate which actions make sense (and visibility/ownership rules are
        /// thereby enforced at the boundary, not here).</summary>
        private static EntitySnapshot? Resolve(EntityState entityState, GlobalUIState state)
            => entityState.StarSystemId is { } systemId
                ? state.GameClient?.Galaxy.GetSystem(systemId)?.GetEntity(entityState.Id)
                : null;

        //checks if given menu can be opened for given entity
        internal static bool CheckIfCanOpenWindow(Type T, EntityState entityState, GlobalUIState state)
        {
            // Always-available actions that don't read the entity.
            if (T == typeof(PinCameraBlankMenuHelper) || T == typeof(SelectPrimaryBlankMenuHelper)
                || T == typeof(RenameWindow))
            {
                return true;
            }

            var snapshot = Resolve(entityState, state);
            if (snapshot == null)
                return false;

            if (T == typeof(PowerGenWindow))
                return snapshot.HasView<EnergyView>();
            if (T == typeof(GotoSystemBlankMenuHelper))
                return snapshot.GetView<GravSurveyView>()?.JumpPointToSystemId != null;
            if (T == typeof(WarpOrderWindow))
                return snapshot.HasView<WarpAbilityView>();
            if (T == typeof(ChangeCurrentOrbitWindow) || T == typeof(NavWindow))
                return snapshot.HasView<ThrustView>();
            if (T == typeof(FireControl))
                return snapshot.HasView<FireControlView>();
            if (T == typeof(CreateTransferWindow))
                return snapshot.HasView<CargoStorageView>();
            if (T == typeof(OrdersListWindow))
                return snapshot.HasView<OrdersView>();

            return false;
        }

        // use type PinCameraBlankMenuHelper to pin camara, should use checkIfCanOpenWindow with type before trying to open a given window
        //type parameter is the type of window opened, first parameter indicates wether the window should be opened, second parameter is EntityState for the entity using the window
        //(or window using the entity?) third is the GlobalUIState and fourth indicates wether this function should manage closing preopened pop-ups(mostly utility for EntityContextMenu class[should be set to true when this is used in it])
        internal static void OpenUIWindow(Type T, EntityState _entityState , GlobalUIState _state , bool open = true, bool managesUIPopUps = false)
        {
            if (open)
            {
                //If the user has requested a menu be opened and if
                //Menu is pin menu
                if (T == typeof(PinCameraBlankMenuHelper))
                {
                    if (_entityState.StarSystemId != null)
                        _state.Camera.PinToEntity(_entityState.Id, _entityState.StarSystemId, _state);
                    if (managesUIPopUps)
                    {
                        ImGui.CloseCurrentPopup();
                    }
                }
                //Menu is goto system menu
                else if (T == typeof(GotoSystemBlankMenuHelper))
                {
                    var destination = Resolve(_entityState, _state)?.GetView<GravSurveyView>()?.JumpPointToSystemId;
                    if (destination != null)
                        _state.SetActiveSystem(destination);
                }
                else if (T == typeof(SelectPrimaryBlankMenuHelper) && _entityState.StarSystemId != null)
                {
                    _state.EntitySelectedAsPrimary(_entityState.Id, _entityState.StarSystemId);
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
                //Menu is fire control menu
                else if (T == typeof(FireControl))
                {
                    var instance = FireControl.GetInstance(_entityState);
                    instance.ToggleActive();
                    _state.ActiveWindow = instance;
                }
                //Menu is rename menu
                else if (T == typeof(RenameWindow))
                {
                    var renameWindow = RenameWindow.GetInstance();
                    renameWindow.SetTarget(_entityState.Id, _entityState.Name);
                    _state.ActiveWindow = renameWindow;
                    if (managesUIPopUps)
                    {
                        ImGui.CloseCurrentPopup();
                    }

                }
                //Menu is cargo menu
                else if (T == typeof(CreateTransferWindow) && _entityState.StarSystemId != null)
                {
                    var instance = CreateTransferWindow.GetInstance();
                    instance.SetLeft(_entityState.Id, _entityState.StarSystemId);
                    instance.ToggleActive();
                    _state.ActiveWindow = instance;
                }
                else if (T == typeof(PowerGenWindow))
                {
                    var instance = PowerGenWindow.GetInstance();
                    instance.ToggleActive();
                    _state.ActiveWindow = instance;
                }
                else if (T == typeof(NavWindow))
                {
                    var instance = NavWindow.GetInstance(_entityState);
                    instance.ToggleActive();
                    _state.ActiveWindow = instance;
                }
                else if (T == typeof(OrdersListWindow))
                {
                    _state.WindowManager.ActivateOrderListWindow(_entityState);
                }
            }
        }

        public static bool CheckOpenUIWindow(Type T, EntityState _entityState, GlobalUIState _state)
        {
            // Global Windows
            if (T == typeof(WarpOrderWindow))
            {
                return WarpOrderWindow.GetInstance(_entityState).GetActive();
            }
            else if (T == typeof(ChangeCurrentOrbitWindow))
            {
                return ChangeCurrentOrbitWindow.GetInstance(_entityState).GetActive();
            }
            else if (T == typeof(FireControl))
            {
                return FireControl.GetInstance(_entityState).GetActive();
            }
            else if (T == typeof(NavWindow))
            {
                return NavWindow.GetInstance(_entityState).GetActive();
            }
            else if (T == typeof(CreateTransferWindow))
            {
                return CreateTransferWindow.GetInstance().GetActive();
            }
            else if (T == typeof(PowerGenWindow))
            {
                return PowerGenWindow.GetInstance().GetActive();
            }
            // Instance Windows
            else if (T == typeof(OrdersListWindow))
            {
                return OrdersListWindow.GetInstance(_entityState).GetActive();
            }
            else return false;
        }
    }

}
