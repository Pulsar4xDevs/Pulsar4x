using SDL3;

namespace Pulsar4X.Input
{
    public interface IPointerHandler
    {
        // Returning true means that the event was handled

        bool OnPointerEnter(SDL.Event sevent) => false;
        bool OnPointerExit(SDL.Event sevent) => false;
        bool OnPointerMove(SDL.Event sevent) => false;

        bool OnPointerDown(SDL.Event sevent) => false;
        bool OnPointerUp(SDL.Event sevent) => false;
    }
}
