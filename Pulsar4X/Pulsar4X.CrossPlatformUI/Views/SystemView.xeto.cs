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
		//protected RenderCanvas RenderCanvas;
		private UITimer timDraw;

		protected DropDown systems;

		protected SystemVM CurrentSystem;


		private bool mouse_held = false;
		private bool continue_drag = false;
		private Vector2 mouse_held_position;
		private Vector2 mouse_released_position;
		private const float mouse_move_threshold = 20f;
		
		
        protected SystemMap_DrawableView SysMapDrawable;

		public SystemView(StarSystemVM viewmodel) {
			
			DataContext = viewmodel;			
			XamlReader.Load(this);

            systems.DataContext = viewmodel.StarSystems;
            systems.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            systems.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);
            
            SysMapDrawable.SetViewmodel(viewmodel.SelectedSystemVM);
            
        }
	}
}
