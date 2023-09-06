namespace Pulsar4X.ECSLib
{
    public abstract class BaseAction : IAction
    {
        private IAction _nextAction;

        public abstract void Execute(Entity fleet);

        public void ExecuteNext(Entity fleet)
        {
            _nextAction?.Execute(fleet);
        }

        public void AddNextAction(IAction nextAction)
        {
            if(_nextAction == null)
            {
                _nextAction = nextAction;
            }
            else
            {
                _nextAction.AddNextAction(nextAction);
            }
        }
    }
}