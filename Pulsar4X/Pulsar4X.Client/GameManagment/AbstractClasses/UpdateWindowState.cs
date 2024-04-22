using System;

namespace Pulsar4X.SDL2UI
{
    public abstract class UpdateWindowState
    {
        internal static GlobalUIState _uiState;

        public abstract bool GetActive();

        public abstract void OnGameTickChange(DateTime newDate);
        public abstract void OnSystemTickChange(DateTime newDate);

        protected UpdateWindowState()
        {
            _uiState.UpdateableWindows.Add(this);
        }

        public void Deconstructor()
        {
            _uiState.UpdateableWindows.Remove(this);
        }

    }
}