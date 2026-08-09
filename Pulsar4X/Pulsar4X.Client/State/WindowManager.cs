using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Pulsar4X.Client
{
    internal sealed class WindowManager
    {
        internal List<UpdateWindowState> AllWindows { get; init; } = [];
        internal Dictionary<Type, UniquePulsarGuiWindow> UniqueWindows { get; init; } = [];
        internal Dictionary<string, NamedPulsarGuiWindow> NamedWindows { get; init; } = [];

        internal NamedPulsarGuiWindow? GetNamedWindow(string name)
        {
            if (TryGetNamedWindow(name, out var window))
            {
                return window;
            }
            return null;
        }

        internal bool TryGetNamedWindow(string name, [NotNullWhen(true)] out NamedPulsarGuiWindow? window)
        {
            if (NamedWindows.TryGetValue(name, out var foundWindow))
            {
                window = foundWindow;
                return true;
            }

            window = null;
            return false;
        }

        internal bool TryGetNamedWindow<T>(string name, [NotNullWhen(true)] out T? window) where T : NamedPulsarGuiWindow
        {
            if (TryGetNamedWindow(name, out var foundWindow))
            {
                window = (T)foundWindow;
                return true;
            }
            window = null;
            return false;
        }

        /// <summary>
        /// Gets a unique window of type T. Only one instance of a unique window can exist at a time. 
        /// </summary>
        /// <typeparam name="T">The type of window.</typeparam>
        /// <returns>The unique window instance, or <see langword="null"/> if no instance exists.</returns>
        internal T? GetUniqueWindow<T>() where T : UniquePulsarGuiWindow
        {
            if (TryGetUniqueWindow<T>(out var window))
            {
                return window;
            }
            return null;
        }

        internal bool TryGetUniqueWindow<T>([NotNullWhen(true)] out T? window) where T : UniquePulsarGuiWindow
        {
            if (UniqueWindows.TryGetValue(typeof(T), out var foundWindow))
            {
                window = (T)foundWindow;
                return true;
            }

            window = null;
            return false;
        }

        internal T AddNamedWindow<T>(string name, T window) where T : NamedPulsarGuiWindow
        {
            AddWindow(window);
            NamedWindows.Add(name, window);
            return window;
        }

        internal T AddUniqueWindow<T>(T window) where T : UniquePulsarGuiWindow
        {
            AddWindow(window);
            UniqueWindows.Add(typeof(T), window);
            return window;
        }

        internal T AddWindow<T>(T window) where T : UpdateWindowState
        {
            AllWindows.Add(window);
            return window;
        }

        internal void UnloadAllWindows()
        {
            UniqueWindows.Clear();
            NamedWindows.Clear();
            AllWindows.Clear();
        }

        internal void CloseAllWindows()
        {
            foreach (var window in UniqueWindows.Values)
            {
                window.SetActive(false);
            }
        }

        public IEnumerable<UpdateWindowState> GetActiveWindows() => AllWindows.Where(x => x.GetActive());
    }

}
