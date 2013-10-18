using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities.Components
{
    public class ShieldDefTN : ComponentDefTN
    {
        /// <summary>
        /// Shield strength tech associated with this shield.
        /// For absorption shields this will be absorption strength.
        /// </summary>
        private int ShieldStrength;
        public int shieldStrength
        {
            get { return ShieldStrength; }
        }

        /// <summary>
        /// Regeneration tech associated with this shield.
        /// for absorption shields this will be radiation rate.
        /// </summary>
        private int ShieldRegen;
        public int shieldRegen
        {
            get { return ShieldRegen; }
        }

        /// <summary>
        /// How much fuel this shield uses on a per Hour basis.
        /// </summary>
        private float FuelCostPerHour;
        public float fuelCostPerHour
        {
            get { return FuelCostPerHour; }
        }

        /// <summary>
        /// Modifier to shield fuel usage.
        /// </summary>
        private float FuelConsumptionMod;
        public float fuelConsumptionMod
        {
            get { return FuelConsumptionMod; }
        }

        /// <summary>
        /// Fully charged shield pool of this shield component.
        /// </summary>
        private float ShieldPool;
        public float shieldPool
        {
            get { return ShieldPool; }
        }

        /// <summary>
        /// How much a shield recharges, radiates over a 3(1) minute interval.
        /// </summary>
        private float ShieldGen;
        public float shieldGen
        {
            get { return ShieldGen; }
        }

        /// <summary>
        /// How much the shield regens/radiates in 5 seconds.
        /// </summary>
        private float ShieldGenPerTick;
        public float shieldGenPerTick
        {
            get { return ShieldGenPerTick; }
        }

        /// <summary>
        /// ShieldDefTN is the constructor for both regular shield definitions, and absorption shield definitions.
        /// </summary>
        /// <param name="ShieldName">Name of the shield.</param>
        /// <param name="StrTech">Strength/Absorption strength tech level.</param>
        /// <param name="RegenTech">Regeneration/Radiation tech level.</param>
        /// <param name="FuelCon">Fuel consumption modifier: 1.0-0.1</param>
        /// <param name="HS">Size. Regular Shields are always 1 HS, absorption shields may be up to 1000 HS in size(only 1 per ship however.)</param>
        /// <param name="TypeOfShield">Shield or AbsorptionShield only please.</param>
        public ShieldDefTN(string ShieldName, int StrTech, int RegenTech, float FuelCon, float HS, ComponentTypeTN TypeOfShield)
        {
            Id = Guid.NewGuid();
            componentType = TypeOfShield;

            ShieldStrength = StrTech;
            ShieldRegen = RegenTech;
            FuelConsumptionMod = FuelCon;

            Name = ShieldName;
            size = HS;

            if (TypeOfShield == ComponentTypeTN.Shield)
            {
                crew = 1;
                htk = 1;
                cost = 4 + Constants.ShieldTN.CostBase[ShieldStrength] + Constants.ShieldTN.CostBase[ShieldRegen];

                ShieldPool = Constants.ShieldTN.ShieldBase[ShieldStrength];
                ShieldGen = Constants.ShieldTN.ShieldBase[ShieldRegen];
                ShieldGenPerTick = ShieldGen / 60.0f;
            }
            else if (TypeOfShield == ComponentTypeTN.AbsorptionShield)
            {
                crew = 3;
                htk = (byte)Math.Round(size / 4.0f);

                ShieldPool = Constants.ShieldTN.ShieldBase[ShieldStrength] * 3.0f * size;
                ShieldGen = Constants.ShieldTN.ShieldBase[ShieldRegen] * 0.5f * size;
                ShieldGenPerTick =  ShieldGen / 12.0f;

                cost = (decimal)((2.0f * ShieldGen) + (2.0f * ShieldPool));
            }

            FuelCostPerHour = 10.0f * ShieldPool * FuelConsumptionMod;

            isMilitary = true;
            isObsolete = false;
            isSalvaged = false;
            isDivisible = false;

        }
    }

    public class ShieldTN : ComponentTN
    {
        /// <summary>
        /// definition for this shield component
        /// </summary>
        private ShieldDefTN ShieldDef;
        public ShieldDefTN shieldDef
        {
            get { return ShieldDef; }
        }

        /// <summary>
        /// Constructor for the individual shield component in each ship.
        /// </summary>
        /// <param name="definition">definition of the shield.</param>
        public ShieldTN(ShieldDefTN definition)
        {
            ShieldDef = definition;

            Name = definition.Name;

            isDestroyed = false;
        }
    }
}
