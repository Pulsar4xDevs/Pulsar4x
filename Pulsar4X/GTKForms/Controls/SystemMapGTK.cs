using System;

namespace GTKForms
{
	[System.ComponentModel.ToolboxItem(true)]
	public class SystemMapGTK : Gtk.DrawingArea
	{
		public SystemMapGTK ()
		{
			// Insert initialization code here.
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton ev)
		{
			// Insert button press handling code here.
			return base.OnButtonPressEvent (ev);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose ev)
		{
			base.OnExposeEvent (ev);
			// Insert drawing code here.
			return true;
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			// Insert layout code here.
		}

		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			// Calculate desired size here.
			requisition.Height = 50;
			requisition.Width = 50;
		}
	}
}

