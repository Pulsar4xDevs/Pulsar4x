using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    public static class ComponentInstanceFactory
    {

        internal static Entity NewInstanceFromDesignEntity(Entity design, Guid factionID, EntityManager manager)
        {

            List<BaseDataBlob> blobs = new List<BaseDataBlob>();
            ComponentInstanceInfoDB info = new ComponentInstanceInfoDB(design);
            blobs.Add(info);
            blobs.Add(new DesignInfoDB(design));
            // Added because each component instance needs its own copy of this datablob

            //Components have a mass and volume.
            MassVolumeDB mvDB = new MassVolumeDB();
            blobs.Add(mvDB);

            Entity newInstance = new Entity(manager, factionID, blobs);
            return newInstance;
        }
    }



}
