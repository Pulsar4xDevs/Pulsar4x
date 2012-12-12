using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;
using log4net;
using System.ComponentModel;

/// <summary>
/// The engine class defines the power, fuel consumption, thermal signatures, and other assorted characteristics that ship engines will have.
/// </summary>

//minimum cost is 5, civ costs are wierd.

namespace Pulsar4X.Entities.Components
{
    /// <summary>
    /// EngineDefTN defines the 6.0 engine class from aurora, though provisions will be made for hyperdrives.
    /// </summary>
    public class EngineDefTN
    {
        /// <summary>
        /// The user defined name of the engine.
        /// </summary>
        private string Name;
        public string name
        {
            get { return Name; }
        }

        /// <summary>
        /// The raw EP per HS of this engine.
        /// </summary>
        private byte EngineBase;
        public byte engineBase
        {
            get { return EngineBase; }
        }

        /// <summary>
        /// 0.1 - 3.0 modifier on engine base
        /// </summary>
        private float PowerMod;
        public float powerMod
        {
            get { return PowerMod; }
        }

        /// <summary>
        /// modifier on fuel use. 1.0 - 0.1
        /// </summary>
        private float FuelConsumptionMod;
        public float fuelConsumptionMod
        {
            get { return FuelConsumptionMod; }
        }

        /// <summary>
        /// Modifier on thermal signature. Sig = EP * reduction. 1.0 - 0.01.
        /// </summary>
        private float ThermalReduction;
        public float thermalReduction
        {
            get { return ThermalReduction; }
        }

        /// <summary>
        /// Size in HS of engine.
        /// </summary>
        private byte EngineSize;
        public byte engineSize
        {
            get { return EngineSize; }
        }

        /// <summary>
        /// Identifier for whether a hyperdrive is present, and how much size should be adjusted. 2.0-1.0
        /// </summary>
        private float HyperDriveMod;
        public float hyperDriveMod
        {
            get { return HyperDriveMod; }
        }

        /// <summary>
        /// Cost of the engine. 1/2EP * (0.25 * ThermalReductionTechLevel).
        /// </summary>
        private decimal Cost;
        public decimal cost
        {
            get { return Cost; }
        }

        /// <summary>
        /// Crew requirement for the engine
        /// </summary>
        private byte Crew;
        public byte crew
        {
            get { return Crew; }
        }

        /// <summary>
        /// likelyhood of an engine explosion due to damage
        /// </summary>
        private byte ExpRisk;
        public byte expRisk
        {
            get { return ExpRisk; }
        }

        /// <summary>
        /// Is this engine a military component? If base size is less than 25 or EPM is greater than 50. 
        /// </summary>
        private bool IsMilitary;
        public bool isMilitary
        {
            get { return IsMilitary; }
        }
        /// <summary>
        /// Engine power determined by size, and base. Speed and thermal signature are derived from this.
        /// </summary>
        private ushort EnginePower;
        public ushort enginePower
        {
            get { return EnginePower; }
        }

        /// <summary>
        /// Thermal Signature is EnginePower * ThermalReduction.
        /// </summary>
        private ushort ThermalSignature;
        public ushort thermalSignature
        {
            get { return ThermalSignature; }
        }

        /// <summary>
        /// How much fuel this engine will use per hour.
        /// </summary>
        private float FuelUsePerHour;
        public float fuelUsePerHour
        {
            get { return FuelUsePerHour; }
        }

        /// <summary>
        /// likelyhood of destruction by enemy fire.
        /// </summary>
        private byte HTK;
        public byte htk
        {
            get { return HTK; }
        }

        /// <summary>
        /// This constructor builds the engine definition based on the given input. no input checking is done here beyond that which the compiler might do.
        /// </summary>
        /// <param name="EngName">String identifier that will be displayed to the player for this engine</param>
        /// <param name="EngBase">Base power per HS, engine tech in other words.</param>
        /// <param name="EngPowMod">Power modifier determines power output and fuel consumption. Military drives are less than 0.5x Power.</param>
        /// <param name="FuelCon">Straight fuel usage reduction modifier.</param>
        /// <param name="ThmRed">Thermal reduction modifies thermal signature and cost. 1.0 to 0.01</param>
        /// <param name="ThmRedTech">The tech level of the thermal reduction modifier. 1 to 13</param>
        /// <param name="EngSize">Size of the engine determines size,power,and fuel consumption. Military drives are less than 25 HS.</param>
        /// <param name="HyperMod">If  this engine is hyper capable, and how much this modifies the size of the engine.</param>
        public EngineDefTN(string EngName, byte EngBase, float EngPowMod, float FuelCon, float ThmRed, byte ThmRedTech, byte EngSize, float HyperMod)
        {
            /// <summary>
            /// EngineDef stores all of these variables, so move them over.
            /// </summary>
            Name = EngName;
            EngineBase = EngBase;
            PowerMod = EngPowMod;
            FuelConsumptionMod = FuelCon;
            ThermalReduction = ThmRed;
            EngineSize = EngSize;
            HyperDriveMod = HyperMod;

            if (EngineSize < 25 || PowerMod > 0.5)
                IsMilitary = true;
            else if (EngineSize >= 25 && PowerMod <= 0.5)
                IsMilitary = false;

            /// <summary>
            /// The float typecast probably isn't necessary but I'll do it anyway.
            /// This is the overall engine power of the craft, and consequently its thermal signature as well.
            /// </summary>
            EnginePower = (ushort)((float)(EngineBase * EngineSize) * PowerMod);
            ThermalSignature = (ushort)((float)EnginePower * ThermalReduction);

            /// <summary>
            /// The power modifier adjusts fuel consumption by PowerMod^2.5
            /// Each HS of engineSize reduces fuel consumption by 1%.
            /// FuelUsePerHour is the product of engine power and these various modifiers.
            /// </summary>
            float fuelPowerMod = (float)Math.Pow((double)PowerMod,2.5);
            float EngineSizeMod = (float)(1.0 - ((double)EngineSize * 0.01));
            FuelUsePerHour = EnginePower * fuelPowerMod * FuelConsumptionMod * EngineSizeMod;

            /// <summary>
            /// HTK appears to be rounded up in most cases.
            /// </summary.
            HTK = (byte)Math.Ceiling(((double)EngineSize / 2.0));

            /// <summary>
            /// The Thermal reduction modifier is merely tech level * 0.25.
            /// Cost is 1/2 of EnginePower modified by thermal reduction tech.
            /// </summary>
            float ThermalReductionCostMod = (float)((float)(ThmRedTech-1) * 0.25f);
            Cost = (decimal)((ushort)(((float)EnginePower / 2.0f) * (float)ThermalReductionCostMod) + (ushort)(((float)EnginePower / 2.0f)));

            /// <summary>
            /// Cost may not dip below 5.
            /// </summary>
            if (Cost < 5)
                Cost = 5;

            /// <summary>
            /// Crew required is EngineSize * Power Mod with a minimum of 1 crewman required.
            /// </summary>
            Crew = (byte)((float)EngineSize * PowerMod);
            if (Crew < 1)
                Crew = (byte)1;

            /// <summary>
            /// Explosion Risk is a function of enginePower, with a 3xEngine of any size having a 30% chance of exploding,
            /// and a 0.1xEngine having a 1% chance respectively.
            /// </summary>
            ExpRisk = (byte)((double)PowerMod * 10.0);

            if (HyperDriveMod != -1.0)
            {
                EngineSize = (byte)((float)EngineSize * HyperDriveMod);
            }
        }
        /// <summary>
        /// End EngineDefTN()
        /// </summary>
    }
    /// <summary>
    /// End EngineDefTN Class
    /// </summary>

    /// <summary>
    /// EngineTN contains the relevant data for the engine component itself.
    /// </summary>
    public class EngineTN
    {
        /// <summary>
        /// EngineDef contains the data for this engine's class.
        /// </summary>
        private EngineDefTN EngineDef;
        public EngineDefTN engineDef
        {
            get { return EngineDef; }
        }

        /// <summary>
        /// Has this component taken damage sufficient to destroy it?
        /// </summary>
        private bool IsDestroyed;
        public bool isDestroyed
        {
            get { return IsDestroyed; }
        }

        /// <summary>
        /// This constructor initializes IsDestroyed and sets the definition for the engine.
        /// </summary>
        /// <param name="definition">Engine Class definition</param>
        public EngineTN(EngineDefTN definition)
        {
            EngineDef = definition;
            IsDestroyed = false;
        }
    }
    ///<summary>
    /// End EngineTN Class
    /// </summary>
}
