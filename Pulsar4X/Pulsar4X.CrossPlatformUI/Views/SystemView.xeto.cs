using Eto.Drawing;
using Eto.Forms;
using Eto.Serialization.Xaml;
using OpenTK;
using OpenTK.Graphics;
using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

//#TODO Move the actual rendering stuff out into a partial and separate it from the VMs
//I might also want to make windows render with DirectX so that it plays nicer with wpf, 
//so we'll have to see if we can abstract that too
namespace Pulsar4X.CrossPlatformUI.Views {
	public class SystemView : Panel {
		protected Panel RenderCanvasLocation;

		protected DropDown systems;

		protected SystemVM CurrentSystem;
        		
        protected SystemMap_DrawableView SysMapDrawable;

        private StarSystemSelectionVM _viewmodel;

		public SystemView(StarSystemSelectionVM viewmodel) {
			
			DataContext = viewmodel;
            _viewmodel = viewmodel;
			XamlReader.Load(this);

            systems.DataContext = viewmodel.StarSystems;

            systems.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            systems.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);
            

            if (!viewmodel.Enable)
                viewmodel.StarSystems.SelectionChangedEvent += StarSystems_SelectionChangedEvent;
            else
            {
                SysMapDrawable.SetViewmodel(viewmodel.SelectedSystemVM);
            }
        }

        void StarSystems_SelectionChangedEvent(int oldSelection, int newSelection)
        {
            if (_viewmodel.Enable)
            {
                _viewmodel.StarSystems.SelectionChangedEvent -= StarSystems_SelectionChangedEvent;
                SysMapDrawable.SetViewmodel(_viewmodel.SelectedSystemVM);
            }
            else
            {
                SysMapDrawable.SetViewmodel(null);
            }
        }
    }
}
