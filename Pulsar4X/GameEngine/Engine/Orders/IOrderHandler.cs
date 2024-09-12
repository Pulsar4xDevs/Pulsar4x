using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.Interfaces
{
    public interface IOrderHandler
    {
        Game Game { get; }

        void HandleOrder(EntityCommand entityCommand);
    }
}
