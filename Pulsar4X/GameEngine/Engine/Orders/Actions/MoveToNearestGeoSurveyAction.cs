using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;

namespace Pulsar4X.Engine.Orders
{
    public class MoveToNearestGeoSurveyAction : MoveToNearestAction
    {
        public override string Name => "Move to Nearest Geo Survey";
        public override string Details => "Moves the fleet to the nearest system body that can be geo surveyed.";
        private bool GeoSurveyFilter(Entity entity)
        {
            return entity.HasDataBlob<SystemBodyInfoDB>()
                && !entity.HasDataBlob<StarInfoDB>()
                && (!entity.HasDataBlob<GeoSurveyableDB>() || !entity.GetDataBlob<GeoSurveyableDB>().IsSurveyComplete(RequestingFactionGuid));
        }

        public static MoveToNearestGeoSurveyAction CreateCommand(int factionId, Entity commandingEntity)
        {
            var command = MoveToNearestAction.CreateCommand<MoveToNearestGeoSurveyAction>(factionId, commandingEntity);
            command.Filter = command.GeoSurveyFilter;
            return command;
        }
    }
}