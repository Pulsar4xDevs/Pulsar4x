using Pulsar4X.Datablobs;

namespace Pulsar4X.Engine.Orders
{
    public class MoveToNearestColonyAction : MoveToNearestAction
    {
        public override string Name => "Move to Nearest Colony";
        public override string Details => "Moves the fleet to the nearest colony.";
        private static bool ColonyFilter(Entity entity)
        {
            return entity.HasDataBlob<ColonyInfoDB>();
        }

        private static Entity ColonySelector(Entity entity)
        {
            return entity.GetDataBlob<PositionDB>().Parent ?? entity;
        }

        public static MoveToNearestColonyAction CreateCommand(int factionId, Entity commandingEntity)
        {
            var command = MoveToNearestAction.CreateCommand<MoveToNearestColonyAction>(factionId, commandingEntity);
            command.Filter = ColonyFilter;
            command.TargetSelector = ColonySelector;
            return command;
        }
    }
}