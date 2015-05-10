using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X;
using Pulsar4X.UI;
using Pulsar4X.UI.GLUtilities;
using OpenTK;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;
using System.ComponentModel;

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
                    {
                        switch (m_oSystemContect.SSEntity)
                        {
                                ///< @todo Check if we really need these switches.
                            case StarSystemEntityType.TaskGroup:
                                m_oSystemContect.Entity.PropertyChanged -= m_oSystemContect_PropertyChanged;
                                break;
                            case StarSystemEntityType.Population:
                                m_oSystemContect.Entity.PropertyChanged -= m_oSystemContect_PropertyChanged;
                                break;
                            case StarSystemEntityType.Missile:
                                m_oSystemContect.Entity.PropertyChanged -= m_oSystemContect_PropertyChanged;
                                break;
                        }

                    }
                    m_oSystemContect = value as SystemContact;
                    if (value != null)
                    {
                        switch (m_oSystemContect.SSEntity)
                        {
                            case StarSystemEntityType.TaskGroup:
                                m_oSystemContect.Entity.PropertyChanged += m_oSystemContect_PropertyChanged;
                                break;
                            case StarSystemEntityType.Population:
                                m_oSystemContect.Entity.PropertyChanged += m_oSystemContect_PropertyChanged;
                                break;
                            case StarSystemEntityType.Missile:
                                m_oSystemContect.Entity.PropertyChanged += m_oSystemContect_PropertyChanged;
                                break;
                        }

                    }
                }
            }
        }

        void m_oSystemContect_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Name")
            {
                Lable.Text = m_oSystemContect.Entity.Name;
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
        private TravelLine m_oTravelLine { get; set; }

        /// <summary>
        /// What was the last tick this contact element checked its sensors? lib should store the tick when sensors are modified.
        /// </summary>
        private uint _LastSensorUpdateTick { get; set; }

        /// <summary>
        /// List of sensor contact elements.
        /// </summary>
        private BindingList<SensorElement> _SensorContactElements { get; set; }

        /// <summary>
        /// Contact Element's copy of default effect.
        /// </summary>
        GLEffect _DefaultEffect { get; set; }


        public ContactElement()
            : base()
        {
            _SensorContactElements = new BindingList<SensorElement>();
            _DefaultEffect = null;
        }

        public ContactElement(GLEffect a_oDefaultEffect, SystemContact a_oContact)
            : base(a_oContact)
        {
            // Create travel Line element:
            m_oTravelLine = new TravelLine(a_oDefaultEffect, a_oContact.faction.FactionColor);
            this.Children.Add(m_oTravelLine);

            _LastSensorUpdateTick = 0;
            _SensorContactElements = new BindingList<SensorElement>();
            _DefaultEffect = a_oDefaultEffect;
        }

        public override void Render()
        {
            switch (m_oSystemContect.SSEntity)
            {
                case StarSystemEntityType.TaskGroup:
                    TaskGroupTN TaskGroup = m_oSystemContect.Entity as TaskGroupTN;
                    /// <summary>
                    /// Update this contact's sensor elements.
                    /// </summary>
                    if (TaskGroup.SensorUpdateTick != _LastSensorUpdateTick)
                    {
                        if (_SensorContactElements.Count == 0)
                        {
                            foreach (ActiveSensorTN Sensor in TaskGroup.ActiveSensorQue)
                            {
                                ActiveSensorDefTN SensorDef = Sensor.aSensorDef;

                                double factor = Constants.Units.KmPerAu / 10000.0;
                                double AURadius = (double)SensorDef.maxRange / factor;

                                Vector3 TGPos = new Vector3((float)TaskGroup.Contact.Position.X, (float)TaskGroup.Contact.Position.Y, 0.0f);

                                SensorElement NSE = new SensorElement(_DefaultEffect, TGPos, (float)AURadius, System.Drawing.Color.Yellow, Sensor.Name , Sensor, ParentSceen);
                                _SensorContactElements.Add(NSE);
                            }
                        }
                        else
                        {
                        }
                    }
                    break;
                case StarSystemEntityType.Population:
                    break;
                case StarSystemEntityType.Missile:
                    break;
            }


            foreach (GLPrimitive oPrimitive in m_lPrimitives)
            {
                oPrimitive.Render();
            }

            foreach (SensorElement sElement in _SensorContactElements)
            {
                sElement.Render();
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

            /// <summary>
            /// update position of the selected contact and its travelline:
            /// </summary>
            Vector3 pos = Vector3.Zero, lastPos = Vector3.Zero;
            switch (m_oSystemContect.SSEntity)
            {
                case StarSystemEntityType.TaskGroup:
                    pos = new Vector3((float)m_oSystemContect.Position.X, (float)m_oSystemContect.Position.Y, 0.0f);
                    lastPos = new Vector3((float)m_oSystemContect.LastPosition.X, (float)m_oSystemContect.LastPosition.Y, 0.0f);

                    TaskGroupTN TaskGroup = m_oSystemContect.Entity as TaskGroupTN;

                    if (TaskGroup.DrawTravelLine != 3)
                    {
                        m_oTravelLine.StartPos = lastPos;
                        m_oTravelLine.EndPos = pos;
                    }
                    else if (TaskGroup.DrawTravelLine == 3)
                    {
                        m_oTravelLine.StartPos = pos;
                        m_oTravelLine.EndPos = pos;
                    }

                    if (TaskGroup.DrawTravelLine == 2)
                    {
                        TaskGroup.DrawTravelLine = 3;
                    }
                    break;
                case StarSystemEntityType.Population:
                    break;
                case StarSystemEntityType.Missile:
                    pos = new Vector3((float)m_oSystemContect.Position.X, (float)m_oSystemContect.Position.Y, 0.0f);
                    lastPos = new Vector3((float)m_oSystemContect.LastPosition.X, (float)m_oSystemContect.LastPosition.Y, 0.0f);

                    OrdnanceGroupTN MissileGroup = m_oSystemContect.Entity as OrdnanceGroupTN;

                    if (MissileGroup.DrawTravelLine != 3)
                    {
                        m_oTravelLine.StartPos = lastPos;
                        m_oTravelLine.EndPos = pos;
                    }
                    else if (MissileGroup.DrawTravelLine == 3)
                    {
                        m_oTravelLine.StartPos = pos;
                        m_oTravelLine.EndPos = pos;
                    }

                    if (MissileGroup.DrawTravelLine == 2)
                    {
                        MissileGroup.DrawTravelLine = 3;
                    }
                    break;
            }

            if (_SensorContactElements.Count != 0)
            {
                /// <summary>
                /// Update the positions of the sensors. pos should already be calculated by the above.
                /// </summary>
                foreach( SensorElement SElement in _SensorContactElements)
                {
                    SElement.SetActualPosition(pos);
                }
            }




            PrimaryPrimitive.Position = pos;
            Lable.Position = pos;

            // Adjust the size of the text so it is always 10 point:
            Lable.Size = UIConstants.DEFAULT_TEXT_SIZE / a_fZoomScaler;

            switch (m_oSystemContect.SSEntity)
            {
                case StarSystemEntityType.TaskGroup:
                    Lable.Text = m_oSystemContect.Entity.Name;
                    break;
            }

            foreach (SensorElement sElement in _SensorContactElements)
            {
                sElement.Refresh(a_fZoomScaler);
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
    }
}
