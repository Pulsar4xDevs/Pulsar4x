using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Entities;
using Newtonsoft.Json;
using Pulsar4X.Lib;
using System.ComponentModel;

namespace Pulsar4X.Entities
{
    [TypeDescriptionProvider(typeof(OrbitingEntityTypeDescriptionProvider))]
    public abstract class OrbitingEntity : StarSystemEntity
    {
        [System.ComponentModel.Browsable(true)]
        public Orbit Orbit { get; set; }

        /// <summary>
        /// The Parent Orbiting Body, for Planets and stars this is the same as Primary, for moons it will be a planet.
        /// </summary>
        [System.ComponentModel.Browsable(false)]
        public OrbitingEntity Parent { get; set; }

        /// <summary>
        /// equitorial radius (in AU)
        /// </summary>
        [System.ComponentModel.Browsable(true)]
        public double Radius { get; set; }

        /// <summary>
        /// Indicates weither the system body supports populations and can be settled by Plaerys/NPRs..
        /// </summary>
        public bool SupportsPopulations { get; set; }

        public OrbitingEntity()
            : base()
        {
        }
    }

    #region Data Binding Stuff

    /// <summary>
    /// Used for databinding, see here: http://blogs.msdn.com/b/msdnts/archive/2007/01/19/how-to-bind-a-datagridview-column-to-a-second-level-property-of-a-data-source.aspx
    /// </summary>
    public class SubPropertyDescriptor : PropertyDescriptor
    {
        private PropertyDescriptor _subPD;
        private PropertyDescriptor _parentPD;

        public SubPropertyDescriptor(PropertyDescriptor parentPD, PropertyDescriptor subPD, string pdname)
            : base(pdname, null)
        {
            _subPD = subPD;
            _parentPD = parentPD;
        }

        public override bool IsReadOnly { get { return false; } }
        public override void ResetValue(object component) { }
        public override bool CanResetValue(object component) { return false; }
        public override bool ShouldSerializeValue(object component)
        {
            return true;
        }

        public override Type ComponentType
        {
            get { return _parentPD.ComponentType; }
        }

        public override Type PropertyType { 
            get { 
                return _subPD.PropertyType; 
            } 
        }

        public override object GetValue(object component)
        {
            return _subPD.GetValue(_parentPD.GetValue(component));
        }

        public override void SetValue(object component, object value)
        {
            _subPD.SetValue(_parentPD.GetValue(component), value);
            OnValueChanged(component, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Used for databinding, see here: http://blogs.msdn.com/b/msdnts/archive/2007/01/19/how-to-bind-a-datagridview-column-to-a-second-level-property-of-a-data-source.aspx
    /// </summary>
    public class OrebitTypeDescriptor : CustomTypeDescriptor
    {
        public OrebitTypeDescriptor(ICustomTypeDescriptor parent)
            : base(parent)
        { }

        public override PropertyDescriptorCollection GetProperties()
        {
            PropertyDescriptorCollection cols = base.GetProperties();
            PropertyDescriptor addressPD = cols["Orbit"];
            PropertyDescriptorCollection Orbit_child = addressPD.GetChildProperties();
            PropertyDescriptor[] array = new PropertyDescriptor[cols.Count + 3];

            cols.CopyTo(array, 0);
            array[cols.Count] = new SubPropertyDescriptor(addressPD, Orbit_child["Mass"], "Orbit_Mass");
            array[cols.Count + 1] = new SubPropertyDescriptor(addressPD, Orbit_child["SemiMajorAxis"], "Orbit_SemiMajorAxis");
            array[cols.Count + 2] = new SubPropertyDescriptor(addressPD, Orbit_child["OrbitalPeriod"], "Orbit_OrbitalPeriod");

            PropertyDescriptorCollection newcols = new PropertyDescriptorCollection(array);
            return newcols;
        }

        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            PropertyDescriptorCollection cols = base.GetProperties(attributes);
            PropertyDescriptor addressPD = cols["Orbit"];
            PropertyDescriptorCollection Orbit_child = addressPD.GetChildProperties();
            PropertyDescriptor[] array = new PropertyDescriptor[cols.Count + 3];

            cols.CopyTo(array, 0);
            array[cols.Count] = new SubPropertyDescriptor(addressPD, Orbit_child["Mass"], "Orbit_Mass");
            array[cols.Count + 1] = new SubPropertyDescriptor(addressPD, Orbit_child["SemiMajorAxis"], "Orbit_SemiMajorAxis");
            array[cols.Count + 2] = new SubPropertyDescriptor(addressPD, Orbit_child["OrbitalPeriod"], "Orbit_OrbitalPeriod");

            PropertyDescriptorCollection newcols = new PropertyDescriptorCollection(array);
            return newcols;
        }
    }

    /// <summary>
    /// Used for databinding, see here: http://blogs.msdn.com/b/msdnts/archive/2007/01/19/how-to-bind-a-datagridview-column-to-a-second-level-property-of-a-data-source.aspx
    /// </summary>
    public class OrbitingEntityTypeDescriptionProvider : TypeDescriptionProvider
    {
        private ICustomTypeDescriptor td;

        public OrbitingEntityTypeDescriptionProvider()
            : this(TypeDescriptor.GetProvider(typeof(OrbitingEntity)))
        { }

        public OrbitingEntityTypeDescriptionProvider(TypeDescriptionProvider parent)
            : base(parent)
        { }

        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
        {
            if (td == null)
            {
                td = base.GetTypeDescriptor(objectType, instance);
                td = new OrebitTypeDescriptor(td);
            }

            return td;
        }
    }


    #endregion
}