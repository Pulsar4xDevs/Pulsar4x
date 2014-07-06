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
    /// <summary>
    /// A Planet node/element in a sceen graph.
    /// </summary>
    class PlanetElement : SceenElement
    {

        private Planet m_oPlanet;

        public override GameEntity SceenEntity
        {
            get
            {
                return m_oPlanet;
            }
            set
            {
                m_oPlanet = value as Planet;
            }
        }


        public PlanetElement()
            : base()
        {
        }

        public PlanetElement(Planet a_oPlanet)
            : base(a_oPlanet)
        {
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
            // Adjust the size of the Primary Sprite (the planet icon) if necessary.
            if (MinimumSize.X > 0 && MinimumSize.Y > 0)
            {
                // calc size in pixels given current zoom factor:
                Vector2 v2CurrSize = RealSize * a_fZoomScaler;

                if (MinimumSize.X > v2CurrSize.X && MinimumSize.Y > v2CurrSize.Y)
                {
                    // then it is too small, make it display at a proper size: 
                    PrimaryPrimitive.SetSize(MinimumSize / a_fZoomScaler);

                    // turn off moons if min orbit < icon size in world coords:
                    float test = (MinimumSize.X / a_fZoomScaler);
                    if (SmallestOrbit < test)
                    {
                        // we are overlapping, weill not draw children:
                        RenderChildren = false;
                    }
                    else
                    {
                        // we are not overlapping, will draw children:
                        RenderChildren = true;
                    }
                }
                else
                {
                    // we want to draw to scale:
                    PrimaryPrimitive.SetSize(RealSize);

                    // turn off moons if min orbit < icon size in world coords:
                    float test = (RealSize.X / a_fZoomScaler);
                    if (SmallestOrbit < test)
                    {
                        // we are overlapping, weill not draw children:
                        RenderChildren = false;
                    }
                    else
                    {
                        // we are not overlapping, will draw children:
                        RenderChildren = true;
                    }
                }
            }

            // Adjust the size of the text so it is always 10 point:
            Lable.Size = UIConstants.DEFAULT_TEXT_SIZE / a_fZoomScaler; 

            // loop through any children:
            foreach (SceenElement oElement in m_lChildren)
            {
                oElement.Refresh(a_fZoomScaler);
            }
        }
    }
}
