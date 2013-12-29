using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X;
using Pulsar4X.UI;
using Pulsar4X.UI.GLUtilities;
using OpenTK;
using Pulsar4X.Entities;
using log4net;

namespace Pulsar4X.UI.SceenGraph
{
    public class MapMarker : SceenElement
    {

        public static readonly ILog logger = LogManager.GetLogger(typeof(MapMarker));

        private GameEntity m_oGameEntity;

        public override GameEntity SceenEntity
        {
            get
            {
                return m_oGameEntity;
            }
            set
            {
                if (m_oGameEntity != value)
                {
                    if(m_oGameEntity!= null)
                        m_oGameEntity.PropertyChanged -= m_oGameEntity_PropertyChanged;
                    m_oGameEntity = value;
                    if(value != null)
                        m_oGameEntity.PropertyChanged += m_oGameEntity_PropertyChanged;
                }
            }
        }

        void m_oGameEntity_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "Name")
            {
                // Change Label here! if this ever works.
                 Lable.Text = m_oGameEntity.Name;

                 GLUtilities.GLFont oNameLable = new GLUtilities.GLFont(ParentSceen.ParentSystemMap.oGLCanvas.DefaultEffect, Lable.Position,
                                                                                Lable.Size, System.Drawing.Color.Tan, UIConstants.Textures.DEFAULT_GLFONT2, m_oGameEntity.Name);

                 Lable = oNameLable;

                 ParentSceen.Refresh();

            }
        }

        /// <summary>
        /// The parent has to be referenceable for name changes.
        /// </summary>
        public Sceen ParentSceen { get; set; }

        public MapMarker()
            : base()
        { }

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


            // loop through any children:
            foreach (SceenElement oElement in m_lChildren)
            {
                oElement.Refresh(a_fZoomScaler);
            }
        }

        public override string ToString()
        {
            return Lable.Text;
        }
    }
}
