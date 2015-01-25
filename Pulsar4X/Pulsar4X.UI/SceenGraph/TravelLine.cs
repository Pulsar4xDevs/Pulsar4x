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
    /// Repesents a line indicating the distance traveled by an object.
    /// </summary>
    class TravelLine : SceenElement
    {
        public override GameEntity SceenEntity { get; set; }

        // Chache start and end positions:
        private Vector3 m_v3Start;
        private Vector3 m_v3End;

        /// <summary>
        /// Set the starting position of the travel line, i.e. the previos position of the travelling entity.
        /// </summary>
        public Vector3 StartPos
        {
            get
            {
                return m_v3Start;
            }
            set
            {
                m_v3Start = value;
                SetMeasurementStartPos(m_v3Start);
            }
        }

        /// <summary>
        /// The ending position of the travel line, i.e. the current position of the travelling entity.
        /// </summary>
        public Vector3 EndPos
        {
            get
            {
                return m_v3End;
            }
            set
            {
                m_v3End = value;
                SetMeasurementEndPos(m_v3End);
            }
        }


        public TravelLine()
            : base()
        {

        }

        public TravelLine(GLEffect a_oDefaultEffect, System.Drawing.Color a_oColor)
            : base()
        {
            m_oPrimaryPrimitive = new GLLine(a_oDefaultEffect, Vector3.Zero, new Vector2(1f, 1f), a_oColor, UIConstants.Textures.DEFAULT_TEXTURE);
            m_lPrimitives.Add(m_oPrimaryPrimitive);
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

        public override void Refresh(float a_fZoomScaler)
        {
            // loop through any children:
            foreach (SceenElement oElement in m_lChildren)
            {
                oElement.Refresh(a_fZoomScaler);
            }
        }

        public override Guid GetSelected(Vector3 a_v3AtPos)
        {
            Guid oElementID = Guid.Empty;

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

        private void SetMeasurementStartPos(Vector3 a_v3Pos)
        {
            GLLine temp = this.PrimaryPrimitive as GLLine;

            a_v3Pos.X = a_v3Pos.X / 2;
            a_v3Pos.Y = a_v3Pos.Y / 2;

            if (temp != null)
            {
                temp.Position = a_v3Pos;
                temp.Verticies[0].m_v4Position = new Vector4(a_v3Pos.X, a_v3Pos.Y, a_v3Pos.Z, 1.0f);
                temp.UpdateVBOs();
            }
        }

        private void SetMeasurementEndPos(Vector3 a_v3Pos)
        {
            GLLine temp = this.PrimaryPrimitive as GLLine;

            a_v3Pos.X = a_v3Pos.X / 2;
            a_v3Pos.Y = a_v3Pos.Y / 2;

            // atart pos (vert 0) is 0, 0 in the lines coord space, so we need to setup our end accordingly.
            float XAdjust = a_v3Pos.X - temp.Verticies[0].m_v4Position.X;
            float YAdjust = a_v3Pos.Y - temp.Verticies[0].m_v4Position.Y;

            a_v3Pos.X = a_v3Pos.X + XAdjust;
            a_v3Pos.Y = a_v3Pos.Y + YAdjust;

            if (temp != null)
            {
                temp.PosEnd = a_v3Pos;
            }
        }
    }
}
