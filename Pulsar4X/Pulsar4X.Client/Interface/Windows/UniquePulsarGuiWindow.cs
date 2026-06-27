using ImGuiNET;
using System;

namespace Pulsar4X.Client
{
    public abstract class UniquePulsarGuiWindow : UpdateWindowState
    {
        protected ImGuiWindowFlags _flags = ImGuiWindowFlags.None;
        //internal bool IsLoaded;
        internal bool CanActive = true;

        protected bool IsActive = false;
        //internal int StateIndex = -1;
        //protected bool _IsOpen;
        public bool ClickedEntityIsPrimary = true;

        protected UniquePulsarGuiWindow(string name)
        {
            int x = 1;
            // _uiState.LoadedWindows[this.GetType()] = this;
        }

        public void SetActive(bool ActiveVal = true)
        {
            if(CanActive)
                IsActive = ActiveVal;
            else
                IsActive = false;

        }

        public void ToggleActive()
        {
            if(CanActive)
                IsActive = !IsActive;
            else
                IsActive = false;
        }

        public override bool GetActive()
        {
            return IsActive;
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
    }

    /// <summary>
    /// A generic version of <see cref="UniquePulsarGuiWindow"/> that automatically generates a unique name based on the type parameter <see cref="T"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class UniquePulsarGuiWindow<T> : UniquePulsarGuiWindow
    {
        protected UniquePulsarGuiWindow() : base(typeof(T).FullName ?? typeof(T).Name)
        {}
    }
}
