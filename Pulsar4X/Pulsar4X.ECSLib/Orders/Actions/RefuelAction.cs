namespace Pulsar4X.ECSLib
{
    public class RefuelAction : IAction
    {
        public void Execute(Entity fleet)
        {
            // TODO
        }

        public bool IsFinished { get; private set; } = false;
    }
}