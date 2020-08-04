using Pulsar4X.Orbital;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Small helper class for Pressure unit conversions
    /// </summary>
    public static class Pressure
    {
        public static float AtmToKPa(float atm)
        {
            return atm * UniversalConstants.Units.PascalsPerATM / 1000f;
        }

        public static float KPaToAtm(float kpa)
        {
            return kpa * 1000f * UniversalConstants.Units.ATMPerPascal;
        }
        public static float AtmToPa(float atm)
        {
            return atm * UniversalConstants.Units.PascalsPerATM;
        }

        public static float PaToAtm(float pa)
        {
            return pa * UniversalConstants.Units.ATMPerPascal;
        }

        public static float AtmToBar(float atm)
        {
            return atm * UniversalConstants.Units.BarPerATM;
        }

        public static float BarToAtm(float bar)
        {
            return bar * UniversalConstants.Units.ATMPerBar;
        }

        public static float AtmToTorr(float atm)
        {
            return atm * UniversalConstants.Units.TorrPerATM;
        }

        public static float TorrToAtm(float torr)
        {
            return torr * UniversalConstants.Units.ATMPerTorr;
        }
    }
}
