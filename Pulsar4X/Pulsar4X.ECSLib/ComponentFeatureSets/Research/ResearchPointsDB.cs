namespace Pulsar4X.ECSLib
{
    public class  EntityResearchDB : BaseDataBlob
    {
        public EntityResearchDB()
        {
        }

        public EntityResearchDB(EntityResearchDB db)
        {

        }

        public override object Clone()
        {
            return new EntityResearchDB(this);
        }
    }
}