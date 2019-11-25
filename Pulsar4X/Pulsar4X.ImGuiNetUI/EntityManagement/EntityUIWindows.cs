using System;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;
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

    //has all initialization rutines for common entity management related UI windows, also has a function that checks if a window can be opened for a given EntityState
    public class EntityUIWindows
    {
        //checks if given menu can be opened for given entity
        [PublicAPI]
        internal static bool checkIfCanOpenWindow<T>(EntityState _entityState) where T : PulsarGuiWindow
        {
            //Checks if the power gen menu can be opened
            if (_entityState.Entity.HasDataBlob<EnergyGenAbilityDB>() && typeof(T) == typeof(PowerGen))
            {
                return true;
            }

            //Check if the pin menu can be opened
            if (typeof(T) == typeof(PinCameraBlankMenuHelper))
            {
                return true;
            }
            //if can be used to go to another system
            if (_entityState.Entity.HasDataBlob<JPSurveyableDB>() && typeof(T) == typeof(GotoSystemBlankMenuHelper) )
            {
                if (_entityState.Entity.GetDataBlob<JPSurveyableDB>().JumpPointTo != null)
                {
                    return true;
                }
            }
            //if can be selected as primary
            if (typeof(T) == typeof(SelectPrimaryBlankMenuHelper))
            {
                return true;
            }
            //if entity can warp
            if (_entityState.Entity.HasDataBlob<WarpAbilityDB>() && typeof(T) == typeof(OrbitOrderWindow))
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
        //(or window using the entity?) third is the GlobalUIState and fourth indicates wether this function should manage closing preopened pop-ups(mostly utility for EntityContextMenu class[should be set to true when this is used in it])
        [PublicAPI]
        internal static void openUIWindow(Type T, EntityState _entityState , GlobalUIState _state , bool open = true, bool managesUIPopUps = false)
        {
            if (open)
            {
                
                if (T == typeof(PinCameraBlankMenuHelper))
                {
                    _state.Camera.PinToEntity(_entityState.Entity);
                    if (managesUIPopUps)
                    {
                        ImGui.CloseCurrentPopup();
                    }
                    
                }
                else if (T == typeof(GotoSystemBlankMenuHelper))
                {
                    _state.SetActiveSystem(_entityState.Entity.GetDataBlob<JPSurveyableDB>().JumpPointTo.GetDataBlob<PositionDB>().SystemGuid);
                }else if(T==typeof(SelectPrimaryBlankMenuHelper)){
                    _state.EntitySelectedAsPrimary(_entityState.Entity.Guid, _entityState.StarSysGuid);
                }
                //if entity can warp
                else if (T == typeof(OrbitOrderWindow))
                {
                    OrbitOrderWindow.GetInstance(_entityState).IsActive = true;
                    _state.ActiveWindow = OrbitOrderWindow.GetInstance(_entityState);
                }
               
                else if (T == typeof(ChangeCurrentOrbitWindow))
                {
                    ChangeCurrentOrbitWindow.GetInstance(_entityState).IsActive = true;
                    _state.ActiveWindow = ChangeCurrentOrbitWindow.GetInstance(_entityState);
                }
               
                else if (T == typeof(WeaponTargetingControl))
                {
                    var instance = WeaponTargetingControl.GetInstance(_entityState);
                    instance.SetOrderEntity(_entityState);
                    instance.IsActive = true;
                    _state.ActiveWindow = instance;
                }
               
                else if (T == typeof(RenameWindow))
                {
                    RenameWindow.GetInstance(_entityState).IsActive = true;
                    _state.ActiveWindow = RenameWindow.GetInstance(_entityState);
                    ImGui.CloseCurrentPopup();
                }
                

                else if (T == typeof(CargoTransfer))
                {
                    var instance = CargoTransfer.GetInstance(_state.Game.StaticData, _entityState);
                    instance.IsActive = true;
                    _state.ActiveWindow = instance;
                }
                
                //econOrderwindow

                else if (T == typeof(ColonyPanel))
                {
                    var instance = ColonyPanel.GetInstance(_state.Game.StaticData, _entityState);
                    instance.IsActive = true;
                    _state.ActiveWindow = instance;
                }
            }
        }
    }
}
