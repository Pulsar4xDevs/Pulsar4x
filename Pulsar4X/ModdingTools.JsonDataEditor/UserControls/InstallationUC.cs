using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Pulsar4X.ECSLib;
namespace ModdingTools.JsonDataEditor.UserControls
{
    public partial class InstallationUC : UserControl
    {
        private InstallationSD _staticData;
        public InstallationSD StaticData
        {
            get { return _staticData; }
            set
            {
                _staticData = value;
                setData();
            }
        }
        public InstallationUC()
        {
            InitializeComponent();
        }

        void setData()
        {
            numericUpDown_Population.Value = StaticData.PopulationRequired;
            numericUpDown_Wealth.Value = StaticData.WealthCost;
            numericUpDown_CargoSize.Value = StaticData.CargoSize;
            numericUpDown_BuildPoints.Value = StaticData.BuildPoints;
        }
    }
}
