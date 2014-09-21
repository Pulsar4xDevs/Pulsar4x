using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X;
using Pulsar4X.UI;
using Pulsar4X.UI.GLUtilities;
using OpenTK;
using Pulsar4X.Entities;
using Pulsar4X.Lib;

namespace Pulsar4X.UI.SceenGraph
{
    public class CircleElement : SceenElement
    {
        public override GameEntity SceenEntity { get; set; }

        private Vector3 m_v3CurrentPosition;
        public Vector3 CurrentPosition
        {
            get { return m_v3CurrentPosition; }
            set
            {
                m_v3CurrentPosition = value;
                SetActualPosition(m_v3CurrentPosition);
            }
        }

        private OrbitingEntity m_oOrbitEntity;


        public CircleElement()
            : base()
        {
            
        }

        public CircleElement(GLEffect a_oDefaultEffect, Vector3 a_oPosition, OrbitingEntity a_oOrbitEntity, System.Drawing.Color a_oColor)
            : base()
        {

            m_oPrimaryPrimitive = new GLCircle(a_oDefaultEffect,
                        a_oPosition,
                        a_oOrbitEntity,
                        a_oColor,
                        UIConstants.Textures.DEFAULT_TEXTURE);

            m_oOrbitEntity = a_oOrbitEntity;


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

        /// <summary>
        /// Temporary as hell kludge just to see if it works.
        /// </summary>
        /// <param name="a_v3Pos"></param>
        private void SetActualPosition(Vector3 a_v3Pos)
        {

            GLCircle temp = this.PrimaryPrimitive as GLCircle;

            if (temp != null)
            {

                Vector3 PositionChange = a_v3Pos - temp.Position;

                if (PositionChange.X != 0.0 && PositionChange.Y != 0.0)
                {
                    String Entry = String.Format("Position:{0} Set:{1} Change:{2}", temp.Position, a_v3Pos, PositionChange);
                    MessageEntry Msg = new MessageEntry(MessageEntry.MessageType.Count, null, null, GameState.Instance.GameDateTime,
                                                       (GameState.SE.CurrentTick - GameState.SE.lastTick), Entry);
                    GameState.Instance.Factions[0].MessageLog.Add(Msg);
                }
                temp.Position = a_v3Pos;

#warning you know, there is almost certainly a better way to do this.
                for (int i = 0; i < 360; ++i)
                {
                    temp.Verticies[i].m_v4Position.X = temp.Verticies[i].m_v4Position.X + PositionChange.X;
                    temp.Verticies[i].m_v4Position.Y = temp.Verticies[i].m_v4Position.Y + PositionChange.Y;
                }


                temp.UpdateVBOs();
            }
        }
    }
}
