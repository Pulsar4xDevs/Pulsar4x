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
                    if (m_oGameEntity != null)
                        m_oGameEntity.PropertyChanged -= m_oGameEntity_PropertyChanged;
                    m_oGameEntity = value;
                    if (value != null)
                        m_oGameEntity.PropertyChanged += m_oGameEntity_PropertyChanged;
                }
            }
        }

        void m_oGameEntity_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Name")
            {
                /// <summary>
                /// Change Label here!
                /// </summary>
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



        /// <summary>
        /// Line from Last Position to Current Position for taskgroups.
        /// </summary>
        private MeasurementElement TravelLine { get; set; }


        public MapMarker()
            : base()
        { }

        public MapMarker(GLEffect a_oDefaultEffect, System.Drawing.Color MMColor)
        {
            // Create measurement element:
            TravelLine = new MeasurementElement();
            TravelLine.PrimaryPrimitive = new GLLine(a_oDefaultEffect, Vector3.Zero, new Vector2(1.0f, 1.0f), MMColor, UIConstants.Textures.DEFAULT_TEXTURE);
            TravelLine.AddPrimitive(TravelLine.PrimaryPrimitive);
            TravelLine.Lable = new GLUtilities.GLFont(a_oDefaultEffect, Vector3.Zero, UIConstants.DEFAULT_TEXT_SIZE, MMColor, UIConstants.Textures.DEFAULT_GLFONT2, "");

        }

        public override void Render()
        {
            foreach (GLPrimitive oPrimitive in m_lPrimitives)
            {
                oPrimitive.Render();

                if (TravelLine != null)
                {
                    TravelLine.Render();
                }
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

            if (TravelLine != null)
            {
                TravelLine.Refresh(a_fZoomScaler);
            }

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

        public void SetMeasurementStartPos(Vector3 a_v3Pos)
        {

            GLLine temp = TravelLine.PrimaryPrimitive as GLLine;

            a_v3Pos.X = a_v3Pos.X / 2;
            a_v3Pos.Y = a_v3Pos.Y / 2;

            if (temp != null)
            {
                temp.Position = a_v3Pos;
                temp.Verticies[0].m_v4Position = new Vector4(a_v3Pos.X, a_v3Pos.Y, a_v3Pos.Z, 1.0f);
                temp.UpdateVBOs();
            }
        }

        public void SetMeasurementEndPos(Vector3 a_v3Pos)
        {
            GLLine temp = TravelLine.PrimaryPrimitive as GLLine;

            a_v3Pos.X = a_v3Pos.X / 2;
            a_v3Pos.Y = a_v3Pos.Y / 2;

            float XAdjust = a_v3Pos.X - TravelLine.Primitives[0].Verticies[0].m_v4Position.X;
            float YAdjust = a_v3Pos.Y - TravelLine.Primitives[0].Verticies[0].m_v4Position.Y;

            a_v3Pos.X = a_v3Pos.X + XAdjust;
            a_v3Pos.Y = a_v3Pos.Y + YAdjust;

            if (temp != null)
            {
                temp.PosEnd = a_v3Pos;
            }
        }
    }
}
