using ImGuiNET;
using Pulsar4X.Engine;
using System;

namespace Pulsar4X.SDL2UI
{
    public abstract class NonUniquePulsarGuiWindow : UpdateWindowState
    {
        protected ImGuiWindowFlags _flags = ImGuiWindowFlags.None;
        internal bool CanActive = false;
        internal bool IsActive = false;
        internal string UniqueName = "test";
        internal static GlobalUIState _state;

        protected EntityState _lookedAtEntity;

        public void SetActive(bool ActiveVal = true)
        {
            IsActive = ActiveVal;
        }

        public void ToggleActive()
        {
            IsActive = !IsActive;
        }

        public override bool GetActive()
        {
            return IsActive;
        }

        public void SetName(string Newname)
        {
            UniqueName = Newname;
        }

        public virtual string GetName()
        {
            return UniqueName;
        }

        public void StartDisplay()
        {
            _state.LoadedNonUniqueWindows[this.UniqueName] = this;
        }

        protected NonUniquePulsarGuiWindow()
        {

        }

        internal abstract void Display();

        internal virtual void EntityClicked(EntityState entity, MouseButtons button) { }

        internal virtual void EntitySelectedAsPrimary(EntityState entity) { }

        internal virtual void MapClicked(Orbital.Vector3 worldPos_m, MouseButtons button) { }

        internal void Destroy()
        {
        }

        public override void OnGameTickChange(DateTime newDate)
        {
        }

        public override void OnSystemTickChange(DateTime newDate)
        {
        }

        public override void OnSelectedSystemChange(StarSystem newStarSys)
        {
        }
    }
}