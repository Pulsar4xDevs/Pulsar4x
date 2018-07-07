using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    public static class ComponentInstanceFactory
    {

        internal static Entity NewInstanceFromDesignEntity(Entity design, Entity faction, FactionOwnerDB ownerdb, EntityManager manager)
        {

            List<BaseDataBlob> blobs = new List<BaseDataBlob>();
            ComponentInstanceInfoDB info = new ComponentInstanceInfoDB(design);
            blobs.Add(info);
            blobs.Add(new DesignInfoDB(design));
            // Added because each component instance needs its own copy of this datablob

            //Components have a mass and volume.
            MassVolumeDB mvDB = new MassVolumeDB();
            blobs.Add(mvDB);

            //TODO: this seems ugly, consider using an Interface on the datablobs for this? YES: TODO put this in the IComponentDesignAttribute
            if (design.HasDataBlob<BeamFireControlAtbDB>())
                blobs.Add(new FireControlInstanceStateDB());

            if (design.HasDataBlob<BeamWeaponAtbDB>() || design.HasDataBlob<SimpleBeamWeaponAtbDB>())
                blobs.Add(new WeaponInstanceStateDB());
            if (design.HasDataBlob<SensorReceverAtbDB>())
                blobs.Add((SensorReceverAtbDB)design.GetDataBlob<SensorReceverAtbDB>().Clone());

            Entity newInstance = new Entity(manager, faction.Guid, blobs);
            new OwnedDB(faction, newInstance); //the constructor of OwnedDB sets itself to the entity
            return newInstance;
        }
    }



}
