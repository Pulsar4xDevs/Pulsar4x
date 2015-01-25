using OpenTK;
using Pulsar4X.Entities;
using Pulsar4X.UI;
using Pulsar4X.UI.GLUtilities;
using Pulsar4X.UI.SceenGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.UI.SceenGraph
{
    class JumpPointElement : SceenElement
    {
        private JumpPoint m_oJumpPoint;

        public override GameEntity SceenEntity
        {
	        get 
	        {
                return m_oJumpPoint;
	        }
	        set 
	        {
                m_oJumpPoint = value as JumpPoint;
            }
        }

        public JumpPointElement()
            : base()
        {

        }

        public JumpPointElement(JumpPoint jp)
            : base()
        {
            m_oJumpPoint = jp;
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

            if (m_oLable != null)
            {
                m_oLable.Render();
            }
        }

        public override Guid GetSelected(Vector3 a_v3AtPos)
        {
            Guid oElementID = Guid.Empty;

            // check To see if position provided is close to our primary primitive:
            float fDist = (m_oPrimaryPrimitive.Position - a_v3AtPos).Length;
            if (m_oPrimaryPrimitive.Size.X > fDist)
            {
                // then we are selecting this object!!
                return this.EntityID;
            }

            // else go though this elements children.
            foreach (SceenElement oElement in m_lChildren)
            {
                oElementID = oElement.GetSelected(a_v3AtPos);

                if (oElementID != Guid.Empty)
                {
                    // we have found something, retur its ID:
                    return oElementID;
                }
            }

            return Guid.Empty;
        }

        public override void Refresh(float a_fZoomScaler)
        {
            // Adjust the size of the Primary Sprite (the star icon) if necessary.
            if (MinimumSize.X > 0 && MinimumSize.Y > 0)
            {
                // calc size in pixels given current zoom factor:
                Vector2 v2CurrSize = RealSize * a_fZoomScaler;

                if (MinimumSize.X > v2CurrSize.X && MinimumSize.Y > v2CurrSize.Y)
                {
                    // then it is too small, make it display at a proper size: 
                    PrimaryPrimitive.SetSize(MinimumSize / a_fZoomScaler);
                }
                else
                {
                    // we want to draw to scale:
                    PrimaryPrimitive.SetSize(RealSize);
                }
            }

            // Adjust the size of the text so it is always 10 point:
            Lable.Size = UIConstants.DEFAULT_TEXT_SIZE / a_fZoomScaler;

            /// <summary>
            /// Update the position of this element.
            /// </summary>
            Vector3 pos = new Vector3((float)m_oJumpPoint.XSystem, (float)m_oJumpPoint.YSystem, 0.0f);

            PrimaryPrimitive.Position = pos;
            Lable.Position = pos;

            // loop through any children:
            foreach (SceenElement oElement in m_lChildren)
            {
                oElement.Refresh(a_fZoomScaler);
            }
        }
    }
}
