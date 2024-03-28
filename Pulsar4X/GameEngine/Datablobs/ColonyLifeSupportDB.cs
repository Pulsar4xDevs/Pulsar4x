namespace Pulsar4X.Datablobs
{
    public class ColonyLifeSupportDB : BaseDataBlob
    {
        public long MaxPopulation { get; set; }

        public ColonyLifeSupportDB()
        {
            MaxPopulation = new long();
        }

        public ColonyLifeSupportDB(ColonyLifeSupportDB db)
        {
            MaxPopulation = db.MaxPopulation;
        }

        public override object Clone()
        {
            return new ColonyLifeSupportDB(this);
        }
    }
}
