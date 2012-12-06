using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X;
using Pulsar4X.UI;
using Pulsar4X.UI.GLUtilities;
using OpenTK;
using Pulsar4X.Entities;

namespace Pulsar4X.UI.SceenGraph
{
    class MeasurementElement : SceenElement
    {

        public MeasurementElement() : base()
        {
        }

        public void UpdateEndPos(Vector3 a_v3Pos)
        {
            GLLine temp = m_oPrimaryPrimitive as GLLine;
            if (temp != null)
            {
                temp.PosEnd = a_v3Pos;
            }
        }

        GameEntity m_oGameEntity;
        public override GameEntity SceenEntity
        {
            get
            {
                return m_oGameEntity;
            }
            set
            {
                m_oGameEntity = value;
            }
        }

        public override void Render()
        {
            foreach (GLPrimitive oPrimitive in m_lPrimitives)
            {
                oPrimitive.Render();
            }

            if (RenderChildren == true)
            {
                foreach (SceenElement oElement in m_lChildren)
                {
                    oElement.Render();
                }
            }

            // render lable:
            if (m_oLable != null)
            {
                m_oLable.Render();
            }
        }

        public override Guid GetSelected(Vector3 a_v3AtPos)
        {
            return Guid.Empty;
        }

        public override void Refresh(float a_fZoomScaler)
        {
        }

    }
}
