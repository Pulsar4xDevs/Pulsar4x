using System;
using System.ComponentModel;
using Pulsar4X.ECSLib;

namespace Pulsar4X.WPFUI.ViewModels
{
    public class StarVM : IViewModel
    {
        #region Children And Parents
        // these are the stars children, if any. Mostly used for rendering and/or navigating to children.

        private BindingList<StarVM> _childStars;

        private BindingList<PlanetVM> _childPlanets;

        // the stars parent system and star (if it is not the root/primary star of the system) 
        private SystemVM _system;

        private StarVM _parentStar;

        #endregion

        #region Stars Properties

        private string _name;

        private Guid _id;

        private string _starClass;

        private int _age;

        private double _ecoSphereRadius;

        private double _luminosity;

        private int _temperature;

        // in solar masses would be best.
        private double _mass;

        private double _density;

        private double _radius;

        private double _volume;

        private double _surfaceGravity;

        private double _semiMajorAxis;

        private double _apoapsis;

        private double _periapsis;

        private double _eccentricity;

        private double _inclination;

        private TimeSpan _year;

        #endregion


        #region IViewModel

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}