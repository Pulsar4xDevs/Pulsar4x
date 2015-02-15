using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities
{
    /// <summary>
    /// Base Game Entity. All objects/entities in the game should inherit from this.
    /// </summary>
    public class GameEntity : INotifyPropertyChanged
    {
        public Guid Id { get; set; }

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        public GameEntity()
        {
        }

        public override string ToString()
        {
            if (Name != null)
            {
                return Name;
            }

            return this.GetType().FullName;
        }

        /// <summary>
        /// list of legal orders a taskgroup or unit can use againsed this entity ie when this entity is the target.
        /// </summary>
        public List<Constants.ShipTN.OrderType> LegalOrders(Faction faction)
        {
            List<Constants.ShipTN.OrderType> legalOrders = new List<Constants.ShipTN.OrderType>();
            //generic orders a selected tg targeting anything will have all these options if the selecteed tg can do these.
            legalOrders.Add(Constants.ShipTN.OrderType.MoveTo);
            legalOrders.Add(Constants.ShipTN.OrderType.ExtendedOrbit);
            legalOrders.Add(Constants.ShipTN.OrderType.Picket);
            legalOrders.Add(Constants.ShipTN.OrderType.SendMessage);
            legalOrders.Add(Constants.ShipTN.OrderType.EqualizeFuel);
            legalOrders.Add(Constants.ShipTN.OrderType.EqualizeMSP);
            legalOrders.Add(Constants.ShipTN.OrderType.ActivateTransponder);
            legalOrders.Add(Constants.ShipTN.OrderType.DeactivateTransponder);
            legalOrders.Add(Constants.ShipTN.OrderType.ActivateSensors);
            legalOrders.Add(Constants.ShipTN.OrderType.DeactivateSensors);
            legalOrders.Add(Constants.ShipTN.OrderType.ActivateShields);
            legalOrders.Add(Constants.ShipTN.OrderType.DeactivateShields);
            legalOrders.Add(Constants.ShipTN.OrderType.DivideFleetToSingleShips);
            legalOrders.Add(Constants.ShipTN.OrderType.DetachNonGeoSurvey);
            legalOrders.Add(Constants.ShipTN.OrderType.DetachNonGravSurvey);
            legalOrders.Add(Constants.ShipTN.OrderType.RefuelFromOwnTankers);
            legalOrders.Add(Constants.ShipTN.OrderType.DetachTankers);
            legalOrders.Add(Constants.ShipTN.OrderType.ResupplyFromOwnSupplyShips);
            legalOrders.Add(Constants.ShipTN.OrderType.DetachSupplyShips);
            legalOrders.Add(Constants.ShipTN.OrderType.ReloadFromOwnColliers);
            legalOrders.Add(Constants.ShipTN.OrderType.DetachColliers);
            legalOrders.Add(Constants.ShipTN.OrderType.ReleaseAt);

            if (this is JumpPoint)
            {
                JumpPoint thisjp = (JumpPoint)this;
                legalOrders.Add(Constants.ShipTN.OrderType.StandardTransit);
                legalOrders.Add(Constants.ShipTN.OrderType.TransitAndDivide);
                legalOrders.Add(Constants.ShipTN.OrderType.SquadronTransit);
                if (!thisjp.IsGated)
                    legalOrders.Add(Constants.ShipTN.OrderType.BuildJumpGate);
            }
            if (this is Population)
            {
                Population pop = (Population)this;
                if (faction == pop.Faction)
                {
                    legalOrders.Add(Constants.ShipTN.OrderType.LoadCrewFromColony);
                    if (pop.FuelStockpile > 0)
                        legalOrders.Add(Constants.ShipTN.OrderType.RefuelFromColony);
                    if (pop.MaintenanceSupplies > 0)
                        legalOrders.Add(Constants.ShipTN.OrderType.ResupplyFromColony);
                    if (Array.Exists(pop.Installations, x => x.Type == Installation.InstallationType.MaintenanceFacility))
                        legalOrders.Add(Constants.ShipTN.OrderType.BeginOverhaul);
                    if (pop.Installations.Count() > 0)
                        legalOrders.Add(Constants.ShipTN.OrderType.LoadInstallation);
                    if (pop.ComponentStockpile.Count() > 0)
                        legalOrders.Add(Constants.ShipTN.OrderType.LoadShipComponent);
                    legalOrders.Add(Constants.ShipTN.OrderType.LoadAllMinerals);
                    legalOrders.Add(Constants.ShipTN.OrderType.UnloadAllMinerals);
                    legalOrders.Add(Constants.ShipTN.OrderType.LoadMineral);
                    legalOrders.Add(Constants.ShipTN.OrderType.LoadMineralWhenX);
                    legalOrders.Add(Constants.ShipTN.OrderType.UnloadMineral);
                    legalOrders.Add(Constants.ShipTN.OrderType.LoadOrUnloadMineralsToReserve);
                    if (pop.CivilianPopulation > 0)
                        legalOrders.Add(Constants.ShipTN.OrderType.LoadColonists);
                    legalOrders.Add(Constants.ShipTN.OrderType.UnloadColonists);
                    legalOrders.Add(Constants.ShipTN.OrderType.UnloadFuelToPlanet);
                    legalOrders.Add(Constants.ShipTN.OrderType.UnloadSuppliesToPlanet);
                    if (Array.Exists(pop.Installations, x => x.Type == Installation.InstallationType.OrdnanceFactory) || pop.MissileStockpile.Count > 0)
                        legalOrders.Add(Constants.ShipTN.OrderType.LoadMineral);
                    legalOrders.Add(Constants.ShipTN.OrderType.LoadOrdnanceFromColony);
                    legalOrders.Add(Constants.ShipTN.OrderType.UnloadOrdnanceToColony);
                }
            }
            if (this is SystemContact)
            {
                legalOrders.Add(Constants.ShipTN.OrderType.Follow);
            }
            if (this is TaskGroupTN)
            {
                TaskGroupTN tg = (TaskGroupTN)this;
                ShipTN[] shipsArray = tg.Ships.ToArray();
                legalOrders.Add(Constants.ShipTN.OrderType.Follow);
                legalOrders.Add(Constants.ShipTN.OrderType.Join);
                legalOrders.Add(Constants.ShipTN.OrderType.Absorb);
                legalOrders.Add(Constants.ShipTN.OrderType.RefuelTargetFleet);
                legalOrders.Add(Constants.ShipTN.OrderType.ResupplyTargetFleet);
                legalOrders.Add(Constants.ShipTN.OrderType.ReloadTargetFleet);

                if (Array.Exists(shipsArray, x => x.ShipClass.IsTanker)) //if this fleet is targeted and has a IsTanker.
                    legalOrders.Add(Constants.ShipTN.OrderType.RefuelFromTargetFleet);
                if (Array.Exists(shipsArray, x => x.ShipClass.IsSupply))//if this fleet is targeted and has a IsSupply.
                    legalOrders.Add(Constants.ShipTN.OrderType.ResupplyFromTargetFleet);
                if (Array.Exists(shipsArray, x => x.ShipClass.IsCollier))//if this fleet is targeted and has a IsCollier.
                    legalOrders.Add(Constants.ShipTN.OrderType.ReloadFromTargetFleet);

                legalOrders.Add(Constants.ShipTN.OrderType.LandOnAssignedMothership);
                legalOrders.Add(Constants.ShipTN.OrderType.LandOnMotherShipNoAssign);
                legalOrders.Add(Constants.ShipTN.OrderType.LandOnMothershipAssign);
                legalOrders.Add(Constants.ShipTN.OrderType.TractorSpecifiedShip);
                legalOrders.Add(Constants.ShipTN.OrderType.TractorSpecifiedShipyard);
            }
            return legalOrders;
        }

        public virtual void OnPropertyChanged(string propertyName)
        {
            var propertyChanged = PropertyChanged;
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
