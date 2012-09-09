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
    /// A generic Class which define the interface for a node in a Sceen graph, specific types of nodes should inherit from here.
    /// </summary>
    public abstract class SceenElement
    {
        /// <summary>
        /// List of Primitives that make up this sprite.
        /// </summary>
        protected List<GLPrimitive> m_lPrimitives = new List<GLPrimitive>();

        /// <summary>
        /// Get Primitives List.
        /// </summary>
        public List<GLPrimitive> Primitives
        {
            get
            {
                return m_lPrimitives;
            }
        }

        /// <summary>
        /// The Controlling Primitive is used to to work out things like if this boject has been selected, 
        /// e.g. For a Planet the Planet icon in the Controlling Primitive, everthing else is just UI chrome that hangs off mit.
        /// </summary>
        protected GLPrimitive m_oControllingPrimitive;

        /// <summary>
        /// List of children
        /// </summary>
        protected List<SceenElement> m_lChildren = new List<SceenElement>();

        /// <summary>
        /// Get Children List.
        /// </summary>
        public List<SceenElement> Children
        {
            get
            {
                return Children;
            }
        }

        /// <summary>
        /// The ID of the entity this object repesents.
        /// </summary>
        public Guid EntityID { get; set; }

        /// <summary>
        /// Define the minium size in pixels for this object, 0 means that it can disapear, 
        /// </summary>
        public Vector3 MinimumSize { get; set; }

        /// <summary>
        /// Defines the distance from the ControllingPrimitive's Position for testing for selection. 
        /// </summary>
        public float SelectionDistance { get; set; }

        public bool RenderChildren { get; set; }


        /// <summary>
        /// Default Constructor
        /// </summary>
        public SceenElement()
        {
            // set some defaults:
            MinimumSize = new Vector3(32, 32, 0);
            RenderChildren = true;
        }

        /// <summary>
        /// Renders this Sprite's Primitives to the screen, then calles Redner() on aly Children
        /// </summary>
        public abstract void Render();

        /// <summary>
        /// Gets the Selected Entity at the Position specified (in world coords). if it is not this sprite then it will 
        /// recurse down the Children until it finds it. it will return null if it cannot be found
        /// </summary>
        public abstract Guid GetSelected(Vector3 a_v3AtPos);

        /// <summary>
        /// Will update/refresh the position and Size of the sprite, handles things like making sure that this sprite is not too small, etc.
        /// </summary>
        /// <param name="a_fZoomScaler"></param>
        public abstract void Refresh(float a_fZoomScaler);

        /// <summary>
        /// Add a primitive to the Sceen Element
        /// </summary>
        /// <param name="a_oPrimitive">The Primitive to Add</param>
        public void AddPrimitive(GLPrimitive a_oPrimitive)
        {
            m_lPrimitives.Add(a_oPrimitive);
        }

        /// <summary>
        /// Add a Child Element to this element.
        /// </summary>
        /// <param name="a_oElement">The element to add.</param>
        public void AddChildElement(SceenElement a_oElement)
        {
            m_lChildren.Add(a_oElement);
        }
    }
}
