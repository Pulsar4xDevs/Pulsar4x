using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Pulsar4X.Entities.Components
{
    public class ComponentDefListTN
    {
        /// <summary>
        /// Crew Quarter components.
        /// </summary>
        public BindingList<GeneralComponentDefTN> CrewQuarters { get; set; }

        /// <summary>
        /// Fuel Tank components.
        /// </summary>
        public BindingList<GeneralComponentDefTN> FuelStorage { get; set; }

        /// <summary>
        /// Engineering bay components.
        /// </summary>
        public BindingList<GeneralComponentDefTN> EngineeringSpaces { get; set; }

        /// <summary>
        /// other specialized general components such as the bridge.
        /// </summary>
        public BindingList<GeneralComponentDefTN> OtherComponents { get; set; }

        /// <summary>
        /// Engine Components.
        /// </summary>
        public BindingList<EngineDefTN> Engines { get; set; }

        /// <summary>
        /// Passive Sensor components.
        /// </summary>
        public BindingList<PassiveSensorDefTN> PassiveSensorDef { get; set; }

        /// <summary>
        /// Default passive sensor suite that all ships must as a minimum possess.
        /// </summary>
        public PassiveSensorDefTN DefaultPassives { get; set; }

        /// <summary>
        /// Active Sensor Components.
        /// </summary>
        public BindingList<ActiveSensorDefTN> ActiveSensorDef { get; set; }

        /// <summary>
        /// Cargo holds,small, regular and perhaps larger variants.
        /// </summary>
        public BindingList<CargoDefTN> CargoHoldDef { get; set; }

        /// <summary>
        /// Cryogenic storage: standard, small, and emergency.
        /// </summary>
        public BindingList<ColonyDefTN> ColonyBayDef { get; set; }

        /// <summary>
        /// Cargo handling systems which decrease load times.
        /// </summary>
        public BindingList<CargoHandlingDefTN> CargoHandleSystemDef { get; set; }

        /// <summary>
        /// Beam fire controls, which are necessary to direct beam weapons.
        /// </summary>
        public BindingList<BeamFireControlDefTN> BeamFireControlDef { get; set; }

        /// <summary>
        /// All beam weapons which shoot other ships
        /// </summary>
        public BindingList<BeamDefTN> BeamWeaponDef { get; set; }

        /// <summary>
        /// All reactors which power beam weapons.
        /// </summary>
        public BindingList<ReactorDefTN> ReactorDef { get; set; }

        /// <summary>
        /// all defensive shield designs.
        /// </summary>
        public BindingList<ShieldDefTN> ShieldDef { get; set; }

        /// <summary>
        /// all launch tubes.
        /// </summary>
        public BindingList<MissileLauncherDefTN> MLauncherDef { get; set; }

        /// <summary>
        /// Magazines for carrying missiles.
        /// </summary>
        public BindingList<MagazineDefTN> MagazineDef { get; set; }

        /// <summary>
        /// Missile fire controls, the definition uses the same def as active sensors.
        /// </summary>
        public BindingList<ActiveSensorDefTN> MissileFireControlDef { get; set; }

        /// <summary>
        /// Number of the total components this faction has.
        /// </summary>
        public int TotalComponents { get; set; }

        /// <summary>
        /// ComponentDefList creates lists for every TN component. ClassTN and Faction should eventually both use this, but only faction does right now.
        /// </summary>
        public ComponentDefListTN()
        {
            TotalComponents = 0;

            CrewQuarters = new BindingList<GeneralComponentDefTN>();
            FuelStorage = new BindingList<GeneralComponentDefTN>();
            EngineeringSpaces = new BindingList<GeneralComponentDefTN>();
            OtherComponents = new BindingList<GeneralComponentDefTN>();

            Engines = new BindingList<EngineDefTN>();
            PassiveSensorDef = new BindingList<PassiveSensorDefTN>();
            ActiveSensorDef = new BindingList<ActiveSensorDefTN>();

            CargoHoldDef = new BindingList<CargoDefTN>();
            ColonyBayDef = new BindingList<ColonyDefTN>();
            CargoHandleSystemDef = new BindingList<CargoHandlingDefTN>();

            BeamFireControlDef = new BindingList<BeamFireControlDefTN>();
            BeamWeaponDef = new BindingList<BeamDefTN>();
            ReactorDef = new BindingList<ReactorDefTN>();

            ShieldDef = new BindingList<ShieldDefTN>();

            MLauncherDef = new BindingList<MissileLauncherDefTN>();
            MagazineDef = new BindingList<MagazineDefTN>();
            MissileFireControlDef = new BindingList<ActiveSensorDefTN>();

            DefaultPassives = new PassiveSensorDefTN("Default, Don't display this one.", 1.0f, 1, PassiveSensorType.Thermal, 1.0f, 1);
        }

        /// <summary>
        /// Every faction will start with some components defined and ready to use, though the engines and sensors shouldn't be here just yet.
        /// </summary>
        public void AddInitialComponents()
        {
            /// <summary>
            /// If components are removed here reduce this.
            /// </summary>
            TotalComponents = 39;

            GeneralComponentDefTN CrewQ = new GeneralComponentDefTN("Crew Quarters", 1.0f, 0, 10.0m, ComponentTypeTN.Crew);
            GeneralComponentDefTN CrewQS = new GeneralComponentDefTN("Crew Quarters - Small", 0.2f, 0, 2.0m, ComponentTypeTN.Crew);
            GeneralComponentDefTN FuelT = new GeneralComponentDefTN("Fuel Storage", 1.0f, 0, 10.0m, ComponentTypeTN.Fuel);
            GeneralComponentDefTN FuelTS = new GeneralComponentDefTN("Fuel Storage - Small", 0.2f, 0, 3.0m, ComponentTypeTN.Fuel);
            GeneralComponentDefTN EBay = new GeneralComponentDefTN("Engineering Spaces", 1.0f, 5, 10.0m, ComponentTypeTN.Engineering);
            GeneralComponentDefTN Bridge = new GeneralComponentDefTN("Bridge", 1.0f, 5, 10.0m, ComponentTypeTN.Bridge);

            CrewQuarters.Add(CrewQ);
            CrewQuarters.Add(CrewQS);
            FuelStorage.Add(FuelT);
            FuelStorage.Add(FuelTS);
            EngineeringSpaces.Add(EBay);
            OtherComponents.Add(Bridge);

            /// <summary>
            /// These components aren't really basic, but I'll put them in anyway for the time being.
            /// </summary>
            EngineDefTN EngDef = new EngineDefTN("25 EP Nuclear Thermal Engine", 5.0f, 1.0f, 1.0f, 1.0f, 1, 5, -1.0f);
            ActiveSensorDefTN ActDef = new ActiveSensorDefTN("Search 5M - 5000", 1.0f, 10, 5, 100, false, 1.0f, 1);
            PassiveSensorDefTN ThPasDef = new PassiveSensorDefTN("Thermal Sensor TH1-5", 1.0f, 5, PassiveSensorType.Thermal, 1.0f, 1);
            PassiveSensorDefTN EMPasDef = new PassiveSensorDefTN("EM Sensor EM1-5", 1.0f, 5, PassiveSensorType.EM, 1.0f, 1);
            BeamFireControlDefTN BFCDef = new BeamFireControlDefTN("Fire Control S01 10-1250", 0, 0, 1.0f, 1.0f, false, false, 1.0f, 1);
            BeamDefTN BeamDef = new BeamDefTN("10cm C1 Infrared Laser", ComponentTypeTN.Laser, 0, 0, 0, 1.0f);
            ReactorDefTN ReactDef = new ReactorDefTN("PWR S1 P2", 0, 1.0f, 1.0f);
            ShieldDefTN AShieldDef = new ShieldDefTN("Alpha R300/240 Shields", 0, 0, 1.0f, 1.0f, ComponentTypeTN.Shield);
            MissileLauncherDefTN TubeDef = new MissileLauncherDefTN("Size 1 Launcher", 1.0f, 1, false, 0);
            MagazineDefTN MagDef = new MagazineDefTN("Mag S1", 1.0f, 1, 0, 0, 1);
            ActiveSensorDefTN MFCDef = new ActiveSensorDefTN("Wasp I FC", 1.0f, 10, 5, 100, true, 1.0f, 1);


            Engines.Add(EngDef);
            ActiveSensorDef.Add(ActDef);
            PassiveSensorDef.Add(ThPasDef);
            PassiveSensorDef.Add(EMPasDef);

            BeamFireControlDef.Add(BFCDef);
            BeamWeaponDef.Add(BeamDef);
            ReactorDef.Add(ReactDef);

            ShieldDef.Add(AShieldDef);

            MLauncherDef.Add(TubeDef);
            MagazineDef.Add(MagDef);
            MissileFireControlDef.Add(MFCDef);


            /// <summary>
            /// Everyone starts with cargoholds.
            /// </summary>
            CargoDefTN CargoStandard = new CargoDefTN("Cargo Hold - Standard", 500.0f, 50.0m, 5);
            CargoDefTN CargoSmall = new CargoDefTN("Cargo Hold - Small", 100.0f, 12.5m, 2);

            CargoHoldDef.Add(CargoStandard);
            CargoHoldDef.Add(CargoSmall);

            /// <summary>
            /// Cryostorage is a TN only starting option. otherwise it must be researched.
            /// </summary>
            ColonyDefTN ColonyStandard = new ColonyDefTN("Cryogenic Transport", 50.0f, 100.0m, 10);
            ColonyDefTN ColonySmall = new ColonyDefTN("Cryogenic Transport - Small", 5.0f, 20.0m, 2);
            ColonyDefTN ColonyEmergency = new ColonyDefTN("Cryogenic Transport - Emergency", 1.0f, 5.0m, 0);

            ColonyBayDef.Add(ColonyStandard);
            ColonyBayDef.Add(ColonySmall);
            ColonyBayDef.Add(ColonyEmergency);


            /// <summary>
            /// Only TN starts begin with this component for now. the improved,advanced, and grav-assisted variants have to be researched.
            /// </summary>
            CargoHandlingDefTN CHS = new CargoHandlingDefTN("Cargo Handling System", 5);
            CargoHandleSystemDef.Add(CHS);


            /// <summary>
            /// Alpha build components:
            /// </summary>

            EngineDefTN AlphaEngine = new EngineDefTN("Ion Engine 120", 12.0f, 1.0f, 0.7f, 1.0f, 1, 10.0f, -1.0f);
            ActiveSensorDefTN AlphaBigSensor = new ActiveSensorDefTN("Search 181M - 10000", 10.0f, 16, 8, 200, false, 1.0f, 1);
            ActiveSensorDefTN AlphaSmallSensor = new ActiveSensorDefTN("Search 57M - 1000", 10.0f, 16, 8, 20, false, 1.0f, 1);
            ActiveSensorDefTN AlphaMissileSensor = new ActiveSensorDefTN("Search 1.4M - Missile", 10.0f, 16, 8, 1, false, 1.0f, 1);
            PassiveSensorDefTN AlphaEMSensor = new PassiveSensorDefTN("EM Detection Sensor EM10-80", 10.0f, 8, PassiveSensorType.EM, 1.0f, 1);
            PassiveSensorDefTN AlphaTHSensor = new PassiveSensorDefTN("TH Detection Sensor TH10-80", 10.0f, 8, PassiveSensorType.Thermal, 1.0f, 1);
            BeamFireControlDefTN AlphaFireControl = new BeamFireControlDefTN("Primary III FC R96K T6K", 2, 2, 4.0f, 2.0f, false, false, 1.0f, 1);

            BeamDefTN AlphaRailGun = new BeamDefTN("15cm Railgun V3/C3", ComponentTypeTN.Rail, 2, 2, 2, 1.0f);
            BeamDefTN AlphaLaser = new BeamDefTN("15cm C3 Near UV Laser", ComponentTypeTN.Laser, 2, 2, 2, 1.0f);
            BeamDefTN AlphaParticle = new BeamDefTN("PBW-4 150K", ComponentTypeTN.Particle, 2, 2, 2, 1.0f);
            BeamDefTN AlphaPlasma = new BeamDefTN("15cm C3 Plasma Beam", ComponentTypeTN.Plasma, 2, 2, 2, 1.0f);
            BeamDefTN AlphaMeson = new BeamDefTN("R9/C3 Meson Cannon", ComponentTypeTN.Meson, 2, 2, 2, 1.0f);
            BeamDefTN AlphaHPM = new BeamDefTN("R9/C3 Microwave", ComponentTypeTN.Microwave, 2, 2, 2, 1.0f);
            BeamDefTN AlphaGauss = new BeamDefTN("Gauss R3-100", ComponentTypeTN.Gauss, 2, 2, 2, 1.0f);

            ReactorDefTN AlphaReactor = new ReactorDefTN("GCFR S1 P4.5", 2, 1.0f, 1.0f);

            ShieldDefTN AlphaShield = new ShieldDefTN("Gamma R300/336 Shields", 2, 2, 0.7f, 1.0f, ComponentTypeTN.Shield);

            Engines.Add(AlphaEngine);

            ActiveSensorDef.Add(AlphaBigSensor);
            ActiveSensorDef.Add(AlphaSmallSensor);
            ActiveSensorDef.Add(AlphaMissileSensor);

            PassiveSensorDef.Add(AlphaEMSensor);
            PassiveSensorDef.Add(AlphaTHSensor);

            BeamFireControlDef.Add(AlphaFireControl);

            BeamWeaponDef.Add(AlphaRailGun);
            BeamWeaponDef.Add(AlphaLaser);
            BeamWeaponDef.Add(AlphaParticle);
            BeamWeaponDef.Add(AlphaPlasma);
            BeamWeaponDef.Add(AlphaMeson);
            BeamWeaponDef.Add(AlphaHPM);
            BeamWeaponDef.Add(AlphaGauss);

            ReactorDef.Add(AlphaReactor);

            ShieldDef.Add(AlphaShield);
        }

        /// <summary>
        /// Verify the Total Component value here as needed.
        /// </summary>
        public void SanityCheck()
        {
            TotalComponents = CrewQuarters.Count + FuelStorage.Count + EngineeringSpaces.Count + OtherComponents.Count + Engines.Count +
                              PassiveSensorDef.Count + ActiveSensorDef.Count + CargoHoldDef.Count + ColonyBayDef.Count + CargoHandleSystemDef.Count +
                              BeamFireControlDef.Count + BeamWeaponDef.Count + ReactorDef.Count + ShieldDef.Count + MLauncherDef.Count + MagazineDef.Count +
                              MissileFireControlDef.Count;

        }
    }
}
