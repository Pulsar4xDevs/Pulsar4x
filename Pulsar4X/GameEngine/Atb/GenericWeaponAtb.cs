using Newtonsoft.Json;
using Pulsar4X.Engine;
using Pulsar4X.Components;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Atb
{
    public class GenericWeaponAtb : IComponentDesignAttribute
    {
        public int InternalMagSize;
        public int ReloadAmountPerSec;
        public int AmountPerShot;
        public int MinShotsPerfire;
        
        private GenericWeaponAtb()
        {
        }

        /// <summary>
        /// constructor for json, all values get cast to ints (not rounded)
        /// </summary>
        /// <param name="magSize"></param>
        /// <param name="reloadPerSec"></param>
        /// <param name="amountPerShot"></param>
        /// <param name="minShotsPerfire"></param>
        public GenericWeaponAtb(double magSize, double reloadPerSec, double amountPerShot, double minShotsPerfire)
        {
            InternalMagSize = (int)magSize;
            ReloadAmountPerSec = (int)reloadPerSec;
            AmountPerShot = (int)amountPerShot;
            MinShotsPerfire = (int)minShotsPerfire;
            //WpnType = type;
        }
        
        public GenericWeaponAtb(int magSize, int reloadPerSec, int amountPerShot, int minShotsPerfire)
        {
            InternalMagSize = magSize;
            ReloadAmountPerSec = reloadPerSec;
            AmountPerShot = amountPerShot;
            MinShotsPerfire = minShotsPerfire;
            //WpnType = type;
        }

        
        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            
        }
        
        public string AtbName()
        {
            return "Generic Weapon";
        }

        public string AtbDescription()
        {
            return "";
        }
        
    }
}