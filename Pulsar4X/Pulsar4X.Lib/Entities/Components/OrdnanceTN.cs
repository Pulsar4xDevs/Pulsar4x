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
        private float EnginePower;
        public float enginePower
        {
            get { return EnginePower; }
        }

        /// <summary>
        /// Thermal Signature is EnginePower
        /// </summary>
        private float ThermalSignature;
        public float thermalSignature
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
        /// <param name="hs">size of engine, 0.1 to 5.0 if 6.3, 0.1 to 5.0 if 6.21. for the purposes of this function it doesn't care.</param>
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


            EnginePower = ((EngBase * size) * PowerMod);
            ThermalSignature = EnginePower;
            if (ThermalSignature == 0.0f)
                ThermalSignature = 0.1f;

            /// <summary>
            /// Change this in Components later if it isn't 5.0
            /// Int ((Engine Size in MSP / 5) ^ (-0.683))
            /// Int ((Engine Size in MSP / 0.5) ^ (-0.683))??
            /// </summary>
            float SizeEPMod = (float)Math.Pow((msp / 5.0f), -0.683);
            SizeEPMod = SizeEPMod * 100.0f;
            SizeEPMod = (float)Math.Floor(SizeEPMod);
            SizeEPMod = SizeEPMod / 100.0f;

            float FuelEPM = (float)Math.Pow(PowerMod, 2.5f);

            FuelConsumption = (float)EnginePower * 5.0f * SizeEPMod * FuelConsumptionMod * FuelEPM;

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

    public class OrdnanceSeriesTN : GameEntity
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
        public OrdnanceSeriesTN(String Title)
        {
            Name = Title;
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
        private float TotalEnginePower;
        public float totalEnginePower
        {
            get { return TotalEnginePower; }
        }

        /// <summary>
        /// Total thermal signature of the missile.
        /// </summary>
        private float TotalThermalSignature;
        public float totalThermalSignature
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
        /// MSPs worth of fuel this missile carries. 1 MSP = 2500 fuel.
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
        private float Armor;
        public float armor
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
        private OrdnanceSeriesTN OrdSeries;
        public OrdnanceSeriesTN ordSeries
        {
            get { return OrdSeries; }
            set { OrdSeries = value; }
        }

        /// <summary>
        /// Speed in KM/s of this missile.
        /// </summary>
        private float MaxSpeed;
        public float maxSpeed
        {
            get { return MaxSpeed; }
        }

        /// <summary>
        /// Manueverability rating of this missile.
        /// </summary>
        private float Manuever;
        public float manuever
        {
            get { return Manuever; }
        }

        /// <summary>
        /// Warhead MSP value for the design page.
        /// </summary>
        private float WMSP;
        public float wMSP
        {
            get { return WMSP; }
        }

        /// <summary>
        /// Agility MSP value for the design page
        /// </summary>
        private float AgMSP;
        public float agMSP
        {
            get { return AgMSP; }
        }

        /// <summary>
        /// Active MSP value for design page.
        /// </summary>
        private float AcMSP;
        public float acMSP
        {
            get { return AcMSP; }
        }

        /// <summary>
        /// Thermal MSP value for design page.
        /// </summary>
        private float TMSP;
        public float tMSP
        {
            get { return TMSP; }
        }

        /// <summary>
        /// EM MSP value for design page.
        /// </summary>
        private float EMSP;
        public float eMSP
        {
            get { return EMSP; }
        }

        /// <summary>
        /// Geo MSP value for the design page. everything else can be derived.
        /// </summary>
        private float GMSP;
        public float gMSP
        {
            get { return GMSP; }
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
        public OrdnanceDefTN(string title, OrdnanceSeriesTN Series, 
                             float whMSP, int whTech, float fuelMSP, float AgilityMSP, int agilTech, 
                             float activeMSP, int activeTech, float thermalMSP, int thermalTech, float emMSP, int emTech, float geoMSP, int geoTech, ushort aRes, int reactorTech,
                             float armorMSP, float ECMMSP, int ecmTech, bool enhanced, int radTech, bool laser, int laserTech,MissileEngineDefTN Engine, int missileEngineCount,
                             OrdnanceDefTN SubMunition=null, int SubMunitionCount=0,int SeparationDist=-1)
        {
            /// <summary>
            /// Ignore these:
            /// </summary>
            Name = title;
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

            WMSP = whMSP;
            AgMSP = AgilityMSP;
            AcMSP = activeMSP;
            TMSP = thermalMSP;
            EMSP = emMSP;
            GMSP = geoMSP;

            /// <summary>
            /// Warhead handling section.
            /// </summary>
            Warhead = (int)Math.Floor(whMSP * (float)Constants.OrdnanceTN.warheadTech[whTech]);
            RadValue = Warhead;

            if (enhanced == true)
            {
                RadValue = Warhead * Constants.OrdnanceTN.radTech[radTech];
                Warhead = Warhead / Constants.OrdnanceTN.radTech[radTech];
                IsLaser = false;
            }
            else if (laser == true)
            {

                /// <summary>
                /// These weren't ever really implemented, so I'm not quite sure what to do with them. I'll have them use laser damage penetration and atmospheric reductions.
                /// </summary>
                Warhead = (int)Math.Floor(whMSP * (float)Constants.OrdnanceTN.laserTech[laserTech]);
                IsLaser = true;

                /// <summary>
                /// Laser warheads won't do radiation, but won't pierce atmosphere.
                /// </summary>
                RadValue = 0; 

            }
            size = size + whMSP;

            /// <summary>
            /// Fuel handling Section.
            /// </summary>
            Fuel = fuelMSP * 2500.0f;
            size = size + fuelMSP;


            /// <summary>
            /// Engine Handling.
            /// </summary>
            OrdnanceEngine = Engine;
            EngineCount = missileEngineCount;

            if (OrdnanceEngine != null)
            {
                TotalEnginePower = (OrdnanceEngine.enginePower * (float)EngineCount);
                TotalThermalSignature = (OrdnanceEngine.thermalSignature * (float)EngineCount);
                TotalFuelConsumption = (OrdnanceEngine.fuelConsumption * (float)EngineCount);

                /// <summary>
                /// Engine sizes are divided by 20 to make their formula work.
                /// </summary>
                size = size + (OrdnanceEngine.size * (float)EngineCount * 20.0f);
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

            if (ActiveStr != 0.0f)
            {
                ASD = new ActiveSensorDefTN(ActiveStr, (byte)Math.Floor(Constants.OrdnanceTN.passiveTech[emTech] * 20.0f), aRes);
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

            if (ECMMSP != 0.0f)
            {
                ECMValue = (ecmTech+1) * 10;
                size = size + ECMMSP;
            }

            Armor = armorMSP;
            size = size + Armor;

            /// <summary>
            /// Do secondary missiles here.
            /// </summary>
            /// 
            if (SubMunition != null)
            {
                SubRelease = SubMunition;
                SubReleaseCount = SubMunitionCount;
                SubReleaseDistance = SeparationDist;
            }
            else
            {
                SubRelease = null;
                SubReleaseCount = -1;
                SubReleaseDistance = -1;
            }

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
            if (OrdnanceEngine != null)
            {
                cost = cost + (OrdnanceEngine.cost * EngineCount);
            }
            cost = cost + (decimal)((Agility) / 50.0f);
            cost = cost + (decimal)(ReactorValue * 3.0f);
            cost = cost + (decimal)(ThermalStr);
            cost = cost + (decimal)(EMStr);
            cost = cost + (decimal)(ActiveStr);
            cost = cost + (decimal)(GeoStr * 25.0f);
            cost = cost + (decimal)((float)Armor / 4.0f);
            cost = cost + (decimal)((float)ECMValue / 2.0f);

            if (SubMunition != null)
            {
                size = size + (SubMunition.size * SubMunitionCount);
                cost = cost + (SubMunition.cost * SubMunitionCount);
            }

            isObsolete = false;

            MaxSpeed = (float)TotalEnginePower * (1000.0f / (size * 0.05f));

            if (MaxSpeed > Constants.OrdnanceTN.MaximumSpeed)
            {
                MaxSpeed = Constants.OrdnanceTN.MaximumSpeed;
            }
            
            /// <summary>
            /// Bombs dropped directly on target, or otherwise things I really don't want to move, but don't want to screw up TimeReq for.
            /// </summary>
            if (MaxSpeed == 0)
                MaxSpeed = 1; 

            Manuever = 10.0f + (Agility / size);

            if (Series != null)
            {
                OrdSeries = Series;
                Series.AddMissileToSeries(this);
            }
        }

        /// <summary>
        /// To hit calculation function for this missile type.
        /// </summary>
        /// <param name="targetSpeed">Speed in KM of the target.</param>
        /// <returns></returns>
        public int ToHit(float targetSpeed)
        {
            int chance = (int)Math.Floor((MaxSpeed / targetSpeed) * Manuever);
            return chance;
        }
    }

    public class OrdnanceTargetTN
    {
        /// <summary>
        /// Missiles can be fired at many potential targets, hence falling back to the SSE
        /// </summary>
        private StarSystemEntityType TargetType;
        public StarSystemEntityType targetType
        {
            get { return TargetType; }
            set { TargetType = value; }
        }

        /// <summary>
        /// Ship killer ordnance usually.
        /// </summary>
        private ShipTN Ship;
        public ShipTN ship
        {
            get { return Ship; }
        }

        /// <summary>
        /// Survey typically.
        /// </summary>
        private Planet Body;
        public Planet body
        {
            get { return body; }
        }

        /// <summary>
        /// Planetary bombardment typically.
        /// </summary>
        private Population Pop;
        public Population pop
        {
            get { return Pop; }
        }

        /// <summary>
        /// Waypoint target, Drones, sensor missiles, and mines will make use of this for now.
        /// specialized minelaying code may be put in later.
        /// </summary>
        private Waypoint WP;
        public Waypoint wp
        {
            get { return WP; }
        }

        /// <summary>
        /// The target for this ordnance group is another ordnance group.
        /// </summary>
        private OrdnanceGroupTN MissileGroup;
        public OrdnanceGroupTN missileGroup
        {
            get { return MissileGroup; }
        }

        /// <summary>
        /// Constructor for ship targets.
        /// </summary>
        /// <param name="ShipTarget">Ship that will be the target</param>
        public OrdnanceTargetTN(ShipTN ShipTarget)
        {
            TargetType = ShipTarget.ShipsTaskGroup.SSEntity;
            Ship = ShipTarget;
        }

        /// <summary>
        /// Constructor for planetary targets.
        /// </summary>
        /// <param name="BodyTarget">Body which is the target</param>
        public OrdnanceTargetTN(Planet BodyTarget)
        {
            TargetType = BodyTarget.SSEntity;
            Body = BodyTarget;
        }

        /// <summary>
        /// Constructor for population targets.
        /// </summary>
        /// <param name="PopTarget">Population that is targeted.</param>
        public OrdnanceTargetTN(Population PopTarget)
        {
            TargetType = StarSystemEntityType.Population;
            Pop = PopTarget;
        }

        /// <summary>
        /// Constructor for Waypoint targets.
        /// </summary>
        /// <param name="WPTarget">waypoint to be targeted.</param>
        public OrdnanceTargetTN(Waypoint WPTarget)
        {
            TargetType = WPTarget.SSEntity;
            WP = WPTarget;
        }

        /// <summary>
        /// Constructor for Missile group targets.
        /// </summary>
        /// <param name="OGTarget">missile group that this group is targeted on.</param>
        public OrdnanceTargetTN(OrdnanceGroupTN OGTarget)
        {
            TargetType = OGTarget.SSEntity;
            MissileGroup = OGTarget;
        }
    }

    public class OrdnanceTN : GameEntity
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
            set { MissileGroup = value; }
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

        /// <summary>
        /// Target this missile is headed for.
        /// </summary>
        private OrdnanceTargetTN Target;
        public OrdnanceTargetTN target
        {
            get { return Target; }
            set { Target = value; }
        }

        /// <summary>
        /// which ordnance group is handling the movement for this missile(and others)
        /// </summary>
        private OrdnanceGroupTN OrdGroup;
        public OrdnanceGroupTN ordGroup
        {
            get { return OrdGroup; }
            set { OrdGroup = value; }
        }

        /// <summary>
        /// Fuel the missile has to use.
        /// </summary>
        private float Fuel;
        public float fuel
        {
            get { return Fuel; }
            set { Fuel = value; }
        }

        /// <summary>
        /// Which ship fired this missile?
        /// </summary>
        private ShipTN FiringShip;
        public ShipTN firingShip
        {
            get { return FiringShip; }
            set { FiringShip = value; }
        }

        /// <summary>
        /// Constructor for missiles.
        /// </summary>
        /// <param name="mfCtrl">MFC directing this missile.</param>
        /// <param name="definition">definition of the missile.</param>
        public OrdnanceTN(MissileFireControlTN mfCtrl, OrdnanceDefTN definition, ShipTN ShipFiredFrom)
        {
            Name = definition.Name;
            Id = Guid.NewGuid();

            MFC = mfCtrl;

            Target = MFC.target;

            FiringShip = ShipFiredFrom;

            MissileDef = definition;

            /// <summary>
            /// Litres of fuel available to this missile.
            /// </summary>
            Fuel = definition.fuel * 2500.0f;

            Separated = false;
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

        /// <summary>
        /// The contact for this missile.
        /// </summary>
        private SystemContact Contact;
        public SystemContact contact
        {
            get { return Contact; }
        }

        /// <summary>
        /// Heading this missilegroup is travelling on.
        /// </summary>
        private float CurrentHeading;
        public float currentHeading
        {
            get { return CurrentHeading; }
        }

        /// <summary>
        /// Distance X component.
        /// </summary>
        private float dX;
        public float dx
        {
            get { return dX; }
        }

        /// <summary>
        /// Distance Y component.
        /// </summary>
        private float dY;
        public float dy
        {
            get { return dY; }
        }

        /// <summary>
        /// Speed X component.
        /// </summary>
        private float CurrentSpeedX;
        public float currentSpeedX
        {
            get { return CurrentSpeedX; }
        }

        /// <summary>
        /// Speed Y component.
        /// </summary>
        private float CurrentSpeedY;
        public float currentSpeedY
        {
            get { return CurrentSpeedY; }
        }

        /// <summary>
        /// Time to impact.
        /// </summary>
        private uint TimeReq;
        public uint timeReq
        {
            get { return TimeReq; }
        }

        /// <summary>
        /// The faction these missiles belong to.
        /// </summary>
        private Faction OrdnanceGroupFaction;
        public Faction ordnanceGroupFaction
        {
            get { return OrdnanceGroupFaction; }
        }

        /// <summary>
        /// Controls whether or not the travel line will be set. 0 means draw the line, 1 means set last position to current position. 2 means that last position has been set to current position.
        /// 3 means that the line has been updated on the system map as being not drawn, so do not display it again.
        /// This is referenced in ContactElement and maybe simentity,SystemMap as well as in OrdnanceTN(the TG version is referenced in Taskgroup).
        /// </summary>
        private byte _DrawTravelLine;
        public byte DrawTravelLine 
        {
            get { return _DrawTravelLine; }
            set { _DrawTravelLine = value; } 
        }
        
        /// <summary>
        /// Constructor for missile groups.
        /// </summary>
        /// <param name="LaunchedFrom">TG this launched from. additional missiles may be added this tick but afterwards no more.</param>
        /// <param name="Missile">Initial missile that prompted the creation of this ordnance group.</param>
        /// <param name="MissileTarget">The target this group is aimed at.</param>
        public OrdnanceGroupTN(TaskGroupTN LaunchedFrom, OrdnanceTN Missile)
        {
            Name = String.Format("Ordnance Group #{0}", LaunchedFrom.TaskGroupFaction.MissileGroups.Count);

            if (LaunchedFrom.IsOrbiting == true)
            {
                LaunchedFrom.GetPositionFromOrbit();
            }

            XSystem = LaunchedFrom.Contact.XSystem;
            YSystem = LaunchedFrom.Contact.YSystem;

            Contact = new SystemContact(LaunchedFrom.TaskGroupFaction, this);

            Missiles = new BindingList<OrdnanceTN>();
            Missiles.Add(Missile);
            Missile.ordGroup = this;

            SSEntity = StarSystemEntityType.Missile;

            OrdnanceGroupFaction = LaunchedFrom.TaskGroupFaction;

            Contact.CurrentSystem = LaunchedFrom.Contact.CurrentSystem;
            LaunchedFrom.Contact.CurrentSystem.AddContact(Contact);
            DrawTravelLine = 0;
        }

        /// <summary>
        /// Adds a missile to the ordnance group.
        /// </summary>
        /// <param name="Missile">Missile to add, think of this as the ShipTN to OrdnanceGroupTN's TaskGroupTN</param>
        public void AddMissile(OrdnanceTN Missile)
        {
            Missiles.Add(Missile);

            /// <summary>
            /// When missile detection stats are revisited, they'll have to be updated here and in the constructor.
            /// </summary>
        }

        /// <summary>
        /// Direction this missile is headed.
        /// </summary>
        public void GetHeading()
        {
            /// <summary>
            /// Add others here for planets, populations, other missile groups
            /// </summary>
            switch (Missiles[0].target.targetType)
            {
                case StarSystemEntityType.TaskGroup:
                    dX = (float)(Contact.XSystem - Missiles[0].target.ship.ShipsTaskGroup.Contact.XSystem);
                    dY = (float)(Contact.YSystem - Missiles[0].target.ship.ShipsTaskGroup.Contact.YSystem);
                break;
            }


            CurrentHeading = (float)(Math.Atan((dY / dX)) / Constants.Units.RADIAN);
        }

        /// <summary>
        /// directional speed of this missilegroup.
        /// </summary>
        public void GetSpeed()
        {
            float sign = 1.0f;
            if (dX > 0.0f)
            {
                sign = -1.0f;
            }

            /// <summary>
            /// minor matrix multiplication here.
            /// </summary>
            CurrentSpeedX = (float)(Missiles[0].missileDef.maxSpeed * Math.Cos(CurrentHeading * Constants.Units.RADIAN) * sign);
            CurrentSpeedY = (float)(Missiles[0].missileDef.maxSpeed * Math.Sin(CurrentHeading * Constants.Units.RADIAN) * sign);
        }

        /// <summary>
        /// How long will this order take given the missileGroup's current speed?
        /// </summary>
        public void GetTimeRequirement()
        {
            float dZ = (float)Math.Sqrt(((dX * dX) + (dY * dY)));
            double MissileSpeedInAU = 0.0;

            if (dZ >= Constants.Units.MAX_KM_IN_AU)
            {
                double Count = dZ / Constants.Units.MAX_KM_IN_AU;

                /// <summary>
                /// TimeRequirement is safe to calculate.
                /// </summary>
                if (Count < (double)Missiles[0].missileDef.maxSpeed)
                {
                    MissileSpeedInAU = (double)Missiles[0].missileDef.maxSpeed / Constants.Units.KM_PER_AU;
                    TimeReq = (uint)Math.Ceiling((dZ / MissileSpeedInAU));
                }
                else
                {
                    /// <summary>
                    /// even though TimeReq is a uint I'll treat it as a "signed" int in this case.
                    /// </summary>
                    TimeReq = 2147483649;
                    MissileSpeedInAU = (double)Missiles[0].missileDef.maxSpeed / Constants.Units.KM_PER_AU;
                }
            }
            else
            {
                MissileSpeedInAU = (double)Missiles[0].missileDef.maxSpeed / Constants.Units.KM_PER_AU;
                TimeReq = (uint)Math.Ceiling((dZ / MissileSpeedInAU));
            }
        }

        /// <summary>
        /// I need to move everyone to their target in this function.
        /// TODO:Upon missiles reaching a waypoint, or geo survey target travelline must be set to 1.
        /// </summary>
        public int ProcessOrder(uint TimeSlice, Random RNG)
        {
            int MissilesToDestroy = -1;
            GetHeading();
            GetSpeed();
            GetTimeRequirement();

            if (TimeReq < TimeSlice)
            {
                /// <summary>
                /// Use fuel and either impact missile or bring missile to halt.
                /// </summary>
                Contact.LastXSystem = Contact.XSystem;
                Contact.LastYSystem = Contact.YSystem;

                switch(Missiles[0].target.targetType)
                {
                    case StarSystemEntityType.TaskGroup:
                        Contact.XSystem = Missiles[0].target.ship.ShipsTaskGroup.Contact.XSystem;
                        Contact.YSystem = Missiles[0].target.ship.ShipsTaskGroup.Contact.YSystem;

                        CheckFuel(TimeSlice);

                        /// <summary>
                        /// Impact time. Mark every missile for destruction now, but later if some missiles turn out to have survived, set MissilesToDestroy to the last missile responsible for the
                        /// ship kill.
                        /// </summary>
                        MissilesToDestroy = Missiles.Count - 1;
                        
                        for (int loop = 0; loop < Missiles.Count; loop++)
                        {
                            ushort ToHit = 0;

                            if (Missiles[loop].target.ship.ShipsTaskGroup.CurrentSpeed == 1 || Missiles[loop].target.ship.ShipsTaskGroup.CurrentSpeed == 0)
                                ToHit = 100;
                            else
                               ToHit = (ushort)((Missiles[loop].missileDef.maxSpeed / (float)Missiles[loop].target.ship.ShipsTaskGroup.CurrentSpeed) * Missiles[loop].missileDef.manuever);

                            ushort HitChance = (ushort)RNG.Next(1, 100);

                            if (ToHit > HitChance)
                            {
                                String Entry = String.Format("Missile {0} #{1} in Missile Group {2} Hit.", Missiles[loop].Name, loop, Name);
                                MessageEntry Msg = new MessageEntry(MessageEntry.MessageType.MissileMissed, Contact.CurrentSystem, Contact, GameState.Instance.GameDateTime,
                                                                   (GameState.SE.CurrentTick - GameState.SE.lastTick), Entry);
                                OrdnanceGroupFaction.MessageLog.Add(Msg);

                                ushort Columns = Missiles[loop].target.ship.ShipArmor.armorDef.cNum;

                                ushort location = (ushort)RNG.Next(0, Columns);

                                ///<summary>
                                ///Missile damage type always? laser damage type if implemented will need to change this.
                                ///</summary>
                                bool ShipDest = Missiles[loop].target.ship.OnDamaged(DamageTypeTN.Missile, (ushort)Missiles[loop].missileDef.warhead, location, Missiles[loop].firingShip);

                                /// <summary>
                                /// Handle ship destruction at the ship level, to inform all incoming missiles that they need a new target.
                                /// </summary>
                                if (ShipDest == true)
                                {
                                    MissilesToDestroy = loop;
                                    break;
                                }
                            }
                            else
                            {
                                String Entry = String.Format("Missile {0} #{1} in Missile Group {2} Missed.", Missiles[loop].Name, loop, Name);
                                MessageEntry Msg = new MessageEntry(MessageEntry.MessageType.MissileMissed, Contact.CurrentSystem, Contact, GameState.Instance.GameDateTime,
                                                                   (GameState.SE.CurrentTick - GameState.SE.lastTick), Entry);
                                OrdnanceGroupFaction.MessageLog.Add(Msg);
                            }
                        }
                    break;
                }
                
            }
            else if(Missiles[0].missileDef.ordnanceEngine != null)
            {
                /// <summary>
                /// Move missile closer to its target. provided of course that it has an engine.
                /// </summary>
                Contact.LastXSystem = Contact.XSystem;
                Contact.LastYSystem = Contact.YSystem;

                Contact.XSystem = Contact.XSystem + ((double)(TimeSlice * CurrentSpeedX) / Constants.Units.KM_PER_AU);
                Contact.YSystem = Contact.YSystem + ((double)(TimeSlice * CurrentSpeedY) / Constants.Units.KM_PER_AU);

                CheckFuel(TimeSlice);

                /// <summary>
                /// This probably isn't needed since timeReqs are constantly recalculated.
                /// </summary>
                TimeReq = TimeReq - TimeSlice;

                TimeSlice = 0;
            }

            return MissilesToDestroy;
        }


        /// <summary>
        /// Missiles which have used up their fuel should be destroyed if they run out under the right circumstances.
        /// </summary>
        /// <param name="TimeSlice">Time advancement to check for missile fuel usage.</param>
        public void CheckFuel(uint TimeSlice)
        {
            for (int loop = 0; loop < Missiles.Count; loop++)
            {
                float hourPer = TimeSlice / 3600.0f;

                Missiles[loop].fuel = Missiles[loop].fuel - (Missiles[loop].missileDef.totalFuelConsumption * hourPer);

                if (Missiles[loop].fuel <= 0.0f)
                {
                    String Entry = String.Format("Missile {0} #{1} in Missile Group {2} has run out of fuel.", Missiles[loop].Name, loop, Name);
                    MessageEntry Msg = new MessageEntry(MessageEntry.MessageType.MissileOutOfFuel, Contact.CurrentSystem, Contact, GameState.Instance.GameDateTime,
                                                       (GameState.SE.CurrentTick - GameState.SE.lastTick), Entry);
                    OrdnanceGroupFaction.MessageLog.Add(Msg);

                    Missiles.RemoveAt(loop);
                    /// <summary>
                    /// Since Missiles.Count just got decremented, decrement the loop as well.
                    /// </summary>
                    loop--;
                }
            }        
        }
    }
}
