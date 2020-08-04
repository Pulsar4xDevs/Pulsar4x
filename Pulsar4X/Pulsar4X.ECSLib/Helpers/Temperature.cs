using Pulsar4X.Orbital;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Small helper class for Temperature unit conversions
    /// </summary>
    public static class Temperature
    {
        public static double ToKelvin(double celsius)
        {
            return celsius + UniversalConstants.Units.DegreesCToKelvin;
        }

        public static float ToKelvin(float celsius)
        {
            return (float)(celsius + UniversalConstants.Units.DegreesCToKelvin);
        }

        public static double ToCelsius(double kelvin)
        {
            return kelvin + UniversalConstants.Units.KelvinToDegreesC;
        }

        public static float ToCelsius(float kelvin)
        {
            return (float)(kelvin + UniversalConstants.Units.KelvinToDegreesC);
        }
    }
}
