using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    public static class ComponentInstanceFactory
    {

        internal static Entity NewInstanceFromDesignEntity(Entity design, Entity faction)
        {

            List<BaseDataBlob> blobs = new List<BaseDataBlob>();
            ComponentInstanceInfoDB info = new ComponentInstanceInfoDB(design);
            OwnedDB owned = new OwnedDB(faction);
            blobs.Add(info);
            blobs.Add(owned);

            Entity newInstance = new Entity(design.Manager, blobs);
            return newInstance;
        }
    }



}
