using System;
using System.ComponentModel;
using System.Windows;
using Pulsar4X.ECSLib;

namespace Pulsar4X.WPFUI.ViewModels
{
    public class SystemVM
    {
        private BindingList<StarVM> _stars;

        private StarVM _parentStar;

        private BindingList<PlanetVM> _Planets;

        private string _name;

        private Guid _id;
 
        // add list of ships

        // add list of waypoints

        // Add list of colonies? maybe?

        #region IViewModel

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}