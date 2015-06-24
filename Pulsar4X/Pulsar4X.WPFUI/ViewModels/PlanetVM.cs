using System;
using System.ComponentModel;
using Pulsar4X.ECSLib;

namespace Pulsar4X.WPFUI.ViewModels
{
    /// <summary>
    /// This class is a view modle for all planets, asteroids, comets, moons, etc.
    /// </summary>
    public class PlanetVM : IViewModel
    {
        private BindingList<PlanetVM> _children;

        private StarVM _parentStar;

        private PlanetVM _parentPlanet; // null if not a moon.

        #region Planet Properties

        private string _name;

        private Guid _id;

        private string _planetType;

        // in earth masses would be best.
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

        private double _axialTilt;

        private double _baseTemperature;

        private TimeSpan _lengthofDay;

        private double _magneticField;

        private string _techtonics;

        private string _atmosphereInATM;

        private string _atmosphereInPercent;

        private double _pressure;

        private double _albedo;

        private double _surfaceTemp;

        private double _greenhouseFactor;

        private double _greenhousePressure;

        private bool _hydrosphere;

        private float _hydrosphereExtent;

        /// < @todo add runis data.

        #endregion

        #region IViewModel

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}