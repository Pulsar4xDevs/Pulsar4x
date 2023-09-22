namespace Pulsar4X.DataStructures
{
    /// <summary>
    /// Small helper struct to make all these min/max dicts. nicer.
    /// </summary>
    public struct MinMaxStruct
    {
        public double Min, Max;

        public MinMaxStruct(double min, double max)
        {
            Min = min;
            Max = max;
        }
    }
}