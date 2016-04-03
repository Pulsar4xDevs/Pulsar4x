using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X;
using Pulsar4X.UI;
using Pulsar4X.ViewModel.GLUtilities;
using OpenTK;
using Pulsar4X.Entities;

namespace Pulsar4X.UI.SceenGraph
{
    /// <summary>
    /// A Star node/element in a scene graph.
    /// </summary>
    class StarElement : SceenElement
    {

        private Star m_oStar;

        public override GameEntity SceenEntity
        {
            get
            {
                return m_oStar;
            }
            set
            {
                m_oStar = value as Star;
            }
        }

        /// <summary>
        /// This is the display element for the orbit this planet will make.
        /// </summary>
        private CircleElement m_oOrbitCircle { get; set; }

        public StarElement()
            : base()
        {

        }

        public StarElement(Star a_oStar, GLEffect a_oDefaultEffect, Vector3 a_oPosition, System.Drawing.Color a_oColor, bool a_bPrimary = true)
            : base(a_oStar)
        {
            if (!a_bPrimary)
            {
                // Do Non Primary Star Stuff here (e.g. orbit circle).
                ///< @todo...

                m_oOrbitCircle = new CircleElement(a_oDefaultEffect, a_oPosition, a_oStar, a_oColor);
#warning As with planet, m_oOrbitCircle will not be added as a child.
            }
            else
                m_oOrbitCircle = null;


        }

        public override void Render()
        {
            foreach (GLPrimitive oPrimitive in m_lPrimitives)
            {
                oPrimitive.Render();
            }

            /// <summary>
            /// Putting this as a child means that it runs afoul of the "don't render children" check.
            /// </summary>
            if (m_oOrbitCircle != null)
            {
                m_oOrbitCircle.Render();
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
            Vector3 pos = new Vector3((float)m_oStar.Position.X, (float)m_oStar.Position.Y, 0.0f);

            PrimaryPrimitive.Position = pos;
            Lable.Position = pos;

            if (m_oOrbitCircle != null)
            {
                /// <summary>
                /// Parent star is always at 0.0,0.0,0.0
                /// </summary>
                m_oOrbitCircle.CurrentPosition = Vector3.Zero;
                /// <summary>
                /// Putting this as a child means that it runs afoul of the "don't render children" check.
                /// </summary>
                m_oOrbitCircle.Refresh(a_fZoomScaler);
            }


            // loop through any children:
            foreach (SceenElement oElement in m_lChildren)
            {
                oElement.Refresh(a_fZoomScaler);
            }
        }
    }
}
