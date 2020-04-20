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
            if (!componentInstance.HasAblity<WeaponState>())
            {
                var wpnState = new WeaponState();
                wpnState.WeaponType = "Rail Gun";
                wpnState.WeaponStats = new (string name, double value, ValueTypeStruct valueType)[3];
                wpnState.WeaponStats[0] = ("Calibre:", Calibre, new ValueTypeStruct(ValueTypeStruct.ValueTypes.Distance, ValueTypeStruct.ValueSizes.Milli));
                wpnState.WeaponStats[1] = ("Length:", Length, new ValueTypeStruct(ValueTypeStruct.ValueTypes.Distance, ValueTypeStruct.ValueSizes.BaseUnit));
                wpnState.WeaponStats[2] = ("Rate Of Fire:", RateOfFire, new ValueTypeStruct(ValueTypeStruct.ValueTypes.Number, ValueTypeStruct.ValueSizes.BaseUnit));
                wpnState.WeaponStats[3] = ("Power Use:", PowerUsePerShot, new ValueTypeStruct(ValueTypeStruct.ValueTypes.Power, ValueTypeStruct.ValueSizes.BaseUnit));
                componentInstance.SetAbilityState<WeaponState>(wpnState);
            }
        }
    }


    public class RailGunAmmo
    {
        public IWarhead Warhead;
        public float Calibre;
        public float Mass;


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