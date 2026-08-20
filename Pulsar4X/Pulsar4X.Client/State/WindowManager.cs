using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Pulsar4X.Client
{
    internal sealed class WindowManager
    {
        // Do not modify these collections directly. Use the provided methods to add/remove windows.
        internal List<UpdateWindowState> AllWindows { get; init; } = [];
        internal Dictionary<Type, UniquePulsarGuiWindow> UniqueWindows { get; init; } = [];
        internal Dictionary<string, NamedPulsarGuiWindow> NamedWindows { get; init; } = [];

        // This is a map of window type to all named windows of that type.
        internal Dictionary<Type, List<NamedPulsarGuiWindow>> NamedWindowsByType { get; init; } = [];

        internal void RenderActiveWindows()
        {
            foreach (var item in UniqueWindows.Values.ToArray())
            {
                item.Display();
            }

            /*
            foreach (var entityWindow in _state.EntityWindows.Values.ToArray())
            {
                entityWindow.Display();
            }
            */

            foreach (var item in NamedWindows.Values.ToArray())
            {
                item.Display();
            }
        }

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
            // Add to the dictionary first, so if it throws an exception it is not added to the main list.
            NamedWindows.Add(name, window);
            AddWindow(window);

            if(!NamedWindowsByType.TryGetValue(typeof(T), out var windowList))
            {
                NamedWindowsByType.Add(typeof(T), [window]);
            }
            else
            {
                windowList.Add(window);
            }
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
            NamedWindowsByType.Clear();
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
