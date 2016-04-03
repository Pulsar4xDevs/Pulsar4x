using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Pulsar4X.Helpers;
using Pulsar4X.Helpers.GameMath;

namespace Pulsar4X.Entities
{
    
    /// <summary>
    /// The Atmosphere of a SystemBody or Moon.
    /// </summary>
    public class Atmosphere : GameEntity
    {
        /// <summary>
        /// Atmospheric Presure
        /// In Earth Atmospheres (atm).
        /// </summary>
        public float Pressure { get; set; }
        
        /// <summary>
        /// Weather or not the planet has abundent water.
        /// </summary>
        public bool Hydrosphere { get; set; }

        /// <summary>
        /// The percentage of the bodies sureface covered by water.
        /// </summary>
        public short HydrosphereExtent { get; set; }

        /// <summary>
        /// A measure of the greenhouse factor provided by this Atmosphere.
        /// </summary>
        public float GreenhouseFactor { get; set; }

        private float _greenhousePressure = 0;
        /// <summary>
        /// Pressure (in atm) of greenhouse gasses. but not really.
        /// to get this figure for a given gass toy would take its pressure 
        /// in the atmosphere and times it by the gasses GreenhouseEffect 
        /// which is a number between 1 and -1 normally.
        /// </summary>
        public float GreenhousePressure { get { return _greenhousePressure; } }

        /// <summary>
        /// How much light the body reflects. Affects temp.
        /// a number from 0 to 1.
        /// </summary>
        public float Albedo { get; set; }

        /// <summary>
        /// Temperature of the planet AFTER greenhouse effects are taken into considuration. 
        /// This is a factor of the base temp and Green House effects.
        /// In Degrees C.
        /// </summary>
        public float SurfaceTemperature { get; set; }

        private Dictionary<AtmosphericGas, float> _composition = new Dictionary<AtmosphericGas, float>();
        /// <summary>
        /// The composition of the atmosphere, i.e. what gases make it up and in what ammounts.
        /// In Earth Atmospheres (atm).
        /// </summary>
        public Dictionary<AtmosphericGas, float> Composition { get { return _composition; } }

        private string _atmosphereDescriptionInPercent = "";
        /// <summary>
        /// A sting describing the Atmosphere in Percentages, like this:
        /// "75% Nitrogen (N), 21% Oxygen (O), 3% Carbon dioxide (CO2), 1% Argon (Ar)"
        /// By Default ToString return this.
        /// </summary>
        public string AtomsphereDescriptionInPercent
        {
            get { return _atmosphereDescriptionInPercent; }
        }

        private string _atmosphereDescriptionInATM = "";
        /// <summary>
        /// A sting describing the Atmosphere in Atmospheres (atm), like this:
        /// "0.75atm Nitrogen (N), 0.21atm Oxygen (O), 0.03atm Carbon dioxide (CO2), 0.01atm Argon (Ar)"
        /// </summary>
        public string AtomsphereDescriptionATM
        {
            get { return _atmosphereDescriptionInATM; }
        }

        /// <summary>
        /// Returns true if The atmosphere exists (i.e. there are any gases in it), else it return false.
        /// </summary>
        public bool Exists 
        {
            get
            {
                if (_composition.Count > 0)
                    return true;

                return false;
            } 
        }

        public bool CanModify
        {
            get
            {
                if (ParentBody.Type == SystemBody.PlanetType.Terrestrial
                    || ParentBody.Type == SystemBody.PlanetType.Moon)
                    return true;  // only these bodies have atmospheres that can be terraformed.

                return false;
            }
        }

        private SystemBody _parentBody;
        /// <summary>
        /// The body this atmosphere belong to.
        /// </summary>
        public SystemBody ParentBody { get { return _parentBody; } }

        /// <summary>
        /// Is a gas responsible for setting hab rating to a minimum 2.0 present?
        /// </summary>
        public bool HazardOne { get; set; }
        /// <summary>
        /// Is a gas responsible for setting hab rating to a minimum 3.0 present?
        /// </summary>
        public bool HazardTwo { get; set; }


        /// <summary>
        /// Atmosphere Constructor
        /// </summary>
        public Atmosphere(SystemBody parentBody)
            : base()
        {
            _parentBody = parentBody;
            HazardOne = false;
            HazardTwo = false;
        }

        public override string ToString()
        {
            return _atmosphereDescriptionInPercent;
        }

        /// <summary>
        /// Updates the state of the bodies atmosphere. Run this after adding removing gasses or modifing albedo.
        /// @note For info on how I have tweaked this from aurora see: http://en.wikipedia.org/wiki/Stefan%E2%80%93Boltzmann_law
        /// @todo Calc Hydrosphere changes & update albedo accordingly.
        /// </summary>
        public void UpdateState()
        {
            if (Exists)
            {
                // clear old values.
                _atmosphereDescriptionInATM = "";
                _atmosphereDescriptionInPercent = "";
                Pressure = 0;
                _greenhousePressure = 0;

                foreach (var gas in _composition)
                {
                    _atmosphereDescriptionInATM += gas.Value.ToString("N4") + "atm " + gas.Key.Name + " " + gas.Key.ChemicalSymbol + ", ";
                    Pressure += gas.Value;

                    // only add a greenhouse gas if it is not frozen:
                    if (SurfaceTemperature >= gas.Key.BoilingPoint)
                    {
                        // actual greenhouse pressure adjusted by gas GreenhouseEffect.
                        // note that this produces the same affect as in aurora if all GreenhouseEffect bvalue are -1, 0 or 1.
                        _greenhousePressure += (float)gas.Key.GreenhouseEffect * gas.Value;
                    }
                }

                if (ParentBody.Type == SystemBody.PlanetType.GasDwarf
                    || ParentBody.Type == SystemBody.PlanetType.GasGiant
                    || ParentBody.Type == SystemBody.PlanetType.IceGiant)
                {
                    // special gas giant stuff, needed because we do not apply greenhouse factor to them:
                    SurfaceTemperature = ParentBody.BaseTemperature * (1 - Albedo);
                    Pressure = 1;       // because thats the deffenition of the surface of these planets, when 
                    // atmosphereic pressure = the pressure of earths atmosphere at its surface (what we call 1 atm).
                }
                else
                {
                    // From Aurora: Greenhouse Factor = 1 + (Atmospheric Pressure /10) + Greenhouse Pressure   (Maximum = 3.0)
                    GreenhouseFactor = (Pressure * 0.035F) + GreenhousePressure;  // note that we do without the extra +1 as it seems to give us better temps.
                    GreenhouseFactor = (float)GMath.Clamp(GreenhouseFactor, -3.0, 3.0);

                    // From Aurora: Surface Temperature in Kelvin = Base Temperature in Kelvin x Greenhouse Factor x Albedo
                    SurfaceTemperature = Temperature.ToKelvin(ParentBody.BaseTemperature);
                    SurfaceTemperature += SurfaceTemperature * GreenhouseFactor * (float)Math.Pow(1 - Albedo, 0.25);   // We need to raise albedo to the power of 1/4, see: http://en.wikipedia.org/wiki/Stefan%E2%80%93Boltzmann_law
                    SurfaceTemperature = Temperature.ToCelsius(SurfaceTemperature);
                }

                // loop a second time to work out atmo percentages:
                foreach (var gas in _composition)
                {
                    if (Pressure != 0)
                        _atmosphereDescriptionInPercent += (gas.Value / Pressure).ToString("P0") + " " + gas.Key.Name + " " + gas.Key.ChemicalSymbol + ", ";  ///< @todo this is not right!!
                }

                // trim trailing", " from the strings.
                _atmosphereDescriptionInATM = _atmosphereDescriptionInATM.Remove(_atmosphereDescriptionInATM.Length - 2);
                _atmosphereDescriptionInPercent = _atmosphereDescriptionInPercent.Remove(_atmosphereDescriptionInPercent.Length - 2);
            }
            else
            {
                // simply apply albedo, see here: http://en.wikipedia.org/wiki/Stefan%E2%80%93Boltzmann_law
                Pressure = 0;
                SurfaceTemperature = Temperature.ToKelvin(ParentBody.BaseTemperature);
                SurfaceTemperature = SurfaceTemperature * (float)Math.Pow(1 - Albedo, 0.25);   // We need to raise albedo to the power of 1/4
                SurfaceTemperature = Temperature.ToCelsius(SurfaceTemperature);
                _atmosphereDescriptionInATM = "None";
                _atmosphereDescriptionInPercent = "None";
            }
        }

        /// <summary>
        /// Use this when adding gass for terrforming.
        /// @note System gen does not use this function, instead it adds gasses directly to the Composition.
        /// </summary>
        /// <param name="gas">The gass to add.</param>
        /// <param name="ammount">The ammount of gass to add in atm. Provide a negative number to remove gas.</param>
        public void AddGas(AtmosphericGas gas, float ammount)
        {
            if (CanModify == false && Constants.GameSettings.TNTerraformingRules == false)
                return; // we dont care!!

            if (_composition.ContainsKey(gas))
            {
                _composition[gas] += ammount;

                if (ammount > 0)
                {
                    if(Constants.GameSettings.TNTerraformingRules == true)
                    {
                        if (gas.HazardOne == true && HazardTwo == false) //This planet has no hazard two gases that will override a hazard one setting.
                        {
                             HazardOne = true;
                        }

                        if (gas.HazardTwo == true) //Override the hazard one setting if set, or set hazard two directly.
                        {
                            HazardOne = false;
                            HazardTwo = true;
                        }
                    }
                    else
                    {
                        if(gas.IsToxic == true)
                        {
                            HazardOne = true;
                        }
                    }
                }

                if (_composition[gas] <= 0)
                {
                    if (Constants.GameSettings.TNTerraformingRules == true)
                    {
                        if (gas.HazardOne == true && HazardTwo == false)
                        {
                            bool hazard = false;
                            foreach (KeyValuePair<AtmosphericGas, float> atmGas in _composition)
                            {
                                /// <summary>
                                /// This planet just got rid of a hazard one gas, so look for another hazard one gas. if one is found the planet is still hazard one.
                                /// This planet is already not hazard two, so don't worry about that.
                                /// </summary>
                                if (atmGas.Key.HazardOne == true)
                                {
                                    hazard = true;
                                    break;
                                }
                            }
                            HazardOne = hazard;
                        }

                        if (gas.HazardTwo == true)
                        {
                            bool hazardOne = false;
                            bool hazardTwo = false;
                            foreach (KeyValuePair<AtmosphericGas, float> atmGas in _composition)
                            {
                                /// <summary>
                                /// This planet just got rid of a hazard two gas, so look for another hazard two gas. also keep an eye out for hazard one gases.
                                /// </summary>
                                if (atmGas.Key.HazardTwo == true)
                                {
                                    hazardTwo = true;
                                    break;
                                }
                                else if (atmGas.Key.HazardOne == true)
                                {
                                    hazardOne = true;
                                }
                            }
                            HazardTwo = hazardTwo;
                            if (HazardTwo == false)
                                HazardOne = hazardOne;
                        }
                    }
                    else
                    {
                        if (gas.IsToxic == true)
                        {
                            bool hazard = false;
                            foreach (KeyValuePair<AtmosphericGas, float> atmGas in _composition)
                            {
                                /// <summary>
                                /// This planet just got rid of a hazard one gas, so look for another hazard one gas. if one is found the planet is still hazard one.
                                /// This planet is already not hazard two, so don't worry about that.
                                /// </summary>
                                if (atmGas.Key.IsToxic == true)
                                {
                                    hazard = true;
                                    break;
                                }
                            }
                            ///<summary>
                            ///Atmosphere uses HazardOne to indicate IsToxic for non-TN rules.
                            ///</summary>
                            HazardOne = hazard;
                        }
                    }
                    _composition.Remove(gas);  // if there is none left, remove it.
                }

            }
            else if (ammount > 0)               // only add new gas if it is actuall adding (i.e. ammount is positive).
            {
                _composition.Add(gas, ammount);

                if (Constants.GameSettings.TNTerraformingRules == true)
                {
                    if (gas.HazardOne == true && HazardTwo == false)
                    {
                        HazardOne = true;
                    }

                    if (gas.HazardTwo == true)
                    {
                        HazardOne = false;
                        HazardTwo = true;
                    }
                }
                else
                {
                    /// <summary>
                    /// As before, HazardOne indicates IsToxic for non-TN terraforming rules.
                    /// </summary>
                    if (gas.IsToxic == true)
                        HazardOne = true;
                }
            }

            UpdateState();                  // update other state to reflect the new gas ammount.
        }
    }

    #region Data Binding

    /// <summary>
    /// Used for databinding, see here: http://blogs.msdn.com/b/msdnts/archive/2007/01/19/how-to-bind-a-datagridview-column-to-a-second-level-property-of-a-data-source.aspx
    /// </summary>
    public class AtmosphereTypeDescriptor : CustomTypeDescriptor
    {
        public AtmosphereTypeDescriptor(ICustomTypeDescriptor parent)
            : base(parent)
        { }

        public override PropertyDescriptorCollection GetProperties()
        {
            PropertyDescriptorCollection cols = base.GetProperties();
            PropertyDescriptor addressPD = cols["Atmosphere"];
            PropertyDescriptorCollection Atmo_child = addressPD.GetChildProperties();
            PropertyDescriptor[] array = new PropertyDescriptor[cols.Count + 6];

            cols.CopyTo(array, 0);
            array[cols.Count] = new SubPropertyDescriptor(addressPD, Atmo_child["AtomsphereDescriptionInPercent"], "Atmosphere_InPercent");
            array[cols.Count + 1] = new SubPropertyDescriptor(addressPD, Atmo_child["AtomsphereDescriptionATM"], "Atmosphere_InATM");
            array[cols.Count + 2] = new SubPropertyDescriptor(addressPD, Atmo_child["Pressure"], "Atmosphere_Pressure");
            array[cols.Count + 3] = new SubPropertyDescriptor(addressPD, Atmo_child["HydrosphereExtent"], "Atmosphere_HydrosphereExtent");
            array[cols.Count + 4] = new SubPropertyDescriptor(addressPD, Atmo_child["Albedo"], "Atmosphere_Albedo");
            array[cols.Count + 5] = new SubPropertyDescriptor(addressPD, Atmo_child["SurfaceTemperature"], "Atmosphere_SurfaceTemperature");

            PropertyDescriptorCollection newcols = new PropertyDescriptorCollection(array);
            return newcols;
        }

        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            PropertyDescriptorCollection cols = base.GetProperties(attributes);
            PropertyDescriptor addressPD = cols["Atmosphere"];
            PropertyDescriptorCollection Atmo_child = addressPD.GetChildProperties();
            PropertyDescriptor[] array = new PropertyDescriptor[cols.Count + 6];

            cols.CopyTo(array, 0);
            array[cols.Count] = new SubPropertyDescriptor(addressPD, Atmo_child["AtomsphereDescriptionInPercent"], "Atmosphere_InPercent");
            array[cols.Count + 1] = new SubPropertyDescriptor(addressPD, Atmo_child["AtomsphereDescriptionATM"], "Atmosphere_InATM");
            array[cols.Count + 2] = new SubPropertyDescriptor(addressPD, Atmo_child["Pressure"], "Atmosphere_Pressure");
            array[cols.Count + 3] = new SubPropertyDescriptor(addressPD, Atmo_child["HydrosphereExtent"], "Atmosphere_HydrosphereExtent");
            array[cols.Count + 4] = new SubPropertyDescriptor(addressPD, Atmo_child["Albedo"], "Atmosphere_Albedo");
            array[cols.Count + 5] = new SubPropertyDescriptor(addressPD, Atmo_child["SurfaceTemperature"], "Atmosphere_SurfaceTemperature");

            PropertyDescriptorCollection newcols = new PropertyDescriptorCollection(array);
            return newcols;
        }
    }

    #endregion
}
