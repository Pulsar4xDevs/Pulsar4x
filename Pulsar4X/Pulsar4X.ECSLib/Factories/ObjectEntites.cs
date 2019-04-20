using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Explorations & experiments with an eaiser to use datastructure
    /// </summary>
    public class ShipEntity
    {
        public Guid FactionOwner;
        public Guid DesignFactionOwner;
        public Entity DesignEntity;
        public Entity PhysicalEntity;

        public string Name { get; set; }
        public string DesignName { get; set; }
        public double Mass_kg { get; set; }
        public double DesignMass_kg { get; set; }

        public Vector4 AbsolutePosition { get; set; }
        public Vector4 RalitivePosition { get; set; }
        public Entity SOIParentEntity { get; set; }


        List<ComponentEntity> ComponentEntites;
        public ShipEntity(Entity shipEntity)
        {
            FactionOwner = shipEntity.FactionOwner;
            var designInfo = shipEntity.GetDataBlob<DesignInfoDB>();
            DesignEntity = designInfo.DesignEntity;
            DesignFactionOwner = DesignEntity.FactionOwner;
            PhysicalEntity = shipEntity;

            Mass_kg = shipEntity.GetDataBlob<MassVolumeDB>().Mass;
            DesignMass_kg = DesignEntity.GetDataBlob<MassVolumeDB>().Mass;
            DesignName = DesignEntity.GetDataBlob<NameDB>().DefaultName;
            Name = shipEntity.GetDataBlob<NameDB>().GetName(FactionOwner);

            var componentInstances = shipEntity.GetDataBlob<ComponentInstancesDB>();
            foreach (var component in componentInstances.AllComponents)
            {
                var compObjEnt = new ComponentEntity(component);
                compObjEnt.InstalledOnEntity = this;
                ComponentEntites.Add(compObjEnt);
            }
        }
    }

    public class ComponentEntity
    {

        public Guid FactionOwner;
        public Guid DesignFactionOwner;
        public Entity DesignEntity;
        public Entity PhysicalEntity;
        public ShipEntity InstalledOnEntity;

        public string DesignName { get; set; }
        public double Mass_kg { get; set; }
        public double DesignMass_kg { get; set; }

        public ComponentEntity(Entity componentEntity)
        {
            var info = componentEntity.GetDataBlob<ComponentInfoDB>(); //is this still a thing?
            var instanceInfo = componentEntity.GetDataBlob<ComponentInstanceInfoDB>();
            var designInfo = componentEntity.GetDataBlob<DesignInfoDB>();

            FactionOwner = componentEntity.FactionOwner;
            DesignFactionOwner = DesignEntity.FactionOwner;
            DesignEntity = designInfo.DesignEntity;
            PhysicalEntity = componentEntity;
            Mass_kg = componentEntity.GetDataBlob<MassVolumeDB>().Mass;
            DesignMass_kg = DesignEntity.GetDataBlob<MassVolumeDB>().Mass;
            DesignName = DesignEntity.GetDataBlob<NameDB>().DefaultName;
        }
    }
}
