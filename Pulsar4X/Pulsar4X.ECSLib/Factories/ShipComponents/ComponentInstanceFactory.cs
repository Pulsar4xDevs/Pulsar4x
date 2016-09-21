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
            blobs.Add(new DesignInfoDB(design));
            // Added because each component instance needs its own copy of this datablob

            if (design.HasDataBlob<BeamFireControlAtbDB>())
                blobs.Add(new FireControlInstanceAbilityDB());

            if (design.HasDataBlob<BeamWeaponAtbDB>() || design.HasDataBlob<SimpleBeamWeaponAtbDB>())
                blobs.Add(new WeaponStateDB());


            Entity newInstance = new Entity(design.Manager, blobs);
            return newInstance;
        }
    }



}
