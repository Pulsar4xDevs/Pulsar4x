using System;
using Pulsar4X.Interfaces;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Atb
{
    public class OrdnancePayloadAtb : IComponentDesignAttribute
    {
        public TriggerTypes Trigger;
        public double Mass;
        public OrdnancePayloadAtb(TriggerTypes trigger, double totalMass)
        {
            Trigger = trigger;
            Mass = totalMass;
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {

        }
        public string AtbName()
        {
            return "Ordnance Payload";
        }

        public string AtbDescription()
        {

            return "";
        }
    }

    public class OrdnanceExplosivePayload : OrdnancePayloadAtb
    {
        public double ExposiveTnTEQMass; //tnt equvelent
        public double FragMass;
        public double FragCount;
        public double FragCone;

        public OrdnanceExplosivePayload(int trigger, double totalMass, double tntEqMass, double fragMass, double fragCount, double fragCone) : base((TriggerTypes)trigger, totalMass)
        {
            ExposiveTnTEQMass = tntEqMass;
            FragMass = fragMass;
            FragCount = fragCount;
            FragCone = fragCone;
        }
    }

    public class OrdnanceShapedPayload : OrdnancePayloadAtb
    {
        double ExposiveTnTEQMass;
        double LinerRadius;
        double LinerDepth;
        double LinerAngle;
        double LinerThickness;
        public OrdnanceShapedPayload(int trigger, double totalMass, double tntEqMass, double linerRadius, double linerDepth, double linerThickness): base((TriggerTypes)trigger, totalMass)
        {
            ExposiveTnTEQMass = tntEqMass;
            LinerRadius = linerRadius;
            LinerDepth = linerDepth;
            LinerAngle = Math.Asin(LinerRadius / LinerDepth);
            LinerThickness = linerThickness;
        }
    }
    public class OrdnanceLaserPayload : OrdnancePayloadAtb
    {
        public OrdnanceLaserPayload(int trigger, double totalMass, Guid designID): base((TriggerTypes)trigger, totalMass)
        {
        }
    }
    public class OrdnanceSubmunitionsPayload : OrdnancePayloadAtb
    {
        public OrdnanceSubmunitionsPayload(int trigger, double totalMass, Guid designID, int count): base((TriggerTypes)trigger, totalMass)
        {
        }
    }
 
    public class ElectronicsSuite : IComponentDesignAttribute
    {


        public TriggerTypes TriggerType = TriggerTypes.Contact;

        public GuidanceTypes GuidenceType = GuidanceTypes.Dumbfire;



        public ElectronicsSuite(int guidenceType)
        {
            GuidenceType = (GuidanceTypes)guidenceType;
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {

        }

        public string AtbName()
        {
            return "EW Suite";
        }

        public string AtbDescription()
        {

            return " ";
        }
    }

    interface IGuidenceType
    {
        GuidanceTypes GetGuidenceType { get; }
    }

    struct DumbfireGuidence : IGuidenceType
    {
        public GuidanceTypes GetGuidenceType => GuidanceTypes.Dumbfire;
        //public double TriggerAfterSeconds;
    }
    struct ParentGuidence : IGuidenceType
    {
        public GuidanceTypes GetGuidenceType => GuidanceTypes.Parent;
    }
    struct PassiveGuidence : IGuidenceType
    {
        public GuidanceTypes GetGuidenceType => GuidanceTypes.Passive;
    }
    struct ActiveGuidence : IGuidenceType
    {
        public GuidanceTypes GetGuidenceType => GuidanceTypes.Active;
    }

    interface ITriggerType
    {
        TriggerTypes GetTriggerType { get; }
    }

    struct ContactTrigger : ITriggerType
    {
        public TriggerTypes GetTriggerType => TriggerTypes.Contact;
        //public double TriggerAfterSeconds;
    }
    struct TimerTrigger : ITriggerType
    {
        public TriggerTypes GetTriggerType => TriggerTypes.Timer;
        public double TriggerAfterSeconds;
    }
    struct ProxTrigger : ITriggerType
    {
        public TriggerTypes GetTriggerType => TriggerTypes.Prox;
        public double TriggerWhenDistanceFromTarget;
    }
    struct DepthTrigger : ITriggerType
    {
        public TriggerTypes GetTriggerType => TriggerTypes.Depth;
        public double TriggerWhenDistanceAfterContact;
    }
}