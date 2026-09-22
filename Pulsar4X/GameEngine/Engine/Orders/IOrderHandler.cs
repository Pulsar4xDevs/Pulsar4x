using GameEngine.Engine.Orders;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.Interfaces
{
    public interface IOrderHandler
    {
        Game Game { get; }

        /// <summary>
        /// Validates and dispatches a command. Returns true if it passed validation and was
        /// queued/executed; false if it was rejected (e.g. failed <c>IsValidCommand</c>).
        /// </summary>
        bool HandleOrder(EntityAction entityAction);
    }
}
