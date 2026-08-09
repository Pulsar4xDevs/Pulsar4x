using System;

namespace Pulsar4X.Client
{
    public abstract class UpdateWindowState : IDisposable
    {
        internal static GlobalUIState _uiState;

        public abstract bool GetActive();

        public virtual void OnSystemTickChange(DateTime newDate) { }

        protected UpdateWindowState()
        {}

        public void Dispose()
        {}
    }
}