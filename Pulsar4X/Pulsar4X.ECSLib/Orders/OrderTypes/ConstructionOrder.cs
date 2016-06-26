using System;
using System.Collections.Generic;


namespace Pulsar4X.ECSLib
{ 
    public class ConstructionOrder : BaseOrder
    {
    #region Properties
    public ConstructionType ConstructionType { get; internal set; }

    public Dictionary<Guid, int> MineralsRequired { get; internal set; }
    public Dictionary<Guid, int> MaterialsRequired { get; internal set; }
    public Dictionary<Guid, int> ComponentsRequired { get; internal set; }

    #endregion

    #region Constructors

    public ConstructionOrder()
    {
        ConstructionType = ConstructionType.None;

        MineralsRequired = new Dictionary<Guid, int>();
        MaterialsRequired = new Dictionary<Guid, int>();
        ComponentsRequired = new Dictionary<Guid, int>();
    }

    public ConstructionOrder(ConstructionType constructionType, Dictionary<Guid, int> minRequired, Dictionary<Guid, int> matRequired, Dictionary<Guid, int> compRequired)
    {
        ConstructionType = constructionType;

        MineralsRequired = new Dictionary<Guid, int>(minRequired);
        MaterialsRequired = new Dictionary<Guid, int>(matRequired);
        ComponentsRequired = new Dictionary<Guid, int>(compRequired);
    }


    #endregion

    #region Public API

    public override bool isValid()
    {
        throw new NotImplementedException();
    }

    public override bool processOrder()
    {
        throw new NotImplementedException();
    }

    public override object Clone()
    {
        ConstructionOrder order = new ConstructionOrder(ConstructionType, MineralsRequired, MaterialsRequired, ComponentsRequired);

        return order;
    }



    #endregion
    }
}
