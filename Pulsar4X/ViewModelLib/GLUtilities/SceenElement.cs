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
        /// e.g. For a SystemBody the SystemBody icon in the Controlling Primitive, everthing else is just UI chrome that hangs off mit.
        /// </summary>
        protected GLPrimitive m_oPrimaryPrimitive;

        /// <summary>
        /// Gets or sets the Controlling Primitive.
        /// </summary>
        public GLPrimitive PrimaryPrimitive
        {
            get
            {
                return m_oPrimaryPrimitive;
            }
            set
            {
                m_oPrimaryPrimitive = value;
            }
        }


        /// <summary> The text lable primitive for this sceen element </summary>
        protected GLFont m_oLable;

        /// <summary>   Gets or sets the lable primitive. </summary>
        public GLFont Lable
        {
            get
            {
                return m_oLable;
            }
            set
            {
                m_oLable = value;
            }
        }

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
                return m_lChildren;
            }
        }

        /// <summary>
        ///  This SceenElements Parent.
        /// </summary>
        protected SceenElement m_oParent;

        /// <summary>
        /// Gets or Sets the Parent Sceen Element of this Object.
        /// </summary>
        public SceenElement Parent
        {
            get
            {
                return m_oParent;
            }
            set
            {
                m_oParent = value;
            }
        }

        /// <summary>
        /// The ID of the entity this object repesents.
        /// </summary>
        public Guid EntityID { get; set; }

        /// <summary>
        /// Define the minium size in pixels for this object, 0 means that it can disapear.
        /// </summary>
        public Vector2 MinimumSize { get; set; }

        /// <summary>
        /// Defines the real size of this object in Km.
        /// </summary>
        public Vector2 RealSize { get; set; }

        /// <summary>   
        /// The Smallest Orbit around this object. Used to deterimine if this object should draw its children.
        /// </summary>
        public float SmallestOrbit { get; set; }

        /// <summary>
        /// Defines the distance from the ControllingPrimitive's Position for testing for selection. 
        /// </summary>
        public float SelectionDistance { get; set; }

        /// <summary>
        /// If False this element will not render any of its children
        /// </summary>
        public bool RenderChildren { get; set; }

        /// <summary>
        /// The Game Entity this Sceen Element repesents.
        /// </summary>
        public abstract GameEntity SceenEntity { get; set; }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public SceenElement()
        {
            // set some defaults:
            MinimumSize = new Vector2(8, 8);
            RenderChildren = true;
        }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="a_GameEntity">The Game Entity this Sceen Element Repesents</param>
        public SceenElement(GameEntity a_oGameEntity)
        {
            // set some defaults:
            MinimumSize = new Vector2(8, 8);
            RenderChildren = true;
            SceenEntity = a_oGameEntity;
            EntityID = a_oGameEntity.Id;
        }

        /// <summary>
        /// Renders this Sprite's Primitives to the screen, then calles Redner() on aly Children
        /// </summary>
        public abstract void Render();

        /// <summary>
        /// Gets the Selected Entity at the Position specified (in world coords). If it is not this sprite then it will 
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
            a_oElement.Parent = this;
            m_lChildren.Add(a_oElement);
        }
    }
}
