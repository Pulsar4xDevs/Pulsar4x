using System;
using System.Collections.Generic;
using Gtk;

namespace Pulsar4X.GTKForms.Controls
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class SystemDisplayGTK : Gtk.Bin
	{
		private Entities.StarSystem selectedSystem;
		private Gtk.ListStore starDataShown;
		private Gtk.TreeView starView;
		private List<Gtk.ListStore> planetList;
		private List<Gtk.TreeView> planetView;
		private bool isSetupFinished = false;

		public SystemDisplayGTK ()
		{
			this.Build ();	//Assembles window as layed out in Designer mode

			// Fill Stars Frame
			starDataShown = new ListStore (typeof(string), typeof(string), typeof(double), typeof(double),
			                               typeof(double), typeof(double), typeof(double), typeof(double));

			foreach (Entities.StarSystem systemMember in GameState.Instance.StarSystems) {
				SystemList.AppendText (systemMember.ToString ());
			}
			Gtk.TreeModel systemModel = SystemList.Model;
			Gtk.TreeIter firstItem;
			systemModel.GetIterFirst (out firstItem);
			SystemList.SetActiveIter (firstItem);

			starView = new Gtk.TreeView (starDataShown);
			starsFrame.Add (starView);
			string[] starHeader = {
				"Name",
				"Class",
				"Radius",
				"Mass",
				"Luminosity",
				"Temperature",
				"Habitable Zone",
				"Orbital Radius (AU)"
			};
			int headerIndex = 0;
			foreach (string header in starHeader) {
				starView.AppendColumn (header, new CellRendererText (), "text", headerIndex++);
			}

			starView.HeadersVisible = true;
			starView.ExpandAll ();

			// Fill Planet list frames
			string[] planetHeader = {
				"Name",
				"Type",
				"Surface\nTemp.",
				"Surface\nGravity",
				"Atmospher\n(Earth Masses)",
				"Orbit Dist\n(Avg)",
				"Pressure",
				"Radius"
			};

			planetList = new List<ListStore>();
			planetView = new List<TreeView>();
			for (int j = 0; j < 4; j++) {
				planetList.Add( new Gtk.ListStore (typeof(string), typeof(string), typeof(string), typeof(string), typeof(string),
				                                typeof(double), typeof(double), typeof(double)));

				planetView.Add( new Gtk.TreeView (planetList[j]));
				headerIndex = 0;
				foreach (string header in planetHeader) {
					planetView [j].AppendColumn (header, new CellRendererText (), "text", headerIndex++);
				}
			}

			StarA_Space.Add (planetView[0]);
			StarB_Space.Add (planetView[1]);
			StarC_Space.Add (planetView[2]);
			StarD_Space.Add (planetView[3]);

			// After Setupt iniltialize data for default system
			isSetupFinished = true;
			selectedSystem = GameState.Instance.StarSystems [0];
			setSystemType (selectedSystem.Stars.Count);
		}

		//This method is called everytime the System combobox selection is changed
		protected void SystemSelect (object sender, EventArgs e)
		{
			if(!isSetupFinished) return;

			selectedSystem = GameState.Instance.StarSystems [SystemList.Active];
			setSystemType (selectedSystem.Stars.Count);

			starDataShown.Clear ();
			int starCount = 0;
			foreach (Entities.Star aStar in selectedSystem.Stars) {
				starDataShown.AppendValues (aStar.Name, aStar.Class, aStar.Radius, aStar.Mass,
				                            aStar.Luminosity, aStar.Temperature, aStar.Life, aStar.SemiMajorAxis);

				planetList[starCount].Clear ();
				foreach (Entities.Planet aPlanet in aStar.Planets) {
					planetList[starCount].AppendValues (aPlanet.Name, aPlanet.PlanetTypeView, aPlanet.SurfaceTemperatureView, aPlanet.MassOfGasInEarthMassesView,
					                         aPlanet.SemiMajorAxis, aPlanet.SurfacePressure, aPlanet.Radius);
				}

				switch(starCount)
				{
				case 0:
					StarA_Label.Text = aStar.Name;
					break;
				case 1:
					StarB_Label.Text = aStar.Name;
					break;
				case 2:
					StarC_Label.Text = aStar.Name;
					break;
				case 3:
					StarD_Label.Text = aStar.Name;
					break;
				}
					
				starCount++;
			} 

			for (; starCount<4; starCount++) {
				planetList[starCount].Clear ();
				switch(starCount)
				{
				case 0:
					StarA_Label.Text = "N/A";
					break;
				case 1:
					StarB_Label.Text = "N/A";
					break;
				case 2:
					StarC_Label.Text = "N/A";
					break;
				case 3:
					StarD_Label.Text = "N/A";
					break;
				}
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

