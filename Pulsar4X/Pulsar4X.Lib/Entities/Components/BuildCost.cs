using System;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;
using log4net;


namespace Pulsar4X.Entities.Components
{
	/// <summary>
	/// The total cost to construct anything is the total mineral cost plus a new
	/// conventional labor premium for labor intensive tasks like ship refiting or prefab PDC assembly.
	/// </summary>
	public class BuildCost
	{

		/// <summary>
		/// Gets or sets the duranium cost.
		/// </summary>
		public double Duranium{ get; set; }

		/// <summary>
		/// Gets or sets the Neutronium cost.
		/// </summary>
		public double Neutronium{ get; set; }

		/// <summary>
		/// Gets or sets the Corbomite cost.
		/// </summary>
		public double Corbomite{ get; set; }

		/// <summary>
		/// Gets or sets the Tritanium cost.
		/// </summary>
		public double Tritanium{ get; set; }

		/// <summary>
		/// Gets or sets the Boronide cost.
		/// </summary>
		public double Boronide{ get; set; }

		/// <summary>
		/// Gets or sets the Mercassium cost.
		/// </summary>
		public double Mercassium{ get; set; }

		/// <summary>
		/// Gets or sets the Vendarite cost.
		/// </summary>
		public double Vendarite{ get; set; }

		/// <summary>
		/// Gets or sets the Sorium cost.
		/// </summary>
		public double Sorium{ get; set; }

		/// <summary>
		/// Gets or sets the Uridium cost.
		/// </summary>
		public double Uridium{ get; set; }

		/// <summary>
		/// Gets or sets the Corundium cost.
		/// </summary>
		public double Corundium{ get; set; }

		/// <summary>
		/// Gets or sets the Gallacite cost.
		/// </summary>
		public double Gallacite{ get; set; }

		/// <summary>
		/// Gets or sets any non-mineral credit/time cost.
		/// </summary>
		public double ConventionalLabor{ get; set; }

		/// <summary>
		/// Gets the total build point cost.
		/// </summary>
		public double TotalCost {
			get {
				return (Duranium + Neutronium + Corbomite + Tritanium + Boronide + Mercassium
					+ Vendarite + Sorium + Uridium + Corundium + Gallacite + ConventionalLabor); 
			}
		}


		public BuildCost ()
		{
		}
	}
}

