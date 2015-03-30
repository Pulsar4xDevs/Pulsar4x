using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.ECSLib.DataBlobs
{
    public class AtmosphericGasDB
    {
        public string Name;
        public string ChemicalSymbol;
        public bool IsToxic;
        public double BoilingPoint;
        public double MeltingPoint;

        public AtmosphericGasDB(string name, string chemSymbol, bool isToxic, double boilingPoint, double meltingPoint)
        {
            Name = name;
            ChemicalSymbol = chemSymbol;
            IsToxic = isToxic;
            BoilingPoint = boilingPoint;
            MeltingPoint = meltingPoint;
        }
    }
}
