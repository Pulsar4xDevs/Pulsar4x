using System;

namespace Pulsar4X.ECSLib.ComponentFeatureSets.Missiles
{
    public class ElectronicsSuite : IComponentDesignAttribute
    {
        public enum GuidanceTypes
        {
            Dumbfire,
            Parent,
            Passive,
            Active
        }

        public enum TriggerTypes
        {
            Contact,
            Timer, 
            Prox,
            Depth,
        }

        public TriggerTypes TriggerType = TriggerTypes.Contact;

        public GuidanceTypes GuidenceType = GuidanceTypes.Dumbfire;



        public ElectronicsSuite(int guidenceType, int triggerType)
        {
            GuidenceType = (GuidanceTypes)guidenceType;
            TriggerType = (TriggerTypes)triggerType;
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            throw new System.NotImplementedException();
        }
    }

    interface IGuidenceType
    {
        ElectronicsSuite.GuidanceTypes GetGuidenceType { get; }
    }

    struct DumbfireGuidence : IGuidenceType
    {
        public ElectronicsSuite.GuidanceTypes GetGuidenceType => ElectronicsSuite.GuidanceTypes.Dumbfire;
        //public double TriggerAfterSeconds;
    }
    struct ParentGuidence : IGuidenceType
    {
        public ElectronicsSuite.GuidanceTypes GetGuidenceType => ElectronicsSuite.GuidanceTypes.Parent;
    }
    struct PassiveGuidence : IGuidenceType
    {
        public ElectronicsSuite.GuidanceTypes GetGuidenceType => ElectronicsSuite.GuidanceTypes.Passive;
    }
    struct ActiveGuidence : IGuidenceType
    {
        public ElectronicsSuite.GuidanceTypes GetGuidenceType => ElectronicsSuite.GuidanceTypes.Active;
    }
    
    interface ITriggerType
    {
        ElectronicsSuite.TriggerTypes GetTriggerType { get; }
    }

    struct ContactTrigger : ITriggerType
    {
        public ElectronicsSuite.TriggerTypes GetTriggerType => ElectronicsSuite.TriggerTypes.Contact;
        //public double TriggerAfterSeconds;
    }
    struct TimerTrigger : ITriggerType
    {
        public ElectronicsSuite.TriggerTypes GetTriggerType => ElectronicsSuite.TriggerTypes.Timer;
        public double TriggerAfterSeconds;
    }
    struct ProxTrigger : ITriggerType
    {
        public ElectronicsSuite.TriggerTypes GetTriggerType => ElectronicsSuite.TriggerTypes.Prox;
        public double TriggerWhenDistanceFromTarget;
    }
    struct DepthTrigger : ITriggerType
    {
        public ElectronicsSuite.TriggerTypes GetTriggerType => ElectronicsSuite.TriggerTypes.Depth;
        public double TriggerWhenDistanceAfterContact;
    }
}