namespace Pulsar4X.ECSLib
{
    public static class JPFactory
    {
        public static void LinkJumpPoints(Entity JP1, Entity JP2)
        {
            var jp1TransitableDB = JP1.GetDataBlob<TransitableDB>();
            var jp2TransitableDB = JP2.GetDataBlob<TransitableDB>();

            jp1TransitableDB.Destination = JP2;
            jp2TransitableDB.Destination = JP1;
        }

        public static Entity CreateJumpPoint(StarSystem system)
        {
            NameDB jpNameDB = new NameDB("Jump Point");
            PositionDB jpPositionDB = new PositionDB(0,0,0, system);
            TransitableDB jpTransitableDB = new TransitableDB();
            
            Entity jumpPoint = Entity.Create(system.SystemManager);
            return jumpPoint;
        }
    }
}
