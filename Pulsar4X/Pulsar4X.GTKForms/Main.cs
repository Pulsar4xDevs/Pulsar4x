using System;
using Gtk;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Stargen;
using log4net.Config;
using log4net;
using System.Threading;

namespace Pulsar4X.GTKForms
{
	class MainClass
	{
		public static readonly ILog logger = LogManager.GetLogger(typeof(Main));

		public static void Main (string[] args)
		{
			Application.Init ();

			XmlConfigurator.Configure();
			logger.Info("Program Started");

			var ssf = new StarSystemFactory(true);
			GameState.Instance.StarSystems.Add(ssf.Create("Test"));
			GameState.Instance.StarSystems.Add(ssf.Create("Foo"));
			GameState.Instance.StarSystems.Add(ssf.Create("Bar"));

			MainWindowGTK win = new MainWindowGTK ();
			win.Show ();
			Application.Run ();
		}
	}
}
