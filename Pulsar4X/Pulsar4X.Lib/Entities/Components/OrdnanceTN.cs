using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Pulsar4X.Entities.Components
{
    public class MissileEngineDefTN : ComponentDefTN
    {
        /// <summary>
        /// The raw EP per HS of this engine.
        /// </summary>
        private float EngineBase;
        public float engineBase
        {
            get { return EngineBase; }
        }

        /// <summary>
        /// 0.1 - 6.0 modifier on engine base. missiles are double regular engines.
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
        /// Engine power determined by size, and base. Speed and thermal signature are derived from this.
        /// </summary>
        private ushort EnginePower;
        public ushort enginePower
        {
            get { return EnginePower; }
        }

        /// <summary>
        /// Thermal Signature is EnginePower
        /// </summary>
        private ushort ThermalSignature;
        public ushort thermalSignature
        {
            get { return ThermalSignature; }
        }

        /// <summary>
        /// How much this engine will burn through per hour.
        /// </summary>
        private float FuelConsumption;
        public float fuelConsumption
        {
            get { return FuelConsumption; }
        }

        /// <summary>
        /// Constructor for missile engine definitions/
        /// </summary>
        /// <param name="EngName">Name of this engine.</param>
        /// <param name="EngBase">Base technology.</param>
        /// <param name="EngPowMod">power modification to engine base. also affects fuel consumption. 0.1 to 6.0</param>
        /// <param name="FuelCon">Fuel consumption mod. 1.0 to 0.1</param>
        /// <param name="hs">size of engine, 0.01 to 5.0 if 6.3, 0.1 to 5.0 if 6.21. for the purposes of this function it doesn't care.</param>
        public MissileEngineDefTN(string EngName, float EngBase, float EngPowMod, float FuelCon, float msp)
        {
            Name = EngName;

            /// <summary>
            /// acceptable engine base numbers from conventional to photonic:
            /// float engineBase[13] = { 0.2,5,8,12,16,20,25,32,40,50,60,80,100 };
            /// </summary>
            EngineBase = EngBase;
            PowerMod = EngPowMod;
            FuelConsumptionMod = FuelCon;

            size = msp / 20.0f;


            EnginePower = (ushort)((EngBase * size) * PowerMod);
            ThermalSignature = EnginePower;

            //Int ((Engine Size in MSP / 5) ^ (-0.683))

            float FuelEPMod = (float)Math.Pow((msp / 5.0f), -0.683);
            FuelEPMod = FuelEPMod * 100.0f;
            FuelEPMod = (float)Math.Floor(FuelEPMod);
            FuelEPMod = FuelEPMod / 100.0f;

            FuelConsumption = (float)EnginePower * 5.0f * FuelEPMod * FuelConsumptionMod;

            FuelConsumption = FuelConsumption * 100.0f;
            FuelConsumption = (float)Math.Floor(fuelConsumption);
            FuelConsumption = FuelConsumption / 100.0f;


            isObsolete = false;
            cost = (decimal)(EnginePower / 4.0f);

            /// <summary>
            /// Ignore these:
            /// </summary>
            isSalvaged = false;
            isDivisible = false;
            isElectronic = false;
            isMilitary = true;
            crew = 0; 
            
        }
    }

    public class OrdnanceSeries
    {
        /// <summary>
        /// Just a list of all the missiles in this series. ships reloading use this as a hint for which missile to load.
        /// </summary>
        private BindingList<OrdnanceDefTN> MissilesInSeries;
        public BindingList<OrdnanceDefTN> missilesInSeries
        {
            get { return MissilesInSeries; }
        }

        /// <summary>
        /// Constructor for series.
        /// </summary>
        public OrdnanceSeries()
        {
            MissilesInSeries = new BindingList<OrdnanceDefTN>();
        }

        /// <summary>
        /// Adds a missile to the series
        /// </summary>
        /// <param name="Missile">missile to be added.</param>
        public void AddMissileToSeries(OrdnanceDefTN Missile)
        {
            MissilesInSeries.Add(Missile);
        }
    }

    public class OrdnanceDefTN : ComponentDefTN
    {
        /// <summary>
        /// Mines don't have to have any engine, this can be null.
        /// </summary>
        private MissileEngineDefTN OrdnanceEngine;
        public MissileEngineDefTN ordnanceEngine
        {
            get { return OrdnanceEngine; }
        }

        /// <summary>
        /// On the other hand missiles can have many engines.
        /// </summary>
        private int EngineCount;
        public int engineCount
        {
            get { return EngineCount; }
        }

        /// <summary>
        /// Total engine power of every engine on the missile.
        /// </summary>
        private int TotalEnginePower;
        public int totalEnginePower
        {
            get { return TotalEnginePower; }
        }

        /// <summary>
        /// Total thermal signature of the missile.
        /// </summary>
        private int TotalThermalSignature;
        public int totalThermalSignature
        {
            get { return TotalThermalSignature; }
        }

        /// <summary>
        /// Total Fuel consumption of the missile.
        /// </summary>
        private float TotalFuelConsumption;
        public float totalFuelConsumption
        {
            get { return TotalFuelConsumption; }
        }

        /// <summary>
        /// overall warhead strength of this missile.
        /// </summary>
        private int Warhead;
        public int warhead
        {
            get { return Warhead; }
        }

        /// <summary>
        /// radiation produced on planetary impact of this warhead.
        /// </summary>
        private int RadValue;
        public int radValue
        {
            get { return RadValue; }
        }

        /// <summary>
        /// Should this warhead use a laser template?
        /// </summary>
        private bool IsLaser;
        public bool isLaser
        {
            get { return IsLaser; }
        }

        /// <summary>
        /// MSPs worth of fuel this missile carries. 1 MPS = 2500 fuel.
        /// </summary>
        private float Fuel;
        public float fuel
        {
            get { return Fuel; }
        }

        /// <summary>
        /// Manueverability of this missile.
        /// </summary>
        private int Agility;
        public int agility
        {
            get { return Agility; }
        }

        /// <summary>
        /// Active strength of this missile.
        /// </summary>
        private float ActiveStr;
        public float activeStr
        {
            get { return ActiveStr; }
        }

        /// <summary>
        /// Active sensor resolution.
        /// </summary>
        private int Res;
        public int res
        {
            get { return Res; }
        }
        
        /// <summary>
        /// Ship active detection table.
        /// </summary>
        private ActiveSensorDefTN ASD;
        public ActiveSensorDefTN aSD
        {
            get { return ASD; }
        }

        /// <summary>
        /// Passive Thermal Strength
        /// </summary>
        private float ThermalStr;
        public float thermalStr
        {
            get { return ThermalStr; }
        }

        /// <summary>
        /// Thermal detection sensor
        /// </summary>
        private PassiveSensorDefTN THD;
        public PassiveSensorDefTN tHD
        {
            get { return THD; }
        }

        /// <summary>
        /// Passive EM Strength
        /// </summary>
        private float EMStr;
        public float eMStr
        {
            get { return EMStr; }
        }

        /// <summary>
        /// EM detection sensor;
        /// </summary>
        private PassiveSensorDefTN EMD;
        public PassiveSensorDefTN eMD
        {
            get { return EMD; }
        }

        /// <summary>
        /// geosurvey capability.
        /// </summary>
        private float GeoStr;
        public float geoStr
        {
            get { return GeoStr; }
        }

        /// <summary>
        /// Sensor reactor requirements. 5 points of sensor strength require 1 point of reactor strength.
        /// </summary>
        private float ReactorValue;
        public float reactorValue
        {
            get { return ReactorValue; }
        }

        /// <summary>
        /// actual space devoted to the reactor itself, determined by tech.
        /// </summary>
        private float ReactorMSP;
        public float reactorMSP
        {
            get { return ReactorMSP; }
        }

        /// <summary>
        /// ECM value of this missile, will be 10 to 100.
        /// </summary>
        private int ECMValue;
        public int eCMValue
        {
            get { return ECMValue; }
        }

        /// <summary>
        /// Armor represents the chance that a missile won't die to a particular weaponstrike. works the same as HTK.
        /// </summary>
        private int Armor;
        public int armor
        {
            get { return Armor; }
        }

        /// <summary>
        /// Size of this missile for detection purposes.
        /// </summary>
        private int DetectMSP;
        public int detectMSP
        {
            get { return DetectMSP; }
        }

        /// <summary>
        /// Secondary missiles to be released from this missile.
        /// </summary>
        private OrdnanceDefTN SubRelease;
        public OrdnanceDefTN subRelease
        {
            get { return SubRelease; }
        }

        /// <summary>
        /// number of secondary munitions that will be released.
        /// </summary>
        private int SubReleaseCount;
        public int subReleaseCount
        {
            get { return SubReleaseCount; }
        }

        /// <summary>
        /// Distance in KM secondaries are to be released.
        /// </summary>
        private float SubReleaseDistance;
        public float subReleaseDistance
        {
            get { return SubReleaseDistance; }
        }

        /// <summary>
        /// Series of this missile.
        /// </summary>
        private OrdnanceSeries OrdSeries;
        public OrdnanceSeries ordSeries
        {
            get { return OrdSeries; }
        }

        /// <summary>
        /// Ordnance Constructor.
        /// </summary>
        /// <param name="title">Name</param>
        /// <param name="Series">Series of missile</param>
        /// <param name="whMSP">Size devoted to Warhead</param>
        /// <param name="whTech">warhead tech</param>
        /// <param name="fuelMSP">Size devoted to fuel</param>
        /// <param name="AgilityMSP">Size devoted to agility</param>
        /// <param name="agilTech">Agility tech</param>
        /// <param name="activeMSP">Size devoted to actives</param>
        /// <param name="activeTech">Active tech</param>
        /// <param name="thermalMSP">Size devoted to thermal</param>
        /// <param name="thermalTech">thermal tech</param>
        /// <param name="emMSP">size devoted to EM</param>
        /// <param name="emTech">em tech</param>
        /// <param name="geoMSP">size devoted to geosurvey</param>
        /// <param name="geoTech">geo tech</param>
        /// <param name="aRes">Active sensor resolution</param>
        /// <param name="reactorTech">Reactor tech</param>
        /// <param name="armor">Armor amount</param>
        /// <param name="ECM">is ecm present?</param>
        /// <param name="ecmTech">ecm tech</param>
        /// <param name="enhanced">is warhead rad enhanced?</param>
        /// <param name="radTech">rad tech</param>
        /// <param name="laser">is laser warhead?</param>
        /// <param name="laserTech">laser tech</param>
        /// <param name="SubMunition">Secondary munition</param>
        /// <param name="SubMunitionCount">number of secondary munition</param>
        /// <param name="SeparationDist">release separation of secondary munition from target</param>
        /// <param name="Engine">Missile engine</param>
        /// <param name="missileEngineCount">number of missile engines</param>
        public OrdnanceDefTN(string title, OrdnanceSeries Series, 
                             float whMSP, int whTech, float fuelMSP, float AgilityMSP, int agilTech, 
                             float activeMSP, int activeTech, float thermalMSP, int thermalTech, float emMSP, int emTech, float geoMSP, int geoTech, int aRes, int reactorTech,
                             int armor, bool ECM, int ecmTech, bool enhanced, int radTech, bool laser, int laserTech, OrdnanceDefTN SubMunition, int SubMunitionCount,int SeparationDist,
                             MissileEngineDefTN Engine, int missileEngineCount)
        {
            /// <summary>
            /// Ignore these:
            /// </summary>
            crew = 0;
            isMilitary = true;
            isSalvaged = false;
            isDivisible = false;
            isElectronic = false;

            /// <summary>
            /// Size will be total MSP here rather than HS.
            /// </summary>
            size = 0;
            cost = 0;


            OrdSeries = Series;

            /// <summary>
            /// Warhead handling section.
            /// </summary>
            Warhead = (int)Math.Floor(whMSP * (float)Constants.OrdnanceTN.warheadTech[whTech]);
            RadValue = Warhead;
            if (enhanced == true)
            {
                Warhead = Warhead / Constants.OrdnanceTN.radTech[radTech];
                RadValue = RadValue * Constants.OrdnanceTN.radTech[radTech];

                laser = false;
            }
            else if (laser == true)
            {

                /// <summary>
                /// These weren't ever really implemented, so I'm not quite sure what to do with them. I'll have them use laser damage penetration and atmospheric reductions.
                /// </summary>
                Warhead = (int)Math.Floor(whMSP * (float)Constants.OrdnanceTN.laserTech[laserTech]);
                IsLaser = true;
                RadValue = 0; 

            }
            size = size + whMSP;

            /// <summary>
            /// Fuel handling Section.
            /// </summary>
            Fuel = fuelMSP;
            size = size + fuelMSP;


            /// <summary>
            /// Engine Handling.
            /// </summary>
            OrdnanceEngine = Engine;
            EngineCount = missileEngineCount;

            if (OrdnanceEngine != null)
            {
                TotalEnginePower = (int)(OrdnanceEngine.enginePower * EngineCount);
                TotalThermalSignature = TotalEnginePower;
                TotalFuelConsumption = (OrdnanceEngine.fuelConsumption * (float)EngineCount);


                size = size + (OrdnanceEngine.size * (float)EngineCount);
            }

            /// <summary>
            /// Agility Handling.
            /// </summary>
            Agility = (int)Math.Floor(AgilityMSP * (float)Constants.OrdnanceTN.agilityTech[agilTech]);
            size = size + AgilityMSP;

            
            /// <summary>
            /// Sensor Handling Section:
            /// </summary>
            ActiveStr = activeMSP * Constants.OrdnanceTN.activeTech[activeTech];
            size = size + activeMSP;
            Res = aRes;

            if (ActiveStr != 0.0f)
            {
                ASD = new ActiveSensorDefTN(ActiveStr, (byte)Math.Floor(Constants.OrdnanceTN.passiveTech[emTech] * 20.0f), Res);
            }

            ThermalStr = thermalMSP * Constants.OrdnanceTN.passiveTech[thermalTech];
            size = size + thermalMSP;

            if (ThermalStr != 0.0f)
            {
                THD = new PassiveSensorDefTN(ThermalStr, PassiveSensorType.Thermal);
            }

            EMStr = emMSP * Constants.OrdnanceTN.passiveTech[emTech];
            size = size + emMSP;

            if (EMStr != 0.0f)
            {
                EMD = new PassiveSensorDefTN(EMStr, PassiveSensorType.EM);
            }

            GeoStr = geoMSP * Constants.OrdnanceTN.geoTech[geoTech];
            size = size + geoMSP;

            ReactorValue = ((ActiveStr + ThermalStr + EMStr + GeoStr) / 5.0f);
            ReactorMSP = ReactorValue / Constants.OrdnanceTN.reactorTech[reactorTech];
            size = size + ReactorMSP;

            if (ECM == true)
                ECMValue = ecmTech * 10;

            Armor = armor;
            size = size + Armor;

            /// <summary>
            /// Do secondary missiles here.
            /// </summary>
            SubRelease = SubMunition;
            SubReleaseCount = SubMunitionCount;
            SubReleaseDistance = SeparationDist;

            /// <summary>
            /// now that the size of the missile is known, we can see what its detection characteristics should be.
            /// </summary>
            DetectMSP = (int)Math.Floor(size);

            if (DetectMSP <= 6)
            {
                DetectMSP = 0;
            }
            else if (DetectMSP >= 20)
            {
                DetectMSP = 14;
            }
            else
            {
                DetectMSP = DetectMSP - 6;
            }

            cost = cost + (decimal)((float)Warhead / 4.0f);
            cost = cost + (OrdnanceEngine.cost * EngineCount);
            cost = cost + (decimal)((Agility - 10) / 40.0f);
            cost = cost + (decimal)(ReactorValue * 3.0f);
            cost = cost + (decimal)(ThermalStr);
            cost = cost + (decimal)(EMStr);
            cost = cost + (decimal)(ActiveStr);
            cost = cost + (decimal)(GeoStr * 25.0f);
            cost = cost + (decimal)((float)Armor / 4.0f);
            cost = cost + (decimal)((float)ECMValue / 2.0f);
            cost = cost + (SubMunition.cost * SubMunitionCount);

            isObsolete = false;

            Series.AddMissileToSeries(this);
        }

        /// <summary>
        /// To hit calculation function for this missile type.
        /// </summary>
        /// <param name="targetSpeed">Speed in KM of the target.</param>
        /// <returns></returns>
        public int ToHit(float targetSpeed)
        {
            float manuever = 10.0f + (Agility / size);
            float speed = (float)TotalEnginePower * ( 1000.0f / ( size * 0.05f ) );
            int chance = (int)Math.Floor((speed / targetSpeed) * manuever);
            return chance;
        }
    }

    public class OrdnanceTN
    {
        /// <summary>
        /// Definition for this missile.
        /// </summary>
        private OrdnanceDefTN MissileDef;
        public OrdnanceDefTN missileDef
        {
            get { return MissileDef; }
        }

        /// <summary>
        /// If there are any submunitions, are they separated yet?
        /// </summary>
        private bool Separated;
        public bool separated
        {
            get { return Separated; }
        }

        /// <summary>
        /// Which group(these handle system map movement) is this ordnance associated with.
        /// </summary>
        private OrdnanceGroupTN MissileGroup;
        public OrdnanceGroupTN missileGroup
        {
            get { return MissileGroup; }
        }

        /// <summary>
        /// Missile Fire Control this missile is guided by.
        /// </summary>
        private MissileFireControlTN MFC;
        public MissileFireControlTN mFC
        {
            get { return MFC; }
            set { MFC = value; }
        }

        public OrdnanceTN(MissileFireControlTN mfCtrl, OrdnanceDefTN definition)
        {
            MFC = mfCtrl;

            MissileDef = definition;

            Separated = false;

            MissileGroup = new OrdnanceGroupTN();
        }

        /// <summary>
        /// Need functions to check various things such as whether a missile is destroyed by incoming fire, if it hits, damage used, and so on.
        /// </summary>
    }

    public class OrdnanceGroupTN : StarSystemEntity
    {
        /// <summary>
        /// Missiles in this "group" of missiles.
        /// </summary>
        private BindingList<OrdnanceTN> Missiles;
        public BindingList<OrdnanceTN> missiles
        {
            get { return Missiles; }
        }

        /// <summary>
        /// Useless mass override for StarSystem Entity.
        /// </summary>
        public override double Mass
        {
            get { return 0.0; }
            set { value = 0.0; }
        }

        public OrdnanceGroupTN()
        {
            Missiles = new BindingList<OrdnanceTN>();
        }
    }
}
