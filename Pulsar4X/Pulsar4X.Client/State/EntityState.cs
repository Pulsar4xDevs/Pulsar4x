using Pulsar4X.Api;

namespace Pulsar4X.Client
{
    /// <summary>
    /// The identity of a clicked/selected entity, as passed around the click pipeline and window
    /// launchers. Holds no game data — windows resolve the entity's current
    /// <see cref="EntitySnapshot"/> from the replicated galaxy each frame by id.
    /// </summary>
    public class EntityState
    {
        public int Id { get; }
        public string? StarSystemId { get; }
        public string Name { get; set; }
        internal UserOrbitSettings.OrbitBodyType BodyType { get; }

        public EntityState(EntitySnapshot snapshot, string systemId)
        {
            Id = snapshot.Id;
            StarSystemId = systemId;
            Name = snapshot.GetView<NameView>()?.Name ?? "Unknown";
            BodyType = UserOrbitSettings.FromBodyKind(snapshot.Kind);
        }

        public bool IsPlanetOrMoon()
        {
            return BodyType == UserOrbitSettings.OrbitBodyType.Planet
                || BodyType == UserOrbitSettings.OrbitBodyType.DwarfPlanet
                || BodyType == UserOrbitSettings.OrbitBodyType.Moon;
        }

        public bool IsSmallBody()
        {
            return BodyType == UserOrbitSettings.OrbitBodyType.Asteroid
                || BodyType == UserOrbitSettings.OrbitBodyType.Comet;
        }

        public bool IsStar() => BodyType == UserOrbitSettings.OrbitBodyType.Star;
    }
}
