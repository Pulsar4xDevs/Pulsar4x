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
    /// Sensor contact element in a scene graph.
    /// </summary>
    class ContactElement : SceenElement
    {
        private SystemContact m_oSystemContect;

        public override GameEntity SceenEntity
        {
            get
            {
                return m_oSystemContect;
            }
            set
            {
                if (m_oSystemContect != value)
                {
                    if (m_oSystemContect != null)
                        m_oSystemContect.TaskGroup.PropertyChanged -= m_oSystemContect_PropertyChanged;
                    m_oSystemContect = value as SystemContact;
                    if (value != null)
                        m_oSystemContect.TaskGroup.PropertyChanged += m_oSystemContect_PropertyChanged;
                }
            }
        }

        void m_oSystemContect_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Name")
            {
                // Change Label here! If we are doing this by system contact, then we need to check what type of StarSystemEntity this contact is.
                switch(m_oSystemContect.SSEntity)
                {
                    case StarSystemEntityType.TaskGroup:
                        Lable.Text = m_oSystemContect.TaskGroup.Name;
                    break;
                    case StarSystemEntityType.Population:
                        Lable.Text = m_oSystemContect.Pop.Name;
                    break;
                    case StarSystemEntityType.Missile:
                        Lable.Text = m_oSystemContect.MissileGroup.Name;
                    break;
                }

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
        private TravelLine m_oTravelLine;

        public ContactElement()
            : base()
        {

        }

        public ContactElement(GLEffect a_oDefaultEffect, SystemContact a_oContact)
            : base(a_oContact)
        {
            // Create travel Line element:
            m_oTravelLine = new TravelLine(a_oDefaultEffect, a_oContact.faction.FactionColor);
            this.Children.Add(m_oTravelLine);
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

            // render lable:
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
            // Adjust the size of the Primary Sprite (the TG icon) if necessary.
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

            // update position:
            Vector3 pos = new Vector3((float)m_oSystemContect.TaskGroup.Contact.XSystem, (float)m_oSystemContect.TaskGroup.Contact.YSystem, 0.0f);
            Vector3 lastPos = new Vector3((float)m_oSystemContect.TaskGroup.Contact.LastXSystem, (float)m_oSystemContect.TaskGroup.Contact.LastYSystem, 0.0f);

            if (m_oSystemContect.TaskGroup.DrawTravelLine != 3)
            {
                m_oTravelLine.StartPos = lastPos;
                m_oTravelLine.EndPos = pos;
            }
            if (m_oSystemContect.TaskGroup.DrawTravelLine == 2)
            {
                m_oSystemContect.TaskGroup.DrawTravelLine = 3;
            }

            PrimaryPrimitive.Position = pos;
            Lable.Position = pos;

            // Adjust the size of the text so it is always 10 point:
            Lable.Size = UIConstants.DEFAULT_TEXT_SIZE / a_fZoomScaler;
            Lable.Text = m_oSystemContect.TaskGroup.Name;

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
