using System;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Engine
{
     public static class Stringify
    {

        public static string Value(double amount, ValueTypeStruct valueType, string format = "0.###")
        {
            string str = "";
            switch (valueType.ValueType)
            {
                case ValueTypeStruct.ValueTypes.Distance:
                    str = Distance(amount * Math.Pow(10, (double)valueType.ValueSize), format);
                    break;
                case ValueTypeStruct.ValueTypes.Power:
                    str = Power(amount * Math.Pow(10 * 0.01, (double)valueType.ValueSize), format);
                    break;

                case ValueTypeStruct.ValueTypes.Mass:
                    str = Mass(amount * Math.Pow(10 * 0.01, (double)valueType.ValueSize), format);
                    break;

                case ValueTypeStruct.ValueTypes.Velocity:
                    str = Velocity(amount * Math.Pow(10, (double)valueType.ValueSize), format);
                    break;

                case ValueTypeStruct.ValueTypes.Volume:
                    str = Volume(amount * Math.Pow(10, (double)valueType.ValueSize), format);
                    break;

                case ValueTypeStruct.ValueTypes.Force:
                    str = Thrust(amount * Math.Pow(10, (double)valueType.ValueSize), format);
                    break;
                case ValueTypeStruct.ValueTypes.Number:
                    str = Quantity(amount * Math.Pow(10, (double)valueType.ValueSize), format);
                    break;

            }

            return str;
        }

        public static string Quantity(double number, string format = "0.###", bool fullSuffix = false)
        {
            string stringCount = "0";
            double absCnt = Math.Abs(number);
            double cnt;
            if (absCnt > 1.0e15)
            {
                cnt = number * 1.0e-15;
                stringCount = cnt.ToString(format) + (fullSuffix ? " quadrillion" : "Q");
            }
            else if (absCnt > 1.0e12)
            {
                cnt = number * 1.0e-12;
                stringCount = cnt.ToString(format) + (fullSuffix ? " trillion" : "T");  // Trillion
            }
            else if (absCnt > 1.0e9)
            {
                cnt = number * 1.0e-9;
                stringCount = cnt.ToString(format) + (fullSuffix ? " billion" : "B");  // Billion
            }
            else if (absCnt > 1.0e6)
            {
                cnt = number * 1.0e-6;
                stringCount = cnt.ToString(format) + (fullSuffix ? " million" : "M");  // Million
            }
            else if (absCnt > 1.0e3)
            {
                cnt = number * 1.0e-3;
                stringCount = cnt.ToString(format) + (fullSuffix ? " thousand" : "k");  // Thousand
            }
            else if (absCnt > 0)
            {
                stringCount = number.ToString(format);
            }
            else if (absCnt > 1.0e-6)
            {
                cnt = number * 1.0e-3;
                stringCount = cnt.ToString(format) +(fullSuffix? " milli" : "m");
            }
            else {
                stringCount = number.ToString(format);
            }

            return stringCount;
        }


        /// <summary>
        /// Energy over time
        /// </summary>
        /// <param name="amountInKw"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string Power(double amountInKw, string format = "0.###")
        {
            string stringPower = "0 Kw";
            if (amountInKw >= 1e12)
            {
                amountInKw = amountInKw * 1e-12;
                stringPower = amountInKw.ToString(format) + " PW";
            }
            else if (amountInKw >= 1e9)
            {
                amountInKw = amountInKw * 1e-9;
                stringPower = amountInKw.ToString(format) + " TW";
            }
            else if (amountInKw >= 1e6)
            {
                amountInKw = amountInKw * 1e-6;
                stringPower = amountInKw.ToString(format) + " GW";
            }
            else if (amountInKw >= 1000)
            {
                amountInKw = amountInKw * 0.001;
                stringPower = amountInKw.ToString(format) + " MW";
            }
            else if (amountInKw > 0.1)
            {
                stringPower = amountInKw.ToString(format) + " KW";
            }
            else if (amountInKw > 0.001)
            {
                amountInKw = amountInKw * 1000;
                stringPower = amountInKw.ToString(format) + " W";
            }
            else if (amountInKw < 0.001)
            {
                amountInKw = amountInKw * 1e6;
                stringPower = amountInKw.ToString(format) + " mW";
            }

            else { stringPower = amountInKw.ToString(format) + " KW"; }

            return stringPower;
        }

        /// <summary>
        /// stored energy
        /// </summary>
        /// <param name="amountInKj"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string Energy(double amountInKj, string format = "0.###")
        {
            string stringPower = "0 Kj";
            if (amountInKj > 100000000)
            {
                amountInKj = amountInKj * 0.00000001;
                stringPower = amountInKj.ToString(format) + " GJ";
            }
            else if (amountInKj > 100000)
            {
                amountInKj = amountInKj * 0.001;
                stringPower = amountInKj.ToString(format) + " MJ";
            }
            else if (amountInKj < 0.1)
            {
                amountInKj = amountInKj * 1000;
                stringPower = amountInKj.ToString(format) + " J";
            }
            else if (amountInKj < 0.0001)
            {
                amountInKj = amountInKj * 1000000;
                stringPower = amountInKj.ToString(format) + " mJ";
            }

            else { stringPower = amountInKj.ToString(format) + " kJ"; }

            return stringPower;
        }

        // Tonne-based mass units, largest first. 1 tonne (T) = 1000 kg, then each step is
        // x1000: KT, MT, GT, TT, PT, ET, ZT, YT — enough to cover ship parts up to stellar masses.
        private static readonly (double ScaleInKg, string Unit)[] _massTiers =
        {
            (1e27, " YT"),
            (1e24, " ZT"),
            (1e21, " ET"),
            (1e18, " PT"),
            (1e15, " TT"),
            (1e12, " GT"),
            (1e9,  " MT"),
            (1e6,  " KT"),
            (1e3,  " T"),
        };

        /// <summary>
        /// Formats a mass in kilograms into a human-readable string with an appropriate SI prefix
        /// (e.g. "2.5 KT" for 2,500 kg, "4.1 MT" for 4,100,000 kg). For very small masses, it falls
        /// back to kg with the specified number of decimal places (e.g. "0.75 Kg"). Note that this
        /// method is not intended for formatting celestial body masses; use <see cref="CelestialMass"/> instead.
        /// </summary>
        /// <param name="amountInKg"> The mass in kilograms to format. </param>
        /// <param name="format"> The format string for the numeric value. </param>
        /// <returns> A human-readable string representing the mass with an appropriate SI prefix. </returns>
        public static string Mass(double amountInKg, string format = "0.###")
        {
            double absKg = Math.Abs(amountInKg);
            foreach (var (scaleInKg, unit) in _massTiers)
            {
                if (absKg >= scaleInKg)
                    return (amountInKg / scaleInKg).ToString(format) + unit;
            }
            return amountInKg.ToString(format) + " Kg";
        }

        private const double LunarMassKg = 7.342e22;
        private const double EarthMassKg = 5.972e24;
        private const double SolarMassKg = 1.989e30;

        /// <summary>
        /// Formats a celestial body's mass relative to a familiar reference body — Sol,
        /// Earth, or Luna — so the numbers stay human-readable (e.g. "0.11× Earths",
        /// "4.5× Lunas", "1× Sols") instead of astronomically large tonnages. Falls back to
        /// <see cref="Mass"/> for bodies too small to sensibly relate to Luna.
        /// </summary>
        public static string CelestialMass(double amountInKg, string format = "0.##")
        {
            double absKg = Math.Abs(amountInKg);
            if (absKg >= 0.1 * SolarMassKg)
                return (amountInKg / SolarMassKg).ToString(format) + "× Sols";
            if (absKg >= 0.1 * EarthMassKg)
                return (amountInKg / EarthMassKg).ToString(format) + "× Earths";
            if (absKg >= 0.001 * LunarMassKg)
                return (amountInKg / LunarMassKg).ToString(format) + "× Lunas";
            return Mass(amountInKg, format);
        }

        public static string Volume(double volume_m, string format = "0.###")
        {
            string stringVolume = "0 m^3";

            if (volume_m > 1.0e18)
            {
                volume_m = volume_m * 1.0e-18;
                stringVolume = volume_m.ToString(format) + " Em^3";
            }
            else if (volume_m > 1.0e15)
            {
                volume_m = volume_m * 1.0e-15;
                stringVolume = volume_m.ToString(format) + " Pm^3";
            }
            else if(volume_m > 1.0e12)
            {
                volume_m = volume_m * 1.0e-12;
                stringVolume = volume_m.ToString(format) + " Tm^3";
            }
            else if (volume_m > 1.0e9)
            {
                volume_m = volume_m * 1.0e-9;
                stringVolume = volume_m.ToString(format) + " Gm^3";
            }
            else if (volume_m > 1.0e6)
            {
                volume_m = volume_m * 1.0e-6;
                stringVolume = volume_m.ToString(format) + " Mm^3";
            }
            else if (volume_m > 1.0e3)
            {
                volume_m = volume_m * 1.0e-3;
                stringVolume = volume_m.ToString(format) + " Km^3";
            }
            else {
                stringVolume = volume_m.ToString(format) + " m^3";
            }

            return stringVolume;
        }

        public static string Area(double area_m2, string format = "0.###")
        {
            string stringArea = "0 m^2";

            if (area_m2 > 1.0e18)
            {
                area_m2 = area_m2 * 1.0e-18;
                stringArea = area_m2.ToString(format) + " Em^2";
            }
            else if (area_m2 > 1.0e15)
            {
                area_m2 = area_m2 * 1.0e-15;
                stringArea = area_m2.ToString(format) + " Pm^2";
            }
            else if(area_m2 > 1.0e12)
            {
                area_m2 = area_m2 * 1.0e-12;
                stringArea = area_m2.ToString(format) + " Tm^2";
            }
            else if (area_m2 > 1.0e9)
            {
                area_m2 = area_m2 * 1.0e-9;
                stringArea = area_m2.ToString(format) + " Gm^2";
            }
            else if (area_m2 > 1.0e6)
            {
                area_m2 = area_m2 * 1.0e-6;
                stringArea = area_m2.ToString(format) + " Mm^2";
            }
            else if (area_m2 > 1.0e3)
            {
                area_m2 = area_m2 * 1.0e-3;
                stringArea = area_m2.ToString(format) + " Km^2";
            }
            else {
                stringArea = area_m2.ToString(format) + " m^2";
            }

            return stringArea;
        }

        public static string VolumeLtr(double volume_m, string format = "0.###", bool fullSuffix = false)
        {
            string stringVolume = "0 Ltr";
            double volLtr = volume_m * 1000;
            if (volLtr > 1.0e18)
            {
                volLtr *= 1.0e-18;
                stringVolume = volLtr.ToString(format) + (fullSuffix ? " exalitre" : "EL");
            }
            else if (volLtr > 1.0e15)
            {
                volLtr *= 1.0e-15;
                stringVolume = volLtr.ToString(format) + (fullSuffix ? " petalitre" : "PL");
            }
            else if(volLtr > 1.0e12)
            {
                volLtr *= 1.0e-12;
                stringVolume = volLtr.ToString(format) + (fullSuffix ? " teralitre" : "TL");
            }
            else if (volLtr > 1.0e9)
            {
                volLtr *= 1.0e-9;
                stringVolume = volLtr.ToString(format) + (fullSuffix ? " gigalitre" : "ML");
            }
            else if (volLtr > 1.0e6)
            {
                volLtr *= 1.0e-6;
                stringVolume = volLtr.ToString(format) + (fullSuffix ? " megalitre" : "ML");
            }
            else if (volLtr > 1.0e3)
            {
                volLtr *= 1.0e-3;
                stringVolume = volLtr.ToString(format) + (fullSuffix ? " kilolitre" : "KL");
            }
            else {
                stringVolume = volLtr.ToString(format) + " Ltr";
            }

            return stringVolume;
        }

        public static string Distance(double length_m,  string format = "#,0.###")
        {

            string stringDistance = "0 m";
            double abslen = Math.Abs(length_m);
            double len;
            if (abslen > 149597870700.0 * 0.1)
            {
                len = length_m / 149597870700.0;
                stringDistance = len.ToString(format) + " AU";
            }
            else if (abslen > 1.0e3)
            {
                len = length_m * 0.001;
                stringDistance = len.ToString(format) + " Km";
            }
            else if (abslen > 0.1)
            {
                stringDistance = length_m.ToString(format) + " m";
            }
            else if (abslen > 0.001)
            {
                len = length_m * 100;
                stringDistance = len.ToString(format + "cm");
            }
            else
            {
                len = length_m * 1000;
                stringDistance = len.ToString(format + "mm");
            }

            return stringDistance;
        }

        public static string DistanceSmall(double length_nm,  string format = "0.###")
        {

            string stringDistance = "0 m";
            double abslen = Math.Abs(length_nm);
            double len;
            if (abslen > 1.0e9)
            {
                len = length_nm * 1.0e-9;
                stringDistance = len.ToString(format) + " m";
            }
            else if (abslen > 1.0e7)
            {
                len = length_nm * 1.0e-7;
                stringDistance = len.ToString(format) + " cm";
            }
            else if (abslen > 1.0e6)
            {
                len = length_nm * 1.0e-6;
                stringDistance = len.ToString(format) + " mm";
            }
            else if (abslen > 1.0e3)
            {
                len = length_nm * 0.001;
                stringDistance = len.ToString(format) + " um";
            }

            else
            {
                stringDistance = length_nm.ToString(format) + " nm";
            }


            return stringDistance;
        }
        public static string Velocity(double velocity_m, string format = "0.##")
        {
            string stringVelocity = " 0 m/s";
            if (velocity_m > 1.0e9)
            {
                velocity_m = velocity_m * 1.0e-9;
                stringVelocity = velocity_m.ToString(format) + " Gm/s";
            }
            else if (velocity_m > 1.0e6)
            {
                velocity_m = velocity_m * 1.0e-6;
                stringVelocity = velocity_m.ToString(format) + " Mm/s";
            }
            else if (velocity_m > 1.0e3)
            {
                velocity_m = velocity_m * 1.0e-3;
                stringVelocity = velocity_m.ToString(format) + " Km/s";
            }

            else { stringVelocity = velocity_m.ToString(format) + " m/s"; }

            return stringVelocity;
        }


        public static string Thrust(double thrust_n, string format = "0.00")
        {
            string stringThrust = " 0 KN";
            if (thrust_n > 1.0e9)
            {
                thrust_n = thrust_n * 1.0e-9;
                stringThrust = thrust_n.ToString(format) + " GN";
            }
            else if (thrust_n > 1.0e6)
            {
                thrust_n = thrust_n * 1.0e-6;
                stringThrust = thrust_n.ToString(format) + " MN";
            }
            else if (thrust_n > 1.0e3)
            {
                thrust_n = thrust_n * 1.0e-3;
                stringThrust = thrust_n.ToString(format) + " KN";
            }

            else { stringThrust = thrust_n.ToString(format) + " N"; }

            return stringThrust;
        }

    }
}