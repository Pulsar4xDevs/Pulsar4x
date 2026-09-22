using ImGuiNET;
using System;

namespace Pulsar4X.Client
{
    /// <summary>
    /// A base class for GUI windows that have a unique name.
    /// </summary>
    public abstract class NamedPulsarGuiWindow : UpdateWindowState
    {
        protected ImGuiWindowFlags _flags = ImGuiWindowFlags.None;
        
        internal bool CanActive { get; set; } = false;

        private bool _isActive = false;
        
        public bool IsActive
        {
            get { return _isActive; }
            set {  _isActive = value; }
        }
        protected ref bool IsActiveRef => ref _isActive;

        internal string UniqueName { get; init; }

        protected EntityState? _lookedAtEntity;

        protected NamedPulsarGuiWindow(string name)
        {
            UniqueName = name;
        }

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

        public virtual string GetName()
        {
            return UniqueName;
        }

        internal abstract void Display();

        internal virtual void EntityClicked(EntityState entity, MouseButtons button) { }

        internal virtual void EntitySelectedAsPrimary(EntityState entity) { }

        internal void Destroy()
        {
        }
    }
}
