using System;

namespace Pulsar4X.Events
{
    public static class EventTypeHelper
    {
        public static EventType GetAllEventTypes()
        {
            EventType all = EventType.NoEventType;

            foreach(EventType eventType in Enum.GetValues(typeof(EventType)))
            {
                all |= eventType;
            }

            return all;
        }
    }

    //Taken from Aurora's 7.1 Stevefire.mdb database.
    public enum EventType
    {
        NoEventType = 0,

        //gameEvents
        SystemDateChange = 1 << 0,
        GlobalDateChange = 1 << 1,
        EntityDestroyed = 1 << 2,

        #region GameErrors

        DataParseError,

        #endregion

        #region Shipyard Events

        ShipConstructionBegan,
        ShipConstructionAborted,
        ShipConstructionCompleted,

        ShipRepairBegan,
        ShipRepairAborted,
        ShipRepairCompleted,

        ShipRefitBegan,
        ShipRefitCompleted,

        ShipOverhaulBegan,
        ShipOverhaulCompleted,

        ShipScrappingBegan,
        ShipScrappingCompleted,

        ShipyardCapacityExpanded,
        ShipyardSlipwayAdded,

        ShipyardSlipwayDestroyed,
        ShipyardDestroyed,

        #endregion

        #region Population Events

        PopulationBombarded,
        POWRescued, // Friendly POW's returned to population

        // Political Status Events
        PopulationSurrendered,
        PopStatusChanged,
        UnrestIncreased,
        UnrestDecreased,

        // Mining Events
        MineralExhausted,
        AllMineralsExhausted,
        MineralsLocated,
        GeoSurveyCompleted,

        // Research Events
        ResearchStarted,
        ResearchCompleted,
        InactiveLab,
        OverallocationOfLabs,

        // Industry Events
        ProductionStarted,
        ProductionCompleted,
        ProductionQueueEmpty,
        MineralShortage,
        ComponentsScrapped,
        FightersScrapped,
        MissilesScrapped,
        FuelShortage,
        CMCEstablished, // Civilian Mining Colony

        // Ground Unit Events
        UnitTrainingStarted,
        UnitTrainingCompleted,

        GroundForcesUnderAttack,
        GroundUnitLost,
        GroundForcesAttackedEnemy,
        EnemyUnitDestroyed,

        GroundUnitDelivered,
        GroundUnitMoraleIncreased,
        GroundUnitMoraleDecreased,

        ReplacementsExpended,

        // Environment Events
        TerraformingCompleted,
        TerraformingReport,
        IceSheetMelted,
        BreathableAtmosphere,
        RadiationIncreased,

        // Xenology Events
        RuinsLocated,
        AnomalyDiscovered,
        RuinsExploited,

        MineRestored,
        FactoryRestored,
        TechDiscovered,
        RoboticGuardians,
        TechDataLearned,

        #endregion

        #region Leader events

        TeamSkillIncreased,
        TeamSkillDecreased,
        TeamNotFull,

        OfficerUpdate,
        OfficerPromoted,
        OfficerHealth,

        LeaderPickedUp,
        LeaderPickupFailed,
        LeaderDroppedOff,
        LeaderDropoffFailed,
        CommandAssignment,

        OutstandingNewOfficer,
        ExceptionalNewOfficer,
        PromisingNewOfficer,
        NewOfficer,

        NewAdministrator,
        NewScientist,

        #endregion

        #region Ship Combat

        ShipSurrender,
        CrewGradeIncrease,

        // Targeting Events
        Targeting,
        TargetingProblemm,
        ChanceToHit,
        TargetHit,
        TargetMissed,
        NoMissileAssigned,
        FireDelay,
        FiringSummary,
        MissilesDestroyed,
        MissileIntercepted,
        InterceptionSummary,
        MissileSelfDestruct,

        // Weapon Events
        WeaponRecharging,
        WeaponReloading,
        SecondStageRelease,
        MissileLaunch,
        OutOfAmmo,

        // Shield Events
        ShieldsInactive,
        ShieldsDown,
        ShieldRechage,
        ShieldFailure,
        ShieldDeactivation,

        // Damage Events
        DamageAbsorbed,
        SystemDestroyed,
        SystemIntact,
        SecondayExplosion,
        ShipDestroyed,
        Damage,
        ShieldDamage,
        ShipSlowed,
        TargetDestroyed,
        SystemFailureAlert,
        SystemDamaged,
        SystemFailure,
        PowerExplosionDetected,
        MgExplosionDetected,
        ShieldExpDetected,
        InternalDamage,
        RammingAttempt,
        ShockDamage,
        EnemyShipDestroyed,
        ShieldPenetrated,

        // Boarding/Troop Deployment
        BoardingAttempt,
        BoardingCombat,
        CrewLosses,
        ShipCaptured,
        PrisonersTaken,
        CombatDrop,



        #endregion

        //IndustryEvents
        Storage,

        // Diplomatic Events
        TreatyAgreed,
        TechExchange,
        Communication,
        Diplomacy,
        Reparations,

        // Ship State Events
        InsufficientFuel,
        UnableToLand,
        Selfdestruct,
        TransitFailure,
        NoSpareParts,
        SuccessfulRepair,
        LowFuel,
        FuelExhausted,
        MaintenanceProblem,
        HarvesterCapacity,
        WreckSalvaged,
        SalvageFailed,
        LoadingProblem,
        LifeSupportFailure,
        TractorReleased,
        SOIChanged,
        // Crew Morale Events
        CrewMoraleFalling,
        ShoreLeaveComplete,

        // Order Events
        ConditionalOrder,
        ConditionalOrderFailure,
        OrdersAssigned,
        OrdersNotPossible,
        OrdersCompleted,
        OrdersHalt,

        // Life Pod Events
        LifePodExpired,
        SearchAndRescue,

        // Intelligence Events
        IntelligenceUpdate,
        NewAlienRace,
        NewHostileClass,
        NewHostileShip,

        // Utility Events
        FleetMessage,
        MessageContinued,
        IncrementAdjustment,
        ProgramError,
        Opps,

        // Civilian Events
        CivilianActivity,
        CivilianConstruction,
        NewShippingLine,

        // Team Events
        TeamDelivered,
        TeamDisbanded,
        SuccessfulEspionage,
        TeamKilled,
        TeamCaptured,
        EnemyAgentsKilled,
        EnemyAgentsCaptured,

        // Sensor Events
        NewHostileContact,
        NewNeutralContact,
        NewFriendlyContact,
        NewAlliedContact,
        MissileContact,
        WreckContact,
        MineralPacketContact,
        GroundForcesContact,
        ShipyardContact,
        CivilianContact,
        HostileTransitDetected,
        ExplosionDetected,
        ShieldsDetected,
        ActiveSensorDetected,
        PopulationDetected,
        ActiveContactLost,
        ThermalContactLost,
        HostileContactUpdate,
        NeutralContactUpdate,
        FriendlyContactUpdate,
        AlliedContactUpdate,
        CivilianContactUpdate,
        EnergyImpactDetected,
        NewThermalContact,
        PDCLocated,
        TargetLost,
        MissilesLost,
        NewWreck,
        WreckDisappeared,

        // Jump Point/Transit Events
        NewJumpConnection,
        NewSystemDiscovered,
        JumpPointFound,

        // Empire Events
        EmpireInDebt,
        SystemSurveyed,

        // Wormhole Events
        NewStableWormhole,
        WormholeMoved,
        WormholeDisappeared,

        #region Unknown/Depreciated/Unused

        GeologicalSurveyData, // Depreciated in favor of Mineral Deposit found/accessability increased?

        // Unused Damage events?
        TargetUndamaged,
        WeaponIneffective,
        Overkill,
        WreckComponents,
        NoDamage,
        // Non-implemented/depreciated shipboard fire mechanic?
        Fire,
        FireContained,
        // Non-implemented/depreciated ship exchange?
        SaleOfferWithdrawn,
        ShipForSale,
        Sale,
        Purchase,

        // Non-implemented/depreciated convoy feature? Pre-taskgroup?
        ConvoyCreated,
        ConvoyArrival,

        // Weather events? Are these used in Nebulas?
        IonStormAhead,
        MissilesInNebula,
        DestroyedByStorm,

        // Possible fighter-specific messages. May be depreciated.
        GroupLanded,
        GroupDestroyed,
        FighterCasualties,

        // Likely background events. Never displayed to the player.
        TimeCheck,
        InvalidUnloadSystem,
        AlienFleets,
        AlienPopulation,
        AlienShipRefitted,

        // I have no idea
        FireControlPaint,
        FireControlLockLost,
        JumpGateUnderway,
        GroundUnitDest,
        FullSpares,
        MaintenanceZero,
        TechDownloaded,
        TechDataScanned,
        NoFreighterOverhaul,
        GPDContactLost,
        TechRemoved,
        NegotiationModifier,
        IllegalOrder,
        AlienClassScanned,
        OverhaulClockReduced,
        TrainingReset,
        ParasiteLauncherReady,
        InsufficientJGC,
        PlanetLooted,
        UnsuitablePlanet,
        GovernmentChange, // NPR's don't change government? Player government change isn't recorded?
        FighterAssigned,

        NewAlienClass, // Possibly depreciated in favor of "NewHostileClass"
        NewAlienShip, // Possibly depreciated in favor of "NewHostileShip"
        ProbeOutOfFuel, // Possibly used when missle bouys used fuel?

        JumpGateDetected, // Depreciated for "JumpPointFound"?
        JumpPointDetected, // Depreciated for "JumpPointFound"?

        // Known Depreciated
        ShipMothballed,
        ShipReactivation,
        HyperLimitWarning,

        ItsLifeJim, // Easter Egg Event?

        #endregion
    }

}