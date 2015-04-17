namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Contains info on a ships maintance supplies/failure rate/etc.
    /// </summary>
    public class MaintenanceDB : BaseDataBlob
    {
        public int MaintenanceStorageCapicity { get; set; }
        public int CurrentMSP { get; set; }

        public int MaximumRepairCost { get; set; }
        public double AnnualFailureRate { get; set; }
        public double IncrementalFailureRate { get; set; }

        public MaintenanceDB()
        {
        }

        public MaintenanceDB(MaintenanceDB maintenanceDB)
        {
            MaintenanceStorageCapicity = maintenanceDB.MaintenanceStorageCapicity;
            CurrentMSP = maintenanceDB.CurrentMSP;
            MaximumRepairCost = maintenanceDB.MaximumRepairCost;
            AnnualFailureRate = maintenanceDB.AnnualFailureRate;
            IncrementalFailureRate = maintenanceDB.IncrementalFailureRate;
        }

        public override object Clone()
        {
            return new MaintenanceDB(this);
        }
    }
}