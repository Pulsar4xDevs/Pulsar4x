using System;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;


namespace Pulsar4X.SDL2UI
{
    //a do nothing helper class that is plugged into generics for static checks
    public class PinCameraBlankMenuHelper : PulsarGuiWindow
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
        internal static bool checkIfCanOpenWindow<T>(EntityState _entityState) where T : PulsarGuiWindow
        {
           //if can be used to pin
            if (typeof(T) == typeof(PinCameraBlankMenuHelper))
            {
                return true;
            }
            //if entity can warp
            if (_entityState.Entity.HasDataBlob<WarpAbilityDB>() && typeof(T) == typeof(WarpOrderWindow))
            {
                return true;
            }
            //if entity can move
            if (_entityState.Entity.HasDataBlob<NewtonThrustAbilityDB>() && typeof(T) == typeof(ChangeCurrentOrbitWindow))
            {
                return true;
            }
            //if entity can fire?
            if (_entityState.Entity.HasDataBlob<FireControlAbilityDB>() && typeof(T) == typeof(WeaponTargetingControl))
            {
                return true;
            }
            //if entity can be renamed?
            if (typeof(T) == typeof(RenameWindow))
            {
                return true;
            }
            //if entity can target
            if (_entityState.Entity.HasDataBlob<CargoStorageDB>() && typeof(T) == typeof(CargoTransfer))
            {
                return true;
            }
            //if entity can mine || refine || build
            if (_entityState.Entity.HasDataBlob<ColonyInfoDB>() && typeof(T) == typeof(ColonyPanel))
            {
                return true;
            }
            return false;
        }
        // use type PinCameraBlankMenuHelper to pin camara, should use checkIfCanOpenWindow with type before trying to open a given window
        //type parameter is the type of window opened, first parameter indicates wether the window should be opened, second parameter is EntityState for the entity using the window
        //(or window using the entity?) third is the GlobalUIState and fourth indicates wether this function should manage closing preopened pop-ups(mostly utility for EntityContextMenu class)
        [PublicAPI]
        internal static void openUIWindow<T>(bool open, EntityState _entityState, GlobalUIState _state, bool managesUIPopUps = false) where T : PulsarGuiWindow
        {
            if (open)
            {
                if (typeof(T) == typeof(PinCameraBlankMenuHelper))
                {
                    _state.Camera.PinToEntity(_entityState.Entity);
                    if (managesUIPopUps)
                    {
                        ImGui.CloseCurrentPopup();
                    }
                    
                }

                //if entity can warp
                if (typeof(T) == typeof(WarpOrderWindow))
                {
                    WarpOrderWindow.GetInstance(_entityState).IsActive = true;
                    _state.ActiveWindow = WarpOrderWindow.GetInstance(_entityState);
                }
               
                if (typeof(T) == typeof(ChangeCurrentOrbitWindow))
                {
                    ChangeCurrentOrbitWindow.GetInstance(_entityState).IsActive = true;
                    _state.ActiveWindow = ChangeCurrentOrbitWindow.GetInstance(_entityState);
                }
               
                if (typeof(T) == typeof(WeaponTargetingControl))
                {
                    var instance = WeaponTargetingControl.GetInstance(_entityState);
                    instance.SetOrderEntity(_entityState);
                    instance.IsActive = true;
                    _state.ActiveWindow = instance;
                }
               
                if (typeof(T) == typeof(RenameWindow))
                {
                    RenameWindow.GetInstance(_entityState).IsActive = true;
                    _state.ActiveWindow = RenameWindow.GetInstance(_entityState);
                    ImGui.CloseCurrentPopup();
                }
                

                if (typeof(T) == typeof(CargoTransfer))
                {
                    var instance = CargoTransfer.GetInstance(_state.Game.StaticData, _entityState);
                    instance.IsActive = true;
                    _state.ActiveWindow = instance;
                }
                
                //econOrderwindow

                if (typeof(T) == typeof(ColonyPanel))
                {
                    var instance = ColonyPanel.GetInstance(_state.Game.StaticData, _entityState);
                    instance.IsActive = true;
                    _state.ActiveWindow = instance;
                }
            }
        }
    }
}
