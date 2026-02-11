using SDL3;

namespace Pulsar4X.Input
{
    public interface IPointerHandler
    {
        void OnPointerEnter(SDL.Event sevent) {}
        void OnPointerExit(SDL.Event sevent) {}
        void OnPointerMove(SDL.Event sevent) {}

        void OnPointerDown(SDL.Event sevent) {}
        void OnPointerUp(SDL.Event sevent) {}
    }
}
