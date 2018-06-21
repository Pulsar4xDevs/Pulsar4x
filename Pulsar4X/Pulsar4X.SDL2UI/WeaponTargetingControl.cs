using System;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class WeaponTargetingControl : PulsarGuiWindow
    {
        Entity _orderingEntity;
        public WeaponTargetingControl(EntityState entity)
        {
            _orderingEntity = entity.Entity;
            IsActive = true;
        }
        internal static WeaponTargetingControl GetInstance(EntityState entity)
        {
            if (!_state.LoadedWindows.ContainsKey(typeof(WeaponTargetingControl)))
            {
                return new WeaponTargetingControl(entity);
            }
            var instance = (WeaponTargetingControl)_state.LoadedWindows[typeof(WeaponTargetingControl)];
            instance.SetOrderEntity(entity);
            return instance;
        }

        internal void SetOrderEntity(EntityState entity)
        {
            _orderingEntity = entity.Entity;
        }

        internal override void Display()
        {
            if (IsActive)
            {
                ImVec2 size = new ImVec2(200, 100);
                ImVec2 pos = new ImVec2(_state.MainWinSize.x / 2 - size.x / 2, _state.MainWinSize.y / 2 - size.y / 2);

                ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowPos(pos, ImGuiCond.FirstUseEver);

                if (ImGui.Begin("Weapon Targeting", ref IsActive, _flags))
                { 
                

                
                
                
                }
                ImGui.End();
            }
        }
    }
}
