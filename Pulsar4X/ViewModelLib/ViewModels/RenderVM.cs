using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Pulsar4X.ECSLib;
using Pulsar4X.Sceen;

namespace Pulsar4X.ViewModel
{
    /// <summary>
    /// This is the VM for the opengl window, it handles updating the scene manager and triggering draws
    /// </summary>
    public class RenderVM : IViewModel
    {
		private SceenManager SceneManager;
        public RenderVM()
        {
			SceneManager = new SceenManager ();
        }

        public void Draw()
        {

        }

        public void Initialize(object sender, EventArgs e)
        {

        }

        public void Draw(object sender, EventArgs e)
        {
            Draw();
        }

        public void Resize(object sender, EventArgs e)
        {
            Draw();
        }

        public void Teardown(object sender, EventArgs e)
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void Refresh(bool partialRefresh = false)
        {

        }
    }
}
