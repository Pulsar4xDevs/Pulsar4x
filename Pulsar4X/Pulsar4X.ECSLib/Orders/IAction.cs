namespace Pulsar4X.ECSLib
{
    public interface IAction
    {
        void Execute(Entity fleet);
        void ExecuteNext(Entity fleet);
        void AddNextAction(IAction nextAction);
    }
}