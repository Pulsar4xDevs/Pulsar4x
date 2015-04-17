namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Contains info on the Ships missile weapons, including Fire control and Magazine storage.
    /// </summary>
    public class MissileWeaponsDB : BaseDataBlob
    {
        public int MaximumMagazineCapicity { get; set; } // in MSP
        public int UsedMagazineCapicity { get; set; }

        public MissileWeaponsDB()
        {
        }

        public MissileWeaponsDB(MissileWeaponsDB missleWeaponDB)
        {
            MaximumMagazineCapicity = missleWeaponDB.MaximumMagazineCapicity;
            UsedMagazineCapicity = missleWeaponDB.UsedMagazineCapicity;
        }

        public override object Clone()
        {
            return new MissileWeaponsDB(this);
        }
    }
}