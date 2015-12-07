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
        /// Is this contact element's sensor list in sequence with the taskgroup that it represents? every time there is a sensor change the taskgroup ack will be incremented.
        /// </summary>
        private uint _LastSensorUpdateAck { get; set; }

        /// <summary>
        /// List of sensor contact elements.
        /// </summary>
        private Dictionary<Guid, SensorElement> _SensorContactElements { get; set; }

        /// <summary>
        /// Contact Element's copy of default effect.
        /// </summary>
        GLEffect _DefaultEffect { get; set; }


        public ContactElement()
            : base()
        {
            _LastSensorUpdateAck = 0;
            _SensorContactElements = new Dictionary<Guid,SensorElement>();
            _DefaultEffect = null;
        }

        public ContactElement(GLEffect a_oDefaultEffect, SystemContact a_oContact)
            : base(a_oContact)
        {
            // Create travel Line element:
            m_oTravelLine = new TravelLine(a_oDefaultEffect, a_oContact.faction.FactionColor);
            this.Children.Add(m_oTravelLine);

            _LastSensorUpdateAck = 0;
            _SensorContactElements = new Dictionary<Guid,SensorElement>();
            _DefaultEffect = a_oDefaultEffect;
        }

        /// <summary>
        /// Force the Sensor update for this contact.
        /// </summary>
        public void ForceSensorUpdate()
        {
            _LastSensorUpdateAck = 0;
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
                    if (TaskGroup.SensorUpdateAck != _LastSensorUpdateAck)
                    {
                        /// <summary>
                        /// Remove those sensors that are no longer active.
                        /// </summary>
                        BindingList<Guid> SensorRemoveList = new BindingList<Guid>();

                        foreach (KeyValuePair<Guid,SensorElement> SCE in _SensorContactElements)
                        {
                            switch(SCE.Value._sensorType)
                            {
                                case ComponentTypeTN.ActiveSensor:
                                    ActiveSensorTN aSensor = (SCE.Value.SceenEntity as ActiveSensorTN);
                                    if (aSensor.isActive == false || aSensor.isDestroyed == true || ParentSceen.ShowActives == false)
                                    {
                                        SensorRemoveList.Add(SCE.Key);
                                    }
                                break;
                                case ComponentTypeTN.PassiveSensor:
                                PassiveSensorTN pSensor = (SCE.Value.SceenEntity as PassiveSensorTN);
                                    /// <summary>
                                    /// Remove this sensor if it is destroyed, or if it isn't the current best thermal/em sensor. if a sensor is destroyed it should be replaced as the best em/thermal.
                                    /// </summary>
                                    if (pSensor != null)
                                    {
                                        if (pSensor.isDestroyed == true || ParentSceen.ShowPassives == false)
                                        {
                                            SensorRemoveList.Add(SCE.Key);
                                        }
                                        else
                                        {
                                            if (pSensor.pSensorDef.thermalOrEM == PassiveSensorType.Thermal)
                                            {
                                                if (pSensor != TaskGroup.BestThermal)
                                                {
                                                    SensorRemoveList.Add(SCE.Key);
                                                }
                                            }
                                            else
                                            {
                                                if (pSensor != TaskGroup.BestEM)
                                                {
                                                    SensorRemoveList.Add(SCE.Key);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        /// <summary>
                                        /// default passives will be null, so that should be handled here. if show passives is off, or a better passive shows up, remove the default passives.
                                        /// </summary>
                                        if (ParentSceen.ShowPassives == false || TaskGroup.BestThermal != null || TaskGroup.BestEM != null)
                                        {
                                            SensorRemoveList.Add(SCE.Key);
                                        }
                                    }
                                break;
                            }
                        }
                        if (SensorRemoveList.Count != 0)
                        {
                            foreach (Guid SCE in SensorRemoveList)
                                _SensorContactElements.Remove(SCE);
                        }

                        if (ParentSceen.ShowPassives == true)
                        {
                            /// <summary>
                            /// A taskgroup with no sensors still has the default package, so handle that.
                            /// </summary>
                            if (TaskGroup.BestEM == null && _SensorContactElements.ContainsKey(TaskGroup.Id) == false)
                            {
                                PassiveSensorDefTN pSensorDef = TaskGroup.TaskGroupFaction.ComponentList.DefaultPassives;
                                double factor = Constants.Units.KmPerAu / Constants.GameConstants.BasicUnitOfDistance;
                                double AURadius = (double)pSensorDef.range * ((float)Constants.SensorTN.DefaultPassiveSignature / (float)ParentSceen.ShowPassiveSignatureRange) / factor;

                                Vector3 TGPos = new Vector3((float)TaskGroup.Contact.Position.X, (float)TaskGroup.Contact.Position.Y, 0.0f);

                                SensorElement NSE = new SensorElement(_DefaultEffect, TGPos, (float)AURadius, System.Drawing.Color.Blue, "", null, ComponentTypeTN.PassiveSensor, ParentSceen);

                                //definitely a kludge here to make SCE work with default passives.
                                _SensorContactElements.Add(TaskGroup.Id, NSE);
                            }
                            else if (TaskGroup.BestEM != null && _SensorContactElements.ContainsKey(TaskGroup.BestEM.pSensorDef.Id) == false)
                            {
                                PassiveSensorDefTN pSensorDef = TaskGroup.BestEM.pSensorDef;
                                double factor = Constants.Units.KmPerAu / Constants.GameConstants.BasicUnitOfDistance;
                                double AURadius = (double)pSensorDef.range * ((float)Constants.SensorTN.DefaultPassiveSignature / (float)ParentSceen.ShowPassiveSignatureRange) / factor;

                                Vector3 TGPos = new Vector3((float)TaskGroup.Contact.Position.X, (float)TaskGroup.Contact.Position.Y, 0.0f);

                                SensorElement NSE = new SensorElement(_DefaultEffect, TGPos, (float)AURadius, System.Drawing.Color.Blue, TaskGroup.BestEM.Name, TaskGroup.BestEM, TaskGroup.BestEM.pSensorDef.componentType, ParentSceen);
                                _SensorContactElements.Add(TaskGroup.BestEM.pSensorDef.Id, NSE);
                            }

                            /// <summary>
                            /// A taskgroup with no sensors still has the default package, so handle that.
                            /// </summary>
                            if (TaskGroup.BestThermal == null && _SensorContactElements.ContainsKey(TaskGroup.Ships[0].Id) == false)
                            {
                                PassiveSensorDefTN pSensorDef = TaskGroup.TaskGroupFaction.ComponentList.DefaultPassives;
                                double factor = Constants.Units.KmPerAu / Constants.GameConstants.BasicUnitOfDistance;
                                double AURadius = (double)pSensorDef.range * ((float)Constants.SensorTN.DefaultPassiveSignature / (float)ParentSceen.ShowPassiveSignatureRange) / factor;

                                Vector3 TGPos = new Vector3((float)TaskGroup.Contact.Position.X, (float)TaskGroup.Contact.Position.Y, 0.0f);

                                SensorElement NSE = new SensorElement(_DefaultEffect, TGPos, (float)AURadius, System.Drawing.Color.Red, "", null, ComponentTypeTN.PassiveSensor, ParentSceen);

                                //yep, its a kludge.
                                _SensorContactElements.Add(TaskGroup.Ships[0].Id, NSE);
                            }
                            else if (TaskGroup.BestThermal != null && _SensorContactElements.ContainsKey(TaskGroup.BestThermal.pSensorDef.Id) == false)
                            {
                                PassiveSensorDefTN pSensorDef = TaskGroup.BestThermal.pSensorDef;
                                double factor = Constants.Units.KmPerAu / Constants.GameConstants.BasicUnitOfDistance;
                                double AURadius = (double)pSensorDef.range * ((float)Constants.SensorTN.DefaultPassiveSignature / (float)ParentSceen.ShowPassiveSignatureRange) / factor;

                                Vector3 TGPos = new Vector3((float)TaskGroup.Contact.Position.X, (float)TaskGroup.Contact.Position.Y, 0.0f);

                                SensorElement NSE = new SensorElement(_DefaultEffect, TGPos, (float)AURadius, System.Drawing.Color.Red, TaskGroup.BestThermal.Name, TaskGroup.BestThermal, TaskGroup.BestThermal.pSensorDef.componentType, ParentSceen);
                                _SensorContactElements.Add(pSensorDef.Id, NSE);
                            }
                        }


                        /// <summary>
                        /// Check for newly activated sensors.
                        /// </summary>
                        if (ParentSceen.ShowActives == true)
                        {
                            foreach (ActiveSensorTN Sensor in TaskGroup.ActiveSensorQue)
                            {
                                /// <summary>
                                /// This sensor survived the above cleanup, and is in the sensor contact element list, so don't re check whether it should be here.
                                /// </summary>
                                if (_SensorContactElements.ContainsKey(Sensor.aSensorDef.Id) == true)
                                    continue;

                                ActiveSensorDefTN SensorDef = Sensor.aSensorDef;

                                double factor = Constants.Units.KmPerAu / Constants.GameConstants.BasicUnitOfDistance;
                                double AURadius = (double)SensorDef.maxRange / factor;

                                Vector3 TGPos = new Vector3((float)TaskGroup.Contact.Position.X, (float)TaskGroup.Contact.Position.Y, 0.0f);

                                SensorElement NSE = new SensorElement(_DefaultEffect, TGPos, (float)AURadius, System.Drawing.Color.Turquoise, Sensor.Name, Sensor, Sensor.aSensorDef.componentType, ParentSceen);
                                _SensorContactElements.Add(SensorDef.Id, NSE);
                            }
                        }

                        _LastSensorUpdateAck = TaskGroup.SensorUpdateAck;
                    }
                    break;
                case StarSystemEntityType.Population:
                    Population CurrentPop = (m_oSystemContect.Entity as Population);
                    /// <summary>
                    /// Populations only have one sensor element.
                    /// </summary>
                    if (CurrentPop._sensorUpdateAck != _LastSensorUpdateAck)
                    {
                        _SensorContactElements.Clear();

                        if (ParentSceen.ShowPassives == true)
                        {
                            /// <summary>
                            /// This calculates the default detection distance for strength 1000 signatures.
                            /// </summary>
                            int DSTS = (int)Math.Floor(CurrentPop.Installations[(int)Installation.InstallationType.DeepSpaceTrackingStation].Number);
                            int SensorTech = CurrentPop.Faction.FactionTechLevel[(int)Faction.FactionTechnology.DSTSSensorStrength];
                            if (SensorTech > Constants.Colony.DeepSpaceMax)
                                SensorTech = Constants.Colony.DeepSpaceMax;
#warning if EM strength differs from Thermal handle that here.
                            int ScanStrength = DSTS * Constants.Colony.ThermalDeepSpaceStrength[SensorTech] * 100;

                            double factor = Constants.Units.KmPerAu / Constants.GameConstants.BasicUnitOfDistance;
                            double AURadius = (double)ScanStrength * ((float)Constants.SensorTN.DefaultPassiveSignature / (float)ParentSceen.ShowPassiveSignatureRange) / factor;

                            Vector3 PopPosition = new Vector3((float)CurrentPop.Contact.Position.X, (float)CurrentPop.Contact.Position.Y, 0.0f);

                            /// <summary>
                            /// Type is set to TypeCount because only the taskgroup section requires it. Likewise for CurrentPop, and CurrentPop.Id, these aren't strictly necessary, but the
                            /// taskgroup section requires more information as more sensor contact elements can be associated with a taskgroup.
                            /// </summary>
                            SensorElement NSE = new SensorElement(_DefaultEffect, PopPosition, (float)AURadius, System.Drawing.Color.Purple, CurrentPop.Name + " DSTS Coverage", CurrentPop, ComponentTypeTN.TypeCount, ParentSceen);
                            _SensorContactElements.Add(CurrentPop.Id, NSE);
                        }
                        _LastSensorUpdateAck = CurrentPop._sensorUpdateAck;
                    }
                    break;
                case StarSystemEntityType.Missile:
                    OrdnanceGroupTN MissileGroup = (m_oSystemContect.Entity as OrdnanceGroupTN);
                    /// <summary>
                    /// This ordnance group was just created, so check whether or not any sensors need to be displayed. Or the display could request that passives or actives not be displayed.
                    /// </summary>
                    if (_LastSensorUpdateAck != MissileGroup._sensorUpdateAck)
                    {
                        _SensorContactElements.Clear();
                        OrdnanceDefTN OrdDef = MissileGroup.missiles[0].missileDef;

                        if (OrdDef.aSD != null && ParentSceen.ShowActives == true)
                        {
                            double factor = Constants.Units.KmPerAu / Constants.GameConstants.BasicUnitOfDistance;
                            double AURadius = (double)OrdDef.aSD.maxRange / factor;

                            Vector3 MGPos = new Vector3((float)MissileGroup.contact.Position.X, (float)MissileGroup.contact.Position.Y, 0.0f);

                             SensorElement NSE = new SensorElement(_DefaultEffect, MGPos, (float)AURadius, System.Drawing.Color.Turquoise, 
                                                                   MissileGroup.missiles[0].missileDef.Name + " Active Sensor", MissileGroup, ComponentTypeTN.TypeCount, ParentSceen);
                             _SensorContactElements.Add(OrdDef.aSD.Id, NSE);
                        }

                        if (OrdDef.tHD != null && ParentSceen.ShowPassives == true)
                        {
                            double factor = Constants.Units.KmPerAu / Constants.GameConstants.BasicUnitOfDistance;
                            double AURadius = (double)OrdDef.tHD.range * ((float)Constants.SensorTN.DefaultPassiveSignature / (float)ParentSceen.ShowPassiveSignatureRange) / factor;

                            Vector3 MGPos = new Vector3((float)MissileGroup.contact.Position.X, (float)MissileGroup.contact.Position.Y, 0.0f);

                            SensorElement NSE = new SensorElement(_DefaultEffect, MGPos, (float)AURadius, System.Drawing.Color.Red,
                                                                  MissileGroup.missiles[0].missileDef.Name + " Thermal Sensor", MissileGroup, ComponentTypeTN.TypeCount, ParentSceen);
                            _SensorContactElements.Add(OrdDef.tHD.Id, NSE);
                        }

                        if (OrdDef.eMD != null && ParentSceen.ShowPassives == true)
                        {
                            double factor = Constants.Units.KmPerAu / Constants.GameConstants.BasicUnitOfDistance;
                            double AURadius = (double)OrdDef.eMD.range * ((float)Constants.SensorTN.DefaultPassiveSignature / (float)ParentSceen.ShowPassiveSignatureRange) / factor;

                            Vector3 MGPos = new Vector3((float)MissileGroup.contact.Position.X, (float)MissileGroup.contact.Position.Y, 0.0f);

                            SensorElement NSE = new SensorElement(_DefaultEffect, MGPos, (float)AURadius, System.Drawing.Color.Blue,
                                                                  MissileGroup.missiles[0].missileDef.Name + " EM Sensor", MissileGroup, ComponentTypeTN.TypeCount, ParentSceen);
                            _SensorContactElements.Add(OrdDef.eMD.Id, NSE);
                        }

                        _LastSensorUpdateAck = MissileGroup._sensorUpdateAck;
                    }
                    break;
            }


            foreach (GLPrimitive oPrimitive in m_lPrimitives)
            {
                oPrimitive.Render();
            }

            /// <summary>
            /// Adding sensor elements to children causes wierd behavior that I'd rather not deal with.
            /// </summary>
            foreach ( KeyValuePair<Guid,SensorElement> sElement in _SensorContactElements)
            {
                sElement.Value.Render();
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
                    pos = new Vector3((float)m_oSystemContect.Position.X, (float)m_oSystemContect.Position.Y, 0.0f);
                    lastPos = new Vector3((float)m_oSystemContect.LastPosition.X, (float)m_oSystemContect.LastPosition.Y, 0.0f);
                    m_oTravelLine.StartPos = pos;
                    m_oTravelLine.EndPos = pos;
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
                foreach (KeyValuePair<Guid, SensorElement> SElement in _SensorContactElements)
                {
                    SElement.Value.SetActualPosition(pos);
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

            foreach (KeyValuePair<Guid, SensorElement> sElement in _SensorContactElements)
            {
                sElement.Value.Refresh(a_fZoomScaler);
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
