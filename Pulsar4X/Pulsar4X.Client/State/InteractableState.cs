using Pulsar4X.Input;

namespace Pulsar4X.Client
{
    public interface IInteractable : IShape, IPointerHandler {
        byte Priority {
            get { return 0; } // default priority is 0
        }
    }

    public class InteractableState
    {
        public IInteractable Item;
        public bool IsHovered = false;
        public bool IsPressed = false;
        public bool IsDisabled = false;

        public InteractableState(IInteractable item)
        {
            this.Item = item;
        }
    }
}
