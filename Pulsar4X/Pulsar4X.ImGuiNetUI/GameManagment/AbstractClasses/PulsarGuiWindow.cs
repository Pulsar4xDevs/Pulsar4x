using ImGuiNET;
using Pulsar4X.ECSLib;
using System;

namespace Pulsar4X.SDL2UI
{
    public abstract class PulsarGuiWindow : UpdateWindowState
    {
        protected ImGuiWindowFlags _flags = ImGuiWindowFlags.None;
        //internal bool IsLoaded;
        internal bool CanActive = false;
        protected bool IsActive = false;
        //internal int StateIndex = -1;
        //protected bool _IsOpen;
        public bool ClickedEntityIsPrimary = true;

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

        protected PulsarGuiWindow()
        {
            _uiState.LoadedWindows[this.GetType()] = this;
        }


        /*An example of how the constructor should be for a derived class. 
         * 
        private  DerivedClass (GlobalUIState state):base(state)
        {
            any other DerivedClass specific constrctor stuff here.
        }
        internal static DerivedClass GetInstance(GlobalUIState state)
        {
            if (!state.LoadedWindows.ContainsKey(typeof(DerivedClass)))
            {
                return new DerivedClass(state);
            }
            return (DerivedClass)state.LoadedWindows[typeof(DerivedClass)];
        }
        */

        internal abstract void Display();

        internal virtual void EntityClicked(EntityState entity, MouseButtons button) { }

        internal virtual void EntitySelectedAsPrimary(EntityState entity) { }

        internal virtual void MapClicked(Orbital.Vector3 worldPos_m, MouseButtons button) { }

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