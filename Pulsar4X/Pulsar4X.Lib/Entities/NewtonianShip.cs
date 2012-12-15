using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;
using log4net;
using System.ComponentModel;

namespace Pulsar4X.Entities
{
	public class NewtonianShipClass
	{
		public string className { get; set; }

		public NewtonianEngine shipEngine;

		public BindingList<BasicNewtonian> ComponentList;
		public List<int> ComponentCount;

		public BindingList<NewtonianShip> ShipsInClass;

		public int NextBuildNumber { get; set; }


		public NewtonianShipClass (string Name)
		{
			className = Name;
			ComponentList = new BindingList<BasicNewtonian>();
			ShipsInClass = new BindingList<NewtonianShip>();
			ComponentCount = new List<int>();
			NextBuildNumber = 1;
		}

		public void AddComponent (BasicNewtonian component, int numberAdded)
		{
			if (!ComponentList.Contains (component)) {
				ComponentList.Add (component);
				ComponentCount.Add (numberAdded);
			} else {
				int index = ComponentList.IndexOf (component);
				ComponentCount[index] += numberAdded;
			}
		}

		public void removeCoponent (BasicNewtonian component, int numberTaken)
		{
			if (ComponentList.Contains (component)) {
				int index = ComponentList.IndexOf(component);
				if (numberTaken == 0 || numberTaken >= ComponentCount [index]) {
					ComponentList.RemoveAt (index);
					ComponentCount.RemoveAt (index);
				} else {
					ComponentCount [index] -= numberTaken;
				}
			}
		}

	}

	public class NewtonianShip
	{
		public NewtonianShipClass shipClass;

		public string ShipName { get; set; }

		public List<int> WorkingComponents;

		public NewtonianShip ( NewtonianShipClass parentClass)
		{
			parentClass.ShipsInClass.Add(this);
			ShipName = parentClass.className + " " + parentClass.NextBuildNumber;
			WorkingComponents = new List<int>(parentClass.ComponentCount);
		}
	}
}

