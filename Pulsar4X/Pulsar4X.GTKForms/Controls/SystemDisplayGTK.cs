using System;
using Gtk;

namespace Pulsar4X.GTKForms.Controls
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class SystemDisplayGTK : Gtk.Bin
	{
		private Entities.StarSystem selectedSystem;
		private Gtk.ListStore starDataShown;
		private Gtk.TreeView starView;

		public SystemDisplayGTK ()
		{
			this.Build ();	//Assembles window as layed out in Designer mode

			starDataShown = new ListStore (typeof(string), typeof(string), typeof(double), typeof(double),
			                               typeof(double), typeof(double), typeof(double), typeof(double));

			foreach (Entities.StarSystem systemMember in GameState.Instance.StarSystems) {
				SystemList.AppendText (systemMember.ToString ());
			}
			Gtk.TreeModel systemModel = SystemList.Model;
			Gtk.TreeIter firstItem;
			systemModel.GetIterFirst (out firstItem);
			SystemList.SetActiveIter (firstItem);

			selectedSystem = GameState.Instance.StarSystems [0]; //this is how to get an instance
			setSystemType (selectedSystem.Stars.Count);

			//idEntry.Text = selectedSystem.Id;
			starView = new Gtk.TreeView(starDataShown);
			starsFrame.Add(starView);

			string[] starHeader = {"Name", "Class", "Radius", "Mass","Luminosity", "Temperature", "Habitable Zone", "Orbital Radius (AU)"};
			int headerIndex = 0;
			foreach( string header in starHeader)
			{
				starView.AppendColumn(header, new CellRendererText(), "text", headerIndex++);
			}

			starView.HeadersVisible = true;
			starView.ExpandAll();

		}

		//This method is called everytime the System combobox selection is changed
		protected void SystemSelect (object sender, EventArgs e)
		{
			selectedSystem = GameState.Instance.StarSystems[SystemList.Active];
			setSystemType (selectedSystem.Stars.Count);

			starDataShown.Clear();
			foreach (Entities.Star aStar in selectedSystem.Stars) {
				starDataShown.AppendValues (aStar.Name, aStar.Class, aStar.Radius, aStar.Mass,
				                            aStar.Luminosity, aStar.Temperature, aStar.Life, aStar.OrbitalPeriod);
			} 

		}

		private void setSystemType (int starCount)
		{
			switch (starCount) {
			case 1:
				typeEntry.Text = "Single Star";
				break;
			case 2:
				typeEntry.Text = "Binary System";
				break;
			case 3:
				typeEntry.Text = "Trinary System";
				break;
			case 4:
				typeEntry.Text = "Quad System";
				break;
			default:
				typeEntry.Text = "Buggy System";
				break;
			}
		}
	}
}

