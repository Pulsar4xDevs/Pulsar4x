using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Pulsar4X.Client
{
    internal sealed class WindowManager
    {
        internal List<UpdateWindowState> AllWindows { get; init; } = [];
        internal Dictionary<Type, UniquePulsarGuiWindow> LoadedWindows { get; init; } = [];
        internal Dictionary<string, NamedPulsarGuiWindow> LoadedNonUniqueWindows { get; init; } = [];

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
            if (LoadedNonUniqueWindows.TryGetValue(name, out var foundWindow))
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
            if (LoadedWindows.TryGetValue(typeof(T), out var foundWindow))
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
            LoadedNonUniqueWindows.Add(name, window);
            return window;
        }

        internal T AddUniqueWindow<T>(T window) where T : UniquePulsarGuiWindow
        {
            AddWindow(window);
            LoadedWindows.Add(typeof(T), window);
            return window;
        }

        internal T AddWindow<T>(T window) where T : UpdateWindowState
        {
            AllWindows.Add(window);
            return window;
        }

        internal void UnloadAllWindows()
        {
            LoadedWindows.Clear();
            LoadedNonUniqueWindows.Clear();
            AllWindows.Clear();
        }

        public IEnumerable<UpdateWindowState> GetActiveWindows() => AllWindows.Where(x => x.GetActive());
    }

}
