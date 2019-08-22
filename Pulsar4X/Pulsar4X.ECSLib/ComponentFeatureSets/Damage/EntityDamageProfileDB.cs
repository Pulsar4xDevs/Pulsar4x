using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib.ComponentFeatureSets.Damage
{
    public class EntityDamageProfileDB : BaseDataBlob
    {
        public (string name, double density, float thickness) Armor;
        public List<(Guid id, int count)> PlacementOrder;
        public List<(Guid, RawBmp)> TypeBitmaps;
        //public List<(int index, int size)> Bulkheads; maybe connect armor/skin at these points.
        //if we get around to doing technical stuff like being able to break a ship into two pieces,
        //and having longditudinal structural parts...
        public RawBmp ShipDamageProfile;
        
        public List<ComponentInstance> ComponentLookupTable = new List<ComponentInstance>();

        public EntityDamageProfileDB()
        {
        }

        public EntityDamageProfileDB(EntityDamageProfileDB db )
        {
            Armor = db.Armor;
            PlacementOrder = db.PlacementOrder;
            TypeBitmaps = db.TypeBitmaps;
            ShipDamageProfile = db.ShipDamageProfile;
        }

        public override object Clone()
        {
            return new EntityDamageProfileDB(this);
        }
    }
}