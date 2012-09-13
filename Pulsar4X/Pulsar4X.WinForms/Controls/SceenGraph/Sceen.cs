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
    /// Root Node of a Sceen Graph
    /// </summary>
    public class Sceen
    {
        /// <summary>
        /// List of all top level Sprites That Make up the Sceen.
        /// </summary>
        private List<SceenElement> m_lElements = new List<SceenElement>();

        /// <summary>
        /// Gets a list of the Elements that make up the sceen.
        /// </summary>
        public List<SceenElement> Elements
        {
            get
            {
                return m_lElements;
            }
        }

        /// <summary> 
        /// The zoom scaler, make this smaller to zoom out, larger to zoom in.
        /// </summary>
        private float m_fZoomScaler = UIConstants.ZOOM_DEFAULT_SCALLER;

        /// <summary>
        /// Gets or Sets the ZoomScaler for the Sceen.
        /// </summary>
        public float ZoomSclaer
        {
            get
            {
                return m_fZoomScaler;
            }
            set
            {
                m_fZoomScaler = value;
            }
        }

        /// <summary> 
        /// The view offset, i.e. how much the view should be offset from 0, 0 
        /// </summary>
        private Vector3 m_v3ViewOffset = new Vector3(0, 0, 0);

        public Vector3 ViewOffset
        {
            get
            {
                return m_v3ViewOffset;
            }
            set
            {
                m_v3ViewOffset = value;
            }
        }

        /// <summary>
        /// The Sceen ID, this could be a system ID for example.
        /// </summary>
        public Guid SceenID { get; set; } 


        /// <summary>
        /// Default Constructor.
        /// </summary>
        public Sceen()
        {
        }

        /// <summary>
        /// Render the Sceen.
        /// </summary>
        public void Render()
        {
            foreach (SceenElement oElement in m_lElements)
            {
                oElement.Render();
            }
        }

        /// <summary>
        /// Refresh the Sceen
        /// </summary>
        public void Refresh()
        {
            foreach (SceenElement oElement in m_lElements)
            {
                oElement.Refresh(m_fZoomScaler);
            }
        }

        /// <summary>
        /// Add an element to the sceen
        /// </summary>
        /// <param name="a_oElement"> the Element to add.</param>
        public void AddElement(SceenElement a_oElement)
        {
            m_lElements.Add(a_oElement);
        }

        public Guid GetElementAtCoords(Vector3 a_v3Coords)
        {
            Guid oElementID = Guid.Empty;
            foreach (SceenElement oElement in m_lElements)
            {
                oElementID = oElement.GetSelected(a_v3Coords);

                if (oElementID != Guid.Empty)
                {
                    // we have found something, retur its ID:
                    return oElementID;
                }
            }

            return oElementID;
        }
    }
}
