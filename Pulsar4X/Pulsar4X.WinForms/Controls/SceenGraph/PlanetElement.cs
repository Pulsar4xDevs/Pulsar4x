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
        }
    }
}
