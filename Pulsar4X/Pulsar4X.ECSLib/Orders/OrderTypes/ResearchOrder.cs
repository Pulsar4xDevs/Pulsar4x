using System;
using System.Collections.Generic;


namespace Pulsar4X.ECSLib
{
    public class ResearchOrder : BaseOrder
    {
        #region Properties

        #endregion

        #region Constructors

        public ResearchOrder()
        {
            OrderType = orderType.RESEARCH;
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
            throw new NotImplementedException();
        }
        #endregion
    }
}
