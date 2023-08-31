namespace Pulsar4X.ECSLib
{
    public interface IOrderHandler
    {
        Game Game { get; }

        void HandleOrder(EntityCommand entityCommand);
    }
}
