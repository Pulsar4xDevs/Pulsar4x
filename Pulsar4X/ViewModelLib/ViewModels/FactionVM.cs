using System;
using System.ComponentModel;
using Pulsar4X.ECSLib;

namespace Pulsar4X.WPFUI.ViewModels
{
    public class FactionVM
    {
        /// <summary>
        /// The default name of the faction, i.e. the name it knows itself by.
        /// </summary>
        private string _name;

        private Guid _id;

        #region IViewModel

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion 
    }
}