using System;

namespace Pulsar4X.ECSLib
{
    public struct ValueTypeStruct
    {
        public enum ValueTypes
        {
            Power,
            Distance,
            Volume, 
            Mass,
            Velocity,
            Force,
            Number,
        }

        public enum ValueSizes
        {
            Pico = -12,
            Nano = -9,
            Micro = -6,
            Milli = - 3,
            Centi = -2,
            Deci = -1,
            BaseUnit = 0,
            Deca = 1,
            Hecto = 2,
            Kilo = 3,
            Mega = 6,
            Giga = 9,
            Tera = 12,
            
        }

        public ValueTypes ValueType;
        public ValueSizes ValueSize;

        public ValueTypeStruct(ValueTypes type, ValueSizes size)
        {
            ValueType = type;
            ValueSize = size;
        }

    }
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
                    str = Number(amount * Math.Pow(10, (double)valueType.ValueSize), format);
                    break;

            }

            return str;
        }
        public static string Number(double number,  string format = "0.0##")
        {
            string stringCount = "0";
            double absCnt = Math.Abs(number);
            double cnt;
            if (absCnt > 1.0e12)
            {
                cnt = number * 1.0e-12;
                stringCount = cnt.ToString(format) + " T";
            }
            else if (absCnt > 1.0e9)
            {
                cnt = number * 1.0e-9;
                stringCount = cnt.ToString(format) + " G";
            }
            else if (absCnt > 1.0e6)
            {
                cnt = number * 1.0e-6;
                stringCount = cnt.ToString(format) + " k";
            }
            /*
            else if (absCnt > 1.0e3)
            {
                cnt = number * 1.0e-3;
                stringCount = cnt.ToString(format) + " h";
            }*/
            else if (absCnt > 1.0e-3)
            {
                stringCount = number.ToString(format);
            }

            
            else if (absCnt > 1.0e-6)
            {
                cnt = number * 1.0e-3;
                stringCount = cnt.ToString(format) + " m";
            }

            return stringCount;
        }

        public static string Quantity(double number, string format = "0.###", bool fullSuffix = false)
        {
            string stringCount = "0";
            double absCnt = Math.Abs(number);
            double cnt;
            if (absCnt > 1.0e15)
            {
                cnt = number * 1.0e-15;
                stringCount = cnt.ToString(format) + (fullSuffix ? " quadrillion" : "q");
            }
            else if (absCnt > 1.0e12)
            {
                cnt = number * 1.0e-12;
                stringCount = cnt.ToString(format) + (fullSuffix ? " trillion" : "t");  // Trillion
            }
            else if (absCnt > 1.0e9)
            {
                cnt = number * 1.0e-9;
                stringCount = cnt.ToString(format) + (fullSuffix ? " billion" : "b");  // Billion
            }
            else if (absCnt > 1.0e6)
            {
                cnt = number * 1.0e-6;
                stringCount = cnt.ToString(format) + (fullSuffix ? " million" : "m");  // Million
            }
            else if (absCnt > 1.0e3)
            {
                cnt = number * 1.0e-3;
                stringCount = cnt.ToString(format) + (fullSuffix ? " thousand" : "k");  // Thousand
            }
            else { 
                stringCount = number.ToString(format);
            }

            return stringCount;
        }


        public static string Power(double amountInKw, string format = "0.###")
        {
            string stringPower = "0 Kw";
            if (amountInKw > 100000000)
            {
                amountInKw = amountInKw * 0.00000001;
                stringPower = amountInKw.ToString(format) + " GW";
            }
            else if (amountInKw > 100000)
            {
                amountInKw = amountInKw * 0.00001;
                stringPower = amountInKw.ToString(format) + " MW";
            }
            else if (amountInKw < 0.1)
            {
                amountInKw = amountInKw * 1000;
                stringPower = amountInKw.ToString(format) + " W";
            }
            else if (amountInKw < 0.0001)
            {
                amountInKw = amountInKw * 1000000;
                stringPower = amountInKw.ToString(format) + " mW";
            }

            else { stringPower = amountInKw.ToString(format) + " kW"; }

            return stringPower;
        }
        
        public static string Mass(double amountInKg, string format = "0.###")
        {
            string stringMass = "0 Kg";
            if (amountInKg > 100000000)
            {
                amountInKg = amountInKg * 0.00000001;
                stringMass = amountInKg.ToString(format) + " MT";
            }
            else if (amountInKg > 100000)
            {
                amountInKg = amountInKg * 0.00001;
                stringMass = amountInKg.ToString(format) + " KT";
            }
            else if (amountInKg > 1000)
            {
                amountInKg = amountInKg * 0.001;
                stringMass = amountInKg.ToString(format) + " T";
            }

            else { stringMass = amountInKg.ToString(format) + " Kg"; }

            return stringMass;
        }

        public static string Volume(double volume_m, string format = "0.###")
        {
            string stringVolume = "0 m^3";

            if (volume_m > 1.0e9)
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

            else { stringVolume = volume_m.ToString(format) + " m^3";  }

            return stringVolume;
        }
        
        public static string Distance(double length_m,  string format = "0.###")
        {

            string stringDistance = "0 m";
            double abslen = Math.Abs(length_m);
            double len;
            if (abslen > 1.0e12)
            {
                len = length_m * 1.0e-12;
                stringDistance = len.ToString(format) + " GKm";
            }
            else if (abslen > 1.0e9)
            {
                len = length_m * 1.0e-9;
                stringDistance = len.ToString(format) + " MKm";
            }
            else if (abslen > 1.0e6)
            {
                len = length_m * 1.0e-6;
                stringDistance = len.ToString(format) + " KKm";
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