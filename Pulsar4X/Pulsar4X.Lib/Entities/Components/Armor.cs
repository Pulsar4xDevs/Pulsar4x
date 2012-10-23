using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;
using log4net;
using System.ComponentModel;

//This is a duplicate of the Aurora armor system: Ships are basically spheres with a volume of HullSize.
//Armor is coated around these spheres in ever increasing amounts per armor depth. likewise armor has to cover itself, as well as the ship sphere.
//ShipOnDamage(or its equivilant) will touch on this, though I have yet to figure that out.
//This is certainly up for updating/revision. - NathanH.


namespace Pulsar4X.Entities.Components
{
	public class Armor
	{
		public int ArmorPerHS;		//armor tech level
		public int Depth;		//depth of each column. Armor layers in other words.
		public double Size;		//size of the armor
		public double Area;		//area coverage of armor	
		public double Cost;		//overall cost of armor

		public int CNum;		            //# of columns
		public int[] Columns;    //column storage


	    public Armor(int armorPerHS, int armorDepth)
	    {
		    ArmorPerHS = armorPerHS;
		    Size = 0.0;
		    Cost = 0.0;
		    Area = 0.0;
		    Depth = armorDepth;

            Columns = null;
	    }

	    public void CalcArmor(int armorPerHS, double sizeOfCraft, int armorDepth)
	    {
		    ArmorPerHS = armorPerHS;
		    Depth = armorDepth;

		    //Armor calculation is as follows:
		    //V = 4/3 * pi * r^3 r^3 = 3V/4pi
		    //A = 4 * pi * r^2
		    //Size  = V
		    //find for r from the volume formula
		    //find A from the Surface area formula.
		    //this is required armor strength for 1st level of armor

		    int loop,done;
		    double volume,radius3,radius2,radius,area=0.0, strengthReq=0.0,lastPro;
		    double areaF;
		    double temp1 = 1.0 / 3.0;	//useful constant
		    double pi = 3.14159654;		//who doesn't like pi?

		    Size = 0.0; //important initialization

		    for( loop = 0; loop < Depth; loop++) //this calculation must be looped
		    {
			    done = 0;
			    lastPro = -1;
	    		    volume = Math.Ceiling( sizeOfCraft + Size );
		
			    while( done == 0 ) //repeat until armor covers both the ship and itself.
			    {
				    radius3 = ( 3.0 * volume ) / ( 4.0 * pi ) ;
				    radius = Math.Pow( radius3, temp1 );
				    radius2 = Math.Pow( radius, 2.0 );
				    area = ( 4.0 * pi ) * radius2;

				    areaF = Math.Floor( area * 10.0 ) / 10.0;
				    area = ( Math.Round(area * 10.0) ) / 10.0;
				    area *= (double)( loop + 1 );
				    strengthReq = area / 4.0 ;

				    Size = Math.Ceiling( ( strengthReq / (double) ArmorPerHS ) * 10.0 ) / 10.0;
				    volume = Math.Ceiling(sizeOfCraft + Size);

				    if( Size == lastPro )
					    done = 1;

				    lastPro = Size;
			    }
		    }

		    Area = ( area * Depth ) / 4.0;
		    Cost = Area;
		    CNum = (int)Math.Floor( strengthReq / (double)Depth );

            Columns = new int[ CNum ];

            for (loop = 0; loop < CNum; loop++)
            {

                Columns[loop] = Depth;
            }
	    }//end calcArmor
    }//end Armor Class
}//end namespace