using System;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;
using log4net;


namespace Pulsar4X.Entities.Components
{
	public enum NewtonianType
	{
		Engine,
		Weapon,
		BeamFireCon,
		MissileFireCon,
		Sensor,
		Shield,
		Fuel,
		Engineering,
		Other,
		TypeCount
	};

	/// <summary>
	/// A class with all methods and properties that all internal newtonian ship components will share
	/// </summary>
	public abstract class BasicNewtonian
	{
		public string name {get; set;}

		public BuildCost unitCost;

		/// <summary>
		/// Displacement in tons (meter cubed) for one component.
		/// Typicaly equals 10x mass for storage compartments or 5x mass otherwise
		/// </summary>
		public long volumeUnit { get; set; }

		public long unitCrew { get; set; }

		/// <summary>
		/// Mass in tons for a single component.
		/// </summary>
		public long unitMass { get; set; }

		/// <summary>
		/// MJ of energy required for a 100% destruction chance.
		/// </summary>
		public double integrity { get; set; }

		/// <summary>
		/// NewtonianType of Component
		/// </summary>
		public NewtonianType Type { get; set; }

		public BasicNewtonian ()
		{

		}
	}

}

