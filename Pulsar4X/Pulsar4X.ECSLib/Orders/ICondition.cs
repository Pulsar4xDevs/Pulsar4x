namespace Pulsar4X.ECSLib
{
    public interface ICondition
    {
        bool Evaluate(Entity fleet);
    }
}