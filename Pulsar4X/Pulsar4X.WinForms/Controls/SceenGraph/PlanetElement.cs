using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X;
using Pulsar4X.WinForms;
using Pulsar4X.WinForms.GLUtilities;
using OpenTK;

namespace Pulsar4X.WinForms.Controls.SceenGraph
{
    /// <summary>
    /// A Planet node/element in a sceen graph.
    /// </summary>
    class PlanetElement : SceenElement
    {
        public PlanetElement()
            : base()
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
        }


        public override Guid GetSelected(Vector3 a_v3AtPos)
        {
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

            // loop through any children:
            foreach (SceenElement oElement in m_lChildren)
            {
                oElement.Refresh(a_fZoomScaler);
            }
        }
    }
}
