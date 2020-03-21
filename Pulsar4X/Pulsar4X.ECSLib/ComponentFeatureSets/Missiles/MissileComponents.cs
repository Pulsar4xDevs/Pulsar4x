namespace Pulsar4X.ECSLib.ComponentFeatureSets.Missiles
{
    public class ElectonicsSuite : IComponentDesignAttribute
    {
        public enum GuidanceTypes
        {
            Dumbfire,
            Parent,
            PassiveIR,
            ActiveRadar
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




        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            throw new System.NotImplementedException();
        }
    }
}