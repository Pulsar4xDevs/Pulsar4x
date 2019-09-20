namespace Pulsar4X.ECSLib.ComponentFeatureSets.RailGun
{
    public class RailGunAtb : IComponentDesignAttribute
    {

        public float Calibre;            //mm
        public float Length;             //meters
        public float PowerUsePerShot;     //in Mj
        public float Efficency;
        public float RateOfFire;         //rps
        //public float HeatPerShot;

        public RailGunAtb(double calibre, double length, double pwr, double efficency,  double rof)
        {
            Calibre = (float)calibre;
            Length = (float)length;
            PowerUsePerShot = (float)pwr;
            Efficency = (float)efficency;
            RateOfFire = (float)rof;
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            
        }
    }


    public class RailGunAmmo
    {
        public IWarhead Warhead;
        public float Calibre;
        public float Weight;


    }

    public interface IWarhead
    {
        
    }

    public class Sabot : IWarhead
    {
        public float Length;
        public float Diameter;
        public float Density;
    }

    public class FragShell : IWarhead
    {
        public int NumFragments;
        public int Force;
        public float FragDiameter;
        public float FragDensity;

        public float TimeFuse;

    }
    
    
    
}