namespace Pulsar4X.ECSLib
{
    public class ColonyInfoDB : BaseDataBlob
    {
        public JDictionary<DataBlobRef<SpeciesDB>, double> Population;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="popSize">Species and population number(in Million?)</param>
        public ColonyInfoDB(JDictionary<DataBlobRef<SpeciesDB>, double> popSize)
        {
            Population = popSize;
        }

        public ColonyInfoDB()
            : base()
        { }
    }
}
