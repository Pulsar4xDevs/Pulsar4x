namespace Pulsar4X.Orbital
{
    /// <summary>
    /// Small helper class for Distance unit conversions
    /// </summary>
    public static class Distance
    {
        public static Vector3 MToAU(Vector3 meters)
        {
            return meters / UniversalConstants.Units.MetersPerAu;
        }
        public static double MToAU(double meters)
        {
            return meters / UniversalConstants.Units.MetersPerAu;
        }

        public static double MToKm(double meters)
        {
            return meters / 1000.0;
        }
        public static double KmToM(double kilometers)
        {
            return kilometers * 1000.0;
        }

        public static double KmToAU(double km)
        {
            return km / UniversalConstants.Units.KmPerAu;
        }
        public static Vector3 KmToAU(Vector3 km)
        {
            return km / UniversalConstants.Units.KmPerAu;
        }
        public static Vector2 KmToAU(Vector2 km)
        {
            return km / UniversalConstants.Units.KmPerAu;
        }
        public static double AuToKm(double au)
        {
            return au * UniversalConstants.Units.KmPerAu;
        }
        public static Vector3 AuToKm(Vector3 Au)
        {
            return new Vector3(AuToKm(Au.X), AuToKm(Au.Y), AuToKm(Au.Z));
        }
        public static Vector2 AuToKm(Vector2 Au)
        {
            return new Vector2(AuToKm(Au.X), AuToKm(Au.Y));
        }

        public static Vector3 AuToMt(Vector3 au)
        {
            Vector3 meters = au * UniversalConstants.Units.MetersPerAu;
            return meters;
        }
        public static Vector2 AuToMt(Vector2 au)
        {
            Vector2 meters = au * UniversalConstants.Units.MetersPerAu;
            return meters;
        }
        public static MinMaxStruct AuToMt(MinMaxStruct au)
        {
            return new MinMaxStruct(AuToMt(au.Min), AuToMt(au.Max));
        }
        public static double AuToMt(double au)
        {
            return au * UniversalConstants.Units.MetersPerAu;
        }

        public static double DistanceBetween(Vector3 p1, Vector3 p2)
        {
            return (p1 - p2).Length();
        }

    }
}
