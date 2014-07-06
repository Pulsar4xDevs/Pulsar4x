using System;
using Gtk;
using Pulsar4X.GTKForms.Controls;

namespace Pulsar4X.GTKForms
{
	public partial class MainWindowGTK : Gtk.Window
	{
		public MainWindowGTK () : 
				base(Gtk.WindowType.Toplevel)
		{
			this.Build ();
			//SystemDisplaySpace.Add( Controls.SystemDisplayGTK);
		}

		protected void OnDeleteEvent (object sender, DeleteEventArgs a)
		{
			Application.Quit ();
			a.RetVal = true;
		}
	}
}

